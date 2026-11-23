cbuffer AoConstants : register(b0)
{
    row_major float4x4 InverseViewProjection;
    row_major float4x4 CurrentViewProjection;
    row_major float4x4 PreviousViewProjection;
    row_major float4x4 PreviousInverseViewProjection;

    float4 Resolution;
    float4 Options;
    float4 Camera;
    float4 BlurAxis;
};

Texture2D<float> SceneDepth : register(t0);
Texture2D<float4> SceneNormal : register(t1);
Texture2D<float> AoInput : register(t2);
// AO, linear view depth, and the world normal encoded in two octahedral axes.
Texture2D<float4> AoHistory : register(t3);

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

int2 DepthCoordinate(float2 uv)
{
    return clamp(int2(floor(uv * Resolution.xy)), int2(0, 0), int2(Resolution.xy) - 1);
}

float2 DepthUv(float2 uv)
{
    return (float2(DepthCoordinate(uv)) + 0.5f) / Resolution.xy;
}

float ReadDepth(float2 uv)
{
    return SceneDepth.Load(int3(DepthCoordinate(uv), 0));
}

float3 ReadNormal(float2 uv)
{
    float3 normal =
        SceneNormal.Load(int3(DepthCoordinate(uv), 0)).xyz * 2.0f - 1.0f;

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

float SurfaceSeparation(float3 delta, float3 normal0, float3 normal1)
{
    // A sloped surface may span a large view-depth interval without an edge.
    // Stop filtering across separation from either surface's tangent plane.
    return max(abs(dot(delta, normal0)), abs(dot(delta, normal1)));
}

float GeometryWeight(float3 delta, float3 normal0, float3 normal1)
{
    float normalAgreement = saturate(dot(normal0, normal1));
    float separation = SurfaceSeparation(delta, normal0, normal1);
    float tolerance = max(0.015f, min(Options.x * 0.04f, 0.08f));

    // A radius change must not allow filtering between unrelated surfaces.
    if (normalAgreement < 0.5f || separation > tolerance * 3.0f)
    {
        return 0.0f;
    }

    return exp(-separation / tolerance) * pow(normalAgreement, 12.0f);
}

float2 OctahedralEncode(float3 normal)
{
    normal /= max(abs(normal.x) + abs(normal.y) + abs(normal.z), 0.000001f);
    float2 encoded = normal.xy;
    if (normal.z < 0.0f)
    {
        encoded = (1.0f - abs(encoded.yx)) *
            float2(encoded.x >= 0.0f ? 1.0f : -1.0f,
                   encoded.y >= 0.0f ? 1.0f : -1.0f);
    }
    return encoded;
}

float3 OctahedralDecode(float2 encoded)
{
    float3 normal = float3(encoded, 1.0f - abs(encoded.x) - abs(encoded.y));
    float fold = saturate(-normal.z);
    normal.xy += float2(normal.x >= 0.0f ? -fold : fold,
                        normal.y >= 0.0f ? -fold : fold);
    return normalize(normal);
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
    float3 surfaceNormal,
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

        // Depth is point sampled. Its position must use that texel's centre,
        // rather than the unsnapped ray (including in the half-resolution pass).
        sampleUv = DepthUv(sampleUv);
        if (all(DepthCoordinate(sampleUv) == DepthCoordinate(uv)))
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

        float normalBias = max(0.001f, Options.x * 0.005f);
        if (dot(delta, surfaceNormal) <= normalBias)
        {
            // Samples on the receiver plane cannot occlude that same plane.
            continue;
        }

        float horizonCosine = saturate(
            (dot(delta, projectedNormal) - normalBias) / distanceToSample);

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
    float2 uv = DepthUv(input.Uv);
    float depth = ReadDepth(uv);

    if (depth >= 0.999999f)
    {
        return 1.0f;
    }

    float3 world = ReconstructWorld(uv, depth);
    float viewDistance = ViewDistance(world);

    if (viewDistance <= 0.01f ||
        viewDistance >= 60.0f)
    {
        return 1.0f;
    }

    float3 normal = ReadNormal(uv);
    float3 eyeVector = Camera.xyz - world;

    float eyeLength = length(eyeVector);

    if (eyeLength < 0.00001f)
    {
        return 1.0f;
    }

    float3 eye = eyeVector / eyeLength;

    float2 pixelStep = float2(
        1.0f / Resolution.x,
        0.0f);

    float pixelWorldSize = length(
        ReconstructWorld(
            uv + pixelStep,
            depth) - world);

    float radiusPixels = min(
        Options.x / max(pixelWorldSize, 0.0001f),
        96.0f);

    // Subpixel radii have no neighbouring geometry to measure reliably.
    // Do not enlarge a 10 cm world radius to an arbitrary two-pixel footprint.
    float resolutionFade = smoothstep(1.0f, 2.0f, radiusPixels);
    if (resolutionFade <= 0.0f)
    {
        return 1.0f;
    }

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
    for (int sliceIndex = 0;
         sliceIndex < sliceCount;
         sliceIndex++)
    {
        float angle =
            sliceIndex * 3.14159265f / sliceCount +
            noise;

        float2 direction = float2(
            cos(angle),
            sin(angle));

        // These are synthetic rays at the centre depth, not depth lookups;
        // clipping them at the viewport edge would distort the slice direction.
        float2 adjacentUv = uv + direction / Resolution.xy;

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
            planeNormal *
            dot(normal, planeNormal);

        float projectedLength =
            length(projectedNormal);

        if (projectedLength < 0.00001f)
        {
            continue;
        }

        projectedNormal /= projectedLength;

        float positive = HorizonForSide(
            uv,
            world,
            normal,
            projectedNormal,
            direction,
            radiusPixels,
            stepCount);

        float negative = HorizonForSide(
            uv,
            world,
            normal,
            projectedNormal,
            -direction,
            radiusPixels,
            stepCount);

        total += 0.5f * (
            positive + negative);
    }

    float visibility = saturate(
        1.0f - total / sliceCount);

    float distanceFade =
        1.0f - smoothstep(
            25.0f,
            60.0f,
            viewDistance);

    return lerp(
        1.0f,
        visibility,
        distanceFade * resolutionFade);
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
        DepthUv(uv),
        centerDepth);

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
            DepthUv(sampleUv),
            depth);

        float3 sampleNormal = ReadNormal(sampleUv);
        float spatial = exp(
            -offset * offset / 8.0f);

        float weight = spatial * GeometryWeight(
            world - centerWorld, centerNormal, sampleNormal);

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
    float3 centerWorld,
    out float2 neighborhoodBounds)
{
    float2 halfPixel =
        uv * Resolution.zw - 0.5f;

    int2 nearestPixel = int2(floor(uv * Resolution.zw));

    float sum = 0.0f;
    float weightSum = 0.0f;
    float neighborhoodSum = 0.0f;
    float neighborhoodSquaredSum = 0.0f;
    float neighborhoodWeight = 0.0f;
    float minimumAo = 1.0f;
    float maximumAo = 0.0f;

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            int2 halfCoordinate = nearestPixel + int2(x, y);
            if (any(halfCoordinate < 0) || any(halfCoordinate >= int2(Resolution.zw)))
            {
                continue;
            }

            float2 sampleUv =
                (float2(halfCoordinate) + 0.5f) /
                Resolution.zw;

            float sampleDepth = ReadDepth(sampleUv);

            if (sampleDepth >= 0.999999f)
            {
                continue;
            }

            float2 pixelDelta = float2(halfCoordinate) - halfPixel;
            float2 bilinearAxes = saturate(1.0f - abs(pixelDelta));
            float bilinear = bilinearAxes.x * bilinearAxes.y;

            float3 sampleWorld = ReconstructWorld(
                DepthUv(sampleUv),
                sampleDepth);

            float3 sampleNormal = ReadNormal(sampleUv);
            float geometryWeight = GeometryWeight(
                sampleWorld - centerWorld, centerNormal, sampleNormal);

            if (geometryWeight <= 0.05f)
            {
                continue;
            }

            float ao = AoInput.Load(int3(halfCoordinate, 0));
            float weight = bilinear * geometryWeight;
            sum += ao * weight;
            weightSum += weight;

            float sampleWeight = geometryWeight * exp(-dot(pixelDelta, pixelDelta) * 0.5f);
            neighborhoodSum += ao * sampleWeight;
            neighborhoodSquaredSum += ao * ao * sampleWeight;
            neighborhoodWeight += sampleWeight;
            minimumAo = min(minimumAo, ao);
            maximumAo = max(maximumAo, ao);
        }
    }

    if (weightSum <= 0.00001f)
    {
        // Thin geometry may have no matching half-resolution sample. Copying
        // the nearest sample here would put a foreground contact onto its background.
        neighborhoodBounds = float2(1.0f, 1.0f);
        return 1.0f;
    }

    float currentAo = sum / weightSum;
    float mean = neighborhoodSum / max(neighborhoodWeight, 0.00001f);
    float variance = max(0.0f,
        neighborhoodSquaredSum / max(neighborhoodWeight, 0.00001f) - mean * mean);
    float deviation = sqrt(variance) * 1.25f;
    neighborhoodBounds = float2(
        min(currentAo, max(minimumAo, mean - deviation)),
        max(currentAo, min(maximumAo, mean + deviation)));
    return currentAo;
}

float4 PSTemporal(PixelInput input) : SV_Target
{
    float2 uv = input.Uv;
    float depth = ReadDepth(uv);

    if (depth >= 0.999999f)
    {
        return float4(1.0f, 0.0f, 0.0f, 0.0f);
    }

    float3 world = ReconstructWorld(DepthUv(uv), depth);
    float3 normal = ReadNormal(uv);

    float2 neighborhoodBounds;
    float currentAo = UpsampleAo(
        uv,
        normal,
        world,
        neighborhoodBounds);

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
                int2 historyPixel = DepthCoordinate(previousUv);
                float2 historyUv = (float2(historyPixel) + 0.5f) / Resolution.xy;
                float4 history = AoHistory.Load(int3(historyPixel, 0));

                // Point history lives at the previous pixel centre, which can
                // have a different depth on a slope. Intersect its previous ray
                // with the receiver plane instead of widening depth rejection.
                float2 historyNdc = float2(historyUv.x * 2.0f - 1.0f,
                                          1.0f - historyUv.y * 2.0f);
                float4 nearClip = mul(float4(historyNdc, 0.0f, 1.0f),
                                      PreviousInverseViewProjection);
                // A point farther down the ray reduces cancellation when the
                // camera has large world coordinates, without using far-plane infinity.
                float4 rayClip = mul(float4(historyNdc, 0.99f, 1.0f),
                                     PreviousInverseViewProjection);
                float3 rayOrigin = nearClip.xyz / max(nearClip.w, 0.000001f);
                float3 ray = rayClip.xyz / max(rayClip.w, 0.000001f) - rayOrigin;
                float planeDenominator = dot(normal, ray);
                float planeDistance = dot(normal, world - rayOrigin) /
                    (abs(planeDenominator) > 0.000001f ? planeDenominator : 1.0f);
                float expectedDepth = mul(float4(rayOrigin + ray * planeDistance, 1.0f),
                                          PreviousViewProjection).w;
                // RGBA16F linear depth needs at least one half-float ULP of tolerance.
                float depthTolerance = max(0.01f, abs(expectedDepth) * 0.0015f);

                if (abs(planeDenominator) > 0.000001f && planeDistance >= 0.0f &&
                    expectedDepth > 0.0f && history.y > 0.0f &&
                    abs(history.y - expectedDepth) <= depthTolerance &&
                    dot(OctahedralDecode(history.zw), normal) >= 0.95f)
                {
                    float travelPixels = length(
                        (uv - previousUv) *
                        Resolution.xy);

                    float historyWeight =
                        0.9f * exp(-travelPixels * 0.08f);

                    // Reprojection accounts for camera motion only. With no
                    // object motion vectors, current AO changes must also reject
                    // history when an occluder moves along the same receiver.
                    historyWeight *= 1.0f - smoothstep(
                        0.04f, 0.2f, abs(history.x - currentAo));

                    float boundedHistory = clamp(
                        history.x,
                        neighborhoodBounds.x,
                        neighborhoodBounds.y);

                    currentAo = lerp(
                        currentAo,
                        boundedHistory,
                        historyWeight);
                }
            }
        }
    }

    return float4(
        saturate(currentAo),
        clamp(viewDistance, 0.0f, 65504.0f),
        OctahedralEncode(normal));
}
