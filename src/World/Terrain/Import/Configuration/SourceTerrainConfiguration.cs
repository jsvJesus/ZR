using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain.Import.Configuration;

public sealed record SourceTerrainConfiguration(
    string Directory,
    TerrainDescriptor Terrain,
    IReadOnlyList<TerrainLayer> Layers);

