using System.Text.RegularExpressions;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Roads;

public sealed record RoadMaterialInfo(
    string File,
    string Diffuse,
    string Normal,
    bool IsDirt)
{
    public override string ToString()
    {
        return Path.GetFileNameWithoutExtension(File);
    }
}

public sealed class RoadMaterialCatalog
{
    private readonly Dictionary<string, RoadMaterialInfo> _materials =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<RoadMaterialInfo> Materials =>
        _materials.Values;

    public bool TryGet(string file, out RoadMaterialInfo material)
    {
        return _materials.TryGetValue(file, out material!);
    }

    public RoadMaterialInfo[] GetMaterials(RoadCategory category)
    {
        return _materials.Values
            .Where(material => category == RoadCategory.Dirt
                ? material.IsDirt
                : !material.IsDirt)
            .OrderBy(material => material.File)
            .ToArray();
    }

    public RoadMaterialInfo[] GetShoulderMaterials()
    {
        return _materials.Values
            .Where(material => material.IsDirt)
            .OrderBy(material => material.File)
            .ToArray();
    }

    public static RoadMaterialCatalog Scan(AssetPaths assets)
    {
        var catalog = new RoadMaterialCatalog();

        string directory = Path.Combine(
            assets.Root,
            "textures",
            "roads");

        if (!Directory.Exists(directory))
        {
            return catalog;
        }

        var files = Directory.EnumerateFiles(
                directory,
                "*",
                SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetExtension(path).Equals(
                ".dds",
                StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                path => Path.GetFileName(path),
                path => path,
                StringComparer.OrdinalIgnoreCase);

        foreach (var pair in files)
        {
            string name = Path.GetFileNameWithoutExtension(pair.Key);

            if (!Regex.IsMatch(
                    name,
                    @"_D(?:_DMG)?$",
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant))
            {
                continue;
            }

            string normalName = Regex.Replace(
                name,
                @"_D(?=_DMG$|$)",
                "_N",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant) + ".dds";

            string normal = files.TryGetValue(
                normalName,
                out string? normalPath)
                    ? normalPath
                    : "";

            bool dirt =
                name.Contains("dirt", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("mud", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("gravel", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("tanks_road", StringComparison.OrdinalIgnoreCase);

            string relative = "textures/roads/" + pair.Key;

            catalog._materials.Add(
                relative,
                new RoadMaterialInfo(
                    relative,
                    pair.Value,
                    normal,
                    dirt));
        }

        return catalog;
    }
}
