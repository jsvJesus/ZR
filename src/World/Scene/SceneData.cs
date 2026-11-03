using System.Collections.Concurrent;
using System.Numerics;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene;

public sealed class SceneData
{
    private const int CellSize = 64;
    private readonly Dictionary<(int X, int Z), List<SceneInstance>> _cells = [];
    private readonly List<SceneInstance> _large = [];
    private readonly SceneInstance[] _sources;
    private readonly Dictionary<string, SceneInstance> _originals;
    private readonly Dictionary<string, SceneInstance> _current;
    private readonly List<SceneInstance> _instances = [];
    private readonly Dictionary<string, int> _instanceSlots = new(StringComparer.Ordinal);
    private readonly List<Change> _history = [];
    private SceneInstance? _previewOriginal;
    private SceneCollision _collision = null!;
    private bool _collisionDirty;
    private int _historyPosition;
    private long _revision, _savedRevision;
    public IReadOnlyList<SceneInstance> Instances { get; }
    public string SourceName { get; internal set; } = "";
    public bool CanSave { get; internal set; } = true;
    public bool IsDirty => _revision != _savedRevision;
    public bool HasTransformPreview => _previewOriginal != null;
    public bool CanUndo => CanSave && !HasTransformPreview && _historyPosition > 0;
    public bool CanRedo => CanSave && !HasTransformPreview && _historyPosition < _history.Count;
    public event Action? Changed;
    public ConcurrentQueue<string> Warnings { get; } = new();
    public SceneModelCache Models { get; }
    public ScenePhysicsCache Physics { get; }
    public SceneCollision Collision
    {
        get
        {
            if (_collisionDirty) RefreshCollision();
            return _collision;
        }
    }

    public SceneData(AssetPaths assets, IReadOnlyList<SceneInstance> instances, IEnumerable<string>? warnings = null,
        ScenePhysicsCache? physics = null)
    {
        _sources = instances.ToArray();
        _originals = _sources.ToDictionary(instance => instance.Id, StringComparer.Ordinal);
        _current = new(_originals, StringComparer.Ordinal);
        Instances = _instances.AsReadOnly();
        if (warnings != null) foreach (string warning in warnings) Warnings.Enqueue(warning);
        Models = new SceneModelCache(assets, warning => Warnings.Enqueue(warning));
        Physics = physics ?? new ScenePhysicsCache(assets, warning => Warnings.Enqueue(warning));
        RebuildIndex();
    }

    public SceneInstance? Find(string id) => _current.GetValueOrDefault(id);

    /// <summary>A mouse gesture previews many matrices but commits one history entry.</summary>
    public void BeginTransformPreview(string id)
    {
        RequireEditable();
        _previewOriginal = Find(id) ?? throw new InvalidOperationException("Объект не найден или уже удалён.");
    }

    public SceneInstance PreviewTransform(string id, Matrix4x4 world)
    {
        if (!CanSave || _previewOriginal?.Id != id)
            throw new InvalidOperationException("Нет активного преобразования выбранного объекта.");
        var current = Find(id)!;
        if (current.World == world) return current;
        var preview = Transform(current, world);
        // Moving one object must not rebuild all 29,000 Aspen instances per mouse move.
        Unindex(current);
        _current[id] = preview;
        _instances[_instanceSlots[id]] = preview;
        Index(preview);
        _collisionDirty = true;
        return preview;
    }

    public bool CommitTransformPreview()
    {
        if (_previewOriginal is not { } before) return false;
        var after = Find(before.Id)!;
        if (before.World == after.World) return CancelTransformPreview();
        _previewOriginal = null;
        Record(new(before, after));
        return true;
    }

    public bool CancelTransformPreview()
    {
        if (_previewOriginal is not { } original) return false;
        _previewOriginal = null;
        _current[original.Id] = original;
        RebuildIndex();
        Changed?.Invoke();
        return true;
    }

    public SceneInstance UpdateTransform(string id, Matrix4x4 world)
    {
        RequireEditable();
        var before = Find(id) ?? throw new InvalidOperationException("Объект не найден или уже удалён.");
        if (before.World == world) return before;
        var after = Transform(before, world);
        Record(new(before, after));
        return after;
    }

    public bool Remove(string id)
    {
        RequireEditable();
        if (Find(id) is not { } before) return false;
        Record(new(before, null));
        return true;
    }

    public bool Undo()
    {
        RequireEditable();
        if (!CanUndo) return false;
        var change = _history[--_historyPosition];
        _current[change.Before.Id] = change.Before;
        PublishChange();
        return true;
    }

    public bool Redo()
    {
        RequireEditable();
        if (!CanRedo) return false;
        Apply(_history[_historyPosition++]);
        PublishChange();
        return true;
    }

    private void Record(Change change)
    {
        if (_historyPosition < _history.Count) _history.RemoveRange(_historyPosition, _history.Count - _historyPosition);
        if (_history.Count == 256) { _history.RemoveAt(0); _historyPosition--; }
        _history.Add(change); _historyPosition++;
        Apply(change);
        PublishChange();
    }

    private void Apply(Change change)
    {
        if (change.After == null) _current.Remove(change.Before.Id);
        else _current[change.After.Id] = change.After;
    }

    private void PublishChange()
    {
        RebuildIndex();
        _revision++;
        Changed?.Invoke();
    }

    private void RequireEditable()
    {
        if (!CanSave) throw new InvalidOperationException("Файл правок объектов не прочитан. Редактирование и перезапись отключены.");
        if (HasTransformPreview) throw new InvalidOperationException("Сначала завершите или отмените перетаскивание объекта.");
    }

    internal SceneInstance? Original(string id) => _originals.GetValueOrDefault(id);

    internal static SceneInstance Transform(SceneInstance instance, Matrix4x4 world)
    {
        SceneObjectTransform.ValidateWorld(world);
        Matrix4x4.Invert(world, out var inverse);
        var bounds = instance.LocalBounds.Transform(world);
        if (!SceneTriangle.IsFinite(bounds.Minimum) || !SceneTriangle.IsFinite(bounds.Maximum) ||
            Math.Max(Vector3.Abs(bounds.Minimum).Length(), Vector3.Abs(bounds.Maximum).Length()) > 2e7f)
            throw new InvalidDataException("Границы объекта выходят за допустимые пределы.");
        return instance with { World = world, InverseWorld = inverse, Bounds = bounds };
    }

    internal void LoadOverrides(IReadOnlyList<(string Id, SceneInstance? Instance)> changes)
    {
        foreach (var change in changes)
        {
            if (change.Instance == null) _current.Remove(change.Id);
            else _current[change.Id] = change.Instance;
        }
        RebuildIndex();
        _history.Clear(); _historyPosition = 0;
        _revision = _savedRevision = 0;
    }

    internal (long Revision, SceneObjectOverride[] Edits) CaptureEdits()
    {
        RequireEditable();
        var edits = new List<SceneObjectOverride>();
        foreach (var source in _sources)
        {
            var current = Find(source.Id);
            if (current != null && current.World == source.World) continue;
            edits.Add(new()
            {
                Id = source.Id, SourceStamp = source.SourceStamp, Model = source.Model,
                Deleted = current == null, World = current == null ? null : SceneEditStorage.MatrixValues(current.World)
            });
        }
        return (_revision, edits.ToArray());
    }

    internal void MarkSaved(long revision) => _savedRevision = revision;

    private void RebuildIndex()
    {
        _instances.Clear(); _instanceSlots.Clear(); _cells.Clear(); _large.Clear();
        foreach (var source in _sources)
        {
            if (!_current.TryGetValue(source.Id, out var instance)) continue;
            _instanceSlots[instance.Id] = _instances.Count;
            _instances.Add(instance);
        }
        foreach (var instance in _instances) Index(instance);
        RefreshCollision();
    }

    private void Index(SceneInstance instance)
    {
        int x0 = Cell(instance.Bounds.Minimum.X), x1 = Cell(instance.Bounds.Maximum.X);
        int z0 = Cell(instance.Bounds.Minimum.Z), z1 = Cell(instance.Bounds.Maximum.Z);
        if ((long)(x1 - x0 + 1) * (z1 - z0 + 1) > 4096) { _large.Add(instance); return; }
        for (int z = z0; z <= z1; z++)
        for (int x = x0; x <= x1; x++)
        {
            if (!_cells.TryGetValue((x, z), out var cell)) _cells.Add((x, z), cell = []);
            cell.Add(instance);
        }
    }

    private void Unindex(SceneInstance instance)
    {
        int x0 = Cell(instance.Bounds.Minimum.X), x1 = Cell(instance.Bounds.Maximum.X);
        int z0 = Cell(instance.Bounds.Minimum.Z), z1 = Cell(instance.Bounds.Maximum.Z);
        if ((long)(x1 - x0 + 1) * (z1 - z0 + 1) > 4096) { _large.Remove(instance); return; }
        for (int z = z0; z <= z1; z++)
        for (int x = x0; x <= x1; x++)
            if (_cells.TryGetValue((x, z), out var cell))
            {
                cell.Remove(instance);
                if (cell.Count == 0) _cells.Remove((x, z));
            }
    }

    private void RefreshCollision()
    {
        _collision = new SceneCollision(this);
        _collisionDirty = false;
    }

    public IEnumerable<SceneInstance> Query(SceneBounds bounds)
    {
        var seen = new HashSet<SceneInstance>(ReferenceEqualityComparer.Instance);
        for (int z = Cell(bounds.Minimum.Z); z <= Cell(bounds.Maximum.Z); z++)
        for (int x = Cell(bounds.Minimum.X); x <= Cell(bounds.Maximum.X); x++)
            if (_cells.TryGetValue((x, z), out var cell))
                foreach (var instance in cell)
                    if (seen.Add(instance) && instance.Bounds.Intersects(bounds)) yield return instance;
        foreach (var instance in _large) if (instance.Bounds.Intersects(bounds)) yield return instance;
    }

    public void Preload(Vector3 center, float radius, CancellationToken cancellationToken = default)
    {
        var extent = new Vector3(radius);
        foreach (string model in Query(new(center - extent, center + extent)).Select(instance => instance.Model)
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            Models.Get(model);
            Physics.Get(model);
        }
    }

    private static int Cell(float value) => (int)MathF.Floor(value / CellSize);
    private readonly record struct Change(SceneInstance Before, SceneInstance? After);
}
