#include "Environment/Lighting.hlsli"

cbuffer RoadConstants : register(b0)
{
    row_major float4x4 ViewProjection;
    float4 Parameters;
    float4 CameraPosition;
    float4 SpecularParameters;
};

Texture2D Diffuse : register(t0);
Texture2D NormalMap : register(t1);
Texture2D SpecularMap : register(t2);
Texture2D SpecularPowerMap : register(t3);

SamplerState RoadSampler : register(s0);

struct VertexInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 Uv : TEXCOORD;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float3 WorldPosition : TEXCOORD1;
    float3 Normal : NORMAL;
    float2 Uv : TEXCOORD0;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;

    output.Position = mul(
        float4(input.Position, 1),
        ViewProjection);

    output.WorldPosition = input.Position;
    output.Normal = input.Normal;
    output.Uv = input.Uv;

    return output;
}

float4 PSDepth(PixelInput input) : SV_TARGET
{
    clip(
        Diffuse.Sample(
            RoadSampler,
            input.Uv).a - 0.999);

    return 0;
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    float4 color = Diffuse.Sample(
        RoadSampler,
        input.Uv);

    clip(color.a - 0.02);

    float3 n = dot(input.Normal, input.Normal) > 0.000001
        ? normalize(input.Normal)
        : float3(0, 1, 0);

    float3 dp1 = ddx(input.WorldPosition);
    float3 dp2 = ddy(input.WorldPosition);

    float2 duv1 = ddx(input.Uv);
    float2 duv2 = ddy(input.Uv);

    float3 tangent = dp1 * duv2.y
        - dp2 * duv1.y;

    float determinant = duv1.x * duv2.y
        - duv1.y * duv2.x;

    if (Parameters.y > 0.5 &&
        dot(tangent, tangent) > 0.000001 &&
        abs(determinant) > 0.000001)
    {
        tangent = normalize(
            tangent * (determinant < 0 ? -1 : 1));

        float3 bitangent = normalize(
            cross(n, tangent))
            * (determinant < 0 ? -1 : 1);

        float3 mapNormal = NormalMap.Sample(
            RoadSampler,
            input.Uv).xyz * 2 - 1;

        n = normalize(
            tangent * mapNormal.x +
            bitangent * mapNormal.y +
            n * mapNormal.z);
    }

    float3 lightDirection = normalize(
        EnvironmentSun.xyz);

    float facingLight = saturate(
        dot(n, lightDirection));

    float visibility = ShadowVisibility(
        input.WorldPosition);

    color.rgb = ShadeEnvironment(
        color.rgb,
        n,
        input.WorldPosition);

    float3 viewDirection = CameraPosition.xyz
        - input.WorldPosition;

    viewDirection *= rsqrt(
        max(dot(viewDirection, viewDirection), 0.000001));

    float3 halfway = lightDirection + viewDirection;

    halfway *= rsqrt(
        max(dot(halfway, halfway), 0.000001));

    float gloss = SpecularParameters.y;

    if (Parameters.z > 0.5)
    {
        gloss *= SpecularPowerMap.Sample(
            RoadSampler,
            input.Uv).r;
    }

    float highlight = pow(
        saturate(dot(n, halfway)),
        lerp(4, 256, saturate(gloss)));

    color.rgb += SpecularMap.Sample(
        RoadSampler,
        input.Uv).rgb
        * SpecularParameters.x
        * highlight
        * facingLight
        * visibility
        * EnvironmentSun.w;

    color.rgb = lerp(
        color.rgb,
        float3(1, 0.65, 0.12),
        Parameters.x * 0.35);

    return color;
}