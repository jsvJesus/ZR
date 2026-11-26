Texture2D HudTexture : register(t0);
SamplerState HudSampler : register(s0);

struct VertexOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

VertexOutput VSMain(uint vertexId : SV_VertexID)
{
    VertexOutput output;

    float2 uv = float2(
        (vertexId << 1) & 2,
        vertexId & 2);

    output.Position = float4(
        uv.x * 2.0f - 1.0f,
        1.0f - uv.y * 2.0f,
        0.0f,
        1.0f);

    output.TexCoord = uv;

    return output;
}

float4 PSMain(VertexOutput input) : SV_TARGET
{
    return HudTexture.Sample(
        HudSampler,
        input.TexCoord);
}