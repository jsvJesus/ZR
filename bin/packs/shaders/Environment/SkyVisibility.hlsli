cbuffer EnvironmentSkyShadow : register(b3)
{
    row_major float4x4 EnvironmentSkyShadowMatrix;
    float4 EnvironmentSkyShadowParams;
};

Texture2D<float> EnvironmentSkyShadowTexture : register(t23);
SamplerComparisonState EnvironmentSkyShadowSampler : register(s8);

float SkyVisibility(float3 worldPosition)
{
    if (EnvironmentSkyShadowParams.x < 0.5f)
    {
        return 1.0f;
    }

    float4 lightPosition = mul(
        float4(worldPosition, 1.0f),
        EnvironmentSkyShadowMatrix);

    if (lightPosition.w <= 0.0f)
    {
        return 1.0f;
    }

    float3 projected =
        lightPosition.xyz / lightPosition.w;

    float2 uv = float2(
        projected.x * 0.5f + 0.5f,
        0.5f - projected.y * 0.5f);

    if (any(uv < 0.0f) ||
        any(uv > 1.0f) ||
        projected.z <= 0.0f ||
        projected.z >= 1.0f)
    {
        return 1.0f;
    }

    float texel = 1.0f /
        max(EnvironmentSkyShadowParams.y, 1.0f);

    float visibility = 0.0f;

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 offset =
                float2(x, y) * texel;

            visibility +=
                EnvironmentSkyShadowTexture.SampleCmpLevelZero(
                    EnvironmentSkyShadowSampler,
                    uv + offset,
                    projected.z -
                        EnvironmentSkyShadowParams.z);
        }
    }

    return saturate(visibility / 9.0f);
}

float SurfaceSkyVisibility(
    float3 worldPosition,
    float3 surfaceNormal)
{
    // Do not sample through exterior walls:
    // side probes previously introduced indoor light leaks.
    return SkyVisibility(
        worldPosition + float3(0.0f, 0.05f, 0.0f));
}

