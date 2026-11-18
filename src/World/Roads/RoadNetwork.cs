using System.Numerics;

namespace ZoneReborn.World.Roads;

public enum RoadCategory
{
    Automobile,
    Railway,
    Dirt
}

public enum RoadStructureKind
{
    Bridge,
    Tunnel
}

public readonly record struct RoadPoint(float X, float Z);

public sealed class RoadStructure
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public RoadStructureKind Kind { get; init; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public string Model { get; init; } = "";

    public override string ToString()
    {
        string type = Kind == RoadStructureKind.Bridge
            ? "Мост"
            : "Туннель";

        return $"{type}: {StartIndex + 1}–{EndIndex + 1}, " +
               Path.GetFileNameWithoutExtension(Model);
    }
}

public sealed class RoadPath
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "";
    public RoadCategory Category { get; set; } = RoadCategory.Automobile;

    public string Model { get; set; } = "";
    public string Material { get; set; } = "";
    public string ShoulderMaterial { get; set; } = "";

    public float Width { get; set; } = 6f;
    public float ShoulderWidth { get; set; } = 0.75f;

    public List<RoadPoint> Points { get; init; } = [];
    public List<RoadStructure> Structures { get; init; } = [];

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
            ValidateModel(
                model,
                "objects/roads_segments/",
                "objects/props/railroads/");
        }

        RoadPath road = RequireRoad(roadId);

        if (road.Category == category &&
            road.Model.Equals(model, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        road.Category = category;
        road.Model = model;
        NotifyChanged();
    }

    public void ConfigureMaterials(
        Guid roadId,
        string material,
        string shoulderMaterial)
    {
        ValidateTexture(material);
        ValidateTexture(shoulderMaterial);

        RoadPath road = RequireRoad(roadId);

        if (road.Material == material &&
            road.ShoulderMaterial == shoulderMaterial)
        {
            return;
        }

        road.Material = material;
        road.ShoulderMaterial = shoulderMaterial;
        NotifyChanged();
    }

    public void ConfigureWidth(
        Guid roadId,
        float width,
        float shoulderWidth)
    {
        if (!float.IsFinite(width) ||
            !float.IsFinite(shoulderWidth) ||
            width is < 0.5f or > 40f ||
            shoulderWidth is < 0f or > 8f)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        RoadPath road = RequireRoad(roadId);

        if (road.Width == width &&
            road.ShoulderWidth == shoulderWidth)
        {
            return;
        }

        road.Width = width;
        road.ShoulderWidth = shoulderWidth;
        NotifyChanged();
    }

    public int AddPoint(Guid roadId, RoadPoint point)
    {
        ValidatePoint(point);

        RoadPath road = RequireRoad(roadId);

        if (road.Points.Count >= MaximumPointsPerRoad)
        {
            throw new InvalidOperationException(
                "Достигнут лимит точек дороги.");
        }

        if (road.Points.Count > 0)
        {
            RoadPoint previous = road.Points[^1];

            if (Vector2.Distance(
                    new Vector2(previous.X, previous.Z),
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

        RoadPath road = RequireRoad(roadId);
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
        RoadPath road = RequireRoad(roadId);
        ValidateIndex(road, index);

        road.Points.RemoveAt(index);

        road.Structures.RemoveAll(structure =>
            structure.StartIndex == index ||
            structure.EndIndex == index);

        foreach (RoadStructure structure in road.Structures)
        {
            if (structure.StartIndex > index)
            {
                structure.StartIndex--;
            }

            if (structure.EndIndex > index)
            {
                structure.EndIndex--;
            }
        }

        NotifyChanged();
    }

    public RoadStructure AddStructure(
        Guid roadId,
        RoadStructureKind kind,
        int startIndex,
        int endIndex,
        string model)
    {
        RoadPath road = RequireRoad(roadId);

        if (!Enum.IsDefined(kind) ||
            startIndex < 0 ||
            endIndex != startIndex + 1 ||
            endIndex >= road.Points.Count)
        {
            throw new InvalidOperationException(
                "Мост или туннель назначается между двумя соседними точками.");
        }

        string folder = kind == RoadStructureKind.Bridge
            ? "objects/props/bridges/"
            : "objects/props/tunnels/";

        ValidateModel(model, folder);

        var structure = new RoadStructure
        {
            Kind = kind,
            StartIndex = startIndex,
            EndIndex = endIndex,
            Model = model
        };

        road.Structures.RemoveAll(existing =>
            existing.StartIndex == startIndex &&
            existing.EndIndex == endIndex);

        road.Structures.Add(structure);
        NotifyChanged();

        return structure;
    }

    public void RemoveStructure(Guid roadId, Guid structureId)
    {
        RoadPath road = RequireRoad(roadId);
        int removed = road.Structures.RemoveAll(
            structure => structure.Id == structureId);

        if (removed > 0)
        {
            NotifyChanged();
        }
    }

    public bool RemoveRoad(Guid roadId)
    {
        RoadPath? road = Find(roadId);

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

    private RoadPath RequireRoad(Guid id)
    {
        return Find(id)
            ?? throw new InvalidOperationException("Дорога не найдена.");
    }

    private void NotifyChanged()
    {
        _revision++;
        Changed?.Invoke();
    }

    private static void ValidateTexture(string file)
    {
        if (string.IsNullOrEmpty(file))
        {
            return;
        }

        if (!file.StartsWith(
                "textures/roads/",
                StringComparison.OrdinalIgnoreCase) ||
            !file.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) ||
            file.Contains("..", StringComparison.Ordinal) ||
            file.Contains('\\') ||
            file.Contains(':'))
        {
            throw new InvalidDataException(
                "Некорректный путь материала дороги.");
        }
    }

    private static void ValidateModel(
        string file,
        params string[] folders)
    {
        if (!folders.Any(folder =>
                file.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) ||
            !file.EndsWith(".model", StringComparison.OrdinalIgnoreCase) ||
            file.Contains("..", StringComparison.Ordinal) ||
            file.Contains('\\') ||
            file.Contains(':'))
        {
            throw new InvalidDataException(
                "Некорректный путь модели дороги.");
        }
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
