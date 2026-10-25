using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Levels.Models;

public sealed class LevelManifest
{
    public string Format { get; init; } = "ZoneReborn.Level";
    public int Version { get; init; } = 1;
    public string Name { get; init; } = "";
    public string LayersFile { get; init; } = "layers.json";
    public string OverviewFile { get; init; } = "terrain/overview.terrain";
    public TerrainDescriptor Terrain { get; init; } = new();
    public List<TerrainTileReference> Tiles { get; init; } = [];
    public LevelCamera Camera { get; set; } = new(0, 4, -12, 0, -0.18f, 80);
}

