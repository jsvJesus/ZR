#include "Environment/Lighting.hlsli"
#include "Terrain/TerrainHoleMask.hlsli"

cbuffer TerrainOverviewBuffer : register(b0)
{
    row_major float4x4 ViewProjection;
    float4 TerrainOrigin;
    float4 WorldExtent;
    float4 Material;
    float4 MapMode;
};

Texture2D Diffuse : register(t0);
Texture2D DetailNormal : register(t1);
Texture2D PaintedOverview : register(t6);

SamplerState MaterialSampler : register(s0);
SamplerState OverviewSampler : register(s1);

struct VertexInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float3 World : TEXCOORD0;
    float3 Normal : TEXCOORD1;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;

    output.Position = mul(
        float4(input.Position, 1),
        ViewProjection);

    output.World = input.Position;
    output.Normal = input.Normal;

    return output;
}

float4 PSShadow(PixelInput input) : SV_TARGET
{
    if (WorldExtent.w > 0.5 &&
        IsTerrainHole(input.World.xz, TerrainOrigin.xy, WorldExtent.z))
    {
        discard;
    }

    return 0;
}

SurfaceOutput PSMain(PixelInput input)
{
     if (WorldExtent.w > 0.5 &&
        IsTerrainHole(
            input.World.xz,
            TerrainOrigin.xy,
            WorldExtent.z))
    {
        discard;
    }

    float2 levelUv = (input.World.xz - TerrainOrigin.xy)
        / WorldExtent.xy;

    // Atlas maps follow terrain samples; repeating fallback materials follow the world.
    float2 tiledUv = input.World.xz / WorldExtent.xy;
    float2 diffuseUv = (MapMode.x > 0.5 ? levelUv : tiledUv) * Material.xy;
    float2 normalUv = (MapMode.y > 0.5 ? levelUv : tiledUv) * Material.zw;

    float3 surface = normalize(input.Normal);

    float3 reference = abs(surface.z) < 0.95
        ? float3(0, 0, 1)
        : float3(0, 1, 0);

    float3 tangent = normalize(
        cross(surface, reference));

    float3 bitangent = normalize(
        cross(tangent, surface));

    float3 normalColor = MapMode.y > 0.5
        ? DetailNormal.Sample(
            OverviewSampler,
            normalUv).rgb
        : DetailNormal.Sample(
            MaterialSampler,
            normalUv).rgb;

    float3 detail = normalColor * 2 - 1;

    float3 normal = normalize(
        tangent * detail.x +
        bitangent * detail.y +
        surface * max(detail.z, 0.08));

    float3 color = MapMode.x > 0.5
        ? Diffuse.Sample(
            OverviewSampler,
            diffuseUv).rgb
        : Diffuse.Sample(
            MaterialSampler,
            diffuseUv).rgb;

    if (MapMode.z > 0.5)
    {
        float4 paint = PaintedOverview.Sample(
            OverviewSampler,
            levelUv);

        color = lerp(
            color,
            paint.rgb,
            paint.a);
    }

    float3 ambient;
    color = ShadeEnvironment(
        color,
        normal,
        input.World,
        ambient);

    return MakeSurfaceOutput(float4(color, 1), ambient);
}
