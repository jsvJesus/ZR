namespace ZoneReborn.World.Terrain.Models;

public sealed record TerrainTileReference(int X, int Z, float MinimumHeight, float MaximumHeight, string File);

