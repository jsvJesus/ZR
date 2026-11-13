cbuffer SceneConstants : register(b0)
{
    row_major float4x4 ViewProjection;
    float4 MaterialColor;
    float4 CameraPosition;
    float4 Parameters;
};

Texture2D Diffuse : register(t0);
SamplerState SceneSampler : register(s0);

float4 PSMain(float2 uv : TEXCOORD0) : SV_TARGET
{
    if (Parameters.x > 0.0)
    {
        float alpha = Diffuse.Sample(
            SceneSampler,
            uv).a * MaterialColor.a;

        clip(alpha - Parameters.x);
    }

    return float4(0.0, 0.0, 0.0, 0.0);
}