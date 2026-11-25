using ZoneReborn.World.Roads;
using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Actors;

namespace ZoneReborn.World.Levels.Models;

public sealed record LoadedLevel(
    string Directory,
    LevelManifest Manifest,
    IReadOnlyList<TerrainLayer> Layers)
{
    public ZoneReborn.World.Vegetation.VegetationScene Vegetation { get; init; } = new();
    public ZoneReborn.World.Scene.SceneData? Objects { get; init; }
    public ZoneReborn.World.Terrain.TerrainDocument? TerrainDocument { get; init; }
    public RoadNetwork Roads { get; init; } = new();
    public ZoneReborn.World.Vegetation.GroundCoverDocument GroundCover { get; init; } = new();
    public ActorDocument Actors { get; init; } = new();
}
