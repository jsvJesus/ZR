cbuffer WaterBuffer : register(b0)
{
    row_major float4x4 ViewProjection;
    float4 Lake;
    float4 Shape;
    float4 CameraTime;
    float4 Sun;
};

struct VertexInput
{
    float2 Local : POSITION;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float3 World : TEXCOORD0;
    float2 Local : TEXCOORD1;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;

    float3 world = float3(
        Lake.x + input.Local.x * Shape.x,
        Lake.z,
        Lake.y + input.Local.y * Shape.y);

    output.Position = mul(
        float4(world, 1.0),
        ViewProjection);

    output.World = world;
    output.Local = input.Local;

    return output;
}

float Hash(float2 p)
{
    return frac(
        sin(dot(p, float2(127.1, 311.7)))
        * 43758.5453);
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
    float ellipse = dot(input.Local, input.Local);

    clip(1.0 - ellipse);

    float time = CameraTime.w;
    float2 position = input.World.xz;

    float waveA = sin(
        position.x * 1.65 +
        position.y * 0.72 +
        time * 1.1);

    float waveB = cos(
        position.y * 1.3 -
        position.x * 0.45 -
        time * 0.8);

    float3 normal = normalize(
        float3(
            waveA * 0.045,
            1.0,
            waveB * 0.045));

    float3 view = normalize(
        CameraTime.xyz - input.World);

    float3 light = normalize(
        Sun.xyz + float3(0, 0.0001, 0));

    float fresnel = 0.02 +
        0.98 * pow(
            1.0 - saturate(dot(normal, view)),
            5.0);

    float3 reflected = reflect(
        -view,
        normal);

    float skyFactor = saturate(
        reflected.y * 0.5 + 0.5);

    float daylight = saturate(Sun.w);

    float3 darkSky = float3(
        0.025,
        0.035,
        0.045);

    float3 lightSky = float3(
        0.25,
        0.34,
        0.39);

    float3 sky = lerp(
        darkSky,
        lightSky,
        daylight * skyFactor);

    float3 waterColor = float3(
        0.035,
        0.105,
        0.115);

    float smallVariation = Noise(
        position * 0.13 +
        float2(time * 0.035, 0));

    waterColor *= lerp(
        0.87,
        1.12,
        smallVariation);

    float3 color = lerp(
        waterColor,
        sky,
        saturate(0.28 + fresnel * 0.72));

    float3 halfVector = normalize(
        view + light + float3(0, 0.0001, 0));

    float highlight = pow(
        saturate(dot(normal, halfVector)),
        180.0);

    highlight *= saturate(Sun.w) * 0.85;

    color += highlight *
        float3(0.85, 0.92, 1.0);

    float alpha = saturate(
        0.76 + fresnel * 0.18);

    return float4(color, alpha);
}