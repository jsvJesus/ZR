#include "Environment/Lighting.hlsli"

cbuffer TerrainBuffer : register(b0)
{
    row_major float4x4 ViewProjection;

    float4 Camera;
    float4 WorldExtent;
    float4 Layer;
    float4 LayerPassParameters;

    float4 PbrProperties;
    float4 BlendProperties;
    float4 CoveringSettings;
    float4 CoveringMeters;
    float4 CoveringSlope;
    float4 RoughnessFlags;
    float4 HeightFlags;
};

Texture2D Albedo0 : register(t0);
Texture2D Normal0 : register(t1);

Texture2D SurfaceNormal : register(t2);
Texture2D LayerMask : register(t3);
Texture2D WeightSum : register(t4);
Texture2D TerrainHoles : register(t5);

Texture2D Roughness0 : register(t21);
Texture2D Height0 : register(t8);

Texture2D Albedo1 : register(t9);
Texture2D Normal1 : register(t10);
Texture2D Roughness1 : register(t11);
Texture2D Height1 : register(t12);

Texture2D Albedo2 : register(t13);
Texture2D Normal2 : register(t14);
Texture2D Roughness2 : register(t15);
Texture2D Height2 : register(t16);

Texture2D Albedo3 : register(t17);
Texture2D Normal3 : register(t18);
Texture2D Roughness3 : register(t19);
Texture2D Height3 : register(t20);

SamplerState MaterialSampler : register(s0);
SamplerState TileSampler : register(s1);

static const float PI = 3.14159265359;

struct VertexInput
{
    float3 Position : POSITION;
    float2 TileUv : TEXCOORD0;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float3 World : TEXCOORD0;
    float2 TileUv : TEXCOORD1;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;

    output.Position = mul(
        float4(input.Position, 1),
        ViewProjection);

    output.World = input.Position;
    output.TileUv = input.TileUv;

    return output;
}

float Hash(float2 position)
{
    return frac(
        sin(dot(position, float2(127.1, 311.7)))
        * 43758.5453);
}

float Noise(float2 position)
{
    float2 cell = floor(position);
    float2 fraction = frac(position);

    fraction = fraction * fraction *
        (3.0 - 2.0 * fraction);

    float a = Hash(cell);
    float b = Hash(cell + float2(1, 0));
    float c = Hash(cell + float2(0, 1));
    float d = Hash(cell + float2(1, 1));

    return lerp(
        lerp(a, b, fraction.x),
        lerp(c, d, fraction.x),
        fraction.y);
}

float BandWeight(
    float noiseValue,
    float index,
    float count,
    float transition)
{
    if (index >= count)
    {
        return 0.0;
    }

    float enter = 1.0;
    float leave = 0.0;

    if (index > 0.5)
    {
        float boundary = index / count;

        enter = smoothstep(
            boundary - transition,
            boundary + transition,
            noiseValue);
    }

    if (index < count - 1.5)
    {
        float boundary = (index + 1.0) / count;

        leave = smoothstep(
            boundary - transition,
            boundary + transition,
            noiseValue);
    }

    return saturate(enter - leave);
}

float4 CalculateBlendWeights(
    float3 world,
    float3 surface,
    float2 uv0,
    float2 uv1,
    float2 uv2,
    float2 uv3)
{
    float count = max(CoveringSettings.x, 1.0);

    if (count < 1.5)
    {
        return float4(1, 0, 0, 0);
    }

    float largeNoise = Noise(world.xz * 0.085);
    float detailNoise = Noise(world.xz * 0.29);

    float noiseValue = saturate(
        largeNoise * 0.78 +
        detailNoise * 0.22);

    float transition = min(
        0.1,
        0.4 / count);

    float4 bands = float4(
        BandWeight(noiseValue, 0, count, transition),
        BandWeight(noiseValue, 1, count, transition),
        BandWeight(noiseValue, 2, count, transition),
        BandWeight(noiseValue, 3, count, transition));

    float4 enabled = float4(
        1,
        count > 1.5 ? 1 : 0,
        count > 2.5 ? 1 : 0,
        count > 3.5 ? 1 : 0);

    float4 weights = lerp(
        enabled / count,
        bands,
        saturate(BlendProperties.x));

    if (BlendProperties.y > 0.001)
    {
        float4 heights = float4(0.5, 0.5, 0.5, 0.5);

        if (HeightFlags.x > 0.5)
        {
            heights.x = Height0.Sample(
                MaterialSampler, uv0).r;
        }

        if (count > 1.5 && HeightFlags.y > 0.5)
        {
            heights.y = Height1.Sample(
                MaterialSampler, uv1).r;
        }

        if (count > 2.5 && HeightFlags.z > 0.5)
        {
            heights.z = Height2.Sample(
                MaterialSampler, uv2).r;
        }

        if (count > 3.5 && HeightFlags.w > 0.5)
        {
            heights.w = Height3.Sample(
                MaterialSampler, uv3).r;
        }

        float meanHeight = dot(weights, heights);

        float4 heightGain = max(
            0.05,
            1.0 + (heights - meanHeight) *
            BlendProperties.y * 5.0);

        weights *= heightGain;
    }

    if (BlendProperties.z > 0.001)
    {
        float slope = saturate(
            (1.0 - saturate(surface.y)) * 2.3);

        float centeredSlope = slope - 0.4;

        float4 slopeGain = max(
            0.05,
            1.0 + CoveringSlope *
            centeredSlope *
            BlendProperties.z * 3.0);

        weights *= slopeGain;
    }

    weights *= enabled;

    float total = dot(
        weights,
        float4(1, 1, 1, 1));

    return weights / max(total, 0.00001);
}

float DistributionGGX(
    float noH,
    float roughness)
{
    float alpha = roughness * roughness;
    float alpha2 = alpha * alpha;

    float denominator =
        noH * noH * (alpha2 - 1.0) + 1.0;

    return alpha2 /
        max(PI * denominator * denominator, 0.00001);
}

float GeometrySchlickGGX(
    float noX,
    float roughness)
{
    float r = roughness + 1.0;
    float k = r * r / 8.0;

    return noX /
        max(noX * (1.0 - k) + k, 0.00001);
}

float GeometrySmith(
    float noV,
    float noL,
    float roughness)
{
    return GeometrySchlickGGX(noV, roughness) *
        GeometrySchlickGGX(noL, roughness);
}

float3 FresnelSchlick(
    float voH,
    float3 f0)
{
    return f0 +
        (1.0 - f0) *
        pow(1.0 - saturate(voH), 5.0);
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    if (WorldExtent.w > 0.5)
    {
        clip(
            0.5 -
            TerrainHoles.Load(
                int3(
                    int2(input.World.xz / WorldExtent.z),
                    0)).r);
    }

    float2 tileUv = (
        4.0 + saturate(input.TileUv) * 248.0
    ) / 256.0;

    float sum = WeightSum.Sample(
        TileSampler,
        tileUv).r;

    float3 mask = LayerMask.Sample(
        TileSampler,
        tileUv).rgb;

    float channel = LayerPassParameters.y < 0.5
        ? mask.r
        : LayerPassParameters.y < 1.5
            ? mask.g
            : mask.b;

    float layerWeight = LayerPassParameters.x > 0.5
        ? max(1.0 - sum, 0.0)
        : channel / max(sum, 1.0);

    float3 surface = normalize(
        SurfaceNormal.Sample(
            TileSampler,
            tileUv).rgb * 2.0 - 1.0);

    float2 uv0 = input.World.xz
        / WorldExtent.xy
        * Layer.xy;

    float2 uv1 = input.World.xz * CoveringMeters.y;
    float2 uv2 = input.World.xz * CoveringMeters.z;
    float2 uv3 = input.World.xz * CoveringMeters.w;

    float4 weights = CalculateBlendWeights(
        input.World,
        surface,
        uv0,
        uv1,
        uv2,
        uv3);

    float3 albedo = Albedo0.Sample(
        MaterialSampler, uv0).rgb * weights.x;

    float3 detail = (
        Normal0.Sample(
            MaterialSampler,
            uv0 * Layer.z).rgb * 2.0 - 1.0) * weights.x;

    float roughnessValue = weights.x;

    if (RoughnessFlags.x > 0.5)
    {
        roughnessValue = Roughness0.Sample(
            MaterialSampler, uv0).r * weights.x;
    }

    if (CoveringSettings.x > 1.5)
    {
        albedo += Albedo1.Sample(
            MaterialSampler, uv1).rgb * weights.y;

        detail += (
            Normal1.Sample(
                MaterialSampler,
                uv1 * Layer.z).rgb * 2.0 - 1.0) * weights.y;

        roughnessValue += (
            RoughnessFlags.y > 0.5
                ? Roughness1.Sample(MaterialSampler, uv1).r
                : 1.0) * weights.y;
    }

    if (CoveringSettings.x > 2.5)
    {
        albedo += Albedo2.Sample(
            MaterialSampler, uv2).rgb * weights.z;

        detail += (
            Normal2.Sample(
                MaterialSampler,
                uv2 * Layer.z).rgb * 2.0 - 1.0) * weights.z;

        roughnessValue += (
            RoughnessFlags.z > 0.5
                ? Roughness2.Sample(MaterialSampler, uv2).r
                : 1.0) * weights.z;
    }

    if (CoveringSettings.x > 3.5)
    {
        albedo += Albedo3.Sample(
            MaterialSampler, uv3).rgb * weights.w;

        detail += (
            Normal3.Sample(
                MaterialSampler,
                uv3 * Layer.z).rgb * 2.0 - 1.0) * weights.w;

        roughnessValue += (
            RoughnessFlags.w > 0.5
                ? Roughness3.Sample(MaterialSampler, uv3).r
                : 1.0) * weights.w;
    }

    float macroScale = max(
        CoveringSettings.y,
        1.0);

    float macroNoise = Noise(
        input.World.xz / macroScale);

    float macroFactor = lerp(
        1.0,
        0.82 + macroNoise * 0.36,
        saturate(BlendProperties.w));

    albedo *= macroFactor;

    detail = normalize(
        detail + float3(0, 0, 0.00001));

    detail.z = max(detail.z, 0.08);

    float3 reference = abs(surface.z) < 0.95
        ? float3(0, 0, 1)
        : float3(0, 1, 0);

    float3 tangent = normalize(
        cross(surface, reference));

    float3 bitangent = normalize(
        cross(tangent, surface));

    float3 normal = normalize(
        tangent * detail.x +
        bitangent * detail.y +
        surface * detail.z);

    float roughness = clamp(
        saturate(PbrProperties.x) * roughnessValue,
        0.045,
        1.0);

    float metallic = saturate(PbrProperties.y);
    float specularLevel = saturate(PbrProperties.z);

    float3 view = normalize(
        Camera.xyz - input.World);

    float3 light = normalize(
        EnvironmentSun.xyz);

    float3 halfVector = normalize(
        view + light + float3(0, 0.00001, 0));

    float noV = max(dot(normal, view), 0.001);
    float noL = saturate(dot(normal, light));
    float noH = saturate(dot(normal, halfVector));
    float voH = saturate(dot(view, halfVector));

    float3 dielectricF0 = float3(
        0.08 * specularLevel,
        0.08 * specularLevel,
        0.08 * specularLevel);

    float3 f0 = lerp(
        dielectricF0,
        saturate(albedo),
        metallic);

    float3 fresnel = FresnelSchlick(voH, f0);

    float distribution = DistributionGGX(
        noH,
        roughness);

    float geometry = GeometrySmith(
        noV,
        noL,
        roughness);

    float3 specular = (
        distribution * geometry * fresnel
    ) / max(4.0 * noV * noL, 0.001);

    float3 diffuseLighting = ShadeEnvironment(
        albedo * (1.0 - metallic),
        normal,
        input.World);

    float visibility = ShadowVisibility(
        input.World);

    float3 directSpecular =
        specular *
        noL *
        EnvironmentSun.w *
        visibility *
        0.35;

    float3 finalColor =
        diffuseLighting + directSpecular;

    return float4(
        finalColor * layerWeight,
        LayerPassParameters.x > 0.5 ? 1.0 : 0.0);
}