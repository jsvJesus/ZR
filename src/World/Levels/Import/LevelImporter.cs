using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Levels.Storage;
using ZoneReborn.World.Terrain.Import.Binary;
using ZoneReborn.World.Terrain.Import;
using ZoneReborn.World.Terrain.Import.Configuration;
using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Terrain.Storage;

namespace ZoneReborn.World.Levels.Import;

public sealed class LevelImporter(AssetPaths assets)
{
    public Task<string> ImportAsync(
        string name, IProgress<LevelImportProgress>? progress = null, CancellationToken cancellationToken = default) =>
        Task.Run(() => ImportCoreAsync(name, progress, cancellationToken), cancellationToken);

    private async Task<string> ImportCoreAsync(
        string name, IProgress<LevelImportProgress>? progress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name) || name is "." or ".." ||
            name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains('/') || name.Contains('\\'))
            throw new InvalidDataException("Некорректное имя уровня.");
        string source = AssetPathResolver.Resolve(assets.ImportLevels, name);
        string destination = AssetPathResolver.Resolve(assets.Levels, name);
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException("Не найден исходный уровень.");
        if (Directory.Exists(destination) || File.Exists(destination))
            throw new IOException($"Уровень «{name}» уже существует. Импорт не перезаписывает готовые уровни.");
        var configuration = SourceTerrainConfigurationReader.Read(source, assets);
        var terrain = configuration.Terrain;
        var reader = new SourceTerrainReader(configuration.Directory, terrain, configuration.Layers.Count);
        Directory.CreateDirectory(assets.Levels);
        string staging = AssetPathResolver.Resolve(assets.Levels, $".{name}.import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(staging, "terrain", "tiles"));
        try
        {
            var tiles = new List<TerrainTileReference>();
            var overview = new TerrainOverviewBuilder(terrain);
            int total = terrain.TilesX * terrain.TilesZ;
            progress?.Report(new(name, 0, total));
            for (int z = 0; z < terrain.TilesZ; z++)
            for (int x = 0; x < terrain.TilesX; x++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var tile = reader.ReadTile(x, z);
                overview.Add(tile);
                string file = $"terrain/tiles/{x}_{z}.terrain";
                TerrainTileWriter.Write(AssetPathResolver.Resolve(staging, file), tile);
                tiles.Add(new TerrainTileReference(x, z,
                    terrain.DecodeHeight(tile.Heights.Min()), terrain.DecodeHeight(tile.Heights.Max()), file));
                progress?.Report(new(name, tiles.Count, total));
            }
            var manifest = new LevelManifest
            {
                Name = name, Terrain = terrain, Tiles = tiles,
                Camera = CreateCamera(terrain, tiles)
            };
            TerrainOverviewWriter.Write(Path.Combine(staging, "terrain", "overview.terrain"), overview.Build());
            await LevelJson.WriteAsync(Path.Combine(staging, "layers.json"), configuration.Layers, cancellationToken);
            await LevelJson.WriteAsync(Path.Combine(staging, "level.json"), manifest, cancellationToken);
            await LevelJson.WriteAsync(Path.Combine(staging, "import.json"),
                new LevelImportReport(1, $"import/levels/{name}", DateTimeOffset.UtcNow, tiles.Count,
                    configuration.Layers.Count,
                    ["Импортированы высоты, исходные нормали, маски и слои Terrain.",
                     "Дороги, водные плоскости и объекты сцены в этот импорт не входят.",
                     "Модель освещения редактора отличается от исходного движка."]), cancellationToken);
            string minimap = Path.Combine(source, "minimap.dds");
            if (File.Exists(minimap)) File.Copy(minimap, Path.Combine(staging, "minimap.dds"));
            cancellationToken.ThrowIfCancellationRequested();
            Directory.Move(staging, destination);
            return Path.Combine(destination, "level.json");
        }
        finally
        {
            // This unique staging directory is the only directory owned by this operation.
            if (Directory.Exists(staging))
            {
                foreach (string file in Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories))
                    File.Delete(file);
                foreach (string directory in Directory.EnumerateDirectories(staging, "*", SearchOption.AllDirectories)
                    .OrderByDescending(directory => directory.Length))
                    Directory.Delete(directory);
                Directory.Delete(staging);
            }
        }
    }

    private static LevelCamera CreateCamera(TerrainDescriptor terrain, List<TerrainTileReference> tiles)
    {
        int centerX = terrain.TilesX / 2, centerZ = terrain.TilesZ / 2;
        float ground = tiles.First(tile => tile.X == centerX && tile.Z == centerZ).MaximumHeight;
        return new(terrain.WorldSizeX * 0.5f, ground + 150, terrain.WorldSizeZ * 0.5f - 150,
            0, -0.5f, 80);
    }
}

