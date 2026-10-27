using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Terrain.Materials
{
    public static class MaterialPathResolver
    {
        public static string FromSource(AssetPaths assets, string sourcePath)
        {
            string normalized = sourcePath.Replace('\\', '/');
            const string prefix = "data/terraindata/materials/";
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Неподдерживаемый путь материала: {sourcePath}.");
            string relative = "materials/" + normalized[prefix.Length..];
            string fullPath = AssetPathResolver.Resolve(assets.Root, relative);
            if (!File.Exists(fullPath)) throw new FileNotFoundException($"Не найдена текстура материала: {relative}.", fullPath);
            return relative;
        }
    }
}

