namespace ZoneReborn.World.Terrain.Models
{
    public sealed class TerrainDescriptor
    {
        public int SamplesX { get; init; }
        public int SamplesZ { get; init; }
        public int TileSize { get; init; } = 256;
        public float CellSize { get; init; }
        public float HeightOffset { get; init; }
        public float HeightScale { get; init; }
        public string FarDiffuseTexture { get; init; } = "";
        public string FarNormalTexture { get; init; } = "";
        public int TilesX => SamplesX / TileSize;
        public int TilesZ => SamplesZ / TileSize;
        public float WorldSizeX => (SamplesX - 1) * CellSize;
        public float WorldSizeZ => (SamplesZ - 1) * CellSize;
        public float DecodeHeight(ushort value) => HeightOffset + value * (HeightScale / 65535f);
    }
}

