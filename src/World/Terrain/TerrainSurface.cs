using System.Numerics;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Terrain.Storage;
using ZoneReborn.World.Vegetation;

namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainSurface
{
    private readonly LoadedLevel _level;
    private readonly Dictionary<(int X, int Z), TerrainTileReference> _references;
    private readonly Dictionary<(int X, int Z), TerrainTileData> _tiles = [];
    private readonly Queue<(int X, int Z)> _order = new();
    private SceneCollision? _playCollision;
    private bool _hasVegetationCollision;
    private bool _hasRoadCollision;
    private SceneCollision? Collision => _playCollision ?? _level.Objects?.Collision;

    public TerrainSurface(LoadedLevel level)
    {
        _level = level;
        _references = level.Manifest.Tiles.ToDictionary(t => (t.X, t.Z));
    }

    public bool TryGetHeight(float x, float z, out float height) => TryGetHeight(x, z, _level.Manifest.Terrain.MeshStep, out height);

    public bool TryGetHeight(float x, float z, Func<int, int, float> sample, out float height) =>
        TryGetHeight(x, z, _level.Manifest.Terrain.MeshStep, out height, sample);

    public bool TryGetSourceHeight(float x, float z, out float height) => TryGetHeight(x, z, 1, out height);

    /// <summary>Road source profile: never samples its own effective terrain deformation.</summary>
    public bool TryGetBaseHeight(float x, float z, out float height) =>
        _level.TerrainDocument is { } document
            ? TryGetHeight(x, z, _level.Manifest.Terrain.MeshStep, out height, document.BaseHeight)
            : TryGetHeight(x, z, _level.Manifest.Terrain.MeshStep, out height);

    public void PreparePlayCollision(AssetPaths assets, CancellationToken cancellationToken = default)
    {
        _playCollision = VegetationCollision.Create(_level, assets, this, cancellationToken);
        _hasRoadCollision = _level.Roads.Roads.Any(
            road => road.Nodes.Count >= 2);
        _hasVegetationCollision = _level.Vegetation.Instances.Concat(_level.Vegetation.Generated.Select(item => item.Tree))
            .Any(tree => _level.Vegetation.Assets.TryGetValue(tree.Model, out var asset) &&
                VegetationCategory.HasBarkCollision(asset.File) && asset.Parts.Any(part => part.Kind == TreePartKind.Branches));
    }

    public bool HasSceneObjects =>
        _level.Objects?.Instances.Count > 0 ||
        _hasVegetationCollision ||
        _hasRoadCollision;
    public bool HasTerrainHoles => _level.TerrainDocument?.Holes.Any == true;
    public bool Contains(float x, float z) => _level.Manifest.Terrain.Contains(x, z);

    // Editing and road attachment keep using the terrain-only queries above.
    // Gameplay selects the floor beneath the feet, rather than a roof above the player.
    public bool TryGetGround(float x, float z, float maximumY, out float height)
    {
        bool found = TryGetHeight(x, z, out height);
        if (Collision is { } collision &&
            collision.TryGround(x, z, maximumY, found ? height : -1e7f, out float modelHeight, 0.32f))
        {
            height = found ? Math.Max(height, modelHeight) : modelHeight;
            found = true;
        }
        return found;
    }

    public Vector3 MoveCharacter(
        Vector3 from,
        Vector3 target,
        float height)
    {
        const float radius = 0.32f;
        const float maximumStep = 0.32f;

        var collision = Collision;

        Vector3 moved = collision?.MoveCapsule(
            from,
            target,
            radius,
            height) ?? target;

        if (!TryGetHeight(
                moved.X,
                moved.Z,
                out float terrainHeight))
        {
            return moved;
        }

        if (moved.Y >= terrainHeight - 0.002f)
        {
            return moved;
        }

        bool hasPreviousGround = TryGetHeight(
            from.X,
            from.Z,
            out float previousGround);

        Vector3 previous = from;

        if (hasPreviousGround)
        {
            previous.Y = MathF.Max(
                from.Y,
                previousGround);
        }

        if (terrainHeight > from.Y + maximumStep)
        {
            return previous;
        }

        Vector3 corrected = moved with
        {
            Y = terrainHeight
        };

        if (collision == null ||
            collision.CanOccupy(
                corrected,
                radius,
                height))
        {
            return corrected;
        }

        return previous;
    }

    public bool CanOccupy(Vector3 feet, float height)
    {
        if (TryGetHeight(
                feet.X,
                feet.Z,
                out float terrainHeight) &&
            feet.Y < terrainHeight - 0.002f)
        {
            return false;
        }

        return Collision?.CanOccupy(
            feet,
            0.32f,
            height) ?? true;
    }

    public bool TrySnapToObject(Vector3 feet, float height, float maximumDrop, out Vector3 supported,
        float maximumSurfaceY = float.PositiveInfinity) =>
        TrySnapToObject(feet, height, maximumDrop, out supported, out _, maximumSurfaceY);

    public bool TrySnapToObject(Vector3 feet, float height, float maximumDrop, out Vector3 supported,
        out float surfaceY, float maximumSurfaceY = float.PositiveInfinity)
    {
        supported = feet;
        surfaceY = feet.Y;
        return Collision is { } collision &&
            collision.TrySnapDown(feet, 0.32f, height, maximumDrop, out supported, out surfaceY, maximumSurfaceY);
    }

    public float CameraDistance(Vector3 origin, Vector3 direction, float maximum)
    {
        return Collision is { } collision && collision.Raycast(origin, direction, maximum, out float distance)
            ? Math.Max(0, distance - 0.15f) : maximum;
    }

    private bool TryGetHeight(float x, float z, int step, out float height, Func<int, int, float>? sample = null)
    {
        height = 0;
        var d = _level.Manifest.Terrain;
        if (!d.Contains(x, z) || d.CellSize <= 0)
            return false;
        if (_level.TerrainDocument is { } document && !document.HasSurface(x, z)) return false;
        float sampleX = (x - d.OriginX) / d.CellSize, sampleZ = (z - d.OriginZ) / d.CellSize;
        // Match TerrainMeshBuilder's step and triangle diagonal, including the last, shortened cell.
        int x0 = Math.Min((int)(sampleX / step) * step, (d.SamplesX - 2) / step * step);
        int z0 = Math.Min((int)(sampleZ / step) * step, (d.SamplesZ - 2) / step * step);
        int x1 = Math.Min(x0 + step, d.SamplesX - 1), z1 = Math.Min(z0 + step, d.SamplesZ - 1);
        if (!Read(x0, z0, out float a) || !Read(x1, z0, out float b)
            || !Read(x0, z1, out float c) || !Read(x1, z1, out float e)) return false;
        height = Interpolate(a, b, c, e, (sampleX - x0) / (x1 - x0), (sampleZ - z0) / (z1 - z0));
        return float.IsFinite(height);

        bool Read(int gx, int gz, out float value)
        {
            if (sample == null) return Sample(gx, gz, out value);
            value = sample(gx, gz);
            return float.IsFinite(value);
        }
    }

    public static float Interpolate(float a, float b, float c, float d, float u, float v) =>
        u + v <= 1 ? a + (b - a) * u + (c - a) * v : d + (c - d) * (1 - u) + (b - d) * (1 - v);

    private bool Sample(int x, int z, out float height)
    {
        height = 0;
        var d = _level.Manifest.Terrain;
        if (_level.TerrainDocument is { } document) { height = document.Height(x, z); return true; }
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
