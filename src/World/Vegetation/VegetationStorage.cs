using System.Text.Json;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Storage;

namespace ZoneReborn.World.Vegetation
{
    public static class VegetationStorage
    {
        public const string FileName = "vegetation.json";

        public static async Task<VegetationScene> LoadAsync(string directory, AssetPaths assets, CancellationToken cancellationToken)
        {
            var scene = new VegetationScene();
            string path = Path.Combine(directory, FileName);
            if (!File.Exists(path)) return scene;
            try
            {
                if (new FileInfo(path).Length > 32 * 1024 * 1024) throw new InvalidDataException("Слишком большой файл растительности.");
                var document = await LevelJson.ReadAsync<Document>(path, cancellationToken);
                if (document.Format != "ZoneReborn.Vegetation" || document.Version != 1 || document.Instances == null || document.Instances.Count > 100_000)
                    throw new InvalidDataException("Неподдерживаемый документ растительности.");
                var ids = new HashSet<Guid>();
                foreach (var tree in document.Instances)
                {
                    Validate(tree);
                    if (!ids.Add(tree.Id)) throw new InvalidDataException("Повторяющийся ID дерева.");
                }
                foreach (string model in document.Instances.Select(t => t.Model).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var asset = await Task.Run(() => SpeedTreeReader.Read(assets, model, cancellationToken), cancellationToken);
                    scene.Assets.Add(model, asset);
                }
                scene.Instances.AddRange(document.Instances);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException or ArgumentException)
            {
                scene.Assets.Clear(); scene.Instances.Clear(); scene.CanSave = false;
                scene.Warnings.Add($"{FileName}: {exception.Message} Исходный файл правок сохранён.");
            }
            return scene;
        }

        public static async Task SaveAsync(string directory, VegetationScene scene)
        {
            if (!scene.CanSave) throw new InvalidOperationException("Предыдущий файл растительности не прочитан; перезапись отключена.");
            var document = new Document { Instances = scene.Instances.Select(t => new TreeInstance
            {
                Id = t.Id, Model = t.Model, X = t.X, Y = t.Y, Z = t.Z, Yaw = t.Yaw, Scale = t.Scale,
                FollowTerrain = t.FollowTerrain, HeightOffset = t.HeightOffset
            }).ToList() };
            foreach (var tree in document.Instances) Validate(tree);
            string temporary = Path.Combine(directory, $".vegetation-{Guid.NewGuid():N}.tmp");
            try
            {
                await LevelJson.WriteAsync(temporary, document);
                File.Move(temporary, Path.Combine(directory, FileName), true);
                scene.IsDirty = false;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        public static void Validate(TreeInstance tree)
        {
            if (tree == null || tree.Id == Guid.Empty || string.IsNullOrWhiteSpace(tree.Model) ||
                !tree.Model.StartsWith("speedtree/", StringComparison.OrdinalIgnoreCase) ||
                !Path.GetExtension(tree.Model).Equals(".ctree", StringComparison.OrdinalIgnoreCase) ||
                !float.IsFinite(tree.X) || !float.IsFinite(tree.Y) || !float.IsFinite(tree.Z) ||
                !float.IsFinite(tree.Yaw) || !float.IsFinite(tree.Scale) || !float.IsFinite(tree.HeightOffset) ||
                MathF.Abs(tree.HeightOffset) > 1e6 || tree.Scale is < 0.05f or > 100 ||
                MathF.Abs(tree.X) > 1e7 || MathF.Abs(tree.Y) > 1e7 || MathF.Abs(tree.Z) > 1e7)
                throw new InvalidDataException("Некорректный экземпляр дерева.");
            _ = AssetPathResolver.Resolve(".", tree.Model);
        }

        public sealed class Document
        {
            public string Format { get; init; } = "ZoneReborn.Vegetation";
            public int Version { get; init; } = 1;
            public List<TreeInstance> Instances { get; init; } = [];
        }
    }
}
