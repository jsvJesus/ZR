namespace ZoneReborn.World.Assets;

public static class AssetPathResolver
{
    public static string Resolve(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException("Ожидается относительный путь ресурса.");

        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!path.StartsWith(fullRoot, comparison))
            throw new InvalidDataException("Путь ресурса выходит за пределы каталога.");
        return path;
    }
}

