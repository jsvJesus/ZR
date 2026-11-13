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
        / lightPosition.w;

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

    return EnvironmentSkyShadowTexture.SampleCmpLevelZero(
        EnvironmentSkyShadowSampler,
        uv,
        projected.z - EnvironmentSkyShadowParams.z);
}

float SurfaceSkyVisibility(
    float3 worldPosition,
    float3 surfaceNormal)
{
    float overhead = SkyVisibility(worldPosition);

    if (overhead >= 0.999)
    {
        return overhead;
    }

    float horizontal = length(surfaceNormal.xz);

    if (horizontal < 0.35)
    {
        return overhead;
    }

    float3 outwardDirection = float3(
        surfaceNormal.x / horizontal,
        0.0,
        surfaceNormal.z / horizontal);

    // Проверяем небо с открытой стороны стены,
    // а не только строго над точкой поверхности.
    float3 outsidePosition =
        worldPosition +
        outwardDirection * 1.8 +
        float3(0.0, 0.1, 0.0);

    float sideVisibility = SkyVisibility(
        outsidePosition);

    float wallFactor = smoothstep(
        0.35,
        0.75,
        horizontal);

    return lerp(
        overhead,
        max(overhead, sideVisibility),
        wallFactor);
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

    if (lightPosition.w <= 0.0)
    {
        return 1.0;
    }

    float3 projected = lightPosition.xyz
        / lightPosition.w;

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

    visibility = saturate(visibility / 9.0);

    // В закрытом пространстве солнечный свет
    // должен полностью блокироваться геометрией.
    float overhead = SkyVisibility(worldPosition);

    float strength = lerp(
        1.0,
        saturate(EnvironmentShadowParams.y),
        overhead);

    return lerp(
        1.0,
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

    // Для окружающего освещения выбираем сторону
    // поверхности, обращённую к наблюдателю.
    float3 skyNormal = n;

    float3 toCamera =
        EnvironmentCamera.xyz - worldPosition;

    if (dot(skyNormal, toCamera) < 0.0)
    {
        skyNormal = -skyNormal;
    }

    float skyVisibility = SurfaceSkyVisibility(
        worldPosition,
        skyNormal);

    // Минимальный фоновый свет для закрытых помещений.
    float ambientFactor = lerp(
        0.08,
        1.0,
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

    float fog = 1.0 - exp(
        -max(EnvironmentFog.w, 0.0) *
        distanceToCamera);

    return lerp(
        result,
        EnvironmentFog.rgb,
        saturate(fog));
}