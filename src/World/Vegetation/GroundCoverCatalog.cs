using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Vegetation;

public sealed record GroundCoverCatalogEntry(string File, string Name, string Category)
{
    public override string ToString() => $"{Name} — {Category}";
}

public static class GroundCoverCatalog
{
    public static IReadOnlyList<GroundCoverCatalogEntry> Catalog(AssetPaths assets)
    {
        string directory = Path.Combine(assets.Root, "objects", "flora");
        if (!Directory.Exists(directory)) return [];
        return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(file => Path.GetExtension(file).Equals(".model", StringComparison.OrdinalIgnoreCase))
            .Select(file =>
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string category = name.Contains("kamen", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("stone", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("rock", StringComparison.OrdinalIgnoreCase) ? "Камни" : "Трава";
                return new GroundCoverCatalogEntry(Path.GetRelativePath(assets.Root, file).Replace('\\', '/'), name, category);
            })
            .Where(entry => entry.File.StartsWith("objects/flora/grass/", StringComparison.OrdinalIgnoreCase) ||
                entry.File.StartsWith("objects/flora/grass_custom/", StringComparison.OrdinalIgnoreCase) ||
                entry.Name.StartsWith("field_", StringComparison.OrdinalIgnoreCase) || entry.Category == "Камни")
            .OrderBy(entry => entry.Category).ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
