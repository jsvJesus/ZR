#include "Environment/SurfaceOutput.hlsli"

cbuffer EnvironmentLighting : register(b1)
{
    float4 EnvironmentSun;
    float4 EnvironmentSunColor;
    float4 EnvironmentAmbient;
    float4 EnvironmentFog;
    float4 EnvironmentCamera;
};

#include "Environment/SunShadow.hlsli"

#include "Environment/SkyVisibility.hlsli"

float ShadowVisibility(float3 worldPosition)
{
    float visibility = SunShadowVisibility(worldPosition, EnvironmentCamera.xyz);

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
    float3 worldPosition,
    out float3 ambientContribution)
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

    ambientContribution = surfaceColor * ambientLighting * (1.0f - saturate(fog));

    return lerp(
        result,
        EnvironmentFog.rgb,
        saturate(fog));
}

float3 ShadeEnvironment(float3 surfaceColor, float3 normal, float3 worldPosition)
{
    float3 ambient;
    return ShadeEnvironment(surfaceColor, normal, worldPosition, ambient);
}
