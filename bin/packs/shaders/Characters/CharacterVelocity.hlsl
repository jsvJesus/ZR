#include "PostProcess/TemporalMotion.hlsli"

cbuffer VelocityConstants : register(b0)
{
    row_major float4x4 CurrentWorld;
    row_major float4x4 PreviousWorld;
    row_major float4x4 CurrentViewProjection;
    row_major float4x4 PreviousViewProjection;
    row_major float4x4 ColorViewProjection;
    float4 PreviousCameraOffset;

    // UsesAlpha, alpha cutoff, diffuse tint alpha, transition visibility.
    float4 AlphaSettings;
    float4 OpacityScaleShift;
    // Previous geometry validity, material/first-person reactivity, target size.
    float4 MotionSettings;
};

Texture2D<float4> Diffuse : register(t0);
Texture2D<float4> OpacityMask : register(t1);
SamplerState SurfaceSampler : register(s0);

struct VertexInput
{
    float3 Position : POSITION0;
    float2 Uv : TEXCOORD0;
    float3 PreviousPosition : POSITION1;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float2 Uv : TEXCOORD0;
    float4 CurrentClip : TEXCOORD1;
    float4 PreviousClip : TEXCOORD2;
    float4 PreviousGeometryCurrentClip : TEXCOORD3;
    float ColorClip : SV_ClipDistance0;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;
    float4 currentWorld = mul(float4(input.Position, 1.0f), CurrentWorld);
    float4 previousWorld = mul(float4(input.PreviousPosition, 1.0f), PreviousWorld);
    output.CurrentClip = mul(currentWorld, CurrentViewProjection);
    output.PreviousClip = mul(previousWorld, PreviousViewProjection);
    // Put the previous deformed point in the current camera's origin/projection
    // so geometry reactivity excludes camera movement and the jitter delta.
    output.PreviousGeometryCurrentClip = mul(
        previousWorld + PreviousCameraOffset, CurrentViewProjection);
    output.Position = output.CurrentClip;
    // The body can still use the world near plane while its depth is remapped
    // into the FPS domain. Preserve its actual color-pass clipping coverage.
    output.ColorClip = mul(currentWorld, ColorViewProjection).z;
    output.Uv = input.Uv;
    return output;
}

float CharacterAlpha(PixelInput input)
{
    // Keep this ordering identical to Character.hlsl: tint and opacity mask
    // precede the cutout; distance visibility is applied after the cutout.
    float alpha = 1.0f;
    if (AlphaSettings.x > 0.5f)
    {
        alpha = Diffuse.Sample(SurfaceSampler, input.Uv).a * AlphaSettings.z;
        float2 maskUv = input.Uv * OpacityScaleShift.xy + OpacityScaleShift.zw;
        alpha *= OpacityMask.Sample(SurfaceSampler, maskUv).r;
        clip(alpha - AlphaSettings.y);
    }
    return alpha * saturate(AlphaSettings.w);
}

struct MotionOutput
{
    float4 Motion : SV_Target0;
    float Reactive : SV_Target1;
};

MotionOutput PSMain(PixelInput input)
{
    float alpha = CharacterAlpha(input);
    MotionOutput output;
    output.Motion = MakeTemporalMotion(input.CurrentClip, input.PreviousClip, MotionSettings.x);
    float2 geometryPixels = (
        ClipToTemporalUv(input.CurrentClip) -
        ClipToTemporalUv(input.PreviousGeometryCurrentClip)) * MotionSettings.zw;
    float materialReactive = saturate(MotionSettings.y) * saturate(length(geometryPixels) / 4.0f);
    float alphaWidth = fwidth(alpha);
    float cutoutReactive = AlphaSettings.x > 0.5f
        ? saturate((alphaWidth - abs(alpha - AlphaSettings.y)) / max(alphaWidth, 0.00001f))
        : 0.0f;
    output.Reactive = max(1.0f - output.Motion.w, max(materialReactive, cutoutReactive * 0.25f));
    return output;
}

float PSReactive(PixelInput input) : SV_Target0
{
    float alpha = CharacterAlpha(input);
    if (alpha <= 0.0f)
        discard;
    // Transparent/fading surfaces lack their own depth history. Conservatively
    // reject history wherever they contribute, without modifying opaque motion.
    return 1.0f;
}
