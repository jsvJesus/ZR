cbuffer FrameConstants : register(b0)
{
    row_major float4x4 InverseViewProjection;
    row_major float4x4 PreviousViewProjection;
    row_major float4x4 CurrentViewProjection;

    float4 Parameters;
    float4 Camera;
    float4 Sun;
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

float4 PSPresent(PixelInput input) : SV_Target
{
    int2 size = int2(Parameters.xy);

    int2 pixel = clamp(
        int2(input.Position.xy),
        int2(0, 0),
        size - 1);

    float3 hdr = HdrTexture.Load(
        int3(pixel, 0)).rgb;

    if (Sun.w > 0.5f)
    {
        float depth = ContactDepth.Load(
            int3(pixel, 0));

        if (depth < 0.999999f)
        {
            float3 normal =
                ContactNormal.Load(
                    int3(pixel, 0)).xyz *
                    2.0f - 1.0f;

            float shadow = ScreenContactShadow(
                PixelUv(pixel),
                depth,
                normalize(normal));

            float attenuation = saturate(
                shadow * Camera.w);

            hdr *= 1.0f - attenuation;
        }
    }

    float3 color = ApplyToneMapping(hdr);

    return float4(color, 1.0f);
}