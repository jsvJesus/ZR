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
};

Texture2D Cumulus : register(t0);
Texture2D Cirrus : register(t1);
Texture2D StormCloud : register(t2);
SamplerState CloudSampler : register(s0);

struct PixelInput
{
    float4 Position : SV_POSITION;
};

PixelInput VSMain(uint vertexId : SV_VertexID)
{
    PixelInput output;

    float2 position;

    if (vertexId == 0)
        position = float2(-1, -1);
    else if (vertexId == 1)
        position = float2(-1, 3);
    else
        position = float2(3, -1);

    output.Position = float4(position, 1, 1);

    return output;
}

float Hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float a = Hash(i);
    float b = Hash(i + float2(1, 0));
    float c = Hash(i + float2(0, 1));
    float d = Hash(i + float2(1, 1));

    return lerp(
        lerp(a, b, f.x),
        lerp(c, d, f.x),
        f.y);
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    float2 pixel = input.Position.xy / Viewport.xy;
    float2 ndc = pixel * 2.0 - 1.0;

    float3 direction = normalize(
        CameraForward.xyz
        + CameraRight.xyz * ndc.x * Viewport.z
        - CameraUp.xyz * ndc.y * Viewport.w);

    float daylight = Sun.w;
    float altitude = direction.y;
    float horizon = saturate(altitude * 4.0 + 0.3);

    float3 nightZenith = float3(0.006, 0.011, 0.027);
    float3 nightHorizon = float3(0.018, 0.026, 0.049);

    float3 dayZenith = float3(0.095, 0.255, 0.48);
    float3 dayHorizon = float3(0.54, 0.67, 0.76);

    float3 zenith = lerp(nightZenith, dayZenith, daylight);
    float3 horizonColor = lerp(
        nightHorizon,
        dayHorizon,
        daylight);

    float sunset = saturate(
        (1.0 - abs(Sun.y) * 6.0) * daylight);

    horizonColor = lerp(
        horizonColor,
        float3(0.82, 0.37, 0.14),
        sunset * 0.48);

    float3 sky = lerp(
        horizonColor,
        zenith,
        horizon);

    float2 wind = float2(
        cos(Weather.x),
        sin(Weather.x));

    float2 cloudUv = direction.xz
        / max(direction.y + 0.28, 0.12);

    cloudUv *= 0.045;
    cloudUv += wind * Cloud.z * Cloud.y * 0.0015;

    float cumulus = Noise(cloudUv * 9.0);

    if (TextureFlags.x > 0.5)
    {
        float4 sampleColor = Cumulus.Sample(
            CloudSampler,
            cloudUv);

        cumulus = lerp(
            cumulus,
            dot(sampleColor.rgb, float3(0.299, 0.587, 0.114)),
            0.7);
    }

    float cirrus = Noise(cloudUv * 16.0 + 11.0);

    if (TextureFlags.y > 0.5)
    {
        float4 sampleColor = Cirrus.Sample(
            CloudSampler,
            cloudUv * 0.75 + 0.2);

        cirrus = lerp(
            cirrus,
            dot(sampleColor.rgb, float3(0.299, 0.587, 0.114)),
            0.65);
    }

    float storm = cumulus;

    if (TextureFlags.z > 0.5)
    {
        float4 sampleColor = StormCloud.Sample(
            CloudSampler,
            cloudUv * 0.8);

        storm = dot(
            sampleColor.rgb,
            float3(0.299, 0.587, 0.114));
    }

    float cloudNoise = lerp(
        cumulus * 0.8 + cirrus * 0.2,
        storm,
        Cloud.w);

    float threshold = lerp(0.9, 0.25, Cloud.x);

    float cloudAlpha = smoothstep(
        threshold - 0.12,
        threshold + 0.12,
        cloudNoise);

    cloudAlpha *= smoothstep(
        -0.06,
        0.23,
        altitude);

    float3 cloudColor = lerp(
        float3(0.08, 0.11, 0.16),
        float3(0.92, 0.93, 0.94),
        daylight);

    cloudColor = lerp(
        cloudColor,
        float3(0.26, 0.30, 0.34),
        Cloud.w * 0.78);

    sky = lerp(
        sky,
        cloudColor,
        cloudAlpha * Cloud.x);

    float sunAmount = dot(direction, Sun.xyz);

    float sunDisc = smoothstep(
        0.9995,
        0.99992,
        sunAmount);

    float sunGlow = pow(
        saturate(sunAmount),
        128.0);

    sky += float3(1.0, 0.73, 0.43)
        * (sunDisc * 2.0 + sunGlow * 0.18)
        * daylight
        * Weather.z
        * (1.0 - cloudAlpha * 0.95);
		
	float lightning = saturate(TextureFlags.w);

	sky += float3(
		0.72,
		0.82,
		1.0) * lightning * 0.85;

    return float4(saturate(sky), 1.0);
}