namespace ZoneReborn.World.Terrain.Models;

public sealed record TerrainWaterBody
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = "Водоём";

    public float CenterX { get; init; }
    public float CenterZ { get; init; }

    public float RadiusX { get; init; } = 30f;
    public float RadiusZ { get; init; } = 20f;

    public float SurfaceHeight { get; init; }

    public float ShoreWidth { get; init; } = 6f;
    public float ShoreStrength { get; init; } = 0.9f;
    public float MaxShoreRise { get; init; } = 2f;
    public float MaxShoreSlope { get; init; } = 0.65f;

    public int SandLayerIndex { get; init; } = -1;

    public bool Contains(float x, float z)
    {
        if (RadiusX <= 0f || RadiusZ <= 0f)
        {
            return false;
        }

        float dx = (x - CenterX) / RadiusX;
        float dz = (z - CenterZ) / RadiusZ;

        return dx * dx + dz * dz <= 1f;
    }

    public override string ToString()
    {
        return $"{Name} ({RadiusX:0.#} × {RadiusZ:0.#} м)";
    }
}
