#include "PostProcess/TemporalMotion.hlsli"

cbuffer VelocityConstants : register(b0)
{
    row_major float4x4 CurrentViewProjection;
    row_major float4x4 PreviousViewProjection;
    row_major float4x4 ColorViewProjection;
    float4 MaterialColor;
    float4 CameraPosition;
    float4 Parameters; // alpha cutoff, blended, global history valid, unused
    float4 ViewportSize;
};

Texture2D Diffuse : register(t0);
SamplerState SceneSampler : register(s0);

struct VertexInput
{
    float3 Position : POSITION;
    float2 Uv : TEXCOORD0;
    float4 World0 : WORLD0;
    float4 World1 : WORLD1;
    float4 World2 : WORLD2;
    float4 World3 : WORLD3;
    float4 PreviousWorld0 : PREVIOUSWORLD0;
    float4 PreviousWorld1 : PREVIOUSWORLD1;
    float4 PreviousWorld2 : PREVIOUSWORLD2;
    float4 PreviousWorld3 : PREVIOUSWORLD3;
    float4 History : HISTORY;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float ColorClip : SV_ClipDistance0;
    float2 Uv : TEXCOORD0;
    float4 CurrentClip : TEXCOORD1;
    float4 PreviousClip : TEXCOORD2;
    float4 ObjectPreviousClip : TEXCOORD3;
    float3 WorldPosition : TEXCOORD4;
    nointerpolation float Valid : TEXCOORD5;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;
    float4x4 world = float4x4(input.World0, input.World1, input.World2, input.World3);
    float4x4 previousWorld = float4x4(input.PreviousWorld0, input.PreviousWorld1,
        input.PreviousWorld2, input.PreviousWorld3);
    float4 position = mul(float4(input.Position, 1), world);
    float4 previous = mul(float4(input.Position, 1), previousWorld);
    output.CurrentClip = mul(position, CurrentViewProjection);
    output.PreviousClip = mul(previous, PreviousViewProjection);
    // Applying one camera matrix to both worlds isolates rigid geometry motion.
    output.ObjectPreviousClip = mul(previous, CurrentViewProjection);
    output.Position = output.CurrentClip;
    // Preserve the color pass near clipping when FPS depth uses a closer near
    // plane; motion/depth still remain in the active temporal camera domain.
    output.ColorClip = mul(position, ColorViewProjection).z;
    output.WorldPosition = position.xyz;
    output.Uv = input.Uv;
    output.Valid = input.History.x * Parameters.z;
    return output;
}

float SurfaceAlpha(PixelInput input, out float fadeReactive)
{
    fadeReactive = 0;
    if (CameraPosition.w > 0.0f)
    {
        float distanceFade = 1.0f - smoothstep(CameraPosition.w * 0.75f,
            CameraPosition.w, length(input.WorldPosition - CameraPosition.xyz));
        float dither = frac(52.9829189f * frac(dot(floor(input.Position.xy),
            float2(0.06711056f, 0.00583715f))));
        clip(distanceFade - max(dither, 0.00001f));
        fadeReactive = 1.0f - distanceFade;
    }
    float alpha = Diffuse.Sample(SceneSampler, input.Uv).a * MaterialColor.a;
    if (Parameters.x > 0)
        clip(alpha - Parameters.x);
    else if (Parameters.y > 0.5f)
        clip(alpha - 0.01f);
    else
        alpha = 1;
    return alpha;
}

struct VelocityOutput
{
    float4 Motion : SV_Target0;
    float Reactive : SV_Target1;
};

VelocityOutput PSMain(PixelInput input)
{
    float fadeReactive;
    float alpha = SurfaceAlpha(input, fadeReactive);
    VelocityOutput output;
    output.Motion = MakeTemporalMotion(input.CurrentClip, input.PreviousClip, input.Valid);
    // Fully covered static interiors keep their history. Only the surviving side
    // of alpha edges, dither fades and moving geometry reduce accumulation.
    float edge = Parameters.x > 0
        ? 1.0f - smoothstep(0.0f, max(fwidth(alpha) * 2.0f, 0.015f), alpha - Parameters.x)
        : 0.0f;
    float2 geometryPixels = (ClipToTemporalUv(input.CurrentClip)
        - ClipToTemporalUv(input.ObjectPreviousClip)) * ViewportSize.xy;
    float moving = min(length(geometryPixels) * 0.035f, 0.65f);
    // Static cutout edges still need temporal accumulation to suppress grass
    // shimmer. Reserve full rejection for incompatible history/translucency.
    output.Reactive = output.Motion.w < 0.5f ? 1.0f : max(max(edge * 0.25f, fadeReactive), moving);
    return output;
}

// Translucent layers preserve the opaque motion/depth beneath them. Their
// actual surviving alpha contributes to the independently MAX-blended mask.
float PSReactive(PixelInput input) : SV_Target0
{
    float fadeReactive;
    return saturate(SurfaceAlpha(input, fadeReactive));
}
