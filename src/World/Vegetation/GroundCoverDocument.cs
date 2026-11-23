namespace ZoneReborn.World.Vegetation;

/// <summary>Only settings and sparse one-metre paint cells are retained, never generated meshes.</summary>
public sealed class GroundCoverDocument
{
    internal const int MaximumOverrides = 2_000_000;
    private readonly Dictionary<(int X, int Z), bool> _overrides = [];
    private readonly List<Change> _history = [];
    private Dictionary<(int X, int Z), bool?>? _stroke;
    private GroundCoverSettings _settings = new();
    private int _historyPosition;
    private long _savedRevision;

    public GroundCoverSettings Settings => _settings.Copy();
    public long Revision { get; private set; }
    public bool IsDirty => Revision != _savedRevision;
    public bool CanSave { get; internal set; } = true;
    public List<string> Warnings { get; } = [];
    public bool CanUndo => CanSave && _stroke == null && _historyPosition > 0;
    public bool CanRedo => CanSave && _stroke == null && _historyPosition < _history.Count;
    public event Action? Changed;

    public void SetSettings(GroundCoverSettings settings)
    {
        RequireEditable();
        GroundCoverSettings.Validate(settings);
        EndStroke();
        settings = settings.Copy();
        if ((_settings with { Models = settings.Models }) == settings &&
            _settings.Models.SequenceEqual(settings.Models, StringComparer.OrdinalIgnoreCase)) return;
        var before = _settings;
        _settings = settings;
        Record(new(before, settings, []));
        Publish();
    }

    public void Clear()
    {
        RequireEditable();
        EndStroke();
        if (_overrides.Count == 0 && !_settings.AutoGenerate) return;
        var cells = _overrides.Select(pair => new CellChange(pair.Key, pair.Value, null)).ToArray();
        var before = _settings;
        _settings = _settings with { AutoGenerate = false };
        _overrides.Clear();
        Record(new(before, _settings, cells));
        Publish();
    }

    public void BeginStroke()
    {
        RequireEditable();
        _stroke ??= [];
    }

    public void EndStroke()
    {
        if (_stroke == null) return;
        var cells = _stroke.Select(pair => new CellChange(pair.Key, pair.Value,
            _overrides.TryGetValue(pair.Key, out bool value) ? value : null))
            .Where(cell => cell.Before != cell.After).ToArray();
        _stroke = null;
        if (cells.Length > 0) Record(new(null, null, cells));
    }

    public bool Undo()
    {
        RequireEditable();
        EndStroke();
        if (!CanUndo) return false;
        Apply(_history[--_historyPosition], false);
        return true;
    }

    public bool Redo()
    {
        RequireEditable();
        EndStroke();
        if (!CanRedo) return false;
        Apply(_history[_historyPosition++], true);
        return true;
    }

    internal bool Enabled(int x, int z) => _overrides.TryGetValue((x, z), out bool enabled)
        ? enabled : _settings.AutoGenerate;

    internal int PaintCells(IEnumerable<(int X, int Z)> cells, bool enabled)
    {
        RequireEditable();
        bool ownStroke = _stroke == null;
        if (ownStroke) BeginStroke();
        int changed = 0;
        try
        {
            foreach (var key in cells)
            {
                bool exists = _overrides.TryGetValue(key, out bool before);
                if (exists && before == enabled) continue;
                if (!exists && _overrides.Count >= MaximumOverrides)
                    throw new InvalidOperationException("Достигнут лимит ячеек кисти.");
                _stroke!.TryAdd(key, exists ? before : null);
                // False is retained even with automatic generation currently disabled.
                _overrides[key] = enabled;
                changed++;
            }
        }
        finally
        {
            if (changed > 0) Publish();
            if (ownStroke) EndStroke();
        }
        return changed;
    }

    internal (long Revision, GroundCoverSettings Settings, GroundCoverCell[] Cells) Capture()
    {
        RequireEditable();
        return (Revision, _settings.Copy(), _overrides.OrderBy(pair => pair.Key.Z)
            .ThenBy(pair => pair.Key.X).Select(pair => new GroundCoverCell(pair.Key.X, pair.Key.Z, pair.Value)).ToArray());
    }

    internal void Load(GroundCoverSettings settings, IEnumerable<GroundCoverCell> cells)
    {
        _settings = settings.Copy();
        foreach (var cell in cells) _overrides.Add((cell.X, cell.Z), cell.Enabled);
    }

    internal void MarkSaved(long revision) => _savedRevision = revision;

    private void Apply(Change change, bool after)
    {
        var settings = after ? change.After : change.Before;
        if (settings != null) _settings = settings;
        foreach (var cell in change.Cells)
        {
            bool? value = after ? cell.After : cell.Before;
            if (value.HasValue) _overrides[cell.Key] = value.Value;
            else _overrides.Remove(cell.Key);
        }
        Publish();
    }

    private void Record(Change change)
    {
        if (_historyPosition < _history.Count) _history.RemoveRange(_historyPosition, _history.Count - _historyPosition);
        if (_history.Count == 128) { _history.RemoveAt(0); _historyPosition--; }
        _history.Add(change);
        _historyPosition++;
    }

    private void Publish() { Revision++; Changed?.Invoke(); }
    private void RequireEditable()
    {
        if (!CanSave) throw new InvalidOperationException("groundcover.json не прочитан; изменения и перезапись отключены.");
    }

    private sealed record Change(GroundCoverSettings? Before, GroundCoverSettings? After, CellChange[] Cells);
    private readonly record struct CellChange((int X, int Z) Key, bool? Before, bool? After);
}

internal readonly record struct GroundCoverCell(int X, int Z, bool Enabled);
