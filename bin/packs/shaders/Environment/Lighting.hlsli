cbuffer EnvironmentLighting : register(b1)
{
    float4 EnvironmentSun;
    float4 EnvironmentSunColor;
    float4 EnvironmentAmbient;
    float4 EnvironmentFog;
    float4 EnvironmentCamera;
};

cbuffer EnvironmentShadow : register(b2)
{
    row_major float4x4 EnvironmentShadowMatrix;
    float4 EnvironmentShadowParams;
};

cbuffer EnvironmentSkyShadow : register(b3)
{
    row_major float4x4 EnvironmentSkyShadowMatrix;
    float4 EnvironmentSkyShadowParams;
};

Texture2D EnvironmentShadowTexture : register(t7);
SamplerComparisonState EnvironmentShadowSampler : register(s7);

Texture2D EnvironmentSkyShadowTexture : register(t23);
SamplerComparisonState EnvironmentSkyShadowSampler : register(s8);

float SkyVisibility(float3 worldPosition)
{
    if (EnvironmentSkyShadowParams.x < 0.5)
    {
        return 1.0;
    }

    float4 lightPosition = mul(
        float4(worldPosition, 1.0),
        EnvironmentSkyShadowMatrix);

    if (lightPosition.w <= 0.0)
    {
        return 1.0;
    }

    float3 projected = lightPosition.xyz
        / max(lightPosition.w, 0.00001);

    float2 uv = float2(
        projected.x * 0.5 + 0.5,
        0.5 - projected.y * 0.5);

    if (any(uv < 0.0) ||
        any(uv > 1.0) ||
        projected.z <= 0.0 ||
        projected.z >= 1.0)
    {
        return 1.0;
    }

    float texel = 1.0 /
        max(EnvironmentSkyShadowParams.y, 1.0);

    float visibility = 0.0;

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 offset = float2(x, y) * texel;

            visibility +=
                EnvironmentSkyShadowTexture.SampleCmpLevelZero(
                    EnvironmentSkyShadowSampler,
                    uv + offset,
                    projected.z - EnvironmentSkyShadowParams.z);
        }
    }

    return saturate(visibility / 9.0);
}

float ShadowVisibility(float3 worldPosition)
{
    if (EnvironmentShadowParams.x < 0.5)
    {
        return 1.0;
    }

    float4 lightPosition = mul(
        float4(worldPosition, 1.0),
        EnvironmentShadowMatrix);

    float3 projected = lightPosition.xyz
        / max(lightPosition.w, 0.00001);

    float2 uv = float2(
        projected.x * 0.5 + 0.5,
        0.5 - projected.y * 0.5);

    if (any(uv < 0.0) ||
        any(uv > 1.0) ||
        projected.z <= 0.0 ||
        projected.z >= 1.0)
    {
        return 1.0;
    }

    float texel = 1.0 /
        max(EnvironmentShadowParams.z, 1.0);

    float visibility = 0.0;

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 offset = float2(x, y) * texel;

            visibility +=
                EnvironmentShadowTexture.SampleCmpLevelZero(
                    EnvironmentShadowSampler,
                    uv + offset,
                    projected.z - EnvironmentShadowParams.w);
        }
    }

    visibility /= 9.0;

    float sky = SkyVisibility(worldPosition);

    float shadowStrength = lerp(
        1.0,
        saturate(EnvironmentShadowParams.y),
        sky);

    return lerp(
        1.0,
        saturate(visibility),
        shadowStrength);
}

float3 ShadeEnvironment(
    float3 surfaceColor,
    float3 normal,
    float3 worldPosition)
{
    float3 n = normalize(normal);

    float diffuse = saturate(
        dot(n, EnvironmentSun.xyz));

    float visibility = ShadowVisibility(worldPosition);

    float skyVisibility = SkyVisibility(worldPosition);

    float ambientFactor = lerp(
        0.08,
        1.0,
        skyVisibility);

    float3 ambientLighting =
        EnvironmentAmbient.rgb * ambientFactor;

    float3 directLighting =
        EnvironmentSunColor.rgb
        * EnvironmentSun.w
        * diffuse
        * visibility;

    float3 lighting =
        ambientLighting + directLighting;

    float3 result = surfaceColor * lighting;

    float distanceToCamera = length(
        worldPosition - EnvironmentCamera.xyz);

    float fog = 1.0 - exp(
        -max(EnvironmentFog.w, 0.0)
        * distanceToCamera);

    return lerp(
        result,
        EnvironmentFog.rgb,
        saturate(fog));
}