cbuffer FrameConstants : register(b0)
{
    row_major float4x4 InverseViewProjection;
    row_major float4x4 PreviousViewProjection;
    row_major float4x4 CurrentViewProjection;

    float4 Parameters;
    float4 Camera;
    float4 Sun;
	float4 GradeSettings;
	// x = Bloom Intensity
	// y = LUT Strength
	// z = Sky Dither Strength
	// w = Bloom Enabled
	float4 AoSettings;
};

cbuffer EnvironmentShadow : register(b2)
{
    row_major float4x4 SunShadowMatrices[4];
    float4 SunShadowParams;
    float4 SunShadowSplits;
    float4 SunShadowCameraForward;
};

Texture2D<float> DepthTexture : register(t0);
Texture2D<float4> HdrTexture : register(t0);

Texture2D<float> ContactDepth : register(t1);
Texture2D<float4> ContactNormal : register(t2);
Texture2D<float4> BloomTexture : register(t3);
Texture2D<float4> ColorLutTexture : register(t4);
Texture2D<float2> AmbientOcclusionTexture : register(t5);

SamplerState PostLinearSampler : register(s0);

Texture2DArray<float> SunShadowTexture : register(t7);
SamplerComparisonState SunShadowSampler : register(s7);

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

float2 PixelUv(int2 coordinate)
{
    return (
        float2(coordinate) + 0.5f
    ) / Parameters.xy;
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
        abs(rightDepth - depth) <= abs(leftDepth - depth)
            ? rightWorld - world
            : world - leftWorld;

    float3 tangentY =
        abs(downDepth - depth) <= abs(upDepth - depth)
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

    output.Motion = velocity;

    return output;
}

float SunMapVisibility(
    float3 worldPosition)
{
    if (SunShadowParams.x < 0.5f)
    {
        return 0.0f;
    }

    float viewDepth = dot(
        worldPosition - Camera.xyz,
        SunShadowCameraForward.xyz);

    if (viewDepth < 0.0f ||
        viewDepth > SunShadowSplits.w)
    {
        return 0.0f;
    }

    int cascade = 0;

    if (viewDepth > SunShadowSplits.x)
        cascade = 1;

    if (viewDepth > SunShadowSplits.y)
        cascade = 2;

    if (viewDepth > SunShadowSplits.z)
        cascade = 3;

    float4 lightPosition = mul(
        float4(worldPosition, 1.0f),
        SunShadowMatrices[cascade]);

    if (lightPosition.w <= 0.0f)
    {
        return 0.0f;
    }

    float3 projected =
        lightPosition.xyz / lightPosition.w;

    float2 uv = float2(
        projected.x * 0.5f + 0.5f,
        0.5f - projected.y * 0.5f);

    if (any(uv < 0.0f) ||
        any(uv > 1.0f) ||
        projected.z <= 0.0f ||
        projected.z >= 1.0f)
    {
        return 0.0f;
    }

    return SunShadowTexture.SampleCmpLevelZero(
        SunShadowSampler,
        float3(uv, (float)cascade),
        projected.z -
            SunShadowParams.w * (1.0f + cascade * 0.35f));
}

float ScreenContactShadow(
    float2 uv,
    float depth,
    float3 normal)
{
    if (Sun.w < 0.5f ||
        Sun.y <= 0.04f ||
        depth >= 0.999999f)
    {
        return 0.0f;
    }

    float3 direction = normalize(Sun.xyz);

    float sunFacing = saturate(
        dot(normal, direction));

    if (sunFacing < 0.1f)
    {
        return 0.0f;
    }

    float3 world = ReconstructPosition(
        uv,
        depth);

    float sunVisibility = SunMapVisibility(world);

    if (sunVisibility < 0.8f)
    {
        return 0.0f;
    }

    const int stepCount = 12;
    const float startDistance = 0.06f;
    const float maximumDistance = 0.75f;

    [loop]
    for (int index = 1; index <= stepCount; index++)
    {
        float fraction =
            (float)index / stepCount;

        float distance = lerp(
            startDistance,
            maximumDistance,
            fraction);

        float3 rayPosition =
            world + direction * distance;

        float4 clip = mul(
            float4(rayPosition, 1.0f),
            CurrentViewProjection);

        if (clip.w <= 0.0f)
        {
            break;
        }

        float3 projected =
            clip.xyz / clip.w;

        float2 sampleUv = float2(
            projected.x * 0.5f + 0.5f,
            0.5f - projected.y * 0.5f);

        if (any(sampleUv < 0.0f) ||
            any(sampleUv > 1.0f) ||
            projected.z <= 0.0f ||
            projected.z >= 1.0f)
        {
            break;
        }

        int2 pixel = clamp(
            int2(sampleUv * Parameters.xy),
            int2(0, 0),
            int2(Parameters.xy) - 1);

        float sceneDepth = ContactDepth.Load(
            int3(pixel, 0));

        if (sceneDepth >= 0.999999f ||
            sceneDepth + 0.00001f >= projected.z)
        {
            continue;
        }

        float3 sceneWorld = ReconstructPosition(
            PixelUv(pixel),
            sceneDepth);

        float3 viewDirection = normalize(
            rayPosition - Camera.xyz);

        float separation = dot(
            rayPosition - sceneWorld,
            viewDirection);

        float thickness = lerp(
            0.07f,
            0.18f,
            fraction);

        if (separation > 0.02f &&
            separation < thickness)
        {
            return sunFacing *
                sunVisibility *
                (1.0f - fraction * 0.5f);
        }
    }

    return 0.0f;
}

float3 ApplyToneMapping(float3 color)
{
    color = max(color, 0.0f);

    if (Parameters.w < 0.5f)
    {
        return saturate(color);
    }

    color *= Parameters.z;

    float3 numerator =
        color * (2.51f * color + 0.03f);

    float3 denominator =
        color * (2.43f * color + 0.59f)
        + 0.14f;

    return saturate(
        numerator / denominator);
}

float3 ApplyColorLut(float3 color)
{
    if (GradeSettings.y <= 0.0001f)
    {
        return color;
    }

    const float size = 32.0f;
    const float atlasWidth = 1024.0f;

    float3 coordinate =
        saturate(color) * (size - 1.0f);

    float blue0 = floor(coordinate.b);
    float blue1 = min(blue0 + 1.0f, size - 1.0f);

    float2 uv0 = float2(
        (blue0 * size + coordinate.r + 0.5f) /
            atlasWidth,
        (coordinate.g + 0.5f) / size);

    float2 uv1 = float2(
        (blue1 * size + coordinate.r + 0.5f) /
            atlasWidth,
        (coordinate.g + 0.5f) / size);

    float3 sample0 = ColorLutTexture.SampleLevel(
        PostLinearSampler,
        uv0,
        0.0f).rgb;

    float3 sample1 = ColorLutTexture.SampleLevel(
        PostLinearSampler,
        uv1,
        0.0f).rgb;

    float3 graded = lerp(
        sample0,
        sample1,
        frac(coordinate.b));

    return lerp(
        color,
        graded,
        saturate(GradeSettings.y));
}

float3 LinearToSrgb(float3 color)
{
    color = saturate(color);

    float3 lower = color * 12.92f;

    float3 upper =
        1.055f * pow(color, 1.0f / 2.4f)
        - 0.055f;

    return lerp(
        lower,
        upper,
        step(0.0031308f, color));
}

float InterleavedGradientNoise(float2 pixel)
{
    return frac(
        52.9829189f *
        frac(
            0.06711056f * pixel.x +
            0.00583715f * pixel.y));
}

float4 PSPresent(PixelInput input) : SV_Target
{
    int2 size = int2(Parameters.xy);

    int2 pixel = clamp(
        int2(input.Position.xy),
        int2(0, 0),
        size - 1);

    float2 uv = PixelUv(pixel);

    float3 hdr = HdrTexture.Load(
        int3(pixel, 0)).rgb;

    float depth = ContactDepth.Load(
        int3(pixel, 0));

    // Keep the existing contact shadow pass.
    float aoAttenuation = 0.0f;
    float contactAttenuation = 0.0f;

    if (depth < 0.999999f)
    {
        float3 normal =
            ContactNormal.Load(
                int3(pixel, 0)).xyz *
                2.0f - 1.0f;

        normal = normalize(normal);

        if (AoSettings.y > 0.5f)
        {
            float visibility = saturate(
                AmbientOcclusionTexture.Load(
                    int3(pixel, 0)).x);

            float3 sunDirection =
                Sun.xyz / max(length(Sun.xyz), 0.0001f);

            float sunFacing = saturate(
                dot(normal, sunDirection));

            float ambientWeight =
                1.0f -
                0.55f * sunFacing * saturate(Sun.y);

            aoAttenuation = saturate(
                (1.0f - visibility) *
                AoSettings.x *
                ambientWeight) * 0.65f;
        }

        if (Sun.w > 0.5f)
        {
            float shadow = ScreenContactShadow(
                uv,
                depth,
                normal);

            contactAttenuation = saturate(
                shadow * Camera.w);
        }
    }

    float attenuation = max(
        aoAttenuation,
        contactAttenuation);

    hdr *= 1.0f - attenuation;

    // HDR bloom is combined before tone mapping.
    if (GradeSettings.w > 0.5f)
    {
        float3 bloom = BloomTexture.SampleLevel(
            PostLinearSampler,
            uv,
            0.0f).rgb;

        hdr += bloom * GradeSettings.x;
    }

    // Exposure + ACES tone mapping.
    float3 color = ApplyToneMapping(hdr);

    // Color grading in display-referred linear RGB.
    color = ApplyColorLut(color);

    // Convert linear output into sRGB for the UNorm swap chain.
    color = LinearToSrgb(color);

    // Stable spatial dithering for sky pixels.
    if (depth >= 0.999999f &&
        GradeSettings.z > 0.0001f)
    {
        float noise =
            InterleavedGradientNoise(
                float2(pixel)) - 0.5f;

        color +=
            noise * GradeSettings.z / 255.0f;
    }

    return float4(
        saturate(color),
        1.0f);
}