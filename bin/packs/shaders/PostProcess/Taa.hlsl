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

// A bilinear reconstruction changes its blur with each Halton phase. Cubic
// reconstruction preserves detail while returning color to the fixed output grid.
float CubicWeight(float distance)
{
    float x = abs(distance);
    if (x < 1.0f) return (1.5f * x - 2.5f) * x * x + 1.0f;
    if (x < 2.0f) return ((-0.5f * x + 2.5f) * x - 4.0f) * x + 2.0f;
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
    reactive = 0.0f;
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            int2 samplePixel = ClampPixel(pixel + int2(x, y));
            float3 color = ToYCoCg(max(CurrentColor.Load(int3(samplePixel, 0)).rgb, 0.0f));
            minimumColor = min(minimumColor, color);
            maximumColor = max(maximumColor, color);
            averageColor += color;
            secondMoment += color * color;
            reactive = max(reactive, ReactiveMask.Load(int3(samplePixel, 0)));
        }
    }
    averageColor /= 9.0f;
    deviation = sqrt(max(secondMoment / 9.0f - averageColor * averageColor, 0.000001f));
    float2 samplePosition = float2(pixel) + Jitter.xy;
    int2 firstPixel = int2(floor(samplePosition));
    float2 fraction = samplePosition - floor(samplePosition);
    float3 result = 0.0f;
    float totalWeight = 0.0f;
    float surfaceWeight = 0.0f;
    [unroll]
    for (int row = -1; row <= 2; row++)
    {
        [unroll]
        for (int column = -1; column <= 2; column++)
        {
            float weight = CubicWeight(column - fraction.x) * CubicWeight(row - fraction.y);
            int2 samplePixel = ClampPixel(firstPixel + int2(column, row));
            result += max(CurrentColor.Load(int3(samplePixel, 0)).rgb, 0.0f) * weight;
            totalWeight += weight;
            float depth = CurrentDepth.Load(int3(samplePixel, 0));
            // Use only actual positive reconstruction support. At zero jitter
            // all off-center weights are zero: adjacent geometry cannot turn a
            // pure sky pixel into a moving foreground surface.
            if (weight > max(surfaceWeight, 0.00001f) && depth < 0.999999f)
            {
                surfaceWeight = weight;
                surfaceDepth = depth;
                surfacePixel = samplePixel;
            }
        }
    }
    if (surfaceWeight <= 0.0f)
        return max(CurrentColor.Load(int3(pixel, 0)).rgb, 0.0f); // unjittered sky
    // Bound negative cubic lobes so HDR highlights cannot ring around cutouts.
    return max(FromYCoCg(clamp(ToYCoCg(result / max(totalWeight, 0.00001f)),
        minimumColor, maximumColor)), 0.0f);
}

struct TemporalOutput
{
    float4 Color : SV_Target0;
    float RawDepth : SV_Target1;
};

TemporalOutput ResolveOutput(float3 color, float surfaceDepth, float rawDepth)
{
    TemporalOutput output;
    output.Color = float4(color, surfaceDepth);
    output.RawDepth = rawDepth;
    return output;
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

    int2 previousPixel = ClampPixel(int2(previousDepthUv * Parameters.xy));
    float storedDepth = PreviousRawDepth.Load(int3(previousPixel, 0));
    // Never interpolate depth across silhouettes while checking disocclusion.
    float tolerance = 0.01f + expectedDepth * 0.002f;
    if ((sky && storedDepth > 0.0f) || (!sky &&
        (storedDepth <= 0.0f || abs(storedDepth - expectedDepth) > tolerance)))
        return ResolveOutput(current, encodedDepth, encodedRawDepth);

    float3 history = max(PreviousHistory.SampleLevel(LinearSampler, previousUv, 0.0f).rgb, 0.0f);
    float3 minimumClip = max(minimumColor, averageColor - deviation * 1.5f);
    float3 maximumClip = min(maximumColor, averageColor + deviation * 1.5f);
    float3 clippedHistory = max(FromYCoCg(clamp(ToYCoCg(history), minimumClip, maximumClip)), 0.0f);
    float relativeDifference = abs(Luminance(history) - Luminance(current)) / max(Luminance(current) + 0.12f, 0.12f);
    float clippingDifference = length(history - clippedHistory) / max(length(current) + 0.1f, 0.1f);
    float reactive = saturate(max(materialReactive, relativeDifference * 0.65f + clippingDifference * 0.5f));
    float motionReduction = saturate(length(geometricMotion * Parameters.xy) / 32.0f);
    float historyWeight = min(saturate(Parameters.z), 0.96f) * (1.0f - reactive) * (1.0f - motionReduction * 0.5f);
    return ResolveOutput(max(lerp(current, clippedHistory, historyWeight), 0.0f), encodedDepth, encodedRawDepth);
}
