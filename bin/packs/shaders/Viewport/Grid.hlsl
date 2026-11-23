#include "Environment/SurfaceOutput.hlsli"

cbuffer CameraBuffer : register(b0)
{
    row_major float4x4 ViewProjection;
};

struct VertexInput
{
    float3 Position : POSITION;
    float4 Color : COLOR;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;
    output.Position = mul(float4(input.Position, 1.0f), ViewProjection);
    output.Color = input.Color;
    return output;
}

SurfaceOutput PSMain(PixelInput input)
{
    return MakeSurfaceOutput(input.Color, 0);
}
