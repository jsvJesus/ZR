#include "PostProcess/TemporalMotion.hlsli"

cbuffer TaaConstants : register(b0)
{
    row_major float4x4 InverseViewProjection;
    row_major float4x4 CurrentViewProjection;
    row_major float4x4 PreviousViewProjection;
    float4 Parameters; // width, height, feedback, history valid
    float4 Jitter; // current xy and previous zw, in pixels
    float4 CameraPosition;
};

Texture2D<float4> CurrentColor : register(t0);
Texture2D<float4> PreviousHistory : register(t1);
Texture2D<float> CurrentDepth : register(t2);
Texture2D<float4> MotionVectors : register(t3);
Texture2D<float> ReactiveMask : register(t4);
Texture2D<float> PreviousRawDepth : register(t5);
SamplerState LinearSampler : register(s0);

struct PixelInput
{
    float4 Position : SV_POSITION;
    float2 Uv : TEXCOORD0;
};

PixelInput VSMain(uint vertexId : SV_VertexID)
{
    PixelInput output;
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Uv = uv;
    output.Position = float4(uv.x * 2.0f - 1.0f, 1.0f - uv.y * 2.0f, 0.0f, 1.0f);
    return output;
}

int2 ClampPixel(int2 pixel)
{
    return clamp(pixel, int2(0, 0), int2(Parameters.xy) - 1);
}

float2 PixelUv(int2 pixel)
{
    return (float2(pixel) + 0.5f) / Parameters.xy;
}

float3 ReconstructWorld(float2 uv, float depth)
{
    float4 clip = float4(uv.x * 2.0f - 1.0f, 1.0f - uv.y * 2.0f, depth, 1.0f);
    float4 world = mul(clip, InverseViewProjection);
    return world.xyz / max(world.w, 0.000001f);
}

float3 ToYCoCg(float3 color)
{
    return float3(dot(color, float3(0.25f, 0.5f, 0.25f)),
        (color.r - color.b) * 0.5f, (-color.r + 2.0f * color.g - color.b) * 0.25f);
}

float3 FromYCoCg(float3 color)
{
    return float3(color.x + color.y - color.z, color.x + color.z, color.x - color.y - color.z);
}

float Luminance(float3 color)
{
    return dot(color, float3(0.2126f, 0.7152f, 0.0722f));
}

// Positive compact cubic low-pass: current color, moments and material support
// use one footprint in stable output coordinates. Interpolating signed lobes
// preserved phase-dependent aliases and could select diagonal ringing as geometry.
float ReconstructionWeight(float distance)
{
    // Keep the full cardinal B-spline support. Rescaling/truncating the kernel
    // would break its unit sum/first moment and reintroduce a phase position bias.
    float x = abs(distance);
    if (x < 1.0f) return (4.0f - 6.0f * x * x + 3.0f * x * x * x) / 6.0f;
    if (x < 2.0f) return (2.0f - x) * (2.0f - x) * (2.0f - x) / 6.0f;
    return 0.0f;
}

float3 ReconstructCurrent(int2 pixel, out int2 surfacePixel, out float surfaceDepth,
    out float3 minimumColor, out float3 maximumColor, out float3 averageColor,
    out float3 deviation, out float reactive)
{
    surfacePixel = pixel;
    surfaceDepth = 1.0f;
    minimumColor = float3(65504.0f, 65504.0f, 65504.0f);
    maximumColor = float3(-65504.0f, -65504.0f, -65504.0f);
    averageColor = 0.0f;
    float3 secondMoment = 0.0f;
    float totalWeight = 0.0f;
    float surfaceWeight = 0.0f;
    reactive = 0.0f;
    float2 samplePosition = float2(pixel) + Jitter.xy;
    int2 firstPixel = int2(floor(samplePosition));
    float2 fraction = samplePosition - floor(samplePosition);
    [unroll]
    for (int y = -1; y <= 2; y++)
    {
        [unroll]
        for (int x = -1; x <= 2; x++)
        {
            int2 samplePixel = ClampPixel(firstPixel + int2(x, y));
            float weight = ReconstructionWeight(x - fraction.x) * ReconstructionWeight(y - fraction.y);
            if (weight <= 0.00001f) continue;
            float3 color = ToYCoCg(max(CurrentColor.Load(int3(samplePixel, 0)).rgb, 0.0f));
            minimumColor = min(minimumColor, color);
            maximumColor = max(maximumColor, color);
            averageColor += color * weight;
            secondMoment += color * color * weight;
            totalWeight += weight;
            // Weak reconstruction tails must not turn a neighbouring reactive
            // pixel into full history rejection over the reconstruction footprint.
            reactive = max(reactive, ReactiveMask.Load(int3(samplePixel, 0)) * saturate(weight / 0.25f));
            float depth = CurrentDepth.Load(int3(samplePixel, 0));
            if (weight > surfaceWeight && depth < 0.999999f)
            {
                surfaceWeight = weight;
                surfaceDepth = depth;
                surfacePixel = samplePixel;
            }
        }
    }
    averageColor /= max(totalWeight, 0.00001f);
    deviation = sqrt(max(secondMoment / max(totalWeight, 0.00001f) - averageColor * averageColor, 0.000001f));
    if (surfaceWeight <= 0.0f)
        return max(CurrentColor.Load(int3(pixel, 0)).rgb, 0.0f); // unjittered sky
    return max(FromYCoCg(averageColor), 0.0f);
}

struct TemporalOutput
{
    float4 Color : SV_Target0;
    float RawDepth : SV_Target1;
};

TemporalOutput ResolveOutput(float3 color, float surfaceDepth, float rawDepth, float confidence)
{
    TemporalOutput output;
    // Alpha carries bounded temporal evidence, preserving its sign as the
    // presentation sky marker. Exact depth remains in the separate raw atlas.
    output.Color = float4(color, (surfaceDepth > 0.0f ? 1.0f : -1.0f) * clamp(confidence, 1.0f, 32.0f));
    output.RawDepth = rawDepth;
    return output;
}

TemporalOutput ResolveOutput(float3 color, float surfaceDepth, float rawDepth)
{
    return ResolveOutput(color, surfaceDepth, rawDepth, 1.0f);
}

float DepthFootprintSupport(float2 uv, float expectedDepth, bool sky, out bool foregroundConflict)
{
    float2 coordinate = uv * Parameters.xy - 0.5f;
    int2 first = int2(floor(coordinate));
    float2 fraction = coordinate - floor(coordinate);
    float tolerance = 0.01f + expectedDepth * 0.002f;
    float support = 0.0f;
    foregroundConflict = false;
    [unroll]
    for (int y = 0; y < 2; y++)
    {
        [unroll]
        for (int x = 0; x < 2; x++)
        {
            float weight = (x == 0 ? 1.0f - fraction.x : fraction.x) *
                (y == 0 ? 1.0f - fraction.y : fraction.y);
            if (weight <= 0.0001f) continue;
            float stored = PreviousRawDepth.Load(int3(ClampPixel(first + int2(x, y)), 0));
            bool compatible = sky ? stored <= 0.0f :
                stored > 0.0f && abs(stored - expectedDepth) <= tolerance;
            if (compatible) support += weight;
            // Missing subpixel coverage is different from a known closer occluder.
            if (!sky && stored > 0.0f && stored + tolerance < expectedDepth)
                foregroundConflict = true;
        }
    }
    return support;
}

TemporalOutput PSResolve(PixelInput input)
{
    int2 pixel = ClampPixel(int2(input.Position.xy));
    float2 uv = PixelUv(pixel);
    int2 surfacePixel;
    float depth;
    float3 minimumColor, maximumColor, averageColor, deviation;
    float materialReactive;
    float3 current = ReconstructCurrent(pixel, surfacePixel, depth,
        minimumColor, maximumColor, averageColor, deviation, materialReactive);

    // Depth history is a raw center-sample atlas, independent of the surface
    // selected to reconstruct color at a silhouette. Never dilate its contents.
    float rawDepth = CurrentDepth.Load(int3(pixel, 0));
    float encodedRawDepth = 0.0f;
    if (rawDepth < 0.999999f)
    {
        float3 rawWorld = ReconstructWorld(uv, rawDepth);
        encodedRawDepth = EncodeTemporalDepth(mul(float4(rawWorld, 1.0f), CurrentViewProjection).w);
    }

    bool sky = depth >= 0.999999f;
    float encodedDepth = 0.0f;
    if (!sky)
    {
        float3 world = ReconstructWorld(PixelUv(surfacePixel), depth);
        encodedDepth = EncodeTemporalDepth(mul(float4(world, 1.0f), CurrentViewProjection).w);
    }
    if (Parameters.w < 0.5f || materialReactive >= 0.999f)
        return ResolveOutput(current, encodedDepth, encodedRawDepth);

    float2 previousUv;
    float2 previousDepthUv;
    float2 geometricMotion = 0.0f;
    float expectedDepth = 0.0f;
    if (sky)
    {
        // The sky has no translation or raster jitter. Reconstruct its stable
        // camera ray and remove previous jitter from the history projection.
        float3 direction = ReconstructWorld(uv + Jitter.xy / Parameters.xy, 1.0f) - CameraPosition.xyz;
        float4 previousClip = mul(float4(direction, 0.0f), PreviousViewProjection);
        if (previousClip.w <= 0.00001f) return ResolveOutput(current, 0.0f, encodedRawDepth);
        previousUv = ClipToTemporalUv(previousClip) - Jitter.zw / Parameters.xy;
        previousDepthUv = previousUv + Jitter.zw / Parameters.xy;
        geometricMotion = uv - previousUv;
    }
    else
    {
        float4 motion = MotionVectors.Load(int3(surfacePixel, 0));
        if (motion.w < 0.5f) return ResolveOutput(current, encodedDepth, encodedRawDepth);
        // Remove projection jitter exactly once. Halton offsets must not be
        // interpreted as physical movement when reducing history feedback.
        geometricMotion = motion.xy - (Jitter.xy - Jitter.zw) / Parameters.xy;
        previousUv = uv - geometricMotion;
        // Depth retains raw raster samples, whereas RGB is reconstructed on the
        // stable grid. Address the two histories in their own coordinate domains.
        previousDepthUv = PixelUv(surfacePixel) - motion.xy;
        expectedDepth = motion.z; // previous actual deformed/rigid/billboard surface
    }
    if (any(previousUv <= 0.0f) || any(previousUv >= 1.0f) ||
        any(previousDepthUv <= 0.0f) || any(previousDepthUv >= 1.0f))
        return ResolveOutput(current, encodedDepth, encodedRawDepth);

    float historyMetadata = PreviousHistory.Load(int3(ClampPixel(int2(previousUv * Parameters.xy)), 0)).a;
    // A different surface domain cannot inherit mature evidence from the old
    // one. Start both accumulation weight and confidence from the current frame.
    if ((historyMetadata < 0.0f) != sky)
        return ResolveOutput(current, encodedDepth, encodedRawDepth);
    float historySamples = clamp(abs(historyMetadata), 1.0f, 32.0f);
    float motionPixels = length(geometricMotion * Parameters.xy);
    float stationary = 1.0f - smoothstep(0.1f, 0.75f, motionPixels);
    bool foregroundConflict;
    float depthSupport = DepthFootprintSupport(previousDepthUv, expectedDepth, sky, foregroundConflict);
    // A mature, stationary surface can be thinner than all previous raster
    // samples. Preserve its accumulated coverage only if no closer occluder
    // contradicts it; new/moving/disoccluded geometry cannot take this path.
    bool retainedCoverage = !sky && historyMetadata > 0.0f && historySamples >= 8.0f &&
        stationary > 0.999f && materialReactive < 0.001f && !foregroundConflict;
    if (depthSupport < 0.05f && !retainedCoverage)
        return ResolveOutput(current, encodedDepth, encodedRawDepth);
    // A nearer neighbour is normal beside a static silhouette. It contradicts
    // this query only when matching depth is not the majority of its footprint.
    bool conflictingHistory = foregroundConflict && depthSupport < 0.5f;
    if (conflictingHistory) historySamples = 1.0f;

    float3 history = max(PreviousHistory.SampleLevel(LinearSampler, previousUv, 0.0f).rgb, 0.0f);
    float3 minimumClip = max(minimumColor, averageColor - deviation * 1.5f);
    float3 maximumClip = min(maximumColor, averageColor + deviation * 1.5f);
    float3 clippedHistory = max(FromYCoCg(clamp(ToYCoCg(history), minimumClip, maximumClip)), 0.0f);
    float temporalTrust = saturate((historySamples - 1.0f) / 7.0f) * stationary * (1.0f - materialReactive);
    // A sky/geometry transition has no stable surface correspondence. It must
    // not use the stationary-coverage exception merely because the camera stops.
    if (conflictingHistory) temporalTrust = 0.0f;
    clippedHistory = lerp(clippedHistory, history, temporalTrust);
    float relativeDifference = abs(Luminance(history) - Luminance(current)) / max(Luminance(current) + 0.12f, 0.12f);
    float clippingDifference = length(history - clippedHistory) / max(length(current) + 0.1f, 0.1f);
    // On trusted static geometry, phase disagreement is the signal TAA needs
    // to accumulate, not evidence of physical motion or material reactivity.
    float reactive = saturate(max(materialReactive,
        (relativeDifference * 0.65f + clippingDifference * 0.5f) * (1.0f - temporalTrust)));
    float motionReduction = saturate(motionPixels / 32.0f);
    float feedback = min(saturate(Parameters.z), 0.96f);
    float historyWeight = min(feedback, historySamples / (historySamples + 1.0f)) *
        (1.0f - reactive) * (1.0f - motionReduction * 0.5f);
    // Sampling disagreement must not prevent a genuinely stationary surface
    // from warming up: only geometry/material incompatibility reduces evidence.
    float confidence = 1.0f + min(historySamples, 31.0f) * (1.0f - materialReactive) * stationary;
    if (conflictingHistory) confidence = 1.0f;
    confidence = min(confidence, 1.0f / max(1.0f - feedback, 0.04f));
    return ResolveOutput(max(lerp(current, clippedHistory, historyWeight), 0.0f),
        encodedDepth, encodedRawDepth, confidence);
}
