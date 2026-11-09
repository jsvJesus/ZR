#include "Environment/Lighting.hlsli"

cbuffer TreeConstants : register(b0)
{
    row_major float4x4 World;
    row_major float4x4 ViewProjection;
    float4 CameraRight;
    float4 CameraUp;
    float4 Parameters;
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

    float3 position = mul(
        float4(input.Position, 1),
        World).xyz;

    if (input.Leaf.x >= 0)
    {
        float2 corner = input.Leaf.x < 0.5
            ? float2(0.5, -0.5)
            : input.Leaf.x < 1.5
                ? float2(-0.5, -0.5)
                : input.Leaf.x < 2.5
                    ? float2(-0.5, 0.5)
                    : float2(0.5, 0.5);

        corner = (corner - input.Card.zw)
            * input.Card.xy
            * CameraRight.w;

        position += CameraRight.xyz * corner.x
            + CameraUp.xyz * corner.y;
    }

    output.Position = mul(
        float4(position, 1),
        ViewProjection);

    output.WorldPosition = position;

    output.Normal = mul(
        float4(input.Normal, 0),
        World).xyz;

    output.Uv = input.Uv;
    output.Dimming = input.Leaf.y;

    return output;
}

float4 PSMain(
    PixelInput input,
    bool front : SV_IsFrontFace) : SV_TARGET
{
    float4 color = Diffuse.Sample(
        TreeSampler,
        input.Uv);

    clip(color.a - Parameters.y);

    float3 n = dot(input.Normal, input.Normal) > 0.000001
        ? normalize(input.Normal)
        : float3(0, 1, 0);

    float3 dp1 = ddx(input.WorldPosition);
    float3 dp2 = ddy(input.WorldPosition);

    float2 duv1 = ddx(input.Uv);
    float2 duv2 = ddy(input.Uv);

    float3 t = dp1 * duv2.y
        - dp2 * duv1.y;

    float determinant = duv1.x * duv2.y
        - duv1.y * duv2.x;

    if (Parameters.z > 0.5 &&
        dot(t, t) > 0.000001 &&
        abs(determinant) > 0.000001)
    {
        t = normalize(
            t * (determinant < 0 ? -1 : 1));

        float3 b = cross(n, t);

        if (dot(b, b) > 0.000001)
        {
            b = normalize(b)
                * (determinant < 0 ? -1 : 1);

            float3 mapNormal = NormalMap.Sample(
                TreeSampler,
                input.Uv).xyz * 2 - 1;

            float3 mapped = t * mapNormal.x
                + b * mapNormal.y
                + n * mapNormal.z;

            if (dot(mapped, mapped) > 0.000001)
            {
                n = normalize(mapped);
            }
        }
    }

    if (!front)
        n = -n;

    color.rgb = ShadeEnvironment(
        color.rgb,
        n,
        input.WorldPosition);

    color.rgb *= lerp(
        0.65,
        1.0,
        saturate(input.Dimming));

    color.rgb = lerp(
        color.rgb,
        float3(1, 0.65, 0.12),
        Parameters.x * 0.35);

    return float4(color.rgb, 1);
}