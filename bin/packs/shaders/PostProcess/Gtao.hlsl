cbuffer AoConstants : register(b0)
{
    row_major float4x4 InverseViewProjection;
    row_major float4x4 CurrentViewProjection;
    row_major float4x4 PreviousViewProjection;

    float4 Resolution;
    float4 Options;
    float4 Camera;
    float4 BlurAxis;
};

Texture2D<float> SceneDepth : register(t0);
Texture2D<float4> SceneNormal : register(t1);
Texture2D<float> AoInput : register(t2);
Texture2D<float2> AoHistory : register(t3);

SamplerState PointSampler : register(s0);

struct PixelInput
{
    float4 Position : SV_POSITION;
    float2 Uv : TEXCOORD0;
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

float ReadDepth(float2 uv)
{
    return SceneDepth.SampleLevel(
        PointSampler,
        saturate(uv),
        0.0f);
}

float3 ReadNormal(float2 uv)
{
    float3 normal =
        SceneNormal.SampleLevel(
            PointSampler,
            saturate(uv),
            0.0f).xyz * 2.0f - 1.0f;

    return normal * rsqrt(
        max(dot(normal, normal), 0.000001f));
}

float3 ReconstructWorld(float2 uv, float depth)
{
    float4 clip = float4(
        uv.x * 2.0f - 1.0f,
        1.0f - uv.y * 2.0f,
        depth,
        1.0f);

    float4 world = mul(
        clip,
        InverseViewProjection);

    return world.xyz / max(world.w, 0.000001f);
}

float ViewDistance(float3 world)
{
    return mul(
        float4(world, 1.0f),
        CurrentViewProjection).w;
}

float Hash(float2 position)
{
    float3 p = frac(
        float3(position.xyx) * 0.1031f);

    p += dot(p, p.yzx + 33.33f);

    return frac((p.x + p.y) * p.z);
}

float HorizonForSide(
    float2 uv,
    float3 world,
    float3 projectedNormal,
    float2 direction,
    float radiusPixels,
    int stepCount)
{
    float horizon = 0.0f;

    float jitter = Hash(
        floor(uv * Resolution.xy) +
        Options.w * 19.0f);

    [loop]
    for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
    {
        float stepFraction =
            (stepIndex + 0.65f + jitter * 0.3f) /
            stepCount;

        float2 sampleUv =
            uv + direction *
            (stepFraction * radiusPixels) /
            Resolution.xy;

        if (any(sampleUv <= 0.0f) ||
            any(sampleUv >= 1.0f))
        {
            continue;
        }

        float sampledDepth = ReadDepth(sampleUv);

        if (sampledDepth >= 0.999999f)
        {
            continue;
        }

        float3 sampleWorld = ReconstructWorld(
            sampleUv,
            sampledDepth);

        float3 delta = sampleWorld - world;
        float distanceToSample = length(delta);

        if (distanceToSample < 0.015f ||
            distanceToSample >= Options.x)
        {
            continue;
        }

        float horizonCosine = saturate(
            dot(delta / distanceToSample, projectedNormal));

        float distanceFade = 1.0f - smoothstep(
            Options.x * 0.45f,
            Options.x,
            distanceToSample);

        horizon = max(
            horizon,
            horizonCosine * distanceFade);
    }

    return horizon;
}

float PSGtao(PixelInput input) : SV_Target
{
    float2 uv = input.Uv;
    float depth = ReadDepth(uv);

    if (depth >= 0.999999f)
    {
        return 1.0f;
    }

    float3 world = ReconstructWorld(uv, depth);
    float3 normal = ReadNormal(uv);
    float3 eye = normalize(Camera.xyz - world);

    float2 pixelStep = float2(
        1.0f / Resolution.x,
        0.0f);

    float pixelWorldSize = length(
        ReconstructWorld(uv + pixelStep, depth) -
        world);

    float radiusPixels = clamp(
        Options.x / max(pixelWorldSize, 0.0001f),
        2.0f,
        128.0f);

    int quality = (int)round(Options.y);

    int sliceCount =
        quality >= 3 ? 8 :
        quality == 2 ? 6 : 4;

    int stepCount =
        quality >= 3 ? 4 :
        quality == 2 ? 3 : 2;

    float noise = Hash(
        floor(uv * Resolution.xy)) *
        6.2831853f;

    float total = 0.0f;

    [loop]
    for (int sliceIndex = 0; sliceIndex < sliceCount; sliceIndex++)
    {
        float angle =
            sliceIndex * 3.14159265f / sliceCount +
            noise;

        float2 direction = float2(
            cos(angle),
            sin(angle));

        float2 adjacentUv = saturate(
            uv + direction / Resolution.xy);

        float3 sliceTangent =
            ReconstructWorld(adjacentUv, depth) -
            world;

        float tangentLength = length(sliceTangent);

        if (tangentLength < 0.00001f)
        {
            continue;
        }

        sliceTangent /= tangentLength;

        float3 planeNormal = cross(
            eye,
            sliceTangent);

        float planeLength = length(planeNormal);

        if (planeLength < 0.00001f)
        {
            continue;
        }

        planeNormal /= planeLength;

        float3 projectedNormal =
            normal -
            planeNormal * dot(normal, planeNormal);

        projectedNormal = normalize(
            projectedNormal + eye * 0.00001f);

        float positive = HorizonForSide(
            uv,
            world,
            projectedNormal,
            direction,
            radiusPixels,
            stepCount);

        float negative = HorizonForSide(
            uv,
            world,
            projectedNormal,
            -direction,
            radiusPixels,
            stepCount);

        total += 0.5f * (positive + negative);
    }

    return saturate(
        1.0f - total / sliceCount);
}

float PSBilateral(PixelInput input) : SV_Target
{
    float2 uv = input.Uv;
    float centerDepth = ReadDepth(uv);

    if (centerDepth >= 0.999999f)
    {
        return 1.0f;
    }

    float3 centerWorld = ReconstructWorld(
        uv,
        centerDepth);

    float centerDistance = ViewDistance(centerWorld);
    float3 centerNormal = ReadNormal(uv);

    float sum = 0.0f;
    float weightSum = 0.0f;

    [unroll]
    for (int offset = -3; offset <= 3; offset++)
    {
        float2 sampleUv = saturate(
            uv + BlurAxis.xy * offset / Resolution.zw);

        float depth = ReadDepth(sampleUv);

        if (depth >= 0.999999f)
        {
            continue;
        }

        float3 world = ReconstructWorld(
            sampleUv,
            depth);

        float depthDifference = abs(
            ViewDistance(world) -
            centerDistance);

        float normalSimilarity = pow(
            saturate(dot(
                centerNormal,
                ReadNormal(sampleUv))),
            12.0f);

        float spatial = exp(
            -offset * offset / 8.0f);

        float depthWeight = exp(
            -depthDifference /
            max(Options.x * 0.18f, 0.035f));

        float weight =
            spatial *
            depthWeight *
            normalSimilarity;

        sum += AoInput.SampleLevel(
            PointSampler,
            sampleUv,
            0.0f) * weight;

        weightSum += weight;
    }

    return weightSum > 0.00001f
        ? sum / weightSum
        : AoInput.SampleLevel(PointSampler, uv, 0.0f);
}

float UpsampleAo(
    float2 uv,
    float3 centerNormal,
    float3 centerWorld)
{
    float2 halfPixel =
        uv * Resolution.zw - 0.5f;

    int2 basePixel = int2(floor(halfPixel));
    float2 fraction = frac(halfPixel);

    float centerDistance = ViewDistance(centerWorld);

    float sum = 0.0f;
    float weightSum = 0.0f;

    [unroll]
    for (int y = 0; y < 2; y++)
    {
        [unroll]
        for (int x = 0; x < 2; x++)
        {
            int2 halfCoordinate = clamp(
                basePixel + int2(x, y),
                int2(0, 0),
                int2(Resolution.zw) - 1);

            float2 sampleUv =
                (float2(halfCoordinate) + 0.5f) /
                Resolution.zw;

            float sampleDepth = ReadDepth(sampleUv);

            if (sampleDepth >= 0.999999f)
            {
                continue;
            }

            float bilinear =
                (x == 0 ? 1.0f - fraction.x : fraction.x) *
                (y == 0 ? 1.0f - fraction.y : fraction.y);

            float3 sampleWorld = ReconstructWorld(
                sampleUv,
                sampleDepth);

            float depthDifference = abs(
                ViewDistance(sampleWorld) -
                centerDistance);

            float depthWeight = exp(
                -depthDifference /
                max(Options.x * 0.16f, 0.025f));

            float normalWeight = pow(
                saturate(dot(
                    centerNormal,
                    ReadNormal(sampleUv))),
                16.0f);

            float weight =
                max(bilinear, 0.00001f) *
                depthWeight *
                normalWeight;

            sum += AoInput.Load(
                int3(halfCoordinate, 0)) * weight;

            weightSum += weight;
        }
    }

    if (weightSum <= 0.00001f)
    {
        int2 nearest = clamp(
            int2(uv * Resolution.zw),
            int2(0, 0),
            int2(Resolution.zw) - 1);

        return AoInput.Load(
            int3(nearest, 0));
    }

    return sum / weightSum;
}

float2 PSTemporal(PixelInput input) : SV_Target
{
    float2 uv = input.Uv;
    float depth = ReadDepth(uv);

    if (depth >= 0.999999f)
    {
        return float2(1.0f, 0.0f);
    }

    float3 world = ReconstructWorld(uv, depth);
    float3 normal = ReadNormal(uv);

    float currentAo = UpsampleAo(
        uv,
        normal,
        world);

    float viewDistance = ViewDistance(world);

    if (Options.z > 0.5f)
    {
        float4 previousClip = mul(
            float4(world, 1.0f),
            PreviousViewProjection);

        if (previousClip.w > 0.00001f)
        {
            float2 previousNdc =
                previousClip.xy / previousClip.w;

            float2 previousUv = float2(
                previousNdc.x * 0.5f + 0.5f,
                0.5f - previousNdc.y * 0.5f);

            if (all(previousUv > 0.0f) &&
                all(previousUv < 1.0f))
            {
                float2 history = AoHistory.SampleLevel(
                    PointSampler,
                    previousUv,
                    0.0f);

                float depthTolerance = max(
                    0.075f,
                    previousClip.w * 0.015f);

                if (history.y > 0.0f &&
                    abs(history.y - previousClip.w) <= depthTolerance)
                {
                    float travelPixels = length(
                        (uv - previousUv) *
                        Resolution.xy);

                    float historyWeight =
                        0.9f * exp(-travelPixels * 0.08f);

                    float boundedHistory = clamp(
                        history.x,
                        currentAo - 0.18f,
                        currentAo + 0.18f);

                    currentAo = lerp(
                        currentAo,
                        boundedHistory,
                        historyWeight);
                }
            }
        }
    }

    return float2(
        saturate(currentAo),
        viewDistance);
}