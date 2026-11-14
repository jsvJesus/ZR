using System.Text.Json;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Storage;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Vegetation;

public static class VegetationScatterStorage
{
    public const string FileName = "vegetation.generated.json";
    public const int MaximumGenerated = 20000;

    public static void NormalizeTileAssociations(
        VegetationScene scene,
        TerrainDescriptor terrain)
    {
        float tileSize = terrain.TileSize * terrain.CellSize;

        for (int i = 0; i < scene.Generated.Count; i++)
        {
            var item = scene.Generated[i];
            var tree = item.Tree;

            if (!terrain.Contains(tree.X, tree.Z))
            {
                continue;
            }

            int tileX = Math.Clamp(
                (int)((tree.X - terrain.OriginX) / tileSize),
                0,
                terrain.TilesX - 1);

            int tileZ = Math.Clamp(
                (int)((tree.Z - terrain.OriginZ) / tileSize),
                0,
                terrain.TilesZ - 1);

            if (item.TileX == tileX && item.TileZ == tileZ)
            {
                continue;
            }

            // Expansion shifts tile indices while trees keep their world positions.
            scene.Generated[i] = item with { TileX = tileX, TileZ = tileZ };
            scene.IsDirty = true;
        }
    }

    public static async Task LoadAsync(
        string directory,
        AssetPaths assets,
        VegetationScene scene,
        CancellationToken token = default)
    {
        string path = Path.Combine(directory, FileName);

        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            if (new FileInfo(path).Length > 32 * 1024 * 1024)
            {
                throw new InvalidDataException(
                    "Файл генерации растительности слишком большой.");
            }

            var file = await LevelJson.ReadAsync<Document>(
                path,
                token);

            if (file.Format != "ZoneReborn.Vegetation.Generated" ||
                file.Version != 1 ||
                file.Rules == null ||
                file.Instances == null ||
                file.Rules.Count > 1024 ||
                file.Instances.Count > MaximumGenerated)
            {
                throw new InvalidDataException(
                    "Неподдерживаемый файл генерации растительности.");
            }

            var ruleIds = new HashSet<Guid>();

            foreach (var rule in file.Rules)
            {
                VegetationScatterGenerator.ValidateRule(rule);

                if (!ruleIds.Add(rule.Id))
                {
                    throw new InvalidDataException(
                        "Повторяющийся ID правила растительности.");
                }
            }

            var instanceIds = new HashSet<Guid>();

            foreach (var item in file.Instances)
            {
                if (!ruleIds.Contains(item.RuleId) ||
                    item.TileX < 0 ||
                    item.TileZ < 0)
                {
                    throw new InvalidDataException(
                        "Некорректная привязка экземпляра к правилу.");
                }

                VegetationStorage.Validate(item.Tree);

                if (!instanceIds.Add(item.Tree.Id))
                {
                    throw new InvalidDataException(
                        "Повторяющийся ID экземпляра.");
                }
            }

            var missing = file.Instances
                .Select(item => item.Tree.Model)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(model => !scene.Assets.ContainsKey(model))
                .ToArray();

            var loaded = new Dictionary<string, TreeAsset>(
                StringComparer.OrdinalIgnoreCase);

            foreach (string model in missing)
            {
                token.ThrowIfCancellationRequested();

                loaded[model] = await Task.Run(
                    () => SpeedTreeReader.Read(assets, model, token),
                    token);
            }

            foreach (var pair in loaded)
            {
                scene.Assets[pair.Key] = pair.Value;
            }

            scene.ScatterRules.AddRange(file.Rules);
            scene.Generated.AddRange(file.Instances);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            scene.ScatterRules.Clear();
            scene.Generated.Clear();

            scene.CanSave = false;

            scene.Warnings.Add(
                $"{FileName}: {exception.Message}. " +
                "Исходный файл сохранён, перезапись отключена.");
        }
    }

    public static async Task SaveAsync(
        string directory,
        VegetationScene scene,
        CancellationToken token = default)
    {
        if (!scene.CanSave)
        {
            throw new InvalidOperationException(
                "Сохранение отключено из-за ошибки чтения растительности.");
        }

        if (scene.Generated.Count > MaximumGenerated)
        {
            throw new InvalidOperationException(
                "Превышен лимит сгенерированных экземпляров.");
        }

        foreach (var rule in scene.ScatterRules)
        {
            VegetationScatterGenerator.ValidateRule(rule);
        }

        foreach (var item in scene.Generated)
        {
            VegetationStorage.Validate(item.Tree);
        }

        var file = new Document
        {
            Rules = scene.ScatterRules.ToList(),
            Instances = scene.Generated.ToList()
        };

        string temporary = Path.Combine(
            directory,
            $".vegetation-generated-{Guid.NewGuid():N}.tmp");

        try
        {
            await LevelJson.WriteAsync(temporary, file, token);

            File.Move(
                temporary,
                Path.Combine(directory, FileName),
                true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private sealed class Document
    {
        public string Format { get; init; } =
            "ZoneReborn.Vegetation.Generated";

        public int Version { get; init; } = 1;

        public List<VegetationScatterRule> Rules { get; init; } = [];

        public List<GeneratedVegetationInstance> Instances { get; init; } = [];
    }
}
