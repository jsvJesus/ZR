using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using ZoneReborn.World.Scene;

namespace ZoneReborn.World.Roads;

public enum RoadType { Automobile, Railway, Path }

public sealed class RoadNetwork
{
    public List<RoadDefinition> Roads { get; } = [];
    public List<string> Warnings { get; } = [];
    public bool IsDirty { get; set; }
    public bool CanSave { get; set; } = true;
    public long Revision { get; private set; }
    public event Action? Modified;
    private List<RoadDefinition> _previous = [];
    private readonly List<List<RoadDefinition>> _undo = [], _redo = [];
    public bool CanUndo => CanSave && _undo.Count > 0;
    public bool CanRedo => CanSave && _redo.Count > 0;

    public void ResetHistory()
    {
        _undo.Clear(); _redo.Clear();
        _previous = Roads.Select(RoadStorage.Copy).ToList();
    }

    public void Undo() => Restore(_undo, _redo);
    public void Redo() => Restore(_redo, _undo);

    private void Restore(List<List<RoadDefinition>> source, List<List<RoadDefinition>> destination)
    {
        if (!CanSave || source.Count == 0) return;
        destination.Add(Roads.Select(RoadStorage.Copy).ToList());
        var snapshot = source[^1]; source.RemoveAt(source.Count - 1);
        Roads.Clear(); Roads.AddRange(snapshot.Select(RoadStorage.Copy));
        _previous = Roads.Select(RoadStorage.Copy).ToList();
        Revision++; IsDirty = true;
        Modified?.Invoke();
    }
    public void Changed(RoadDefinition? road = null)
    {
        _undo.Add(_previous);
        if (_undo.Count > 32) _undo.RemoveAt(0);
        _redo.Clear();
        road?.Changed();
        Revision++;
        IsDirty = true;
        _previous = Roads.Select(RoadStorage.Copy).ToList();
        Modified?.Invoke();
    }
}

public sealed class RoadNode
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float HeightOffset { get; set; }
    [JsonIgnore] public Vector3 Position => new(X, Y, Z);
    public RoadNode Copy() => (RoadNode)MemberwiseClone();
    public override string ToString() => $"{X:F1}, {Y:F1}, {Z:F1}";
}

public sealed class RoadDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Дорога";
    public RoadType Type { get; set; }
    public List<RoadNode> Nodes { get; set; } = [];
    public float Width { get; set; } = 7;
    public float ShoulderWidth { get; set; } = 0.75f;
    public float Thickness { get; set; } = 0.2f;
    public float SurfaceOffset { get; set; } = 0.06f;
    public int LaneCount { get; set; } = 2;
    public float RailGauge { get; set; } = 1.52f;
    public float SleeperSpacing { get; set; } = 0.65f;

    public bool FollowTerrain { get; set; } = true;

    public bool TerrainConformed { get; set; }
    public bool TerrainModifierEnabled { get; set; } = true;
    public bool PreserveSourceElevation { get; set; }
    public float TerrainCutLimit { get; set; } = 6;
    public float TerrainFillLimit { get; set; } = 6;
    public float TerrainTransitionWidth { get; set; } = 4;
    public float TerrainBaseDepth { get; set; } = 0.02f;
    public string MaterialId { get; set; } = "";
    public float TextureRepeatLength { get; set; } = 4;
    public float MaterialWetness { get; set; } = 1;

    [JsonIgnore]
    public int Revision { get; private set; }

    public void Changed()
    {
        TerrainConformed = false;
        Revision++;
    }

    public static string TypeName(RoadType type) => type switch
    {
        RoadType.Automobile => "Автомобильная дорога",
        RoadType.Railway => "Железная дорога",
        _ => "Тропинка"
    };
    public static RoadDefinition Create(RoadType type) => new()
    {
        Type = type, Name = TypeName(type), Width = type == RoadType.Automobile ? 7 : type == RoadType.Railway ? 3.6f : 1.5f,
        ShoulderWidth = type == RoadType.Path ? 0.3f : 0.75f,
        Thickness = type == RoadType.Railway ? 0.3f : type == RoadType.Path ? 0.08f : 0.2f,
        LaneCount = type == RoadType.Automobile ? 2 : 1
    };
    public override string ToString() => $"{Name} · {TypeName(Type)} · {Nodes.Count} точек";
}

[StructLayout(LayoutKind.Sequential)]
public struct RoadVertex(Vector3 position, Vector3 normal, Vector2 uv, float surface)
{
    public Vector3 Position = position;
    public Vector3 Normal = normal;
    public Vector2 Uv = uv;
    public float Surface = surface;
}

public readonly record struct RoadSample(Vector3 Position, Vector3 Forward, float Distance, int SegmentIndex);
public sealed record RoadSectionMesh(int SegmentIndex, RoadVertex[] Vertices, int[] Indices, SceneBounds Bounds);
