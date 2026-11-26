using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters;
using ZoneReborn.World.Scene;

namespace ZoneReborn.World.Actors
{
    public sealed record ActorItemDefinition(
        int TypeId,
        int CategoryId,
        string Category,
        string Name)
    {
        public string ModelPath { get; init; } = "";
        public override string ToString()
        {
            return $"[{TypeId}] {Name}";
        }
    }

    public sealed class ActorItemCatalog
    {
        public static readonly string[] Categories =
        [
            "AMMO",
            "ARTIFACT",
            "BUFF",
            "CLOTH",
            "EXPLOSION",
            "GADGET",
            "LOOT",
            "PART",
            "WEAPON"
        ];

        public IReadOnlyList<ActorItemDefinition> Items { get; }
        public IReadOnlyList<string> Warnings { get; }

        private ActorItemCatalog(
            IReadOnlyList<ActorItemDefinition> items,
            IReadOnlyList<string> warnings)
        {
            Items = items;
            Warnings = warnings;
        }

        private static string ReadModelPath(
            AssetPaths assets,
            Dictionary<object, object?> data)
        {
            var candidates = new List<string>();

            if (data.TryGetValue("OnFlorModel", out object? floorModel) &&
                floorModel is string floorPath)
            {
                candidates.Add(floorPath);
            }

            if (data.TryGetValue("ModelPath", out object? modelPath) &&
                modelPath is string directPath)
            {
                candidates.Add(directPath);
            }

            if (data.TryGetValue("ModelNames", out object? rawModels) &&
                rawModels is Dictionary<object, object?> models)
            {
                candidates.AddRange(
                    models
                        .OrderBy(pair => Equals(pair.Key, 0L) ? 0 : 1)
                        .Select(pair => pair.Value)
                        .OfType<string>());
            }

            string fallback = "";

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                string path = candidate
                    .Trim()
                    .Replace('\\', '/');

                if (!path.EndsWith(
                        ".model",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    path = SourceModelReader.Normalize(path);

                    string fullPath =
                        SourceModelReader.Resolve(assets, path);

                    if (string.IsNullOrEmpty(fallback))
                    {
                        fallback = path;
                    }

                    if (File.Exists(fullPath))
                    {
                        return path;
                    }
                }
                catch (Exception exception) when (
                    exception is InvalidDataException ||
                    exception is ArgumentException ||
                    exception is NotSupportedException)
                {
                    continue;
                }
            }

            return fallback;
        }

        public static ActorItemCatalog Read(
            AssetPaths assets,
            CancellationToken cancellationToken = default)
        {
            string directory = Path.GetFullPath(
                Path.Combine(
                    assets.Root,
                    "..",
                    "..",
                    "sdk",
                    "scripts",
                    "common",
                    "data",
                    "items_pyson"));

            var items = new List<ActorItemDefinition>();
            var warnings = new List<string>();

            foreach (string category in Categories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string path = Path.Combine(
                    directory,
                    $"{category}.pyson");

                try
                {
                    object? root = PysonLiteralReader.Read(
                        path,
                        cancellationToken);

                    if (root is not List<object?> entries)
                    {
                        throw new InvalidDataException(
                            $"Некорректный каталог {category}.");
                    }

                    foreach (object? entry in entries)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (entry is not Dictionary<object, object?> data ||
                            !data.TryGetValue("TypeID", out object? rawId) ||
                            rawId is not long id ||
                            id <= 0 ||
                            id > int.MaxValue)
                        {
                            continue;
                        }

                        int categoryId = 0;

                        if (data.TryGetValue("Category", out object? rawCategory) &&
                            rawCategory is long number &&
                            number >= 0 &&
                            number <= int.MaxValue)
                        {
                            categoryId = (int)number;
                        }

                        string name = ReadName(data, (int)id);

                        items.Add(new ActorItemDefinition(
                            (int)id,
                            categoryId,
                            category,
                            name)
                        {
                            ModelPath = ReadModelPath(assets, data)
                        });
                    }
                }
                catch (Exception exception) when (
                    exception is IOException ||
                    exception is InvalidDataException ||
                    exception is UnauthorizedAccessException)
                {
                    warnings.Add(
                        $"{category}: {exception.Message}");
                }
            }

            var result = items
                .DistinctBy(item => (item.Category, item.TypeId))
                .OrderBy(item => item.Category)
                .ThenBy(item => item.Name)
                .ThenBy(item => item.TypeId)
                .ToArray();

            return new ActorItemCatalog(result, warnings);
        }

        public ActorItemDefinition[] FindCategory(string category)
        {
            return Items
                .Where(item => item.Category == category)
                .ToArray();
        }

        private static string ReadName(
            Dictionary<object, object?> data,
            int typeId)
        {
            foreach (string key in new[]
            {
                "nameRU",
                "nameEN",
                "_script_name_"
            })
            {
                if (data.TryGetValue(key, out object? value) &&
                    value is string text &&
                    !string.IsNullOrWhiteSpace(text))
                {
                    return text.Split(
                        '|',
                        StringSplitOptions.TrimEntries |
                        StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault() ?? text;
                }
            }

            return $"Item {typeId}";
        }
    }
}
