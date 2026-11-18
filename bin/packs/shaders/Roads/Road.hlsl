#include "Environment/Lighting.hlsli"

cbuffer RoadConstants : register(b0)
{
    row_major float4x4 ViewProjection;
    // Selected road, pavement width, lane count, editor marker mode.
    float4 Parameters;
    float4 CameraPosition;
    float4 WeatherParameters;
    float4 Marker;
    // Available diffuse/normal/specular maps, then repeat length in metres.
    float4 MaterialParameters;
};

Texture2D RoadDiffuse : register(t0);
Texture2D RoadNormal : register(t1);
Texture2D RoadSpecular : register(t2);
SamplerState RoadSampler : register(s0);

struct VertexInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 Uv : TEXCOORD0;
    float Surface : TEXCOORD1;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float3 WorldPosition : TEXCOORD1;
    float3 Normal : NORMAL;
    float2 Uv : TEXCOORD0;
    nointerpolation float Surface : TEXCOORD2;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;
    float3 world = Parameters.w > 0.5 ? input.Position * Marker.w + Marker.xyz : input.Position;
    output.Position = mul(float4(world, 1), ViewProjection);
    output.WorldPosition = world;
    output.Normal = input.Normal;
    output.Uv = input.Uv;
    output.Surface = input.Surface;
    return output;
}

float RoadHash(float2 position)
{
    return frac(sin(dot(position, float2(127.1, 311.7))) * 43758.5453);
}

float RoadNoise(float2 position)
{
    float2 cell = floor(position);
    float2 fraction = frac(position);
    fraction = fraction * fraction * (3.0 - 2.0 * fraction);
    return lerp(lerp(RoadHash(cell), RoadHash(cell + float2(1, 0)), fraction.x),
        lerp(RoadHash(cell + float2(0, 1)), RoadHash(cell + float2(1, 1)), fraction.x), fraction.y);
}

float RoadWetness(float2 position)
{
    float wetness = saturate(WeatherParameters.x);
    if (WeatherParameters.y > 0.5) return wetness;
    float scale = max(WeatherParameters.z, 1.0);
    float pattern = RoadNoise(position / scale) * 0.75 +
        RoadNoise(position / (scale * 0.35) + float2(17.3, 9.8)) * 0.25;
    return wetness * lerp(1.0, smoothstep(0.28, 0.72, pattern), saturate(WeatherParameters.w));
}

float Stripe(float distance, float halfWidth, float filterWidth)
{
    return 1.0 - smoothstep(halfWidth - filterWidth, halfWidth + filterWidth, abs(distance));
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    if (Parameters.w > 0.5)
    {
        float3 markerColor = Parameters.w > 1.5 ? float3(1.0, 0.35, 0.08) : float3(0.15, 0.85, 1.0);
        return float4(markerColor * (0.65 + 0.35 * abs(normalize(input.Normal).y)), 1);
    }

    float3 normal = normalize(input.Normal);
    float2 uv = input.Uv; // X = cumulative distance, Y = signed lateral metres.
    float detail = RoadNoise(uv * 18.0);
    float broad = RoadNoise(input.WorldPosition.xz * 0.35);
    float detailFade = 1.0 - saturate(max(length(ddx(uv)), length(ddy(uv))) * 12.0);
    detail = lerp(0.5, detail, detailFade);
    float3 albedo;
    float specular = 0.025;
    float gloss = 0.13;
    float wetResponse = 1;
    float markings = 0;
    float2 materialUv = uv / max(MaterialParameters.w, 0.1);
    float2 materialDx = ddx(materialUv), materialDy = ddy(materialUv);
    float3 worldDx = ddx(input.WorldPosition), worldDy = ddy(input.WorldPosition);

    if (input.Surface < 0.5)
    {
        albedo = float3(0.115, 0.125, 0.135) * (0.78 + detail * 0.38 + broad * 0.12);
        float width = max(Parameters.y, 0.2);
        float halfWidth = width * 0.5;
        float aa = max(fwidth(uv.y), 0.012);
        float edgeInset = min(0.24, width * 0.12);
        markings = Stripe(abs(uv.y) - (halfWidth - edgeInset), 0.055, aa);
        float dashDistance = abs(frac(uv.x / 8.0) - 0.25) * 8.0;
        float dash = 1.0 - smoothstep(1.9, 2.1 + fwidth(uv.x), dashDistance);
        [unroll]
        for (int lane = 1; lane < 8; lane++)
        {
            if (lane < Parameters.z)
            {
                float lateral = -halfWidth + width * lane / max(Parameters.z, 1.0);
                markings = max(markings, Stripe(uv.y - lateral, 0.055, aa) * dash);
            }
        }
        // Thickness walls retain asphalt, without painted stripes wrapping down them.
        markings *= smoothstep(0.4, 0.8, normal.y);
    }
    else if (input.Surface < 1.5)
    {
        albedo = lerp(float3(0.24, 0.22, 0.18), float3(0.43, 0.41, 0.35), detail) * (0.85 + broad * 0.2);
        wetResponse = 0.65;
    }
    else if (input.Surface < 2.5)
    {
        float ruts = 1.0 - smoothstep(0.05, 0.28, abs(abs(uv.y) - Parameters.y * 0.23));
        albedo = lerp(float3(0.26, 0.19, 0.115), float3(0.40, 0.31, 0.19), broad) *
            (0.85 + detail * 0.25) * (1.0 - ruts * 0.1);
        wetResponse = 0.6;
    }
    else if (input.Surface < 3.5)
    {
        float stones = RoadNoise(uv * 10);
        stones = lerp(0.5, stones, saturate(detailFade * 2));
        albedo = lerp(float3(0.20, 0.21, 0.22), float3(0.45, 0.43, 0.40), stones);
        wetResponse = 0.5;
    }
    else if (input.Surface < 4.5)
    {
        float grain = RoadNoise(float2(uv.x * 50, uv.y * 2));
        albedo = lerp(float3(0.095, 0.065, 0.038), float3(0.24, 0.17, 0.10), grain);
        specular = 0.045; gloss = 0.2; wetResponse = 0.65;
    }
    else if (input.Surface < 5.5)
    {
        albedo = lerp(float3(0.20, 0.13, 0.09), float3(0.34, 0.29, 0.25), broad);
        specular = 0.25; gloss = 0.5; wetResponse = 0.4;
    }
    else
    {
        albedo = float3(0.56, 0.59, 0.61) * (0.94 + detail * 0.12);
        specular = 0.75; gloss = 0.85; wetResponse = 0.3;
    }

    bool surfaceTop = (input.Surface < 0.5 || (input.Surface > 1.5 && input.Surface < 2.5)) &&
        normal.y > 0.4 && abs(uv.y) <= Parameters.y * 0.5 + 0.001;
    if (surfaceTop)
    {
        wetResponse *= max(CameraPosition.w, 0.0);
        if (MaterialParameters.x > 0.5)
        {
            // Source alpha belongs to authored decals/atlases; the physical road remains opaque.
            albedo = RoadDiffuse.SampleGrad(RoadSampler, materialUv, materialDx, materialDy).rgb;
            if (MaterialParameters.z > 0.5)
                specular = lerp(0.02, 0.55, RoadSpecular.SampleGrad(RoadSampler, materialUv, materialDx, materialDy).r);
            float determinant = materialDx.x * materialDy.y - materialDx.y * materialDy.x;
            if (MaterialParameters.y > 0.5 && abs(determinant) > 0.0000001)
            {
                float3 tangent = (worldDx * materialDy.y - worldDy * materialDx.y) / determinant;
                float3 sourceBitangent = (worldDy * materialDx.x - worldDx * materialDy.x) / determinant;
                tangent -= normal * dot(normal, tangent);
                if (dot(tangent, tangent) > 0.000001)
                {
                    tangent = normalize(tangent);
                    float3 bitangent = normalize(cross(normal, tangent));
                    bitangent *= dot(bitangent, sourceBitangent) < 0 ? -1 : 1;
                    float3 mapped = RoadNormal.SampleGrad(RoadSampler, materialUv, materialDx, materialDy).xyz * 2 - 1;
                    float3 transformed = tangent * mapped.x + bitangent * mapped.y + normal * mapped.z;
                    if (dot(transformed, transformed) > 0.000001) normal = normalize(transformed);
                }
            }
        }
    }
    albedo = lerp(albedo, float3(0.85, 0.84, 0.73), saturate(markings));
    float wetness = saturate(RoadWetness(input.WorldPosition.xz) * wetResponse);
    albedo *= lerp(1.0, 0.64, wetness);
    float3 color = ShadeEnvironment(albedo, normal, input.WorldPosition);
    float3 view = CameraPosition.xyz - input.WorldPosition;
    view *= rsqrt(max(dot(view, view), 0.000001));
    float3 halfway = EnvironmentSun.xyz + view;
    halfway *= rsqrt(max(dot(halfway, halfway), 0.000001));
    float highlight = pow(saturate(dot(normal, halfway)), lerp(8.0, 256.0, lerp(gloss, 0.93, wetness)));
    float fogVisibility = exp(-max(EnvironmentFog.w, 0) * length(CameraPosition.xyz - input.WorldPosition));
    color += EnvironmentSunColor.rgb * (specular + wetness * 0.55) * highlight *
        saturate(dot(normal, EnvironmentSun.xyz)) * ShadowVisibility(input.WorldPosition) *
        EnvironmentSun.w * fogVisibility;
    color = lerp(color, float3(1, 0.65, 0.12), Parameters.x * 0.22);
    return float4(color, 1);
}
