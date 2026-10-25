using System.Diagnostics;
using System.IO;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Textures.Dds;
using Device = SharpDX.Direct3D11.Device;

namespace Editor.Viewport.Terrain;

internal sealed class TerrainMaterialCache : IDisposable
{
    private const int Capacity = 64;
    private readonly Device _device;
    private readonly AssetPaths _assets;
    private readonly Dictionary<string, Entry> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _failed = new(StringComparer.OrdinalIgnoreCase);
    private int _uploads;
    private long _frame;

    public TerrainTexture DiffuseFallback { get; }
    public TerrainTexture NormalFallback { get; }

    public TerrainMaterialCache(Device device, AssetPaths assets)
    {
        _device = device;
        _assets = assets;
        DiffuseFallback = TerrainTexture.Solid(device, 122, 131, 108);
        try { NormalFallback = TerrainTexture.Solid(device, 128, 128, 255); }
        catch { DiffuseFallback.Dispose(); throw; }
    }

    public void BeginFrame() { _frame++; _uploads = 0; }

    public TerrainTexture Get(string relativePath, bool normal)
    {
        var fallback = normal ? NormalFallback : DiffuseFallback;
        if (string.IsNullOrWhiteSpace(relativePath) || _failed.Contains(relativePath)) return fallback;
        if (_textures.TryGetValue(relativePath, out var entry))
        {
            entry.Frame = _frame;
            return entry.Texture;
        }
        if (_uploads >= 2) return fallback;
        _uploads++;
        try
        {
            var texture = new TerrainTexture(_device, DdsReader.Read(AssetPathResolver.Resolve(_assets.Root, relativePath)));
            if (_textures.Count >= Capacity)
            {
                var oldest = _textures.MinBy(pair => pair.Value.Frame);
                oldest.Value.Texture.Dispose();
                _textures.Remove(oldest.Key);
            }
            _textures.Add(relativePath, new Entry(texture, _frame));
            return texture;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            _failed.Add(relativePath);
            Trace.TraceWarning("Terrain material '{0}': {1}", relativePath, exception.Message);
            return fallback;
        }
    }

    public void Dispose()
    {
        foreach (var entry in _textures.Values) entry.Texture.Dispose();
        _textures.Clear();
        DiffuseFallback.Dispose();
        NormalFallback.Dispose();
    }

    private sealed class Entry(TerrainTexture texture, long frame)
    {
        public TerrainTexture Texture { get; } = texture;
        public long Frame { get; set; } = frame;
    }
}
