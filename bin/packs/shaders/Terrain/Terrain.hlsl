#include "Environment/Lighting.hlsli"

cbuffer TerrainBuffer : register(b0)
{
    row_major float4x4 ViewProjection;
    float4 Camera;
    float4 WorldExtent;
    float4 Layer;
    float4 LayerPassParameters;
    float4 Covering;
};

Texture2D Diffuse : register(t0);
Texture2D DetailNormal : register(t1);
Texture2D SurfaceNormal : register(t2);
Texture2D LayerMask : register(t3);
Texture2D WeightSum : register(t4);
Texture2D TerrainHoles : register(t5);

SamplerState MaterialSampler : register(s0);
SamplerState TileSampler : register(s1);

struct VertexInput
{
    float3 Position : POSITION;
    float2 TileUv : TEXCOORD0;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float3 World : TEXCOORD0;
    float2 TileUv : TEXCOORD1;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;

    output.Position = mul(
        float4(input.Position, 1),
        ViewProjection);

    output.World = input.Position;
    output.TileUv = input.TileUv;

    return output;
}

float TerrainVariantHash(float2 position)
{
    return frac(
        sin(dot(position, float2(127.1, 311.7)))
        * 43758.5453);
}

float TerrainVariantNoise(float2 position)
{
    float2 cell = floor(position);
    float2 fraction = frac(position);

    fraction = fraction * fraction *
        (3.0 - 2.0 * fraction);

    float a = TerrainVariantHash(cell);
    float b = TerrainVariantHash(cell + float2(1, 0));
    float c = TerrainVariantHash(cell + float2(0, 1));
    float d = TerrainVariantHash(cell + float2(1, 1));

    return lerp(
        lerp(a, b, fraction.x),
        lerp(c, d, fraction.x),
        fraction.y);
}

float CalculateCoveringWeight(float3 worldPosition)
{
    float count = max(Covering.x, 1.0);

    if (count < 1.5)
    {
        return 1.0;
    }

    float index = Covering.y;

    float largeNoise = TerrainVariantNoise(
        worldPosition.xz * 0.085);

    float detailNoise = TerrainVariantNoise(
        worldPosition.xz * 0.29);

    float noiseValue = saturate(
        largeNoise * 0.78 +
        detailNoise * 0.22);

    float transition = min(
        0.1,
        0.4 / count);

    float left = 1.0;
    float right = 0.0;

    if (index > 0.5)
    {
        float threshold = index / count;

        left = smoothstep(
            threshold - transition,
            threshold + transition,
            noiseValue);
    }

    if (index < count - 1.5)
    {
        float threshold = (index + 1.0) / count;

        right = smoothstep(
            threshold - transition,
            threshold + transition,
            noiseValue);
    }

    if (index < 0.5)
    {
        return 1.0 - right;
    }

    if (index >= count - 1.5)
    {
        return left;
    }

    return max(left - right, 0.0);
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    if (WorldExtent.w > 0.5)
    {
        clip(
            0.5 -
            TerrainHoles.Load(
                int3(
                    int2(input.World.xz / WorldExtent.z),
                    0)).r);
    }

    float2 tileUv = (
        4 + saturate(input.TileUv) * 248
    ) / 256;

    float sum = WeightSum.Sample(
        TileSampler,
        tileUv).r;

    float3 mask = LayerMask.Sample(
        TileSampler,
        tileUv).rgb;

    float channel = LayerPassParameters.y < 0.5
        ? mask.r
        : LayerPassParameters.y < 1.5
            ? mask.g
            : mask.b;

    float layerWeight = LayerPassParameters.x > 0.5
        ? max(1 - sum, 0)
        : channel / max(sum, 1);

    float coveringWeight = CalculateCoveringWeight(
        input.World);

    float weight = layerWeight * coveringWeight;

    float2 uv = input.World.xz
        / WorldExtent.xy
        * Layer.xy;

    if (Covering.w > 0.5)
    {
        uv = input.World.xz * Covering.z;
    }

    float3 color = Diffuse.Sample(
        MaterialSampler,
        uv).rgb;

    float3 surface = normalize(
        SurfaceNormal.Sample(
            TileSampler,
            tileUv).rgb * 2 - 1);

    float3 reference = abs(surface.z) < 0.95
        ? float3(0, 0, 1)
        : float3(0, 1, 0);

    float3 tangent = normalize(
        cross(surface, reference));

    float3 bitangent = normalize(
        cross(tangent, surface));

    float3 detail = DetailNormal.Sample(
        MaterialSampler,
        uv * Layer.z).rgb * 2 - 1;

    detail.z = max(detail.z, 0.08);

    float3 normal = normalize(
        tangent * detail.x +
        bitangent * detail.y +
        surface * detail.z);

    float3 lighting = ShadeEnvironment(
        color,
        normal,
        input.World);

    float3 view = normalize(
        Camera.xyz - input.World);

    float facingLight = saturate(
        dot(normal, EnvironmentSun.xyz));

    float specular = pow(
        saturate(
            dot(
                normal,
                normalize(EnvironmentSun.xyz + view))),
        24);

    specular *= saturate(Layer.w)
        * 0.14
        * facingLight
        * EnvironmentSun.w
        * ShadowVisibility(input.World);

    return float4(
        (lighting + specular) * weight,
        LayerPassParameters.x > 0.5 ? 1 : 0);
}