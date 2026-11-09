#include "Environment/Lighting.hlsli"

cbuffer SceneConstants : register(b0)
{
    row_major float4x4 ViewProjection;
    float4 MaterialColor;
    float4 CameraPosition;
    float4 Parameters;
};

Texture2D Diffuse : register(t0);
Texture2D NormalMap : register(t1);
SamplerState SceneSampler : register(s0);

struct VertexInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 Uv : TEXCOORD0;
    float4 Tangent : TANGENT;
    float4 World0 : WORLD0;
    float4 World1 : WORLD1;
    float4 World2 : WORLD2;
    float4 World3 : WORLD3;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float3 WorldPosition : TEXCOORD1;
    float3 Normal : NORMAL;
    float2 Uv : TEXCOORD0;
    float4 Tangent : TEXCOORD2;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;

    float4x4 world = float4x4(
        input.World0,
        input.World1,
        input.World2,
        input.World3);

    float4 position = mul(
        float4(input.Position, 1),
        world);

    output.Position = mul(position, ViewProjection);
    output.WorldPosition = position.xyz;

    float3 x = input.World0.xyz;
    float3 y = input.World1.xyz;
    float3 z = input.World2.xyz;

    float determinant = dot(x, cross(y, z));
    float handedness = determinant < 0 ? -1 : 1;

    float3 normal = (
        input.Normal.x * cross(y, z)
        + input.Normal.y * cross(z, x)
        + input.Normal.z * cross(x, y)
    ) * handedness;

    output.Normal = dot(normal, normal) > 0.0000001
        ? normalize(normal)
        : float3(0, 1, 0);

    float3 tangent = mul(
        float4(input.Tangent.xyz, 0),
        world).xyz;

    output.Tangent = float4(
        tangent,
        input.Tangent.w * handedness);

    output.Uv = input.Uv;

    return output;
}

float4 PSMain(
    PixelInput input,
    bool frontFace : SV_IsFrontFace) : SV_TARGET
{
    float4 color = Diffuse.Sample(
        SceneSampler,
        input.Uv) * MaterialColor;

    if (Parameters.x > 0)
        clip(color.a - Parameters.x);
    else if (Parameters.y > 0.5)
        clip(color.a - 0.01);
    else
        color.a = 1;

    float3 n = normalize(input.Normal);

    float3 tangent = input.Tangent.xyz
        - n * dot(n, input.Tangent.xyz);

    if (Parameters.z > 0.5 &&
        dot(tangent, tangent) > 0.000001)
    {
        tangent = normalize(tangent);

        float3 bitangent = cross(
            n,
            tangent) * (input.Tangent.w < 0 ? -1 : 1);

        float3 mapNormal = NormalMap.Sample(
            SceneSampler,
            input.Uv).xyz * 2 - 1;

        n = normalize(
            tangent * mapNormal.x +
            bitangent * mapNormal.y +
            n * mapNormal.z);
    }

    if (!frontFace)
        n = -n;

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

    float specular = saturate(Parameters.w)
        * pow(saturate(dot(n, halfway)), 32)
        * facingLight
        * visibility
        * EnvironmentSun.w;

    color.rgb += specular;

    return color;
}