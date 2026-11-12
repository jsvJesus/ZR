using System.Collections.Concurrent;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Levels.Storage;
using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Terrain.Storage;

namespace ZoneReborn.World.Terrain;

public enum TerrainBrushMode { Raise, Lower, Smooth, CutHole, FillHole }

/// <summary>Shared CPU terrain data used by editing, rendering and gameplay.</summary>
public sealed partial class TerrainDocument(string directory, LevelManifest manifest, TerrainHoles holes, List<TerrainLayer> layers)
{
    private readonly ConcurrentDictionary<(int X, int Z), TerrainTileData> _tiles = new();
    private readonly HashSet<(int X, int Z)> _dirty = [], _normalDirty = [];
    private readonly HashSet<(int X, int Z)> _overviewDirty = [];
    private readonly List<Change> _undo = [];
    private TerrainOverviewData? _overview;
    private long _savedRevision;
    public TerrainHoles Holes { get; } = holes;
    public long Revision { get; private set; }
    public long HeightRevision { get; private set; }
    public long HoleRevision { get; private set; }
    public long MaterialRevision { get; private set; }
    public IReadOnlyList<TerrainLayer> Layers => layers;
    public bool IsDirty => Revision != _savedRevision;
    public bool CanUndo => _undo.Count > 0;
    public TerrainDescriptor Descriptor => manifest.Terrain;

    private TerrainTileData Raw(int x, int z)
    {
        if (_tiles.TryGetValue((x, z), out var tile)) return tile;
        var reference = manifest.Tiles.FirstOrDefault(value => value.X == x && value.Z == z)
            ?? throw new InvalidDataException("Не найден тайл Terrain.");
        tile = TerrainTileReader.Read(AssetPathResolver.Resolve(directory, reference.File));
        if (tile.X != x || tile.Z != z) throw new InvalidDataException("Terrain tile coordinates mismatch.");
        tile = _tiles.GetOrAdd((x, z), tile);
        TrimCleanTiles((x, z));
        return tile;
    }

    private void TrimCleanTiles((int X, int Z)? preserve = null)
    {
        if (_tiles.Count <= 64) return;
        foreach (var key in _tiles.Keys)
        {
            if (_tiles.Count <= 64) break;
            if ((!preserve.HasValue || key != preserve.Value) && !_dirty.Contains(key)) _tiles.TryRemove(key, out _);
        }
    }

    public TerrainTileData Tile(int x, int z)
    {
        var tile = Raw(x, z);
        if (_normalDirty.Remove((x, z)))
            _tiles[(x, z)] = tile = tile with { Normals = TerrainNormals.Build(Descriptor, x, z, Height) };
        return tile;
    }

    public float Height(int x, int z)
    {
        x = Math.Clamp(x, 0, Descriptor.SamplesX - 1); z = Math.Clamp(z, 0, Descriptor.SamplesZ - 1);
        return Descriptor.DecodeHeight(Raw(x / 256, z / 256).Heights[(z % 256) * 256 + x % 256]);
    }

    public bool HasSurface(float x, float z) => x >= 0 && z >= 0 && x <= Descriptor.WorldSizeX && z <= Descriptor.WorldSizeZ
        && !Holes.Contains(Math.Min((int)(x / Descriptor.CellSize), Holes.Width - 1),
            Math.Min((int)(z / Descriptor.CellSize), Holes.Height - 1));

    public bool Apply(float x, float z, float radius, float strength, TerrainBrushMode mode)
    {
        var d = Descriptor;
        if (!float.IsFinite(x) || !float.IsFinite(z) || !float.IsFinite(radius) || !float.IsFinite(strength)
            || radius <= 0 || radius > 100 || strength <= 0 || strength > 100) return false;
        if ((mode is TerrainBrushMode.CutHole or TerrainBrushMode.FillHole)
            && (long)(d.SamplesX - 1) * (d.SamplesZ - 1) > 16_777_216)
            throw new InvalidOperationException("Для отверстий поддерживается не более 16 миллионов ячеек.");

        var change = _sculptStroke ?? new Change();
        bool holesChanged = false;

        int x0 = Math.Clamp((int)MathF.Floor((x - radius) / d.CellSize), 0, d.SamplesX - 1);
        int x1 = Math.Clamp((int)MathF.Ceiling((x + radius) / d.CellSize), 0, d.SamplesX - 1);
        int z0 = Math.Clamp((int)MathF.Floor((z - radius) / d.CellSize), 0, d.SamplesZ - 1);
        int z1 = Math.Clamp((int)MathF.Ceiling((z + radius) / d.CellSize), 0, d.SamplesZ - 1);
        var pending = new List<(int X, int Z, ushort Value)>();
        for (int gz = z0; gz <= z1; gz++)
        for (int gx = x0; gx <= x1; gx++)
        {
            bool cut = mode is TerrainBrushMode.CutHole or TerrainBrushMode.FillHole;
            float px = (gx + (cut ? 0.5f : 0)) * d.CellSize, pz = (gz + (cut ? 0.5f : 0)) * d.CellSize;
            float distance = MathF.Sqrt((px - x) * (px - x) + (pz - z) * (pz - z));
            if (distance > radius) continue;
            if (cut)
            {
                if (gx >= Holes.Width || gz >= Holes.Height) continue;
                bool previous = Holes.Contains(gx, gz);

                if (Holes.Set(gx, gz, mode == TerrainBrushMode.CutHole))
                {
                    holesChanged = true;

                    if (_sculptStroke == null ||
                        _sculptHolesTouched.Add((gx, gz)))
                    {
                        change.Holes.Add((gx, gz, previous));
                    }
                }

                continue;
            }
            ushort old = Raw(gx / 256, gz / 256).Heights[(gz % 256) * 256 + gx % 256];
            float height = d.DecodeHeight(old), weight = 1 - distance / radius;
            float next = mode == TerrainBrushMode.Smooth
                ? height + ((Height(gx - 1, gz) + Height(gx + 1, gz) + Height(gx, gz - 1) + Height(gx, gz + 1)) / 4 - height)
                    * Math.Min(1, strength) * weight
                : height + strength * weight * (mode == TerrainBrushMode.Lower ? -1 : 1);
            ushort encoded = (ushort)Math.Clamp((int)MathF.Round((next - d.HeightOffset) / d.HeightScale * 65535), 0, 65535);
            if (encoded == old) continue;

            if (_sculptStroke == null ||
                _sculptHeightsTouched.Add((gx, gz)))
            {
                change.Heights.Add((gx, gz, old));
            }

            pending.Add((gx, gz, encoded));
        }
        foreach (var value in pending) SetHeight(value.X, value.Z, value.Value);

        if (pending.Count == 0 && !holesChanged)
        {
            return false;
        }

        if (_sculptStroke == null)
        {
            if (_undo.Count >= 64)
            {
                _undo.RemoveAt(0);
            }

            _undo.Add(change);
        }

        Revision++;

        if (pending.Count > 0)
        {
            HeightRevision++;
        }

        if (holesChanged)
        {
            HoleRevision++;
        }

        return true;
    }

    public void Undo()
    {
        if (_undo.Count == 0) return;
        var change = _undo[^1]; _undo.RemoveAt(_undo.Count - 1);
        if (change.SourceMaterialLayout is { } sourceLayout) Descriptor.HasSourceMaterialLayout = sourceLayout;
        foreach (var value in change.Heights) SetHeight(value.X, value.Z, value.Value);
        foreach (var value in change.Holes) Holes.Set(value.X, value.Z, value.Value);
        foreach (var value in change.Materials)
        {
            var tile = Raw(value.Key.X, value.Key.Z);
            _tiles[value.Key] = tile with { Masks = value.Value.Masks, ActiveLayers = value.Value.ActiveLayers };
            _dirty.Add(value.Key);
            _materialVersions[value.Key] = MaterialRevision + 1;
            _paintOverviewDirty.Add(value.Key);
        }
        Revision++;
        if (change.Heights.Count > 0) HeightRevision++;
        if (change.Holes.Count > 0) HoleRevision++;
        if (change.Materials.Count > 0) MaterialRevision++;
    }

    private void SetHeight(int x, int z, ushort value)
    {
        Raw(x / 256, z / 256).Heights[(z % 256) * 256 + x % 256] = value;
        _dirty.Add((x / 256, z / 256));
        _overviewDirty.Add((x / 256, z / 256));
        for (int tz = Math.Max(0, z / 256 - 1); tz <= Math.Min(Descriptor.TilesZ - 1, z / 256 + 1); tz++)
        for (int tx = Math.Max(0, x / 256 - 1); tx <= Math.Min(Descriptor.TilesX - 1, x / 256 + 1); tx++)
        { _normalDirty.Add((tx, tz)); _dirty.Add((tx, tz)); }
    }

    public TerrainOverviewData BuildOverview()
    {
        _overview ??= TerrainOverviewReader.Read(AssetPathResolver.Resolve(directory, manifest.OverviewFile));
        foreach (var key in _overviewDirty)
        for (int z = 0; z <= 256; z += 32)
        for (int x = 0; x <= 256; x += 32)
        {
            int gx = Math.Min(key.X * 256 + x, Descriptor.SamplesX - 1);
            int gz = Math.Min(key.Z * 256 + z, Descriptor.SamplesZ - 1);
            int ox = gx == Descriptor.SamplesX - 1 ? _overview.Width - 1 : gx / 32;
            int oz = gz == Descriptor.SamplesZ - 1 ? _overview.Height - 1 : gz / 32;
            _overview.Heights[oz * _overview.Width + ox] = Raw(gx / 256, gz / 256).Heights[(gz % 256) * 256 + gx % 256];
        }
        _overviewDirty.Clear();
        return _overview;
    }

    // Called with editing disabled by the editor's save/close lifecycle.
    public async Task SaveAsync()
    {
        if (!IsDirty) return;
        foreach (var key in _dirty)
        {
            var tile = Tile(key.X, key.Z);
            string file = manifest.Tiles.First(value => value.X == key.X && value.Z == key.Z).File;
            Atomic(file, path => TerrainTileWriter.Write(path, tile));
            int index = manifest.Tiles.FindIndex(value => value.X == key.X && value.Z == key.Z);
            manifest.Tiles[index] = new(key.X, key.Z, Descriptor.DecodeHeight(tile.Heights.Min()), Descriptor.DecodeHeight(tile.Heights.Max()), file);
        }
        if (Holes.Any || !string.IsNullOrWhiteSpace(Descriptor.HolesFile))
        {
            Descriptor.HolesFile = "terrain/holes.bin";
            Atomic(Descriptor.HolesFile, Holes.Write);
        }
        Atomic(manifest.OverviewFile, path => TerrainOverviewWriter.Write(path, BuildOverview()));
        string layerTemporary = Path.Combine(directory, $".terrain-layers-{Guid.NewGuid():N}.tmp");
        try
        {
            await LevelJson.WriteAsync(layerTemporary, layers);
            File.Move(layerTemporary, AssetPathResolver.Resolve(directory, manifest.LayersFile), true);
        }
        finally { if (File.Exists(layerTemporary)) File.Delete(layerTemporary); }

        if (BuildPaintOverview() is { } paintOverview)
            Atomic("terrain/paint-overview.bin", path => WritePaintOverview(path, paintOverview));
        SaveWaterBodies();

        string temporary = AssetPathResolver.Resolve(directory, $".terrain-manifest-{Guid.NewGuid():N}.tmp");
        try { await LevelJson.WriteAsync(temporary, manifest); File.Move(temporary, Path.Combine(directory, "level.json"), true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        _dirty.Clear(); _savedRevision = Revision; TrimCleanTiles();
    }

    private void Atomic(string file, Action<string> write)
    {
        string path = AssetPathResolver.Resolve(directory, file), temporary = path + $".{Guid.NewGuid():N}.tmp";
        try { write(temporary); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private sealed class Change
    {
        public bool? SourceMaterialLayout { get; init; }
        public List<(int X, int Z, ushort Value)> Heights { get; } = [];
        public List<(int X, int Z, bool Value)> Holes { get; } = [];
        public Dictionary<(int X, int Z), TerrainTileData> Materials { get; } = [];
    }
}
