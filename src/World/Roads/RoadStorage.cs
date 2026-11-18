using System.Numerics;
using System.Text.Json;
using ZoneReborn.World.Levels.Storage;

namespace ZoneReborn.World.Roads;

public static class RoadStorage
{
    public const string FileName = "roads.json";
    public const int MaximumRoads = 1024;
    public const int MaximumNodes = 256;

    public static async Task<RoadNetwork> LoadAsync(string directory, CancellationToken token = default)
    {
        var network = new RoadNetwork();
        string path = Path.Combine(directory, FileName);
        if (!File.Exists(path)) return network;
        try
        {
            if (new FileInfo(path).Length > 32 * 1024 * 1024)
                throw new InvalidDataException("Слишком большой файл сети дорог.");
            var document = await LevelJson.ReadAsync<Document>(path, token);
            if (document.Format != "ZoneReborn.RoadNetwork" || document.Version is < 1 or > 2 || document.Roads == null)
                throw new InvalidDataException("Неподдерживаемый формат сети дорог.");
            ValidateNetwork(document.Roads);
            foreach (var road in document.Roads)
            {
                if (document.Version == 1 && (road.TerrainConformed || road.Type == RoadType.Railway))
                {
                    road.PreserveSourceElevation = true;
                    road.TerrainModifierEnabled = false;
                }
                road.TerrainConformed = false;
            }
            network.Roads.AddRange(document.Roads);
            network.ResetHistory();
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException or ArgumentException)
        {
            network.CanSave = false;
            network.Warnings.Add($"{FileName}: {exception.Message} Перезапись отключена; исходный файл сохранён.");
        }
        return network;
    }

    public static async Task SaveAsync(string directory, RoadNetwork network)
    {
        if (!network.CanSave) throw new InvalidOperationException("Сеть дорог не прочитана; перезапись отключена.");
        long revision = network.Revision;
        var document = new Document { Roads = network.Roads.Select(Copy).ToList() };
        ValidateNetwork(document.Roads);
        string temporary = Path.Combine(directory, $".roads-{Guid.NewGuid():N}.tmp");
        try
        {
            await LevelJson.WriteAsync(temporary, document);
            File.Move(temporary, Path.Combine(directory, FileName), true);
            if (network.Revision == revision) network.IsDirty = false;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static RoadDefinition Copy(RoadDefinition road)
    {
        return new RoadDefinition
        {
            Id = road.Id,
            Name = road.Name,
            Type = road.Type,
            Nodes = road.Nodes
                .Select(node => node.Copy())
                .ToList(),
            Width = road.Width,
            ShoulderWidth = road.ShoulderWidth,
            Thickness = road.Thickness,
            SurfaceOffset = road.SurfaceOffset,
            LaneCount = road.LaneCount,
            RailGauge = road.RailGauge,
            SleeperSpacing = road.SleeperSpacing,
            TrackCount = road.TrackCount,
            TrackSpacing = road.TrackSpacing,
            FollowTerrain = road.FollowTerrain,
            TerrainConformed = false,
            TerrainModifierEnabled = road.TerrainModifierEnabled,
            PreserveSourceElevation = road.PreserveSourceElevation,
            TerrainCutLimit = road.TerrainCutLimit,
            TerrainFillLimit = road.TerrainFillLimit,
            TerrainTransitionWidth = road.TerrainTransitionWidth,
            TerrainBaseDepth = road.TerrainBaseDepth,
            MaterialId = road.MaterialId,
            TextureRepeatLength = road.TextureRepeatLength,
            MaterialWetness = road.MaterialWetness
        };
    }

    private static void ValidateNetwork(IReadOnlyList<RoadDefinition> roads)
    {
        if (roads.Count > MaximumRoads) throw new InvalidDataException($"Не более {MaximumRoads} дорог на уровень.");
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int nodes = 0;
        foreach (var road in roads)
        {
            Validate(road);
            if (!ids.Add(road.Id)) throw new InvalidDataException("Повторяющийся ID дороги.");
            nodes += road.Nodes.Count;
        }
        if (nodes > 32768) throw new InvalidDataException("Слишком много опорных точек в сети дорог.");
    }

    public static void Validate(RoadDefinition road)
    {
        if (road == null || !Guid.TryParse(road.Id, out _) || string.IsNullOrWhiteSpace(road.Name) ||
            road.Name.Length > 128 || !Enum.IsDefined(road.Type) || road.Nodes == null || road.Nodes.Count > MaximumNodes)
            throw new InvalidDataException("Некорректная дорога или количество опорных точек.");
        Range(road.Width, 0.2f, 50, "Ширина");
        Range(road.ShoulderWidth, 0, 10, "Обочина");
        Range(road.Thickness, 0.02f, 2, "Толщина основания");
        Range(road.SurfaceOffset, -100, 100, "Смещение поверхности");
        Range(road.RailGauge, 0.5f, 3, "Ширина колеи");

        Range(road.SleeperSpacing, 0.3f, 3, "Шаг шпал");
        if (road.TrackCount is < 1 or > 2)
        {
            throw new InvalidDataException(
                "Количество железнодорожных путей должно быть 1 или 2.");
        }

        Range(
            road.TrackSpacing,
            2.5f,
            15f,
            "Расстояние между железнодорожными путями");

        Range(road.TerrainCutLimit, 0, 20, "Предел выемки");
        Range(road.TerrainFillLimit, 0, 20, "Предел насыпи");
        Range(road.TerrainTransitionWidth, 0.5f, 50, "Переход к Terrain");
        Range(road.TerrainBaseDepth, 0.02f, 2, "Глубина основания");
        Range(road.TextureRepeatLength, 0.1f, 100, "Повтор текстуры");
        Range(road.MaterialWetness, 0, 1, "Намокание материала");

        if (road.MaterialId == null || road.MaterialId.Length > 256)
            throw new InvalidDataException("Некорректный ID материала дороги.");

        if (road.LaneCount is < 1 or > 8) throw new InvalidDataException("Число полос: от 1 до 8.");

        if (road.Type == RoadType.Railway)
        {
            float minimumWidth =
                road.RailGauge + 0.6f;

            if (road.TrackCount == 2)
            {
                minimumWidth += road.TrackSpacing;
            }

            if (road.Width < minimumWidth)
            {
                throw new InvalidDataException(
                    $"Для {road.TrackCount} железнодорожных путей " +
                    $"ширина основания должна быть не менее " +
                    $"{minimumWidth:F2} м.");
            }
        }

        double length = 0;
        for (int i = 0; i < road.Nodes.Count; i++)
        {
            var node = road.Nodes[i];
            if (node == null) throw new InvalidDataException("Пустая опорная точка.");
            Range(node.X, -1e7f, 1e7f, "X"); Range(node.Y, -1e7f, 1e7f, "Y"); Range(node.Z, -1e7f, 1e7f, "Z");
            Range(node.HeightOffset, -10000, 10000, "Смещение точки");
            if (i == 0) continue;
            var previous = road.Nodes[i - 1];
            float horizontal = Vector2.Distance(new(previous.X, previous.Z), new(node.X, node.Z));
            if (horizontal < 0.25f) throw new InvalidDataException("Между соседними точками должно быть не менее 0,25 м по X/Z.");
            length += road.FollowTerrain ? horizontal + Math.Abs(node.HeightOffset - previous.HeightOffset)
                : Vector3.Distance(previous.Position, node.Position);
        }
        if (length > 20000) throw new InvalidDataException("Длина одной дороги ограничена 20 км; разделите её на несколько дорог.");
    }

    private static void Range(float value, float minimum, float maximum, string label)
    {
        if (!float.IsFinite(value) || value < minimum || value > maximum)
            throw new InvalidDataException($"{label}: от {minimum} до {maximum}.");
    }

    public sealed class Document
    {
        public string Format { get; init; } = "ZoneReborn.RoadNetwork";
        public int Version { get; init; } = 2;
        public List<RoadDefinition> Roads { get; init; } = [];
    }
}
