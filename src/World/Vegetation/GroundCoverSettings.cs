using ZoneReborn.World.Assets;
using System.Text.Json.Serialization;

namespace ZoneReborn.World.Vegetation;

public sealed record GroundCoverSettings
{
    [JsonRequired] public string[] Models { get; init; } = [];
    [JsonRequired] public float DensityPerSquareMeter { get; init; } = 4f;
    [JsonRequired] public float MinimumScale { get; init; } = 0.8f;
    [JsonRequired] public float MaximumScale { get; init; } = 1.2f;
    [JsonRequired] public float MaximumSlopeDegrees { get; init; } = 45f;
    [JsonRequired] public float RoadClearance { get; init; } = 1f;
    [JsonRequired] public float ObjectClearance { get; init; } = 0.5f;
    [JsonRequired] public float DrawDistance { get; init; } = 40f;
    [JsonRequired] public float MinimumMaterialWeight { get; init; } = 0.5f;
    [JsonRequired] public int Seed { get; init; } = 12345;
    [JsonRequired] public int MaterialIndex { get; init; } = -1;
    [JsonRequired] public bool AutoGenerate { get; init; }

    internal GroundCoverSettings Copy() => this with { Models = (string[])Models.Clone() };

    public static void Validate(GroundCoverSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Models == null || settings.Models.Length > 256 ||
            settings.MaterialIndex is < -1 or > 254 ||
            !Range(settings.DensityPerSquareMeter, 0.1f, 16f) ||
            !Range(settings.MinimumScale, 0.05f, 10f) ||
            !Range(settings.MaximumScale, settings.MinimumScale, 10f) ||
            !Range(settings.MaximumSlopeDegrees, 0f, 85f) ||
            !Range(settings.RoadClearance, 0f, 30f) ||
            !Range(settings.ObjectClearance, 0f, 30f) ||
            !Range(settings.DrawDistance, 1f, 100f) ||
            !Range(settings.MinimumMaterialWeight, 0.01f, 1f))
            throw new InvalidDataException("Некорректные настройки травы и камней.");
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string file in settings.Models)
        {
            if (string.IsNullOrWhiteSpace(file) || file.Length > 1024 || file.Contains('\\') || file.Contains(':') ||
                !file.StartsWith("objects/flora/", StringComparison.OrdinalIgnoreCase) ||
                !file.EndsWith(".model", StringComparison.OrdinalIgnoreCase) ||
                file.Split('/').Any(part => part is "" or "." or "..") || !unique.Add(file))
                throw new InvalidDataException("Ожидается уникальная .model из objects/flora.");
            _ = AssetPathResolver.Resolve(".", file);
        }
    }

    private static bool Range(float value, float minimum, float maximum) =>
        float.IsFinite(value) && value >= minimum && value <= maximum;
}
