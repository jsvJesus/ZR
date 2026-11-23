using System.Text.RegularExpressions;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Vegetation;

public sealed record GroundCoverCatalogEntry(string File, string Name, string Category)
{
    public override string ToString() => $"{Name} — {Category}";
}

public static class GroundCoverCatalog
{
    private static readonly string[] GroundPlantNames = ["grass", "flower", "fern", "clover", "weed"];

    public static IReadOnlyList<GroundCoverCatalogEntry> Catalog(AssetPaths assets)
    {
        string directory = Path.Combine(assets.Root, "objects", "flora");
        if (!Directory.Exists(directory)) return [];
        var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(SupportsExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return files
            // Prefer the authored SCO over its binary companion; SCB-only assets remain usable.
            .Where(file => !Path.GetExtension(file).Equals(".scb", StringComparison.OrdinalIgnoreCase) ||
                !files.Contains(Path.ChangeExtension(file, ".sco")))
            .Where(file => !Regex.IsMatch(Path.GetFileNameWithoutExtension(file), @"(?:^|[_\-.])lod\d+$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .Select(file =>
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string category = name.Contains("kamen", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("stone", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("rock", StringComparison.OrdinalIgnoreCase) ? "Камни" : "Трава";
                return new GroundCoverCatalogEntry(Path.GetRelativePath(assets.Root, file).Replace('\\', '/'), name, category);
            })
            .Where(entry => !entry.Name.Contains("tree", StringComparison.OrdinalIgnoreCase) &&
                !entry.Name.Contains("bush", StringComparison.OrdinalIgnoreCase))
            .Where(entry => entry.File.StartsWith("objects/flora/grass/", StringComparison.OrdinalIgnoreCase) ||
                entry.File.StartsWith("objects/flora/grass_custom/", StringComparison.OrdinalIgnoreCase) ||
                entry.Name.StartsWith("field_", StringComparison.OrdinalIgnoreCase) || entry.Category == "Камни" ||
                GroundPlantNames.Any(word =>
                    entry.Name.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(entry => entry.Category).ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.File, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    internal static bool SupportsExtension(string file) => Path.GetExtension(file).ToLowerInvariant() is
        ".sco" or ".scb" or ".model";
}
