using System.Numerics;

namespace ZoneReborn.World.Roads;

public enum RoadCategory
{
    Automobile,
    Railway,
    Dirt
}

public readonly record struct RoadPoint(float X, float Z);

public sealed class RoadPath
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "";
    public RoadCategory Category { get; set; } = RoadCategory.Automobile;
    public string Model { get; set; } = "";
    public List<RoadPoint> Points { get; init; } = [];

    public override string ToString() => Name;
}

public sealed class RoadNetwork
{
    public const int MaximumRoads = 1000;
    public const int MaximumPointsPerRoad = 4096;

    private readonly List<RoadPath> _roads = [];
    private long _revision;
    private long _savedRevision;

    public IReadOnlyList<RoadPath> Roads => _roads;
    public bool IsDirty => _revision != _savedRevision;
    public long Revision => _revision;

    public event Action? Changed;

    public RoadPath? Find(Guid id)
    {
        return _roads.FirstOrDefault(road => road.Id == id);
    }

    public RoadPath CreateRoad()
    {
        if (_roads.Count >= MaximumRoads)
        {
            throw new InvalidOperationException(
                "Достигнут лимит дорожных маршрутов.");
        }

        int number = 1;

        while (_roads.Any(road => road.Name == $"Дорога {number}"))
        {
            number++;
        }

        var road = new RoadPath
        {
            Name = $"Дорога {number}"
        };

        _roads.Add(road);
        NotifyChanged();

        return road;
    }

    public void ConfigureRoad(
        Guid roadId,
        RoadCategory category,
        string model)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }

        if (!string.IsNullOrEmpty(model))
        {
            bool validPath =
                model.StartsWith(
                    "objects/roads_segments/",
                    StringComparison.OrdinalIgnoreCase) ||
                model.StartsWith(
                    "objects/props/railroads/",
                    StringComparison.OrdinalIgnoreCase);

            if (!validPath ||
                !model.EndsWith(
                    ".model",
                    StringComparison.OrdinalIgnoreCase) ||
                model.Contains("..", StringComparison.Ordinal) ||
                model.Contains('\\'))
            {
                throw new InvalidDataException(
                    "Некорректный путь дорожной модели.");
            }
        }

        var road = Find(roadId)
            ?? throw new InvalidOperationException("Дорога не найдена.");

        if (road.Category == category &&
            string.Equals(
                road.Model,
                model,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        road.Category = category;
        road.Model = model;

        NotifyChanged();
    }

    public int AddPoint(Guid roadId, RoadPoint point)
    {
        ValidatePoint(point);

        var road = Find(roadId)
            ?? throw new InvalidOperationException("Дорога не найдена.");

        if (road.Points.Count >= MaximumPointsPerRoad)
        {
            throw new InvalidOperationException(
                "Достигнут лимит точек дороги.");
        }

        if (road.Points.Count > 0)
        {
            var last = road.Points[^1];

            if (Vector2.Distance(
                    new Vector2(last.X, last.Z),
                    new Vector2(point.X, point.Z)) < 0.1f)
            {
                return road.Points.Count - 1;
            }
        }

        road.Points.Add(point);
        NotifyChanged();

        return road.Points.Count - 1;
    }

    public void MovePoint(Guid roadId, int index, RoadPoint point)
    {
        ValidatePoint(point);

        var road = Find(roadId)
            ?? throw new InvalidOperationException("Дорога не найдена.");

        ValidateIndex(road, index);

        if (road.Points[index] == point)
        {
            return;
        }

        road.Points[index] = point;
        NotifyChanged();
    }

    public void RemovePoint(Guid roadId, int index)
    {
        var road = Find(roadId)
            ?? throw new InvalidOperationException("Дорога не найдена.");

        ValidateIndex(road, index);

        road.Points.RemoveAt(index);
        NotifyChanged();
    }

    public bool RemoveRoad(Guid roadId)
    {
        var road = Find(roadId);

        if (road == null)
        {
            return false;
        }

        _roads.Remove(road);
        NotifyChanged();

        return true;
    }

    internal void Load(IEnumerable<RoadPath> roads)
    {
        _roads.Clear();
        _roads.AddRange(roads);

        _revision = 0;
        _savedRevision = 0;

        Changed?.Invoke();
    }

    internal void MarkSaved(long revision)
    {
        _savedRevision = revision;
    }

    private void NotifyChanged()
    {
        _revision++;
        Changed?.Invoke();
    }

    private static void ValidatePoint(RoadPoint point)
    {
        if (!float.IsFinite(point.X) ||
            !float.IsFinite(point.Z) ||
            MathF.Abs(point.X) > 1e7f ||
            MathF.Abs(point.Z) > 1e7f)
        {
            throw new InvalidDataException(
                "Некорректные координаты дорожной точки.");
        }
    }

    private static void ValidateIndex(RoadPath road, int index)
    {
        if ((uint)index >= (uint)road.Points.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
