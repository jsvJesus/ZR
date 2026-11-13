namespace ZoneReborn.World.Vegetation;

public enum VegetationScatterKind
{
    Grass,
    Bush,
    Tree,
    Reed
}

public sealed record VegetationScatterRule
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = "Новое правило";
    public string Model { get; init; } = "";

    public VegetationScatterKind Kind { get; init; }

    public int MaterialIndex { get; init; }
    public int Seed { get; init; } = 12345;

    public float DensityPer100SquareMeters { get; init; } = 0.5f;

    public float MinimumScale { get; init; } = 0.8f;
    public float MaximumScale { get; init; } = 1.2f;

    public float MinimumMaterialWeight { get; init; } = 0.55f;
    public float MaximumSlopeDegrees { get; init; } = 35f;

    public float ObjectClearance { get; init; } = 2f;
    public float RoadClearance { get; init; } = 3f;
    public float MinimumSpacing { get; init; } = 2f;

    public bool NearWaterOnly { get; init; }
    public float MaximumShoreDistance { get; init; } = 5f;

    public bool Enabled { get; init; } = true;

    public override string ToString()
    {
        return $"{Name} — {Kind}";
    }
}

public sealed record GeneratedVegetationInstance
{
    public Guid RuleId { get; init; }
    public int TileX { get; init; }
    public int TileZ { get; init; }
    public TreeInstance Tree { get; init; } = new();
}
