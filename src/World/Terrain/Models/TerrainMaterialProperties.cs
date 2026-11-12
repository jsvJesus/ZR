using System.Text.Json.Serialization;

namespace ZoneReborn.World.Terrain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TerrainSurfaceType
{
    Soil,
    Sand,
    Gravel,
    Asphalt,
    Wood,
    Rock,
    Water,
    Glass
}

public sealed record TerrainPhysicalProperties
{
    public float Friction { get; init; } = 0.8f;
    public float Hardness { get; init; } = 0.5f;
    public string FootstepSound { get; init; } = "";
}

public sealed record TerrainVisualProperties
{
    public float Roughness { get; init; } = 0.85f;
    public float Metallic { get; init; }
    public float Specular { get; init; } = 0.5f;
    public float Opacity { get; init; } = 1.0f;
}

public sealed record TerrainMaterialSettings
{
    public TerrainSurfaceType SurfaceType { get; init; } =
        TerrainSurfaceType.Soil;

    public TerrainPhysicalProperties Physics { get; init; } = new();

    public TerrainVisualProperties Visual { get; init; } = new();
}

public sealed record TerrainTextureVariant
{
    public string Name { get; init; } = "Основное покрытие";

    public string AlbedoTexture { get; init; } = "";
    public string NormalTexture { get; init; } = "";
    public string RoughnessTexture { get; init; } = "";
    public string HeightTexture { get; init; } = "";

    public float TilingMeters { get; init; } = 4.0f;

    public override string ToString()
    {
        return Name;
    }
}
