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
    private const string RoadFolder = "textures/roads/";
    private const string ShoulderFolder = "materials/new_terrain/";
    private const string ShoulderPrefix = "Cali_Dirt_03_";

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
            .Where(material =>
                material.File.StartsWith(
                    RoadFolder,
                    StringComparison.OrdinalIgnoreCase))
            .Where(material => category == RoadCategory.Dirt
                ? material.IsDirt
                : !material.IsDirt)
            .OrderBy(material => material.File)
            .ToArray();
    }

    public RoadMaterialInfo[] GetShoulderMaterials()
    {
        return _materials.Values
            .Where(material =>
                material.File.StartsWith(
                    ShoulderFolder + ShoulderPrefix,
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(material => material.File)
            .ToArray();
    }

    public static RoadMaterialCatalog Scan(AssetPaths assets)
    {
        var catalog = new RoadMaterialCatalog();

        catalog.ScanFolder(assets, RoadFolder, false);
        catalog.ScanFolder(assets, ShoulderFolder, true);

        return catalog;
    }

    private void ScanFolder(
        AssetPaths assets,
        string relative,
        bool shoulderOnly)
    {
        string directory = Path.Combine(
            assets.Root,
            relative.Replace('/', Path.DirectorySeparatorChar));

        if (!Directory.Exists(directory))
        {
            return;
        }

        var files = Directory.EnumerateFiles(
                directory,
                "*",
                SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetExtension(path).Equals(
                ".dds",
                StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                Path.GetFileName,
                path => path,
                StringComparer.OrdinalIgnoreCase);

        foreach (var pair in files)
        {
            string name = Path.GetFileNameWithoutExtension(pair.Key);

            if (shoulderOnly &&
                !name.StartsWith(
                    ShoulderPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Match match = Regex.Match(
                name,
                @"^(.*)_(D(?:_DMG)?|Diffuse|Albedo)$",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

            if (!match.Success)
            {
                continue;
            }

            string baseName = match.Groups[1].Value;

            string[] normalCandidates =
            [
                baseName + "_N.dds",
                baseName + "_N_DMG.dds",
                baseName + "_Normal.dds"
            ];

            string normal = normalCandidates
                .Where(files.ContainsKey)
                .Select(candidate => files[candidate])
                .FirstOrDefault() ?? "";

            bool dirt = shoulderOnly ||
                name.Contains("dirt", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("mud", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("gravel", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("tanks_road", StringComparison.OrdinalIgnoreCase);

            string file = relative + pair.Key;

            _materials[file] = new RoadMaterialInfo(
                file,
                pair.Value,
                normal,
                dirt);
        }
    }
}
