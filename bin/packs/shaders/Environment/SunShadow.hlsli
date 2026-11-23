cbuffer EnvironmentShadow : register(b2)
{
    row_major float4x4 EnvironmentShadowMatrices[4];
    // enabled, strength, resolution, cascade blend fraction
    float4 EnvironmentShadowParams;
    float4 EnvironmentShadowSplits;
    // camera forward, camera near plane
    float4 EnvironmentShadowCameraForward;
    // Two millimetres in each cascade's normalized light depth.
    float4 EnvironmentShadowDepthBias;
};

Texture2DArray<float> EnvironmentShadowTexture : register(t7);
SamplerComparisonState EnvironmentShadowSampler : register(s7);

float SampleSunCascade(
    int cascade,
    float3 worldPosition,
    float3 worldDx,
    float3 worldDy)
{
    float3 projected = mul(
        float4(worldPosition, 1.0f),
        EnvironmentShadowMatrices[cascade]).xyz;

    float2 uv = float2(
        projected.x * 0.5f + 0.5f,
        0.5f - projected.y * 0.5f);

    float texel = 1.0f / max(EnvironmentShadowParams.z, 1.0f);

    // Keep the complete 3x3 PCF footprint, including bilinear taps, in this map.
    if (any(uv < 2.0f * texel) ||
        any(uv > 1.0f - 2.0f * texel) ||
        projected.z <= 0.0f || projected.z >= 1.0f)
    {
        return 1.0f;
    }

    // Orthographic light matrices: transform surface derivatives as vectors.
    // Use the geometric receiver plane, independent of material normal maps.
    float3 lightDx = mul(float4(worldDx, 0.0f), EnvironmentShadowMatrices[cascade]).xyz;
    float3 lightDy = mul(float4(worldDy, 0.0f), EnvironmentShadowMatrices[cascade]).xyz;
    float2 uvDx = lightDx.xy * float2(0.5f, -0.5f);
    float2 uvDy = lightDy.xy * float2(0.5f, -0.5f);
    float determinant = uvDx.x * uvDy.y - uvDx.y * uvDy.x;
    float2 depthGradient = 0.0f;

    if (abs(determinant) > 0.00001f * max(length(uvDx) * length(uvDy), 1e-20f))
    {
        depthGradient = float2(
            uvDy.y * lightDx.z - uvDx.y * lightDy.z,
            uvDx.x * lightDy.z - uvDy.x * lightDx.z) / determinant;
    }

    float bias = EnvironmentShadowDepthBias[cascade];
    float2 pixel = uv / texel - 0.5f;
    float2 basePixel = floor(pixel);
    float2 fraction = frac(pixel);

    // Reproduce the 3x3 bilinear PCF kernel with 16 weighted point comparisons.
    // Correct depth at each actual texel centre: hardware bilinear comparison
    // otherwise reuses one depth for four texels and leaves acne on steep slopes.
    float visibility = 0.0f;
    [unroll]
    for (int y = -1; y <= 2; y++)
    {
        float weightY = y == -1 ? 1.0f - fraction.y : (y == 2 ? fraction.y : 1.0f);
        [unroll]
        for (int x = -1; x <= 2; x++)
        {
            float weightX = x == -1 ? 1.0f - fraction.x : (x == 2 ? fraction.x : 1.0f);
            float2 tapUv = (basePixel + float2(x, y) + 0.5f) * texel;
            float receiverDepth = projected.z + dot(depthGradient, tapUv - uv);

            visibility += weightX * weightY * EnvironmentShadowTexture.SampleCmpLevelZero(
                EnvironmentShadowSampler,
                float3(tapUv, (float)cascade),
                receiverDepth - bias);
        }
    }

    return saturate(visibility / 9.0f);
}

float SunShadowVisibility(
    float3 worldPosition,
    float3 cameraPosition,
    float3 worldDx,
    float3 worldDy)
{
    float viewDepth = dot(
        worldPosition - cameraPosition,
        EnvironmentShadowCameraForward.xyz);

    if (EnvironmentShadowParams.x < 0.5f ||
        viewDepth < 0.0f || viewDepth > EnvironmentShadowSplits.w)
    {
        return 1.0f;
    }

    int cascade = 0;
    if (viewDepth > EnvironmentShadowSplits.x) cascade = 1;
    if (viewDepth > EnvironmentShadowSplits.y) cascade = 2;
    if (viewDepth > EnvironmentShadowSplits.z) cascade = 3;

    float visibility = SampleSunCascade(cascade, worldPosition, worldDx, worldDy);
    float startDepth = cascade == 0
        ? EnvironmentShadowCameraForward.w
        : EnvironmentShadowSplits[cascade - 1];
    float endDepth = EnvironmentShadowSplits[cascade];
    float blendRange = max(0.5f, (endDepth - startDepth) * EnvironmentShadowParams.w);
    float blend = smoothstep(endDepth - blendRange, endDepth, viewDepth);

    if (blend > 0.0f)
    {
        float nextVisibility = 1.0f;
        if (cascade < 3)
        {
            nextVisibility = SampleSunCascade(cascade + 1, worldPosition, worldDx, worldDy);
        }

        // Fade the final cascade into unshadowed distance as well.
        visibility = lerp(visibility, nextVisibility, blend);
    }

    return visibility;
}

float SunShadowVisibility(float3 worldPosition, float3 cameraPosition)
{
    // Evaluate derivatives before per-pixel cascade selection/blending.
    return SunShadowVisibility(worldPosition, cameraPosition, ddx(worldPosition), ddy(worldPosition));
}
