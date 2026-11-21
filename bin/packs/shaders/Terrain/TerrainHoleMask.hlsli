Texture2D<uint> TerrainHoles : register(t5);

bool IsTerrainHole(
    float2 worldPosition,
    float2 terrainOrigin,
    float cellSize)
{
    uint width;
    uint height;

    TerrainHoles.GetDimensions(width, height);

    float2 grid = (
        worldPosition - terrainOrigin
    ) / cellSize;

    grid = clamp(
        grid,
        float2(0.0, 0.0),
        float2(width, height) - 0.0001
    );

    int2 cell = int2(floor(grid));

    uint2 subCell = uint2(
        min(
            floor(frac(grid) * 4.0),
            float2(3.0, 3.0)
        )
    );

    uint bitIndex = subCell.y * 4u + subCell.x;
    uint bit = 1u << bitIndex;

    uint mask = TerrainHoles.Load(
        int3(cell, 0)
    ).r;

    return (mask & bit) != 0u;
}