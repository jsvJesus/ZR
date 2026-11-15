using System.Runtime.CompilerServices;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Vegetation;

/// <summary>Resolves references retained inside caches after the library is reorganized.</summary>
internal sealed class SpeedTreePaths
{
    private static readonly ConditionalWeakTable<AssetPaths, SpeedTreePaths> Libraries = new();
    private readonly AssetPaths _assets;
    private readonly Lazy<Dictionary<string, string[]>> _files;

    private SpeedTreePaths(AssetPaths assets)
    {
        _assets = assets;
        _files = new(() =>
        {
            string root = Path.Combine(assets.Root, "speedtree");
            return (Directory.Exists(root) ? Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories) : [])
                .GroupBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        });
    }

    internal static SpeedTreePaths For(AssetPaths assets) => Libraries.GetValue(assets, value => new(value));
    internal static void Refresh(AssetPaths assets) => Libraries.Remove(assets);

    internal string Resolve(string reference, string? modelFile = null)
    {
        reference = reference.Replace('\\', '/');
        string original = AssetPathResolver.Resolve(_assets.Root, reference);
        if (File.Exists(original)) return original;

        if (modelFile != null)
        {
            string sibling = Path.Combine(Path.GetDirectoryName(modelFile)!, Path.GetFileName(reference));
            if (File.Exists(sibling)) return sibling;
        }

        if (!_files.Value.TryGetValue(Path.GetFileName(reference), out var candidates)) return original;
        var ranked = candidates.Select(path => (Path: path, Score: CommonSuffix(reference,
            Path.GetRelativePath(_assets.Root, path).Replace('\\', '/')))).ToArray();
        int best = ranked.Max(item => item.Score);
        var matches = ranked.Where(item => item.Score == best).ToArray();
        if (matches.Length == 1) return matches[0].Path;
        throw new InvalidDataException($"Неоднозначный путь SpeedTree: {reference}. Найдено файлов: {matches.Length}.");
    }

    private static int CommonSuffix(string left, string right)
    {
        var a = left.Split('/');
        var b = right.Split('/');
        int count = 0;
        while (count < a.Length && count < b.Length &&
               string.Equals(a[^(count + 1)], b[^(count + 1)], StringComparison.OrdinalIgnoreCase)) count++;
        return count;
    }
}
