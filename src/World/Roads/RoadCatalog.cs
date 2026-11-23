using System.Numerics;
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

    public float Width => Axis == RoadModelAxis.X
        ? Bounds.Maximum.Z - Bounds.Minimum.Z
        : Bounds.Maximum.X - Bounds.Minimum.X;

    public float Height => Bounds.Maximum.Y - Bounds.Minimum.Y;

    public bool HasDeepFoundation =>
        Category == RoadCategory.Railway && Height > 2f;

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
                     "*",
                     SearchOption.AllDirectories))
        {
            if (!Path.GetExtension(path).Equals(
                    ".model",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string file = Path.GetRelativePath(
                    assets.Root,
                    path)
                .Replace('\\', '/');

            string name = Path.GetFileNameWithoutExtension(path);

            if (!TryCategory(file, name, out RoadCategory category))
            {
                continue;
            }

            try
            {
                SceneBounds bounds = ReadModelBounds(
                    assets,
                    file);

                RoadModelAxis axis = RoadModelAxis.X;

                var info = new RoadModelInfo(
                    file,
                    category,
                    bounds,
                    axis);

                if (!float.IsFinite(info.Length) ||
                    !float.IsFinite(info.Width) ||
                    !float.IsFinite(info.Height) ||
                    info.Length <= 0.05f ||
                    info.Width <= 0.05f)
                {
                    Warnings.Add(
                        $"Некорректные размеры модели: {file}");
                    continue;
                }

                _models[file] = info;
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
                Warnings.Add(
                    $"Модель {file}: {exception.Message}");
            }
        }
    }

    internal static SceneBounds ReadModelBounds(
        AssetPaths assets,
        string file)
    {
        try
        {
            return SourceModelReader.ReadBounds(
                assets,
                file);
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
            string path = AssetPathResolver.Resolve(
                assets.Root,
                file);

            var section = BigWorldSection.Read(
                path,
                CancellationToken.None);

            var box = section.Child("visibilityBox")
                ?? throw new InvalidDataException(
                    $"Не найдены границы модели: {file}",
                    exception);

            float[] minimum = box.Child("min")?.Floats()
                ?? throw new InvalidDataException(
                    "Не найдена минимальная граница модели.");

            float[] maximum = box.Child("max")?.Floats()
                ?? throw new InvalidDataException(
                    "Не найдена максимальная граница модели.");

            if (minimum.Length != 3 ||
                maximum.Length != 3)
            {
                throw new InvalidDataException(
                    "Некорректные границы модели.");
            }

            var min = new Vector3(
                minimum[0],
                minimum[1],
                minimum[2]);

            var max = new Vector3(
                maximum[0],
                maximum[1],
                maximum[2]);

            if (min.X > max.X ||
                min.Y > max.Y ||
                min.Z > max.Z)
            {
                throw new InvalidDataException(
                    "Некорректный порядок границ модели.");
            }

            return new SceneBounds(min, max);
        }
    }

    private static bool TryCategory(
        string file,
        string name,
        out RoadCategory category)
    {
        if (file.StartsWith(
                "objects/props/railroads/",
                StringComparison.OrdinalIgnoreCase))
        {
            category = RoadCategory.Railway;

            return Regex.IsMatch(
                name,
                @"^railroad_straight_.*_lod1$",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);
        }

        if (!file.StartsWith(
                "objects/roads_segments/",
                StringComparison.OrdinalIgnoreCase))
        {
            category = default;
            return false;
        }

        if (Regex.IsMatch(
                name,
                @"^(asphalt\d+[ab]?|black\d+(?:_x2)?|concrete\d+|road_very_dirty)$",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant))
        {
            category = RoadCategory.Automobile;
            return true;
        }

        if (Regex.IsMatch(
                name,
                @"^(dirt\d+|sand\d+)$",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant))
        {
            category = RoadCategory.Dirt;
            return true;
        }

        if (Regex.IsMatch(
                name,
                @"^railscave\d+$",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant))
        {
            category = RoadCategory.Railway;
            return true;
        }

        category = default;
        return false;
    }
}
