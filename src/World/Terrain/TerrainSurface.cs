using System.Numerics;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Terrain.Storage;

namespace ZoneReborn.World.Terrain;

public sealed class TerrainSurface
{
    private readonly LoadedLevel _level;
    private readonly Dictionary<(int X, int Z), TerrainTileReference> _references;
    private readonly Dictionary<(int X, int Z), TerrainTileData> _tiles = [];
    private readonly Queue<(int X, int Z)> _order = new();

    public TerrainSurface(LoadedLevel level)
    {
        _level = level;
        _references = level.Manifest.Tiles.ToDictionary(t => (t.X, t.Z));
    }

    public bool TryGetHeight(float x, float z, out float height) => TryGetHeight(x, z, 4, out height);

    public bool TryGetSourceHeight(float x, float z, out float height) => TryGetHeight(x, z, 1, out height);

    public bool HasSceneObjects => _level.Objects?.Instances.Count > 0;

    // Editing and road attachment keep using the terrain-only queries above.
    // Gameplay selects the floor beneath the feet, rather than a roof above the player.
    public bool TryGetGround(float x, float z, float maximumY, out float height)
    {
        bool found = TryGetHeight(x, z, out height);
        if (_level.Objects is { } objects &&
            objects.Collision.TryGround(x, z, maximumY, found ? height : -1e7f, out float modelHeight, 0.32f))
        {
            height = found ? Math.Max(height, modelHeight) : modelHeight;
            found = true;
        }
        return found;
    }

    public Vector3 MoveCharacter(Vector3 from, Vector3 target, float height) =>
        _level.Objects?.Collision.MoveCapsule(from, target, 0.32f, height) ?? target;

    public bool CanOccupy(Vector3 feet, float height) =>
        _level.Objects?.Collision.CanOccupy(feet, 0.32f, height) ?? true;

    public bool TrySnapToObject(Vector3 feet, float height, float maximumDrop, out Vector3 supported,
        float maximumSurfaceY = float.PositiveInfinity) =>
        TrySnapToObject(feet, height, maximumDrop, out supported, out _, maximumSurfaceY);

    public bool TrySnapToObject(Vector3 feet, float height, float maximumDrop, out Vector3 supported,
        out float surfaceY, float maximumSurfaceY = float.PositiveInfinity)
    {
        supported = feet;
        surfaceY = feet.Y;
        return _level.Objects is { } objects &&
            objects.Collision.TrySnapDown(feet, 0.32f, height, maximumDrop, out supported, out surfaceY, maximumSurfaceY);
    }

    public float CameraDistance(Vector3 origin, Vector3 direction, float maximum)
    {
        return _level.Objects is { } objects && objects.Collision.Raycast(origin, direction, maximum, out float distance)
            ? Math.Max(0, distance - 0.15f) : maximum;
    }

    private bool TryGetHeight(float x, float z, int step, out float height)
    {
        height = 0;
        var d = _level.Manifest.Terrain;
        if (!float.IsFinite(x) || !float.IsFinite(z) || x < 0 || z < 0 || x > d.WorldSizeX || z > d.WorldSizeZ || d.CellSize <= 0)
            return false;
        float sampleX = x / d.CellSize, sampleZ = z / d.CellSize;
        // Match TerrainMeshBuilder's step and triangle diagonal, including the last, shortened cell.
        int x0 = Math.Min((int)(sampleX / step) * step, (d.SamplesX - 2) / step * step);
        int z0 = Math.Min((int)(sampleZ / step) * step, (d.SamplesZ - 2) / step * step);
        int x1 = Math.Min(x0 + step, d.SamplesX - 1), z1 = Math.Min(z0 + step, d.SamplesZ - 1);
        if (!Sample(x0, z0, out float a) || !Sample(x1, z0, out float b)
            || !Sample(x0, z1, out float c) || !Sample(x1, z1, out float e)) return false;
        height = Interpolate(a, b, c, e, (sampleX - x0) / (x1 - x0), (sampleZ - z0) / (z1 - z0));
        return true;
    }

    public static float Interpolate(float a, float b, float c, float d, float u, float v) =>
        u + v <= 1 ? a + (b - a) * u + (c - a) * v : d + (c - d) * (1 - u) + (b - d) * (1 - v);

    private bool Sample(int x, int z, out float height)
    {
        height = 0;
        var d = _level.Manifest.Terrain;
        var key = (x / d.TileSize, z / d.TileSize);
        if (!_references.TryGetValue(key, out var reference)) return false;
        if (!_tiles.TryGetValue(key, out var tile))
        {
            tile = TerrainTileReader.Read(AssetPathResolver.Resolve(_level.Directory, reference.File));
            if (tile.X != key.Item1 || tile.Z != key.Item2) throw new InvalidDataException("Terrain tile coordinates mismatch.");
            if (_tiles.Count >= 16) _tiles.Remove(_order.Dequeue());
            _tiles.Add(key, tile); _order.Enqueue(key);
        }
        height = d.DecodeHeight(tile.Heights[(z % d.TileSize) * d.TileSize + x % d.TileSize]);
        return true;
    }
}
