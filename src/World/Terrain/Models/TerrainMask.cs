namespace ZoneReborn.World.Terrain.Models
{
    // Each BC1 RGB mask contains three consecutive non-base layer weights.
    public sealed record TerrainMask(int Group, byte[] Data);
}

