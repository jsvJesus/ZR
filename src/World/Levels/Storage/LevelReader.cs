using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Levels.Storage;

public static class LevelReader
{
    public static async Task<LoadedLevel> ReadAsync(string manifestPath, CancellationToken cancellationToken = default)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        var manifest = await LevelJson.ReadAsync<LevelManifest>(manifestPath, cancellationToken);
        if (manifest.Format != "ZoneReborn.Level" || manifest.Version != 1)
            throw new InvalidDataException("Неподдерживаемый формат уровня.");
        var terrain = manifest.Terrain ?? throw new InvalidDataException("Отсутствует описание Terrain.");
        if (terrain.TileSize != 256 || terrain.SamplesX < 256 || terrain.SamplesZ < 256 ||
            terrain.SamplesX > 32768 || terrain.SamplesZ > 32768 ||
            terrain.SamplesX % 256 != 0 || terrain.SamplesZ % 256 != 0 ||
            !float.IsFinite(terrain.CellSize) || terrain.CellSize <= 0 ||
            !float.IsFinite(terrain.HeightScale) || terrain.HeightScale <= 0 ||
            !float.IsFinite(terrain.HeightOffset))
            throw new InvalidDataException("Некорректные размеры или масштаб Terrain.");
        if (manifest.Tiles is null || manifest.Tiles.Count != terrain.TilesX * terrain.TilesZ)
            throw new InvalidDataException("Неполный список тайлов Terrain.");

        var coordinates = new HashSet<(int, int)>();
        foreach (var tile in manifest.Tiles)
        {
            if (tile.X < 0 || tile.Z < 0 || tile.X >= terrain.TilesX || tile.Z >= terrain.TilesZ ||
                !coordinates.Add((tile.X, tile.Z)) ||
                !float.IsFinite(tile.MinimumHeight) || !float.IsFinite(tile.MaximumHeight) ||
                tile.MinimumHeight > tile.MaximumHeight)
                throw new InvalidDataException("Некорректный тайл Terrain.");
            if (!File.Exists(AssetPathResolver.Resolve(directory, tile.File)))
                throw new InvalidDataException($"Отсутствует тайл Terrain: {tile.X}, {tile.Z}.");
        }
        var layers = await LevelJson.ReadAsync<List<TerrainLayer>>(
            AssetPathResolver.Resolve(directory, manifest.LayersFile), cancellationToken);
        if (layers.Count is < 1 or > 255 || layers.Where((layer, index) => layer.Index != index).Any())
            throw new InvalidDataException("Некорректный порядок слоёв Terrain.");
        foreach (var layer in layers)
        {
            if (string.IsNullOrWhiteSpace(layer.Name) ||
                string.IsNullOrWhiteSpace(layer.DiffuseTexture) || string.IsNullOrWhiteSpace(layer.NormalTexture) ||
                !float.IsFinite(layer.ScaleU) || layer.ScaleU <= 0 ||
                !float.IsFinite(layer.ScaleV) || layer.ScaleV <= 0 ||
                !float.IsFinite(layer.NormalUvMultiplier) || layer.NormalUvMultiplier <= 0 ||
                !float.IsFinite(layer.Specular))
                throw new InvalidDataException("Некорректные параметры слоя Terrain.");
        }
        if (!File.Exists(AssetPathResolver.Resolve(directory, manifest.OverviewFile)))
            throw new InvalidDataException("Отсутствует обзорная геометрия Terrain.");
        var camera = manifest.Camera;
        if (camera == null || !float.IsFinite(camera.X) || !float.IsFinite(camera.Y) || !float.IsFinite(camera.Z) ||
            !float.IsFinite(camera.Yaw) || !float.IsFinite(camera.Pitch) || !float.IsFinite(camera.Speed))
            throw new InvalidDataException("Некорректная камера уровня.");
        return new LoadedLevel(directory, manifest, layers);
    }
}

