using System.Collections.Concurrent;
using System.Numerics;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Vegetation;

public static class GroundCoverGenerator
{
    public const int MaximumVisibleInstances = 60_000;
    public const float MaximumRadius = 128f;
    public const float MaximumBrushRadius = 100f;
    private static readonly ConcurrentDictionary<(string Root, string Model), Lazy<SceneBounds?>> BoundsCache = new();

    public static int Paint(LoadedLevel level, AssetPaths assets, Vector3 center, float radius, bool erase)
    {
        if (!level.GroundCover.CanSave) throw new InvalidOperationException("groundcover.json недоступен для изменений.");
        if (!Finite(center) || !float.IsFinite(radius) || radius <= 0f || level.TerrainDocument == null) return 0;
        radius = Math.Min(radius, MaximumBrushRadius);
        if (erase)
        {
            // Erase is safe cleanup: old overrides must remain removable after a
            // road/object/material/hole change, even with no models selected.
            return level.GroundCover.PaintCells(Cells(level, center, radius).Where(cell =>
                DistanceSquared(Math.Clamp(center.X, cell.X, cell.X + 1f),
                    Math.Clamp(center.Z, cell.Z, cell.Z + 1f), center) <= radius * radius), false);
        }
        var settings = level.GroundCover.Settings;
        var context = new PlacementContext(level, assets, settings, center, radius);
        var cells = new List<(int X, int Z)>();
        foreach (var cell in Cells(level, center, radius))
        {
            // Painting is a one-metre mask operation. Validate the same actual jittered
            // candidates as runtime; invalid parts of a cell can never emit instances.
            bool allowed = false;
            for (int candidate = 0; candidate < 16 && !allowed; candidate++)
            {
                var position = Candidate(settings, cell.X, cell.Z, candidate);
                if (DistanceSquared(position.X, position.Y, center) > radius * radius) continue;
                allowed = context.TryPlace(cell.X, cell.Z, candidate, out _);
            }
            if (allowed) cells.Add((cell.X, cell.Z));
        }
        return level.GroundCover.PaintCells(cells, true);
    }

    public static IReadOnlyList<SceneInstance> BuildVisible(LoadedLevel level, AssetPaths assets,
        Vector3 cameraPosition, float radius, int maximumInstances = MaximumVisibleInstances)
    {
        if (!level.GroundCover.CanSave || level.TerrainDocument == null || !Finite(cameraPosition) ||
            !float.IsFinite(radius) || radius <= 0f || maximumInstances <= 0) return Array.Empty<SceneInstance>();
        var settings = level.GroundCover.Settings;
        if (settings.Models.Length == 0) return Array.Empty<SceneInstance>();
        // The renderer centers an eight-metre camera chunk and owns the exact fade.
        radius = Math.Min(radius, Math.Min(settings.DrawDistance + 8f, MaximumRadius));
        maximumInstances = Math.Min(maximumInstances, MaximumVisibleInstances);
        var context = new PlacementContext(level, assets, settings, cameraPosition, radius);
        var result = new List<SceneInstance>(Math.Min(maximumInstances, 8192));
        foreach (var cell in Cells(level, cameraPosition, radius)
            .OrderBy(cell => DistanceSquared(cell.X + 0.5f, cell.Z + 0.5f, cameraPosition)))
        {
            if (!level.GroundCover.Enabled(cell.X, cell.Z)) continue;
            float density = settings.DensityPerSquareMeter;
            int count = (int)density;
            if (Random(Hash(settings.Seed, cell.X, cell.Z, 17)) < density - count) count++;
            for (int candidate = 0; candidate < count; candidate++)
            {
                var position = Candidate(settings, cell.X, cell.Z, candidate);
                if (DistanceSquared(position.X, position.Y, cameraPosition) > radius * radius ||
                    !context.TryPlace(cell.X, cell.Z, candidate, out var instance)) continue;
                result.Add(instance!);
                if (result.Count == maximumInstances) return result.ToArray();
            }
        }
        return result.ToArray();
    }

    private static IEnumerable<(int X, int Z)> Cells(LoadedLevel level, Vector3 center, float radius)
    {
        var descriptor = level.TerrainDocument!.Descriptor;
        int minX = (int)MathF.Floor(Math.Max(center.X - radius, descriptor.OriginX));
        int maxX = (int)MathF.Floor(Math.Min(center.X + radius, descriptor.MaxX));
        int minZ = (int)MathF.Floor(Math.Max(center.Z - radius, descriptor.OriginZ));
        int maxZ = (int)MathF.Floor(Math.Min(center.Z + radius, descriptor.MaxZ));
        for (int z = minZ; z <= maxZ; z++)
        for (int x = minX; x <= maxX; x++)
            yield return (x, z);
    }

    private static Vector2 Candidate(GroundCoverSettings settings, int x, int z, int candidate)
    {
        uint hash = Hash(settings.Seed, x, z, candidate);
        int slot = (candidate * 5 + (int)(Hash(settings.Seed, x, z, 19) & 15)) & 15;
        return new(x + ((slot & 3) + Random(hash)) * 0.25f,
            z + ((slot >> 2) + Random(Mix(hash))) * 0.25f);
    }

    private sealed class PlacementContext
    {
        private readonly LoadedLevel _level;
        private readonly TerrainSurface _surface;
        private readonly GroundCoverSettings _settings;
        private readonly (string File, SceneBounds? Bounds)[] _models;
        private readonly VegetationRoadExclusion _roads;
        private readonly Dictionary<(int X, int Z), List<SceneBounds>> _objects = [];

        public PlacementContext(LoadedLevel level, AssetPaths assets, GroundCoverSettings settings, Vector3 center, float radius)
        {
            _level = level;
            _settings = settings;
            _surface = new TerrainSurface(level);
            _models = settings.Models.Select(file => (file, Bounds(assets, file))).ToArray();
            foreach (var model in _models.Where(model => !model.Bounds.HasValue))
            {
                string warning = $"Ground cover: не прочитаны корректные границы модели {model.File}.";
                if (!level.GroundCover.Warnings.Contains(warning)) level.GroundCover.Warnings.Add(warning);
            }
            float margin = _models.Where(model => model.Bounds.HasValue)
                .Select(model => Footprint(model.Bounds!.Value) * settings.MaximumScale)
                .DefaultIfEmpty(0f).Max();
            _roads = VegetationRoadExclusion.ForTile(level.Roads, assets,
                center.X - radius, center.Z - radius, center.X + radius, center.Z + radius,
                margin + settings.RoadClearance, CancellationToken.None);
            if (level.Objects == null) return;
            float area = radius + margin + settings.ObjectClearance;
            var query = new SceneBounds(new(center.X - area, -10_000_000f, center.Z - area),
                new(center.X + area, 10_000_000f, center.Z + area));
            foreach (var instance in level.Objects.Query(query))
            {
                var bounds = instance.Bounds;
                int minX = Grid(Math.Max(bounds.Minimum.X, query.Minimum.X));
                int maxX = Grid(Math.Min(bounds.Maximum.X, query.Maximum.X));
                int minZ = Grid(Math.Max(bounds.Minimum.Z, query.Minimum.Z));
                int maxZ = Grid(Math.Min(bounds.Maximum.Z, query.Maximum.Z));
                for (int z = minZ; z <= maxZ; z++)
                for (int x = minX; x <= maxX; x++)
                {
                    if (!_objects.TryGetValue((x, z), out var list)) _objects[(x, z)] = list = [];
                    list.Add(bounds);
                }
            }
        }

        public bool TryPlace(int x, int z, int candidate, out SceneInstance? instance)
        {
            instance = null;
            if (_models.Length == 0) return false;
            uint hash = Hash(_settings.Seed, x, z, candidate);
            var model = _models[(int)(Mix(Mix(hash)) % (uint)_models.Length)];
            if (model.Bounds is not { } bounds) return false;
            Vector2 position = Candidate(_settings, x, z, candidate);
            float scale = _settings.MinimumScale + Random(Mix(Mix(Mix(hash)))) *
                (_settings.MaximumScale - _settings.MinimumScale);
            float yaw = Random(Mix(Mix(Mix(Mix(hash))))) * MathF.Tau;
            if (!Allowed(position.X, position.Y, bounds, scale, yaw, out var world)) return false;
            Matrix4x4.Invert(world, out var inverse);
            instance = new(model.File, world, inverse, bounds.Transform(world), false)
            {
                Id = $"groundcover/{_settings.Seed}/{x}/{z}/{candidate}",
                LocalBounds = bounds, VisualLocalBounds = bounds
            };
            return true;
        }

        public bool Allowed(float x, float z, SceneBounds localBounds, float scale, float yaw, out Matrix4x4 world)
        {
            world = Matrix4x4.Identity;
            var document = _level.TerrainDocument!;
            if (!_surface.TryGetHeight(x, z, out float height) || !document.HasSurface(x, z) ||
                !VegetationScatterGenerator.MaterialAllowed(document, _settings.MaterialIndex,
                    _settings.MinimumMaterialWeight, x, z) ||
                !VegetationScatterGenerator.SlopeAllowed(document, _settings.MaximumSlopeDegrees, x, z)) return false;
            float footprint = Footprint(localBounds) * scale;
            if (_roads.Intersects(x, z, footprint, _settings.RoadClearance)) return false;
            world = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(x, height, z);
            var bounds = localBounds.Transform(world);
            var descriptor = document.Descriptor;
            if (bounds.Minimum.X < descriptor.OriginX || bounds.Minimum.Z < descriptor.OriginZ ||
                bounds.Maximum.X > descriptor.MaxX || bounds.Maximum.Z > descriptor.MaxZ ||
                IntersectsHole(document, bounds)) return false;
            float? minimumTerrainHeight = null;
            foreach (var water in document.WaterBodies)
            {
                // Nearest point detects even an ellipse entirely inside a broad
                // patch, where its centre and all footprint corners remain dry.
                if (!water.Contains(Math.Clamp(water.CenterX, bounds.Minimum.X, bounds.Maximum.X),
                    Math.Clamp(water.CenterZ, bounds.Minimum.Z, bounds.Maximum.Z))) continue;
                minimumTerrainHeight ??= MinimumSupportingHeight(document, bounds, _level.Manifest.Terrain.MeshStep);
                if (minimumTerrainHeight <= water.SurfaceHeight + 0.01f) return false;
            }
            float clearance = _settings.ObjectClearance;
            bounds = new(bounds.Minimum - new Vector3(clearance, 0.05f, clearance),
                bounds.Maximum + new Vector3(clearance, 0.05f, clearance));
            for (int gz = Grid(bounds.Minimum.Z); gz <= Grid(bounds.Maximum.Z); gz++)
            for (int gx = Grid(bounds.Minimum.X); gx <= Grid(bounds.Maximum.X); gx++)
                if (_objects.TryGetValue((gx, gz), out var objects))
                    foreach (var obstacle in objects)
                        if (bounds.Intersects(obstacle)) return false;
            return true;
        }

        private static int Grid(float value) => (int)MathF.Floor(value / 8f);
    }

    private static bool IntersectsHole(TerrainDocument document, SceneBounds bounds)
    {
        var holes = document.Holes;
        if (!holes.Any) return false;
        var descriptor = document.Descriptor;
        float minX = (bounds.Minimum.X - descriptor.OriginX) / descriptor.CellSize;
        float maxX = (bounds.Maximum.X - descriptor.OriginX) / descriptor.CellSize;
        float minZ = (bounds.Minimum.Z - descriptor.OriginZ) / descriptor.CellSize;
        float maxZ = (bounds.Maximum.Z - descriptor.OriginZ) / descriptor.CellSize;
        int firstX = Math.Clamp((int)MathF.Floor(minX), 0, holes.Width - 1);
        int lastX = Math.Clamp((int)MathF.Floor(maxX), 0, holes.Width - 1);
        int firstZ = Math.Clamp((int)MathF.Floor(minZ), 0, holes.Height - 1);
        int lastZ = Math.Clamp((int)MathF.Floor(maxZ), 0, holes.Height - 1);
        for (int z = firstZ; z <= lastZ; z++)
        for (int x = firstX; x <= lastX; x++)
        {
            ushort mask = holes.Mask(x, z);
            if (mask == 0) continue;
            int sx0 = Math.Clamp((int)MathF.Floor((minX - x) * TerrainHoles.Detail), 0, TerrainHoles.Detail - 1);
            int sx1 = Math.Clamp((int)MathF.Floor((maxX - x) * TerrainHoles.Detail), 0, TerrainHoles.Detail - 1);
            int sz0 = Math.Clamp((int)MathF.Floor((minZ - z) * TerrainHoles.Detail), 0, TerrainHoles.Detail - 1);
            int sz1 = Math.Clamp((int)MathF.Floor((maxZ - z) * TerrainHoles.Detail), 0, TerrainHoles.Detail - 1);
            int row = ((1 << (sx1 - sx0 + 1)) - 1) << sx0;
            for (int subZ = sz0; subZ <= sz1; subZ++)
                if ((mask & (row << (subZ * TerrainHoles.Detail))) != 0) return true;
        }
        return false;
    }

    private static float MinimumSupportingHeight(TerrainDocument document, SceneBounds bounds, int meshStep)
    {
        var descriptor = document.Descriptor;
        meshStep = Math.Max(1, meshStep);
        int minX = Math.Max(0, (int)MathF.Floor((bounds.Minimum.X - descriptor.OriginX) /
            (descriptor.CellSize * meshStep)) * meshStep);
        int maxX = Math.Min(descriptor.SamplesX - 1, (int)MathF.Ceiling((bounds.Maximum.X - descriptor.OriginX) /
            (descriptor.CellSize * meshStep)) * meshStep);
        int minZ = Math.Max(0, (int)MathF.Floor((bounds.Minimum.Z - descriptor.OriginZ) /
            (descriptor.CellSize * meshStep)) * meshStep);
        int maxZ = Math.Min(descriptor.SamplesZ - 1, (int)MathF.Ceiling((bounds.Maximum.Z - descriptor.OriginZ) /
            (descriptor.CellSize * meshStep)) * meshStep);
        float minimum = float.PositiveInfinity;
        // Every rendered triangle under the footprint is a convex combination
        // of these supporting mesh vertices. Their minimum is a safe lower bound;
        // unrelated submerged terrain can conservatively exclude a dry edge.
        for (int z = minZ; ; z = Math.Min(z + meshStep, maxZ))
        {
            for (int x = minX; ; x = Math.Min(x + meshStep, maxX))
            {
                minimum = Math.Min(minimum, document.Height(x, z));
                if (x == maxX) break;
            }
            if (z == maxZ) break;
        }
        return minimum;
    }

    private static SceneBounds? Bounds(AssetPaths assets, string file) =>
        BoundsCache.GetOrAdd((assets.Root.ToLowerInvariant(), file.ToLowerInvariant()), _ => new(() =>
        {
            try
            {
                SceneBounds bounds = RoadCatalog.ReadModelBounds(assets, file);
                if (!Finite(bounds.Minimum) || !Finite(bounds.Maximum) ||
                    bounds.Minimum.X > bounds.Maximum.X || bounds.Minimum.Y > bounds.Maximum.Y ||
                    bounds.Minimum.Z > bounds.Maximum.Z || Footprint(bounds) > 50f ||
                    Vector3.Abs(bounds.Minimum).Y > 100f || Vector3.Abs(bounds.Maximum).Y > 100f) return null;
                return bounds;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
                FormatException or OverflowException or ArgumentException or System.Xml.XmlException) { return null; }
        })).Value;

    private static float Footprint(SceneBounds bounds) => new Vector2(
        Math.Max(Math.Abs(bounds.Minimum.X), Math.Abs(bounds.Maximum.X)),
        Math.Max(Math.Abs(bounds.Minimum.Z), Math.Abs(bounds.Maximum.Z))).Length();
    private static bool Finite(Vector3 point) => float.IsFinite(point.X) && float.IsFinite(point.Y) && float.IsFinite(point.Z) &&
        Math.Abs(point.X) <= 10_000_000f && Math.Abs(point.Y) <= 10_000_000f && Math.Abs(point.Z) <= 10_000_000f;
    private static float DistanceSquared(float x, float z, Vector3 center) =>
        (x - center.X) * (x - center.X) + (z - center.Z) * (z - center.Z);
    private static uint Hash(int seed, int x, int z, int candidate) =>
        Mix(unchecked((uint)seed ^ (uint)x * 0x9e3779b9u ^ (uint)z * 0x85ebca6bu ^ (uint)candidate * 0xc2b2ae35u));
    private static uint Mix(uint value)
    {
        value ^= value >> 16; value = unchecked(value * 0x7feb352du);
        value ^= value >> 15; value = unchecked(value * 0x846ca68bu);
        return value ^ (value >> 16);
    }
    private static float Random(uint value) => (value >> 8) * (1f / 16777216f);
}
