using System.Collections.Concurrent;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene;

public sealed class SceneModelCache
{
    private readonly AssetPaths _assets;
    private readonly Action<string> _warning;
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _background = new(2, 2);
    private SourceMaterialReader _materials;

    public SceneModelCache(AssetPaths assets, Action<string> warning)
    {
        _assets = assets;
        _warning = warning;
        _materials = new(assets, null, warning);
    }

    internal void SetSourceDirectory(string directory) => _materials = new(_assets, directory, _warning);

    public SceneModel? Get(string file)
    {
        Entry entry = EntryFor(file);
        Interlocked.Exchange(ref entry.Started, 1);
        return entry.Model.Value;
    }

    public bool TryGet(string file, out SceneModel model)
    {
        model = null!;
        if (!_entries.TryGetValue(Key(file), out var entry) || !entry.Model.IsValueCreated) return false;
        SceneModel? complete = entry.Model.Value;
        if (complete == null) return false;
        model = complete;
        return true;
    }

    public void Request(string file)
    {
        if (!_background.Wait(0)) return; // Caller can request again next frame; no queued tasks.
        Entry entry = EntryFor(file);
        if (Interlocked.CompareExchange(ref entry.Started, 1, 0) != 0)
        {
            _background.Release();
            return;
        }
        _ = Task.Run(() =>
        {
            try { _ = entry.Model.Value; }
            catch (Exception error) { _warning($"Model {file}: {error.Message}"); }
            finally { _background.Release(); }
        });
    }

    private Entry EntryFor(string file) => _entries.GetOrAdd(Key(file), key => new Entry(new Lazy<SceneModel?>(() => Load(key),
        LazyThreadSafetyMode.ExecutionAndPublication)));

    private SceneModel? Load(string file)
    {
        try { return SourceModelReader.Read(_assets, file, _materials, _warning); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException
            or FormatException or OverflowException or ArgumentException)
        {
            _warning($"Model {file}: {error.Message}");
            return null;
        }
    }

    private static string Key(string file)
    {
        try { return SourceModelReader.Normalize(file); }
        catch (InvalidDataException) { return file.Trim().Replace('\\', '/'); }
    }

    private sealed class Entry(Lazy<SceneModel?> model)
    {
        public Lazy<SceneModel?> Model { get; } = model;
        public int Started;
    }
}
