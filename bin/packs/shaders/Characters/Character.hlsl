cbuffer CharacterConstants : register(b0)
{
    row_major float4x4 World;
    row_major float4x4 ViewProjection;
    float4 DiffuseColor;
    float4 ColourR;
    float4 ColourG;
    float4 ColourB;
    float4 ColourA;
    float4 PatternScaleShift;
    float4 PatternOptions; // cos, sin, use secondary UV, enable colouring
    float4 OpacityScaleShift;
    float4 SurfaceOptions; // use diffuse opacity, alpha cutoff
};
Texture2D Diffuse : register(t0);
Texture2D DiffuseMask : register(t1);
Texture2D ColourMask : register(t2);
Texture2D OpacityMask : register(t3);
SamplerState SurfaceSampler : register(s0);
struct VertexInput { float3 Position : POSITION; float3 Normal : NORMAL; float2 Uv : TEXCOORD0; float2 Uv2 : TEXCOORD1; };
struct PixelInput { float4 Position : SV_POSITION; float3 Normal : NORMAL; float2 Uv : TEXCOORD0; float2 Uv2 : TEXCOORD1; };
PixelInput VSMain(VertexInput input)
{
    PixelInput output;
    output.Position = mul(mul(float4(input.Position, 1), World), ViewProjection);
    output.Normal = mul(float4(input.Normal, 0), World).xyz;
    output.Uv = input.Uv;
    output.Uv2 = input.Uv2;
    return output;
}
float4 PSMain(PixelInput input) : SV_TARGET
{
    float4 color = Diffuse.Sample(SurfaceSampler, input.Uv);
    color.rgb *= DiffuseColor.rgb;
    if (PatternOptions.w > 0.5f)
    {
        float2 uv = lerp(input.Uv, input.Uv2, PatternOptions.z) * PatternScaleShift.xy;
        uv = float2(uv.x * PatternOptions.x - uv.y * PatternOptions.y,
                    uv.x * PatternOptions.y + uv.y * PatternOptions.x) + PatternScaleShift.zw;
        // All four mask channels encode pattern colours. In particular, mask
        // alpha is the fourth colour weight and must never become transparency.
        float4 weights = ColourMask.Sample(SurfaceSampler, uv)
            * float4(ColourR.a, ColourG.a, ColourB.a, ColourA.a);
        float total = dot(weights, float4(1, 1, 1, 1));
        float3 tint = (weights.r * ColourR.rgb + weights.g * ColourG.rgb
            + weights.b * ColourB.rgb + weights.a * ColourA.rgb + max(1 - total, 0)) / max(total, 1);
        float3 coverage = saturate(DiffuseMask.Sample(SurfaceSampler, input.Uv).rgb);
        color.rgb *= lerp(float3(1, 1, 1), tint, coverage);
    }
    if (SurfaceOptions.x > 0.5f)
    {
        float2 opacityUv = input.Uv * OpacityScaleShift.xy + OpacityScaleShift.zw;
        color.a *= OpacityMask.Sample(SurfaceSampler, opacityUv).r;
        clip(color.a - SurfaceOptions.y);
    }
    else color.a = 1; // Opaque textures may store material data in their alpha.
    float light = 0.48f + 0.52f * saturate(dot(normalize(input.Normal), normalize(float3(-0.4f, 0.8f, -0.5f))));
    return float4(color.rgb * light, color.a);
}
