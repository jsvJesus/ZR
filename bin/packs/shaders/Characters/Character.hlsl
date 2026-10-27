cbuffer CharacterConstants : register(b0)
{
    row_major float4x4 World;
    row_major float4x4 ViewProjection;
};
Texture2D Diffuse : register(t0);
SamplerState SurfaceSampler : register(s0);
struct VertexInput { float3 Position : POSITION; float3 Normal : NORMAL; float2 Uv : TEXCOORD; };
struct PixelInput { float4 Position : SV_POSITION; float3 Normal : NORMAL; float2 Uv : TEXCOORD; };
PixelInput VSMain(VertexInput input)
{
    PixelInput output;
    output.Position = mul(mul(float4(input.Position, 1), World), ViewProjection);
    output.Normal = mul(float4(input.Normal, 0), World).xyz;
    output.Uv = input.Uv;
    return output;
}
float4 PSMain(PixelInput input) : SV_TARGET
{
    float4 color = Diffuse.Sample(SurfaceSampler, input.Uv);
    clip(color.a - 0.08f);
    float light = 0.48f + 0.52f * saturate(dot(normalize(input.Normal), normalize(float3(-0.4f, 0.8f, -0.5f))));
    return float4(color.rgb * light, color.a);
}
