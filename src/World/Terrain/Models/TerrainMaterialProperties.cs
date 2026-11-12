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
    public float Opacity { get; init; } = 1f;
}

public sealed record TerrainWeatherProperties
{
    public bool ReactToRain { get; init; } = true;

    public float SoakSeconds { get; init; } = 35f;
    public float DrySeconds { get; init; } = 180f;

    public float Darkening { get; init; } = 0.28f;
    public float WetRoughness { get; init; } = 0.28f;
    public float WetSpecular { get; init; } = 0.75f;

    public float Patchiness { get; init; } = 0.55f;
    public float PatchScaleMeters { get; init; } = 10f;
    public float PuddleStrength { get; init; } = 0.35f;

    public float WetFriction { get; init; } = 0.65f;
    public string WetFootstepSound { get; init; } = "";
}

public sealed record TerrainBlendProperties
{
    public float NoiseIntensity { get; init; } = 1f;
    public float HeightIntensity { get; init; }
    public float SlopeIntensity { get; init; }
    public float MacroIntensity { get; init; }
    public float MacroScaleMeters { get; init; } = 32f;
}

public sealed record TerrainMaterialSettings
{
    public TerrainSurfaceType SurfaceType { get; init; } =
        TerrainSurfaceType.Soil;

    public TerrainPhysicalProperties Physics { get; init; } = new();

    public TerrainVisualProperties Visual { get; init; } = new();

    public TerrainBlendProperties Blend { get; init; } = new();

    public TerrainWeatherProperties Weather { get; init; } = new();
}

public sealed record TerrainTextureVariant
{
    public string Name { get; init; } = "Основное покрытие";

    public string AlbedoTexture { get; init; } = "";
    public string NormalTexture { get; init; } = "";
    public string RoughnessTexture { get; init; } = "";
    public string HeightTexture { get; init; } = "";

    public float TilingMeters { get; init; } = 4f;

    public float SlopeBias { get; init; }

    public override string ToString()
    {
        return Name;
    }
}
