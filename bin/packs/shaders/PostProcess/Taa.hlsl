cbuffer TaaConstants : register(b0)
{
    row_major float4x4 InverseViewProjection;
    row_major float4x4 CurrentViewProjection;
    row_major float4x4 PreviousViewProjection;

    float4 Parameters;
    // x = Width
    // y = Height
    // z = History Feedback
    // w = History Valid

    float4 Jitter;
    // xy = Current Jitter in pixels
    // zw = Previous Jitter in pixels
};

Texture2D<float4> CurrentColor : register(t0);
Texture2D<float4> PreviousHistory : register(t1);
Texture2D<float> CurrentDepth : register(t2);
Texture2D<float2> MotionVectors : register(t3);

SamplerState LinearSampler : register(s0);

struct PixelInput
{
    float4 Position : SV_POSITION;
    float2 Uv : TEXCOORD0;
};

PixelInput VSMain(uint vertexId : SV_VertexID)
{
    PixelInput output;

    float2 uv = float2(
        (vertexId << 1) & 2,
        vertexId & 2);

    output.Uv = uv;

    output.Position = float4(
        uv.x * 2.0f - 1.0f,
        1.0f - uv.y * 2.0f,
        0.0f,
        1.0f);

    return output;
}

float3 ReconstructWorld(float2 uv, float depth)
{
    float4 clip = float4(
        uv.x * 2.0f - 1.0f,
        1.0f - uv.y * 2.0f,
        depth,
        1.0f);

    float4 world = mul(
        clip,
        InverseViewProjection);

    return world.xyz / max(world.w, 0.000001f);
}

float Luminance(float3 color)
{
    return dot(
        color,
        float3(0.2126f, 0.7152f, 0.0722f));
}

float3 ReadColor(float2 uv)
{
    return max(
        CurrentColor.SampleLevel(
            LinearSampler,
            saturate(uv),
            0.0f).rgb,
        0.0f);
}

void Neighborhood(
    float2 uv,
    out float3 minimumColor,
    out float3 maximumColor,
    out float3 averageColor)
{
    float2 texel = 1.0f / Parameters.xy;

    minimumColor = float3(65504.0f, 65504.0f, 65504.0f);
    maximumColor = float3(0.0f, 0.0f, 0.0f);
    averageColor = 0.0f;

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float3 color = ReadColor(
                uv + float2(x, y) * texel);

            minimumColor = min(minimumColor, color);
            maximumColor = max(maximumColor, color);
            averageColor += color;
        }
    }

    averageColor /= 9.0f;
}

float DepthEdgeFactor(int2 pixel, float centerDepth)
{
    int2 size = int2(Parameters.xy);

    float3 centerWorld = ReconstructWorld(
        (float2(pixel) + 0.5f) / Parameters.xy,
        centerDepth);

    float centerDistance = max(
        mul(float4(centerWorld, 1.0f),
            CurrentViewProjection).w,
        0.001f);

    float maximumDifference = 0.0f;

    [unroll]
    for (int direction = 0; direction < 4; direction++)
    {
        int2 offset =
            direction == 0 ? int2(-1, 0) :
            direction == 1 ? int2(1, 0) :
            direction == 2 ? int2(0, -1) :
            int2(0, 1);

        int2 samplePixel = clamp(
            pixel + offset,
            int2(0, 0),
            size - 1);

        float sampleDepth = CurrentDepth.Load(
            int3(samplePixel, 0));

        if (sampleDepth >= 0.999999f)
        {
            return 1.0f;
        }

        float3 sampleWorld = ReconstructWorld(
            (float2(samplePixel) + 0.5f) / Parameters.xy,
            sampleDepth);

        float sampleDistance = mul(
            float4(sampleWorld, 1.0f),
            CurrentViewProjection).w;

        maximumDifference = max(
            maximumDifference,
            abs(sampleDistance - centerDistance));
    }

    return saturate(
        maximumDifference /
        max(centerDistance * 0.08f, 0.08f));
}

float4 PSResolve(PixelInput input) : SV_Target
{
    int2 size = int2(Parameters.xy);

    int2 pixel = clamp(
        int2(input.Position.xy),
        int2(0, 0),
        size - 1);

    float2 uv =
        (float2(pixel) + 0.5f) / Parameters.xy;

    // The projected image is displaced by Halton jitter.
    // Sample the displaced image to reconstruct an
    // unjittered output pixel.
    float2 sceneUv = saturate(
        uv + Jitter.xy / Parameters.xy);

    int2 scenePixel = clamp(
        int2(sceneUv * Parameters.xy),
        int2(0, 0),
        size - 1);

    float depth = CurrentDepth.Load(
        int3(scenePixel, 0));

    // The sky is rendered without projection jitter.
    if (depth >= 0.999999f)
    {
        float3 sky = max(
            CurrentColor.Load(
                int3(pixel, 0)).rgb,
            0.0f);

        return float4(sky, 0.0f);
    }

    float3 current = ReadColor(sceneUv);

    float3 world = ReconstructWorld(
        sceneUv,
        depth);

    float4 currentClip = mul(
        float4(world, 1.0f),
        CurrentViewProjection);

    float viewDepth = max(
        currentClip.w,
        0.0001f);

    float encodedDepth = log2(1.0f + viewDepth);

    if (Parameters.w < 0.5f)
    {
        return float4(current, encodedDepth);
    }

    float2 velocity = MotionVectors.Load(
        int3(scenePixel, 0));

    // Motion buffer contains screen-space motion from
    // the jittered projections. History is stored in
    // unjittered output coordinates.
    float2 previousUv =
        sceneUv -
        velocity -
        Jitter.zw / Parameters.xy;

    if (any(previousUv <= 0.0f) ||
        any(previousUv >= 1.0f))
    {
        return float4(current, encodedDepth);
    }

    float4 previousClip = mul(
        float4(world, 1.0f),
        PreviousViewProjection);

    if (previousClip.w <= 0.00001f)
    {
        return float4(current, encodedDepth);
    }

    float previousDepth =
        previousClip.z / previousClip.w;

    if (previousDepth <= 0.0f ||
        previousDepth >= 1.0f)
    {
        return float4(current, encodedDepth);
    }

    float4 history = PreviousHistory.SampleLevel(
        LinearSampler,
        previousUv,
        0.0f);

    float expectedDepth = log2(
        1.0f + previousClip.w);

    float depthTolerance =
        0.018f + expectedDepth * 0.004f;

    if (history.a <= 0.0f ||
        abs(history.a - expectedDepth) > depthTolerance)
    {
        return float4(current, encodedDepth);
    }

    float3 minimumColor;
    float3 maximumColor;
    float3 averageColor;

    Neighborhood(
        sceneUv,
        minimumColor,
        maximumColor,
        averageColor);

    float3 colorRange = max(
        maximumColor - minimumColor,
        0.00001f);

    float3 expansion = colorRange * 0.05f;

    float3 clippedHistory = clamp(
        history.rgb,
        minimumColor - expansion,
        maximumColor + expansion);

    float currentLuma = Luminance(current);
    float historyLuma = Luminance(history.rgb);

    float relativeDifference =
        abs(historyLuma - currentLuma) /
        max(currentLuma + 0.12f, 0.12f);

    float clippingDifference =
        length(history.rgb - clippedHistory) /
        max(length(current) + 0.1f, 0.1f);

    float velocityPixels = length(
        velocity * Parameters.xy);

    float edgeFactor = DepthEdgeFactor(
        scenePixel,
        depth);

    // Screen-space reactive estimate for changing
    // silhouettes, cutouts and rapidly changing color.
    float reactive = saturate(
        relativeDifference * 1.25f +
        clippingDifference * 0.8f +
        edgeFactor * 0.3f);

    float motionReduction = saturate(
        velocityPixels / 24.0f);

    float historyWeight =
        saturate(Parameters.z) *
        (1.0f - reactive * 0.85f) *
        (1.0f - motionReduction * 0.65f);

    historyWeight = min(historyWeight, 0.96f);

    float3 resolved = lerp(
        current,
        clippedHistory,
        historyWeight);

    return float4(
        max(resolved, 0.0f),
        encodedDepth);
}