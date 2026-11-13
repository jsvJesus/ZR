using System.IO;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Import;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Levels.Storage;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Scene;

namespace Editor.Levels;

public sealed class LevelWorkspace
{
    public AssetPaths Assets { get; } = AssetPaths.FromApplication();

    public IReadOnlyList<string> FindSources() =>
        Directory.Exists(Assets.ImportLevels)
            ? Directory.EnumerateDirectories(Assets.ImportLevels)
                .Select(Path.GetFileName).OfType<string>()
                .Where(name => !name.StartsWith('.')).Order(StringComparer.OrdinalIgnoreCase).ToArray()
            : [];

    public bool IsImported(string name) =>
        File.Exists(Path.Combine(AssetPathResolver.Resolve(Assets.Levels, name), "level.json"));

    public Task<string> ImportAsync(string name, IProgress<LevelImportProgress> progress, CancellationToken cancellationToken, float cellSize = 1) =>
        new LevelImporter(Assets).ImportAsync(name, progress, cancellationToken, cellSize);

    public async Task<LoadedLevel> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        var level = await LevelReader.ReadAsync(path, cancellationToken);
        string source = AssetPathResolver.Resolve(Assets.ImportLevels, level.Manifest.Name);
        var roads = level.Manifest.Terrain.ImportedFromMesh ? new RoadNetwork() : await Task.Run(() =>
        {
            var network = SourceRoadReader.Read(source, cancellationToken);
            RoadSurfaceAttachment.ReadSourceOffsets(level, network, cancellationToken);
            return network;
        }, cancellationToken);
        await RoadEditStorage.LoadAsync(level.Directory, roads, cancellationToken);

        var vegetation = await ZoneReborn.World.Vegetation.VegetationStorage.LoadAsync(level.Directory, Assets, cancellationToken);
        await ZoneReborn.World.Vegetation.VegetationScatterStorage.LoadAsync(
            level.Directory,
            Assets,
            vegetation,
            cancellationToken);

        var objects = level.Manifest.Terrain.ImportedFromMesh
            ? new SceneData(Assets, [], sourceName: level.Manifest.Name)
            : await Task.Run(() => SourceSceneReader.Read(source, Assets, cancellationToken), cancellationToken);
        await SceneEditStorage.LoadAsync(level.Directory, objects, cancellationToken);
        return level with { Roads = roads, Vegetation = vegetation, Objects = objects };
    }

    public static async Task SaveCameraAsync(LoadedLevel level, LevelCamera camera)
    {
        string path = Path.Combine(level.Directory, "level.json");
        string temporary = Path.Combine(level.Directory, $".camera-{Guid.NewGuid():N}.tmp");
        level.Manifest.Camera = camera;
        try
        {
            await LevelJson.WriteAsync(temporary, level.Manifest);
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}

