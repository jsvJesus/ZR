using System.Text.RegularExpressions;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Scene;

namespace ZoneReborn.World.Roads;

public enum RoadModelAxis
{
    X,
    Z
}

public sealed record RoadModelInfo(
    string File,
    RoadCategory Category,
    SceneBounds Bounds,
    RoadModelAxis Axis)
{
    public float Length => Axis == RoadModelAxis.X
        ? Bounds.Maximum.X - Bounds.Minimum.X
        : Bounds.Maximum.Z - Bounds.Minimum.Z;

    public override string ToString()
    {
        return Path.GetFileNameWithoutExtension(File);
    }
}

public sealed class RoadCatalog
{
    private readonly Dictionary<string, RoadModelInfo> _models =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<RoadModelInfo> Models => _models.Values;

    public List<string> Warnings { get; } = [];

    public bool TryGet(string file, out RoadModelInfo info)
    {
        return _models.TryGetValue(file, out info!);
    }

    public RoadModelInfo[] GetModels(RoadCategory category)
    {
        return _models.Values
            .Where(model => model.Category == category)
            .OrderBy(model => model.File, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static RoadCatalog Scan(AssetPaths assets)
    {
        var catalog = new RoadCatalog();

        catalog.ScanFolder(
            assets,
            "objects/roads_segments");

        catalog.ScanFolder(
            assets,
            "objects/props/railroads");

        return catalog;
    }

    private void ScanFolder(AssetPaths assets, string relative)
    {
        string folder = Path.Combine(
            assets.Root,
            relative.Replace('/', Path.DirectorySeparatorChar));

        if (!Directory.Exists(folder))
        {
            Warnings.Add($"Каталог не найден: {relative}");
            return;
        }

        foreach (string path in Directory.EnumerateFiles(
                     folder,
                     "*.model",
                     SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileNameWithoutExtension(path);

            if (!TryCategory(relative, name, out RoadCategory category))
            {
                continue;
            }

            string file = relative + "/" + Path.GetFileName(path);

            try
            {
                SceneBounds bounds = SourceModelReader.ReadBounds(
                    assets,
                    file);

                float extentX = bounds.Maximum.X - bounds.Minimum.X;
                float extentZ = bounds.Maximum.Z - bounds.Minimum.Z;

                RoadModelAxis axis = category == RoadCategory.Railway
                    ? extentX >= extentZ
                        ? RoadModelAxis.X
                        : RoadModelAxis.Z
                    : RoadModelAxis.X;

                float length = axis == RoadModelAxis.X
                    ? extentX
                    : extentZ;

                if (!float.IsFinite(length) || length <= 0.05f)
                {
                    Warnings.Add($"Некорректная длина модели: {file}");
                    continue;
                }

                _models.Add(
                    file,
                    new RoadModelInfo(
                        file,
                        category,
                        bounds,
                        axis));
            }
            catch (Exception exception) when (
                exception is IOException or
                UnauthorizedAccessException or
                InvalidDataException or
                FormatException or
                OverflowException or
                ArgumentException or
                System.Xml.XmlException)
            {
                Warnings.Add($"{file}: {exception.Message}");
            }
        }
    }

    private static bool TryCategory(
        string folder,
        string name,
        out RoadCategory category)
    {
        if (folder.Equals(
                "objects/props/railroads",
                StringComparison.OrdinalIgnoreCase))
        {
            category = RoadCategory.Railway;

            return Regex.IsMatch(
                name,
                @"^railroad_straight_.*_lod1$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        if (Regex.IsMatch(
                name,
                @"^(asphalt\d+[ab]?|black\d+|concrete\d+)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            category = RoadCategory.Automobile;
            return true;
        }

        if (Regex.IsMatch(
                name,
                @"^(dirt\d+|sand\d+)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            category = RoadCategory.Dirt;
            return true;
        }

        if (Regex.IsMatch(
                name,
                @"^railscave\d+$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            category = RoadCategory.Railway;
            return true;
        }

        category = default;
        return false;
    }
}
