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
    row_major float4x4 EnvironmentShadowMatrices[4];
    float4 EnvironmentShadowParams;
    float4 EnvironmentShadowSplits;
    float4 EnvironmentShadowCameraForward;
};

cbuffer EnvironmentSkyShadow : register(b3)
{
    row_major float4x4 EnvironmentSkyShadowMatrix;
    float4 EnvironmentSkyShadowParams;
};

Texture2DArray<float> EnvironmentShadowTexture : register(t7);
SamplerComparisonState EnvironmentShadowSampler : register(s7);

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

float SampleSunCascade(
    int cascade,
    float3 worldPosition)
{
    float4 lightPosition = mul(
        float4(worldPosition, 1.0f),
        EnvironmentShadowMatrices[cascade]);

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
        max(EnvironmentShadowParams.z, 1.0f);

    float bias =
        EnvironmentShadowParams.w *
        (1.0f + cascade * 0.35f);

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
                EnvironmentShadowTexture.SampleCmpLevelZero(
                    EnvironmentShadowSampler,
                    float3(uv + offset, (float)cascade),
                    projected.z - bias);
        }
    }

    return saturate(visibility / 9.0f);
}

float ShadowVisibility(float3 worldPosition)
{
    if (EnvironmentShadowParams.x < 0.5f)
    {
        return 1.0f;
    }

    float viewDepth = dot(
        worldPosition - EnvironmentCamera.xyz,
        EnvironmentShadowCameraForward.xyz);

    if (viewDepth < 0.0f ||
        viewDepth > EnvironmentShadowSplits.w)
    {
        return 1.0f;
    }

    int cascade = 0;

    if (viewDepth > EnvironmentShadowSplits.x)
    {
        cascade = 1;
    }

    if (viewDepth > EnvironmentShadowSplits.y)
    {
        cascade = 2;
    }

    if (viewDepth > EnvironmentShadowSplits.z)
    {
        cascade = 3;
    }

    float visibility = SampleSunCascade(
        cascade,
        worldPosition);

    if (cascade < 3)
    {
        float startDepth = cascade == 0
            ? 0.0f
            : EnvironmentShadowSplits[cascade - 1];

        float endDepth =
            EnvironmentShadowSplits[cascade];

        float blendRange = max(
            0.5f,
            (endDepth - startDepth) * 0.12f);

        float blend = saturate(
            (viewDepth -
                (endDepth - blendRange)) /
            blendRange);

        if (blend > 0.0f)
        {
            float nextVisibility = SampleSunCascade(
                cascade + 1,
                worldPosition);

            visibility = lerp(
                visibility,
                nextVisibility,
                blend);
        }
    }

    float overhead = SkyVisibility(worldPosition);

    // Apply full occlusion under a roof, while preserving
    // the user-defined shadow strength outdoors.
    float strength = lerp(
        1.0f,
        saturate(EnvironmentShadowParams.y),
        overhead);

    return lerp(
        1.0f,
        visibility,
        strength);
}

float3 ShadeEnvironment(
    float3 surfaceColor,
    float3 normal,
    float3 worldPosition)
{
    float3 n = normalize(normal);

    float diffuse = saturate(
        dot(n, EnvironmentSun.xyz));

    float sunlightVisibility = ShadowVisibility(
        worldPosition);

    float skyVisibility = SurfaceSkyVisibility(
        worldPosition,
        n);

    float ambientFactor = lerp(
        0.08f,
        1.0f,
        skyVisibility);

    float3 ambientLighting =
        EnvironmentAmbient.rgb *
        ambientFactor;

    float3 directLighting =
        EnvironmentSunColor.rgb *
        EnvironmentSun.w *
        diffuse *
        sunlightVisibility;

    float3 result = surfaceColor *
        (ambientLighting + directLighting);

    float distanceToCamera = length(
        worldPosition - EnvironmentCamera.xyz);

    float fog = 1.0f - exp(
        -max(EnvironmentFog.w, 0.0f) *
        distanceToCamera);

    return lerp(
        result,
        EnvironmentFog.rgb,
        saturate(fog));
}