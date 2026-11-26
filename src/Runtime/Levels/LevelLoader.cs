using System.IO;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Levels.Storage;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Scene;

namespace Runtime.Levels;

public sealed class LevelLoader
{
    public AssetPaths Assets { get; }

    public LevelLoader()
        : this(AssetPaths.FromApplication())
    {
    }

    public LevelLoader(AssetPaths assets)
    {
        ArgumentNullException.ThrowIfNull(assets);

        Assets = assets;
    }

    public async Task<LoadedLevel> OpenAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var level = await LevelReader.ReadAsync(path, cancellationToken);

        string source = AssetPathResolver.Resolve(
            Assets.ImportLevels,
            level.Manifest.Name);

        var vegetation = await ZoneReborn.World.Vegetation.VegetationStorage.LoadAsync(
            level.Directory,
            Assets,
            cancellationToken);

        await ZoneReborn.World.Vegetation.VegetationScatterStorage.LoadAsync(
            level.Directory,
            Assets,
            vegetation,
            cancellationToken);

        ZoneReborn.World.Vegetation.VegetationScatterStorage.NormalizeTileAssociations(
            vegetation,
            level.Manifest.Terrain);

        var objects = level.Manifest.Terrain.ImportedFromMesh
            ? new SceneData(Assets, [], sourceName: level.Manifest.Name)
            : await Task.Run(
                () => SourceSceneReader.Read(
                    source,
                    Assets,
                    cancellationToken),
                cancellationToken);

        await SceneEditStorage.LoadAsync(
            level.Directory,
            objects,
            cancellationToken);

        var roads = await RoadStorage.LoadAsync(
            level.Directory,
            level.Manifest.Name,
            cancellationToken);

        level = level with
        {
            Vegetation = vegetation,
            Objects = objects,
            Roads = roads,
            GroundCover = await ZoneReborn.World.Vegetation.GroundCoverStorage.LoadAsync(
                level.Directory, cancellationToken)
        };

        objects.AttachTerrain(level);
        return level;
    }

}