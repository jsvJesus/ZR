cbuffer TerrainBuffer : register(b0)
{
    row_major float4x4 ViewProjection;
    float4 Camera;
    float4 WorldExtent;
    float4 Layer;
    float4 LayerPassParameters;
};

Texture2D Diffuse : register(t0);
Texture2D DetailNormal : register(t1);
Texture2D SurfaceNormal : register(t2);
Texture2D LayerMask : register(t3);
Texture2D WeightSum : register(t4);
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
    output.Position = mul(float4(input.Position, 1), ViewProjection);
    output.World = input.Position;
    output.TileUv = input.TileUv;
    return output;
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    // Both the masks and stored normals have a four-texel neighboring border.
    float2 tileUv = (4 + saturate(input.TileUv) * 248) / 256;
    float sum = WeightSum.Sample(TileSampler, tileUv).r;
    float3 mask = LayerMask.Sample(TileSampler, tileUv).rgb;
    float channel = LayerPassParameters.y < 0.5 ? mask.r : (LayerPassParameters.y < 1.5 ? mask.g : mask.b);
    float weight = LayerPassParameters.x > 0.5 ? max(1 - sum, 0) : channel / max(sum, 1);

    float2 uv = input.World.xz / WorldExtent.xy * Layer.xy;
    float3 color = Diffuse.Sample(MaterialSampler, uv).rgb;
    float3 surface = normalize(SurfaceNormal.Sample(TileSampler, tileUv).rgb * 2 - 1);
    float3 reference = abs(surface.z) < 0.95 ? float3(0, 0, 1) : float3(0, 1, 0);
    float3 tangent = normalize(cross(surface, reference));
    float3 bitangent = normalize(cross(tangent, surface));
    float3 detail = DetailNormal.Sample(MaterialSampler, uv * Layer.z).rgb * 2 - 1;
    detail.z = max(detail.z, 0.08);
    float3 normal = normalize(tangent * detail.x + bitangent * detail.y + surface * detail.z);
    float3 light = normalize(float3(-0.4, 0.85, -0.3));
    float diffuse = 0.36 + 0.64 * saturate(dot(normal, light));
    float3 view = normalize(Camera.xyz - input.World);
    float specular = pow(saturate(dot(normal, normalize(light + view))), 24) * saturate(Layer.w) * 0.14;
    return float4((color * diffuse + specular) * weight, LayerPassParameters.x > 0.5 ? 1 : 0);
}
