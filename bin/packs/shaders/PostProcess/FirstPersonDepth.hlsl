cbuffer DepthConstants : register(b0)
{
    float4 DepthPlanes;
    // x = World Near
    // y = First Person Near
    // z = Far Plane
};

Texture2D<float> SceneDepth : register(t0);

float PSMain(float4 position : SV_POSITION) : SV_Depth
{
    int2 pixel = int2(position.xy);

    float sceneDepth = SceneDepth.Load(
        int3(pixel, 0));

    if (sceneDepth >= 0.999999f)
    {
        return 1.0f;
    }

    float worldNear = DepthPlanes.x;
    float firstPersonNear = DepthPlanes.y;
    float farPlane = DepthPlanes.z;

    float denominator = max(
        farPlane -
        sceneDepth * (farPlane - worldNear),
        0.000001f);

    float viewDistance =
        worldNear * farPlane / denominator;

    float firstPersonDepth =
        farPlane / (farPlane - firstPersonNear) *
        (1.0f - firstPersonNear /
        max(viewDistance, firstPersonNear));

    return saturate(firstPersonDepth);
}