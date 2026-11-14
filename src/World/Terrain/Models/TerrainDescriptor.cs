namespace ZoneReborn.World.Terrain.Models
{
    public sealed record TerrainDescriptor
    {
        public int SamplesX { get; init; }
        public int SamplesZ { get; init; }
        public int TileSize { get; init; } = 256;
        public float CellSize { get; init; }
        public float OriginX { get; init; }
        public float OriginZ { get; init; }
        public float HeightOffset { get; init; }
        public float HeightScale { get; init; }
        public int MeshStep { get; init; } = 4;
        public string HolesFile { get; set; } = "";
        public string PaintOverviewFile { get; set; } = "terrain/paint-overview.bin";
        public bool ImportedFromMesh { get; init; }
        public bool HasSourceMaterialLayout { get; set; }
        public float SourceOffsetX { get; init; }
        public float SourceOffsetZ { get; init; }
        public string FarDiffuseTexture { get; init; } = "";
        public string FarNormalTexture { get; init; } = "";
        public int TilesX => SamplesX / TileSize;
        public int TilesZ => SamplesZ / TileSize;
        public float WorldSizeX => (SamplesX - 1) * CellSize;
        public float WorldSizeZ => (SamplesZ - 1) * CellSize;
        public float MaxX => OriginX + WorldSizeX;
        public float MaxZ => OriginZ + WorldSizeZ;
        public bool Contains(float x, float z) => float.IsFinite(x) && float.IsFinite(z)
            && x >= OriginX && z >= OriginZ && x <= MaxX && z <= MaxZ;
        public float DecodeHeight(ushort value) => HeightOffset + value * (HeightScale / 65535f);
    }
}

