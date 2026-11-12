namespace ZoneReborn.World.Terrain.Models;

public sealed record TerrainLayer
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

    public string SourceMaterial { get; init; } = "";

    public bool NeedsTexture { get; init; }

    public float[] AverageColor { get; init; } = [];

    public bool IsArchived { get; init; }

    public TerrainMaterialSettings Settings { get; init; } = new();

    public List<TerrainTextureVariant> TextureVariants { get; init; } = [];

    public List<TerrainTextureVariant> GetTextureVariants()
    {
        var variants = TextureVariants?.ToList()
                       ?? new List<TerrainTextureVariant>();

        if (variants.Count == 0)
        {
            variants.Add(new TerrainTextureVariant
            {
                Name = "Основное покрытие",
                AlbedoTexture = DiffuseTexture,
                NormalTexture = NormalTexture
            });
        }
        else
        {
            variants[0] = variants[0] with
            {
                AlbedoTexture = DiffuseTexture,
                NormalTexture = NormalTexture
            };
        }

        return variants;
    }

    public override string ToString()
    {
        if (IsArchived)
        {
            return Name + " — архив";
        }

        return Name + (NeedsTexture
            ? " — выберите текстуру"
            : "");
    }
}
