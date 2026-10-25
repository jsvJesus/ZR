using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Levels.Models;

public sealed record LoadedLevel(string Directory, LevelManifest Manifest, IReadOnlyList<TerrainLayer> Layers);

