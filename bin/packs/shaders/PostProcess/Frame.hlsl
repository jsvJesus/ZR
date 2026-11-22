cbuffer FrameConstants : register(b0)
{
    row_major float4x4 InverseViewProjection;
    row_major float4x4 PreviousViewProjection;

    // x = width
    // y = height
    // z = exposure
    // w = tone mapping enabled
    float4 Parameters;
};

Texture2D<float> DepthTexture : register(t0);
Texture2D<float4> HdrTexture : register(t0);

struct PixelInput
{
    float4 Position : SV_POSITION;
    float2 Uv : TEXCOORD0;
};

struct SurfaceOutput
{
    float4 Normal : SV_Target0;
    float Roughness : SV_Target1;
    float2 Motion : SV_Target2;
};

PixelInput VSMain(uint vertexId : SV_VertexID)
{
    PixelInput output;

    float2 uv = float2(
        (vertexId << 1) & 2,
        vertexId & 2);

    output.Uv = uv;

    output.Position = float4(
        uv.x * 2.0f - 1.0f,
        1.0f - uv.y * 2.0f,
        0.0f,
        1.0f);

    return output;
}

float3 ReconstructPosition(
    float2 uv,
    float depth)
{
    float4 clipPosition = float4(
        uv.x * 2.0f - 1.0f,
        1.0f - uv.y * 2.0f,
        depth,
        1.0f);

    float4 world = mul(
        clipPosition,
        InverseViewProjection);

    return world.xyz / max(
        world.w,
        0.000001f);
}

float2 PixelUv(int2 coordinate)
{
    return (
        float2(coordinate) + 0.5f
    ) / Parameters.xy;
}

SurfaceOutput PSSurface(PixelInput input)
{
    SurfaceOutput output;

    int2 size = int2(Parameters.xy);
    int2 maximum = size - 1;

    int2 pixel = clamp(
        int2(input.Position.xy),
        int2(0, 0),
        maximum);

    float depth = DepthTexture.Load(
        int3(pixel, 0));

    if (depth >= 0.999999f)
    {
        output.Normal = float4(
            0.5f,
            0.5f,
            1.0f,
            0.0f);

        output.Roughness = 1.0f;
        output.Motion = float2(0.0f, 0.0f);

        return output;
    }

    int2 leftPixel = max(
        pixel - int2(1, 0),
        int2(0, 0));

    int2 rightPixel = min(
        pixel + int2(1, 0),
        maximum);

    int2 upPixel = max(
        pixel - int2(0, 1),
        int2(0, 0));

    int2 downPixel = min(
        pixel + int2(0, 1),
        maximum);

    float leftDepth = DepthTexture.Load(
        int3(leftPixel, 0));

    float rightDepth = DepthTexture.Load(
        int3(rightPixel, 0));

    float upDepth = DepthTexture.Load(
        int3(upPixel, 0));

    float downDepth = DepthTexture.Load(
        int3(downPixel, 0));

    float3 world = ReconstructPosition(
        PixelUv(pixel),
        depth);

    float3 leftWorld = ReconstructPosition(
        PixelUv(leftPixel),
        leftDepth);

    float3 rightWorld = ReconstructPosition(
        PixelUv(rightPixel),
        rightDepth);

    float3 upWorld = ReconstructPosition(
        PixelUv(upPixel),
        upDepth);

    float3 downWorld = ReconstructPosition(
        PixelUv(downPixel),
        downDepth);

    float3 tangentX =
        abs(rightDepth - depth) <=
        abs(leftDepth - depth)
            ? rightWorld - world
            : world - leftWorld;

    float3 tangentY =
        abs(downDepth - depth) <=
        abs(upDepth - depth)
            ? downWorld - world
            : world - upWorld;

    float3 normal = cross(
        tangentX,
        tangentY);

    float normalLengthSquared = dot(
        normal,
        normal);

    if (normalLengthSquared > 0.000000001f)
    {
        normal *= rsqrt(normalLengthSquared);
    }
    else
    {
        normal = float3(
            0.0f,
            1.0f,
            0.0f);
    }

    output.Normal = float4(
        normal * 0.5f + 0.5f,
        1.0f);

    // Temporary material-independent value.
    output.Roughness = 0.75f;

    float4 previousClip = mul(
        float4(world, 1.0f),
        PreviousViewProjection);

    float2 velocity = float2(
        0.0f,
        0.0f);

    if (previousClip.w > 0.000001f)
    {
        float2 previousNdc =
            previousClip.xy / previousClip.w;

        float2 previousUv = float2(
            previousNdc.x * 0.5f + 0.5f,
            0.5f - previousNdc.y * 0.5f);

        velocity =
            PixelUv(pixel) - previousUv;
    }

    // UV displacement from previous frame to current frame.
    output.Motion = velocity;

    return output;
}

float3 ApplyToneMapping(float3 color)
{
    color = max(color, 0.0f);

    if (Parameters.w < 0.5f)
    {
        return saturate(color);
    }

    color *= Parameters.z;

    // ACES filmic approximation.
    float3 numerator =
        color * (2.51f * color + 0.03f);

    float3 denominator =
        color * (2.43f * color + 0.59f)
        + 0.14f;

    return saturate(
        numerator / denominator);
}

float4 PSPresent(PixelInput input) : SV_Target
{
    int2 size = int2(Parameters.xy);

    int2 pixel = clamp(
        int2(input.Position.xy),
        int2(0, 0),
        size - 1);

    float3 hdr = HdrTexture.Load(
        int3(pixel, 0)).rgb;

    float3 color = ApplyToneMapping(hdr);

    return float4(
        color,
        1.0f);
}