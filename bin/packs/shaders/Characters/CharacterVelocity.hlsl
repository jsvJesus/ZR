cbuffer VelocityConstants : register(b0)
{
    row_major float4x4 CurrentWorld;
    row_major float4x4 PreviousWorld;
    row_major float4x4 CurrentViewProjection;
    row_major float4x4 PreviousViewProjection;

    float4 AlphaSettings;
    float4 OpacityScaleShift;
};

Texture2D<float4> Diffuse : register(t0);
Texture2D<float4> OpacityMask : register(t1);

SamplerState SurfaceSampler : register(s0);

struct VertexInput
{
    float3 Position : POSITION0;
    float2 Uv : TEXCOORD0;
    float3 PreviousPosition : POSITION1;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float2 Uv : TEXCOORD0;
    float4 CurrentClip : TEXCOORD1;
    float4 PreviousClip : TEXCOORD2;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;

    float4 currentWorld = mul(
        float4(input.Position, 1.0f),
        CurrentWorld);

    float4 previousWorld = mul(
        float4(input.PreviousPosition, 1.0f),
        PreviousWorld);

    output.CurrentClip = mul(
        currentWorld,
        CurrentViewProjection);

    output.PreviousClip = mul(
        previousWorld,
        PreviousViewProjection);

    output.Position = output.CurrentClip;
    output.Uv = input.Uv;

    return output;
}

float2 ClipToUv(float4 position)
{
    float2 ndc = position.xy /
        max(position.w, 0.000001f);

    return float2(
        ndc.x * 0.5f + 0.5f,
        0.5f - ndc.y * 0.5f);
}

float2 PSMain(PixelInput input) : SV_Target0
{
    if (AlphaSettings.x > 0.5f)
    {
        float alpha = Diffuse.Sample(
            SurfaceSampler,
            input.Uv).a;

        float2 maskUv =
            input.Uv * OpacityScaleShift.xy +
            OpacityScaleShift.zw;

        alpha *= OpacityMask.Sample(
            SurfaceSampler,
            maskUv).r;

        clip(alpha - AlphaSettings.y);
    }

    if (input.CurrentClip.w <= 0.00001f ||
        input.PreviousClip.w <= 0.00001f)
    {
        return float2(0.0f, 0.0f);
    }

    float2 currentUv = ClipToUv(
        input.CurrentClip);

    float2 previousUv = ClipToUv(
        input.PreviousClip);

    float2 velocity = currentUv - previousUv;

    return clamp(
        velocity,
        float2(-1.0f, -1.0f),
        float2(1.0f, 1.0f));
}