using System.Globalization;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Terrain.Materials;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain.Import.Configuration;

public static class SourceTerrainConfigurationReader
{
    public static SourceTerrainConfiguration Read(string sourceLevel, AssetPaths assets)
    {
        var candidates = Directory.EnumerateDirectories(sourceLevel)
            .Where(directory => File.Exists(Path.Combine(directory, "megatexinfo.bin")))
            .ToArray();
        if (candidates.Length != 1) throw new InvalidDataException("Не найден единственный исходный каталог Terrain.");
        string directory = candidates[0];
        var scripts = Directory.EnumerateFiles(directory, "*.ini").ToArray();
        if (scripts.Length != 1) throw new InvalidDataException("Не найден единственный файл настроек Terrain.");
        var script = TerrainScript.Read(scripts[0]);
        var settings = script.Settings;
        int samplesX = ReadInt(settings, "vert_count_x"), samplesZ = ReadInt(settings, "vert_count_z");
        if (samplesX is < 256 or > 32768 || samplesZ is < 256 or > 32768 ||
            samplesX % 256 != 0 || samplesZ % 256 != 0)
            throw new InvalidDataException("Неподдерживаемые размеры Terrain.");
        var terrain = new TerrainDescriptor
        {
            SamplesX = samplesX, SamplesZ = samplesZ,
            CellSize = ReadFloat(settings, "tile_unit_size", positive: true),
            HeightOffset = ReadFloat(settings, "height_offset"),
            HeightScale = ReadFloat(settings, "height_scale", positive: true),
            FarDiffuseTexture = ResolveOptional(settings, "far_diffuse_texture_name", assets),
            FarNormalTexture = ResolveOptional(settings, "far_normal_texture_name", assets)
        };
        if (ReadInt(settings, "splat_res_u") != samplesX || ReadInt(settings, "splat_res_v") != samplesZ)
            throw new InvalidDataException("Разрешения покрытия и высот Terrain различаются.");
        if (script.Layers.Count is < 1 or > 255) throw new InvalidDataException("Некорректное количество слоёв Terrain.");
        var layers = script.Layers.Select((layer, index) => new TerrainLayer
        {
            Index = index,
            Name = Required(layer, "name").Trim(),
            DiffuseTexture = MaterialPathResolver.FromSource(assets, Required(layer, "map_diffuse")),
            NormalTexture = MaterialPathResolver.FromSource(assets, Required(layer, "map_normal")),
            ScaleU = ReadFloat(layer, "scale_u", positive: true),
            ScaleV = ReadFloat(layer, "scale_v", positive: true),
            NormalUvMultiplier = ReadFloat(layer, "normal_uv_mult", positive: true),
            Specular = ReadFloat(layer, "specular"),
            MaterialType = layer.GetValueOrDefault("mat_type", "")
        }).ToArray();
        return new SourceTerrainConfiguration(directory, terrain, layers);
    }

    private static string Required(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value : throw new InvalidDataException($"Отсутствует поле Terrain: {key}.");

    private static int ReadInt(Dictionary<string, string> values, string key) =>
        int.TryParse(Required(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value : throw new InvalidDataException($"Некорректное целое число Terrain: {key}.");

    private static float ReadFloat(Dictionary<string, string> values, string key, bool positive = false)
    {
        if (!float.TryParse(Required(values, key), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
            !float.IsFinite(value) || (positive && value <= 0))
            throw new InvalidDataException($"Некорректное число Terrain: {key}.");
        return value;
    }

    private static string ResolveOptional(Dictionary<string, string> values, string key, AssetPaths assets) =>
        values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? MaterialPathResolver.FromSource(assets, value) : "";
}

