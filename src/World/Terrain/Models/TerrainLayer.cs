namespace ZoneReborn.World.Terrain.Models
{
    public sealed class TerrainLayer
    {
        public int Index { get; init; }
        public string Name { get; init; } = "";
        public string DiffuseTexture { get; init; } = "";
        public string NormalTexture { get; init; } = "";
        public float ScaleU { get; init; } = 1;
        public float ScaleV { get; init; } = 1;
        public float NormalUvMultiplier { get; init; } = 1;
        public float Specular { get; init; }
        public string MaterialType { get; init; } = "";
    }
}

