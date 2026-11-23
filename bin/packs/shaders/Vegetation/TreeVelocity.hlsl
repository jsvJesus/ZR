#include "PostProcess/TemporalMotion.hlsli"

cbuffer TreeVelocityConstants : register(b0)
{
    row_major float4x4 World;
    row_major float4x4 PreviousWorld;
    row_major float4x4 ViewProjection;
    row_major float4x4 PreviousViewProjection;
    row_major float4x4 ColorViewProjection;
    float4 CameraRight;
    float4 CameraUp;
    float4 PreviousCameraRight;
    float4 PreviousCameraUp;
    float4 Parameters;
    float4 ViewportSize;
};

Texture2D Diffuse : register(t0);
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
    float4 CurrentClip : TEXCOORD0;
    float4 PreviousClip : TEXCOORD1;
    float4 PreviousGeometryCurrentClip : TEXCOORD2;
    float2 Uv : TEXCOORD3;
    float LeafCard : TEXCOORD4;
    float ColorClip : SV_ClipDistance0;
};

// Keep this reconstruction in step with Tree.hlsl. No wind deformation exists
// today. Future wind must evaluate current and previous deformation time (or
// retained positions) here before applying the respective billboard basis;
// camera motion is not a substitute for previous deformed geometry.
float3 ReconstructPosition(VertexInput input, row_major float4x4 world,
    float4 right, float4 up)
{
    float3 position = mul(float4(input.Position, 1), world).xyz;
    if (input.Leaf.x >= 0)
    {
        float2 corner = input.Leaf.x < 0.5
            ? float2(0.5, -0.5)
            : input.Leaf.x < 1.5
                ? float2(-0.5, -0.5)
                : input.Leaf.x < 2.5
                    ? float2(-0.5, 0.5)
                    : float2(0.5, 0.5);
        corner = (corner - input.Card.zw) * input.Card.xy * right.w;
        position += right.xyz * corner.x + up.xyz * corner.y;
    }
    return position;
}

PixelInput VSMain(VertexInput input)
{
    PixelInput output;
    float3 currentPosition = ReconstructPosition(input, World, CameraRight, CameraUp);
    float3 previousPosition = ReconstructPosition(input, PreviousWorld,
        PreviousCameraRight, PreviousCameraUp);
    output.CurrentClip = mul(float4(currentPosition, 1), ViewProjection);
    output.PreviousClip = mul(float4(previousPosition, 1), PreviousViewProjection);
    output.PreviousGeometryCurrentClip = mul(float4(previousPosition, 1), ViewProjection);
    output.Position = output.CurrentClip;
    // The shared depth domain may use a closer FPS near plane. Preserve the
    // original tree color visibility while testing depth in that shared domain.
    output.ColorClip = mul(float4(currentPosition, 1), ColorViewProjection).z;
    output.Uv = input.Uv;
    output.LeafCard = input.Leaf.x >= 0 ? 1 : 0;
    return output;
}

struct VelocityOutput
{
    float4 Motion : SV_Target0;
    float Reactive : SV_Target1;
};

VelocityOutput PSMain(PixelInput input)
{
    float alpha = Diffuse.Sample(TreeSampler, input.Uv).a;
    clip(alpha - Parameters.x);

    VelocityOutput output;
    output.Motion = MakeTemporalMotion(input.CurrentClip, input.PreviousClip, Parameters.y);

    // Project both geometry samples through the current camera: this measures
    // object/billboard deformation without mistaking camera jitter for motion.
    float2 geometryMotion = ClipToTemporalUv(input.CurrentClip)
        - ClipToTemporalUv(input.PreviousGeometryCurrentClip);
    float moving = saturate(length(geometryMotion * ViewportSize.xy) / 4.0f);
    float material = max(Parameters.z, input.LeafCard * 0.3);
    float cutoutEdge = Parameters.x > 0 ? saturate((0.75 - alpha) / 0.35) : 0;
    output.Reactive = max(1 - output.Motion.w,
        saturate(material * moving + cutoutEdge * 0.25 * max(moving, Parameters.w)
            + input.LeafCard * Parameters.w * 0.65));
    return output;
}
