namespace ZoneReborn.World.Terrain.Models;

public sealed record TerrainTileData(
    int X,
    int Z,
    ushort[] Heights,
    byte[] Normals,
    byte[] ActiveLayers,
    IReadOnlyList<TerrainMask> Masks);

