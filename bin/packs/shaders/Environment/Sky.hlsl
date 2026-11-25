#include "Environment/SurfaceOutput.hlsli"

cbuffer SkyConstants : register(b0)
{
    float4 CameraRight;
    float4 CameraUp;
    float4 CameraForward;
    float4 Sun;
    float4 Viewport;
    float4 Cloud;
    float4 Weather;
    float4 TextureFlags;
    float4 Moon;
    float4 MoonSettings;
    float4 Jitter;
};

Texture2D Cumulus : register(t0);
Texture2D Cirrus : register(t1);
Texture2D StormCloud : register(t2);

SamplerState CloudSampler : register(s0);

static const float PI = 3.14159265359f;

struct PixelInput
{
    float4 Position : SV_POSITION;
};

PixelInput VSMain(uint vertexId : SV_VertexID)
{
    PixelInput output;

    float2 position;

    if (vertexId == 0)
    {
        position = float2(-1, -1);
    }
    else if (vertexId == 1)
    {
        position = float2(-1, 3);
    }
    else
    {
        position = float2(3, -1);
    }

    output.Position = float4(position, 1, 1);

    return output;
}

float Hash(float2 position)
{
    return frac(
        sin(dot(
            position,
            float2(127.1f, 311.7f))) *
        43758.5453f);
}

float Noise(float2 position)
{
    float2 cell = floor(position);
    float2 fraction = frac(position);

    fraction = fraction * fraction *
        (3.0f - 2.0f * fraction);

    float a = Hash(cell);
    float b = Hash(cell + float2(1, 0));
    float c = Hash(cell + float2(0, 1));
    float d = Hash(cell + float2(1, 1));

    return lerp(
        lerp(a, b, fraction.x),
        lerp(c, d, fraction.x),
        fraction.y);
}

float3 MoonDisc(float3 direction, float cloudOpacity)
{
    float night = 1.0f - Sun.w;

    if (night < 0.001f || Moon.y <= 0.0f)
    {
        return 0.0f;
    }

    float3 moonDirection = normalize(Moon.xyz);

    float facing = dot(
        direction,
        moonDirection);

    if (facing <= 0.0f)
    {
        return 0.0f;
    }

    float3 reference =
        abs(moonDirection.y) < 0.95f
            ? float3(0, 1, 0)
            : float3(1, 0, 0);

    float3 moonRight = normalize(
        cross(reference, moonDirection));

    float3 moonUp = normalize(
        cross(moonDirection, moonRight));

    const float angularRadius = 0.00465f;

    float2 disc = float2(
        dot(direction, moonRight),
        dot(direction, moonUp));

    disc /= max(facing * angularRadius, 0.000001f);

    float radius = length(disc);

    if (radius > 1.1f)
    {
        return 0.0f;
    }

    float edge = 1.0f - smoothstep(
        0.94f,
        1.04f,
        radius);

    float sphereZ = sqrt(saturate(
        1.0f - dot(disc, disc)));

    float3 surfaceNormal = normalize(
        float3(disc, sphereZ));

    float phaseAngle =
        (MoonSettings.x - 4.0f) * PI / 4.0f;

    float3 illuminationDirection = float3(
        sin(phaseAngle),
        0.0f,
        cos(phaseAngle));

    float illuminated = dot(
        surfaceNormal,
        illuminationDirection);

    float phaseMask = smoothstep(
        -0.045f,
        0.045f,
        illuminated);

    float visibility =
        (1.0f - cloudOpacity * 0.95f) *
        night *
        MoonSettings.y;

    float lunarSurface = saturate(
        0.72f + illuminated * 0.28f);

    float3 moonColor =
        float3(0.62f, 0.70f, 0.82f) *
        lunarSurface;

    return moonColor *
        phaseMask *
        edge *
        visibility;
}

SurfaceOutput PSMain(PixelInput input)
{
    float2 pixel = (
		input.Position.xy - Jitter.xy
	) / Viewport.xy;

	float2 ndc = pixel * 2.0f - 1.0f;

    float3 direction = normalize(
        CameraForward.xyz +
        CameraRight.xyz * ndc.x * Viewport.z -
        CameraUp.xyz * ndc.y * Viewport.w);

    float daylight = saturate(Sun.w);
    float altitude = direction.y;

    float horizon = saturate(
        altitude * 3.0f + 0.2f);

    // Clear sky has a subdued blue tone.
    float3 clearZenith =
        float3(0.10f, 0.20f, 0.32f);

    float3 clearHorizon =
        float3(0.39f, 0.47f, 0.52f);

    // Overcast sky is brighter than the ground,
    // but has little blue saturation.
    float3 overcastZenith =
        float3(0.38f, 0.41f, 0.43f);

    float3 overcastHorizon =
        float3(0.43f, 0.45f, 0.46f);

    float overcast = saturate(
        Cloud.x * 0.7f +
        Cloud.w * 0.3f);

    float3 dayZenith = lerp(
        clearZenith,
        overcastZenith,
        overcast);

    float3 dayHorizon = lerp(
        clearHorizon,
        overcastHorizon,
        overcast);

    float nightBrightness =
        0.5f + MoonSettings.y * 0.65f;

    float3 nightZenith =
        float3(0.003f, 0.006f, 0.012f) *
        nightBrightness;

    float3 nightHorizon =
        float3(0.012f, 0.017f, 0.025f) *
        nightBrightness;

    float3 zenith = lerp(
        nightZenith,
        dayZenith,
        daylight);

    float3 horizonColor = lerp(
        nightHorizon,
        dayHorizon,
        daylight);

    float sunset = saturate(
        (1.0f - abs(Sun.y) * 5.0f) *
        daylight);

    horizonColor = lerp(
        horizonColor,
        float3(0.57f, 0.31f, 0.19f),
        sunset * (1.0f - overcast) * 0.45f);

    float3 sky = lerp(
        horizonColor,
        zenith,
        horizon);

    float2 wind = float2(
        cos(Weather.x),
        sin(Weather.x));

    float2 cloudUv = direction.xz /
        max(direction.y + 0.28f, 0.12f);

    cloudUv *= 0.045f;

    cloudUv += wind *
        Cloud.z *
        Cloud.y *
        0.0015f;

    float cumulus = Noise(
        cloudUv * 9.0f);

    if (TextureFlags.x > 0.5f)
    {
        float3 sampleColor = Cumulus.Sample(
            CloudSampler,
            cloudUv).rgb;

        cumulus = lerp(
            cumulus,
            dot(sampleColor,
                float3(0.299f, 0.587f, 0.114f)),
            0.7f);
    }

    float cirrus = Noise(
        cloudUv * 16.0f + 11.0f);

    if (TextureFlags.y > 0.5f)
    {
        float3 sampleColor = Cirrus.Sample(
            CloudSampler,
            cloudUv * 0.75f + 0.2f).rgb;

        cirrus = lerp(
            cirrus,
            dot(sampleColor,
                float3(0.299f, 0.587f, 0.114f)),
            0.65f);
    }

    float storm = cumulus;

    if (TextureFlags.z > 0.5f)
    {
        float3 sampleColor = StormCloud.Sample(
            CloudSampler,
            cloudUv * 0.8f).rgb;

        storm = dot(
            sampleColor,
            float3(0.299f, 0.587f, 0.114f));
    }

    float cloudNoise = lerp(
        cumulus * 0.8f + cirrus * 0.2f,
        storm,
        Cloud.w);

    float threshold = lerp(
        0.9f,
        0.25f,
        Cloud.x);

    float cloudAlpha = smoothstep(
        threshold - 0.12f,
        threshold + 0.12f,
        cloudNoise);

    cloudAlpha *= smoothstep(
        -0.06f,
        0.23f,
        altitude);

    float3 dayCloud = lerp(
        float3(0.62f, 0.65f, 0.67f),
        float3(0.29f, 0.31f, 0.32f),
        Cloud.w);

    float3 nightCloud =
        float3(0.014f, 0.020f, 0.029f) *
        nightBrightness;

    float3 cloudColor = lerp(
        nightCloud,
        dayCloud,
        daylight);

    float cloudOpacity = saturate(
        cloudAlpha * Cloud.x);

    sky = lerp(
        sky,
        cloudColor,
        cloudOpacity);

    float sunAmount = dot(
        direction,
        Sun.xyz);

    float sunDisc = smoothstep(
        0.9995f,
        0.99992f,
        sunAmount);

    float sunGlow = pow(
        saturate(sunAmount),
        128.0f);

    float sunlightVisibility =
        daylight *
        saturate(Weather.z) *
        (1.0f - cloudOpacity * 0.97f);

    sky += float3(1.0f, 0.78f, 0.54f) *
        (sunDisc * 1.65f +
        sunGlow * 0.16f) *
        sunlightVisibility;

    sky += MoonDisc(
        direction,
        cloudOpacity);

    float lightning = saturate(
        TextureFlags.w);

    sky += float3(
        0.64f,
        0.75f,
        0.95f) *
        lightning * 0.85f;

    return MakeSurfaceOutput(
        float4(saturate(sky), 1.0f),
        float3(0.0f, 0.0f, 0.0f));
}