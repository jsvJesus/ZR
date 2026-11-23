cbuffer TaaConstants : register(b0)
{
    row_major float4x4 InverseViewProjection;
    row_major float4x4 CurrentViewProjection;
    row_major float4x4 PreviousViewProjection;

    float4 Parameters;
    // x = width
    // y = height
    // z = history feedback
    // w = history valid
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

    output.Position = float4(
        uv.x * 2.0f - 1.0f,
        1.0f - uv.y * 2.0f,
        0.0f,
        1.0f);

    output.Uv = uv;

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

void Neighborhood(
    int2 pixel,
    out float3 minimumColor,
    out float3 maximumColor,
    out float3 averageColor)
{
    int2 size = int2(Parameters.xy);

    minimumColor = float3(
        65504.0f,
        65504.0f,
        65504.0f);

    maximumColor = 0.0f;
    averageColor = 0.0f;

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            int2 samplePixel = clamp(
                pixel + int2(x, y),
                int2(0, 0),
                size - 1);

            float3 color = max(
                CurrentColor.Load(
                    int3(samplePixel, 0)).rgb,
                0.0f);

            minimumColor = min(
                minimumColor,
                color);

            maximumColor = max(
                maximumColor,
                color);

            averageColor += color;
        }
    }

    averageColor /= 9.0f;
}

float4 PSResolve(PixelInput input) : SV_Target
{
    int2 pixel = clamp(
        int2(input.Position.xy),
        int2(0, 0),
        int2(Parameters.xy) - 1);

    float2 uv =
        (float2(pixel) + 0.5f) / Parameters.xy;

    float3 current = max(
        CurrentColor.Load(
            int3(pixel, 0)).rgb,
        0.0f);

    float depth = CurrentDepth.Load(
        int3(pixel, 0));

    // Sky does not write scene depth.
    // Avoid history smearing caused by camera rotation.
    if (depth >= 0.999999f)
    {
        return float4(current, 0.0f);
    }

    float3 world = ReconstructWorld(uv, depth);

    float4 currentClip = mul(
        float4(world, 1.0f),
        CurrentViewProjection);

    float currentViewDepth = max(
        currentClip.w,
        0.0f);

    float encodedDepth = log2(
        1.0f + currentViewDepth);

    if (Parameters.w < 0.5f)
    {
        return float4(current, encodedDepth);
    }

    float2 velocity = MotionVectors.Load(
        int3(pixel, 0));

    float2 previousUv = uv - velocity;

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

    // Disocclusion / changed geometry.
    if (history.a <= 0.0f ||
        abs(history.a - expectedDepth) >
        depthTolerance)
    {
        return float4(current, encodedDepth);
    }

    float3 minimumColor;
    float3 maximumColor;
    float3 averageColor;

    Neighborhood(
        pixel,
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

    // Current neighborhood limits old-frame color.
    // This prevents bright objects leaving long trails.
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

    float reactive = saturate(
        relativeDifference * 1.25f +
        clippingDifference * 0.8f);

    float motionReduction = saturate(
        velocityPixels / 24.0f);

    float historyWeight =
        saturate(Parameters.z) *
        (1.0f - reactive * 0.85f) *
        (1.0f - motionReduction * 0.65f);

    historyWeight = min(
        historyWeight,
        0.96f);

    float3 resolved = lerp(
        current,
        clippedHistory,
        historyWeight);

    return float4(
        max(resolved, 0.0f),
        encodedDepth);
}