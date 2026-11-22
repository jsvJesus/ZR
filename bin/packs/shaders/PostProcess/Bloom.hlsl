cbuffer BloomConstants : register(b0)
{
    float4 BloomParameters;
};

Texture2D<float4> SourceTexture : register(t0);
Texture2D<float4> LowerTexture : register(t1);

SamplerState BloomSampler : register(s0);

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

float2 SourceTexelSize()
{
    uint width;
    uint height;

    SourceTexture.GetDimensions(width, height);

    return 1.0f / max(
        float2(width, height),
        float2(1.0f, 1.0f));
}

float3 SampleSource(float2 uv)
{
    return max(
        SourceTexture.SampleLevel(
            BloomSampler,
            uv,
            0.0f).rgb,
        0.0f);
}

float3 DownsampleSource(float2 uv)
{
    float2 texel = SourceTexelSize();

    float3 center = SampleSource(uv) * 4.0f;

    float3 corners =
        SampleSource(uv + texel * float2(-1.0f, -1.0f)) +
        SampleSource(uv + texel * float2(1.0f, -1.0f)) +
        SampleSource(uv + texel * float2(-1.0f, 1.0f)) +
        SampleSource(uv + texel * float2(1.0f, 1.0f));

    return (center + corners) * 0.125f;
}

float4 PSPrefilter(PixelInput input) : SV_Target
{
    float3 color = DownsampleSource(input.Uv);

    float brightness = max(
        color.r,
        max(color.g, color.b));

    float threshold = BloomParameters.x;
    float knee = max(BloomParameters.y, 0.0001f);

    float soft = clamp(
        brightness - threshold + knee,
        0.0f,
        2.0f * knee);

    soft = soft * soft / (4.0f * knee);

    float contribution = max(
        brightness - threshold,
        soft);

    contribution /= max(brightness, 0.0001f);

    return float4(
        color * contribution,
        1.0f);
}

float4 PSDownsample(PixelInput input) : SV_Target
{
    return float4(
        DownsampleSource(input.Uv),
        1.0f);
}

float3 SampleLower(float2 uv)
{
    return max(
        LowerTexture.SampleLevel(
            BloomSampler,
            uv,
            0.0f).rgb,
        0.0f);
}

float3 UpsampleLower(float2 uv)
{
    uint width;
    uint height;

    LowerTexture.GetDimensions(width, height);

    float2 texel = 1.0f / max(
        float2(width, height),
        float2(1.0f, 1.0f));

    float3 result =
        SampleLower(uv) * 4.0f;

    result +=
        SampleLower(uv + texel * float2(-1.0f, 0.0f)) +
        SampleLower(uv + texel * float2(1.0f, 0.0f)) +
        SampleLower(uv + texel * float2(0.0f, -1.0f)) +
        SampleLower(uv + texel * float2(0.0f, 1.0f));

    result += 0.5f * (
        SampleLower(uv + texel * float2(-1.0f, -1.0f)) +
        SampleLower(uv + texel * float2(1.0f, -1.0f)) +
        SampleLower(uv + texel * float2(-1.0f, 1.0f)) +
        SampleLower(uv + texel * float2(1.0f, 1.0f)));

    return result * 0.1f;
}

float4 PSUpsample(PixelInput input) : SV_Target
{
    float3 high = SampleSource(input.Uv);
    float3 low = UpsampleLower(input.Uv);

    return float4(
        high + low,
        1.0f);
}