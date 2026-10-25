cbuffer TerrainOverviewBuffer : register(b0)
{
    row_major float4x4 ViewProjection;
    float4 WorldExtent;
    float4 Material;
    float4 MapMode;
};
Texture2D Diffuse : register(t0);
Texture2D DetailNormal : register(t1);
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
    output.Position = mul(float4(input.Position, 1), ViewProjection);
    output.World = input.Position;
    output.Normal = input.Normal;
    return output;
}
float4 PSMain(PixelInput input) : SV_TARGET
{
    float2 levelUv = input.World.xz / WorldExtent.xy;
    float2 diffuseUv = levelUv * Material.xy;
    float2 normalUv = levelUv * Material.zw;
    float3 surface = normalize(input.Normal);
    float3 reference = abs(surface.z) < 0.95 ? float3(0, 0, 1) : float3(0, 1, 0);
    float3 tangent = normalize(cross(surface, reference));
    float3 bitangent = normalize(cross(tangent, surface));
    float3 normalColor = MapMode.y > 0.5
        ? DetailNormal.Sample(OverviewSampler, normalUv).rgb
        : DetailNormal.Sample(MaterialSampler, normalUv).rgb;
    float3 detail = normalColor * 2 - 1;
    float3 normal = normalize(tangent * detail.x + bitangent * detail.y + surface * max(detail.z, 0.08));
    float light = 0.36 + 0.64 * saturate(dot(normal, normalize(float3(-0.4, 0.85, -0.3))));
    float3 color = MapMode.x > 0.5
        ? Diffuse.Sample(OverviewSampler, diffuseUv).rgb
        : Diffuse.Sample(MaterialSampler, diffuseUv).rgb;
    return float4(color * light, 1);
}
