cbuffer TreeConstants : register(b0)
{
    row_major float4x4 World;
    row_major float4x4 ViewProjection;
    float4 CameraRight;
    float4 CameraUp;
    float4 Parameters; // selected, alpha cutoff, has normal map
};
Texture2D Diffuse : register(t0);
Texture2D NormalMap : register(t1);
SamplerState TreeSampler : register(s0);
struct VertexInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 Uv : TEXCOORD0;
    float4 Card : TEXCOORD1;
    float2 Leaf : TEXCOORD2;
};
struct PixelInput
{
    float4 Position : SV_POSITION;
    float3 WorldPosition : TEXCOORD0;
    float3 Normal : TEXCOORD1;
    float2 Uv : TEXCOORD2;
    float Dimming : TEXCOORD3;
};
PixelInput VSMain(VertexInput input)
{
    PixelInput output;
    float3 position = mul(float4(input.Position, 1), World).xyz;
    if (input.Leaf.x >= 0)
    {
        float2 corner = input.Leaf.x < 0.5f ? float2(0.5f, -0.5f)
            : input.Leaf.x < 1.5f ? float2(-0.5f, -0.5f)
            : input.Leaf.x < 2.5f ? float2(-0.5f, 0.5f) : float2(0.5f, 0.5f);
        corner = (corner - input.Card.zw) * input.Card.xy * CameraRight.w;
        position += CameraRight.xyz * corner.x + CameraUp.xyz * corner.y;
    }
    output.Position = mul(float4(position, 1), ViewProjection);
    output.WorldPosition = position;
    output.Normal = mul(float4(input.Normal, 0), World).xyz;
    output.Uv = input.Uv;
    output.Dimming = input.Leaf.y;
    return output;
}
float4 PSMain(PixelInput input, bool front : SV_IsFrontFace) : SV_TARGET
{
    float4 color = Diffuse.Sample(TreeSampler, input.Uv);
    clip(color.a - Parameters.y);
    float3 n = dot(input.Normal, input.Normal) > 0.000001f ? normalize(input.Normal) : float3(0, 1, 0);
    float3 dp1 = ddx(input.WorldPosition), dp2 = ddy(input.WorldPosition);
    float2 duv1 = ddx(input.Uv), duv2 = ddy(input.Uv);
    float3 t = dp1 * duv2.y - dp2 * duv1.y;
    float determinant = duv1.x * duv2.y - duv1.y * duv2.x;
    if (Parameters.z > 0.5f && dot(t, t) > 0.000001f && abs(determinant) > 0.000001f)
    {
        t = normalize(t * (determinant < 0 ? -1 : 1));
        float3 b = normalize(cross(n, t)) * (determinant < 0 ? -1 : 1);
        float3 map = NormalMap.Sample(TreeSampler, input.Uv).xyz * 2 - 1;
        n = normalize(t * map.x + b * map.y + n * map.z);
    }
    if (!front) n = -n;
    float light = 0.48f + 0.52f * saturate(dot(n, normalize(float3(-0.4f, 0.8f, -0.5f))));
    color.rgb *= light * lerp(0.65f, 1, saturate(input.Dimming));
    color.rgb = lerp(color.rgb, float3(1, 0.65f, 0.12f), Parameters.x * 0.35f);
    return float4(color.rgb, 1);
}
