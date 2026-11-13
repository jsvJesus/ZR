using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Terrain;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Vegetation;

public static class VegetationScatterGenerator
{
    private const int MaximumCandidatesPerTile = 4000;

    public static void ValidateRule(VegetationScatterRule rule)
    {
        if (rule.Id == Guid.Empty ||
            string.IsNullOrWhiteSpace(rule.Name) ||
            !Enum.IsDefined(rule.Kind) ||
            rule.MaterialIndex < 0 ||
            rule.MaterialIndex >= 255 ||
            !rule.Model.StartsWith(
                "speedtree/",
                StringComparison.OrdinalIgnoreCase) ||
            !Path.GetExtension(rule.Model).Equals(
                ".ctree",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Некорректное правило SpeedTree.");
        }

        if (!InRange(rule.DensityPer100SquareMeters, 0, 50) ||
            !InRange(rule.MinimumScale, 0.05f, 100) ||
            !InRange(rule.MaximumScale, rule.MinimumScale, 100) ||
            !InRange(rule.MinimumMaterialWeight, 0.01f, 1) ||
            !InRange(rule.MaximumSlopeDegrees, 0, 85) ||
            !InRange(rule.ObjectClearance, 0, 30) ||
            !InRange(rule.RoadClearance, 0, 30) ||
            !InRange(rule.MinimumSpacing, 0, 30) ||
            !InRange(rule.MaximumShoreDistance, 0.5f, 100))
        {
            throw new InvalidDataException(
                "Некорректная плотность, масштаб или расстояния.");
        }
    }

    public static int RegenerateTile(
        LoadedLevel level,
        AssetPaths assets,
        int tileX,
        int tileZ,
        CancellationToken token = default)
    {
        var document = level.TerrainDocument
            ?? throw new InvalidOperationException(
                "Terrain не загружен.");

        var scene = level.Vegetation;

        if (!scene.CanSave)
        {
            throw new InvalidOperationException(
                "Растительность недоступна для сохранения.");
        }

        var descriptor = document.Descriptor;

        if (tileX < 0 || tileZ < 0 ||
            tileX >= descriptor.TilesX ||
            tileZ >= descriptor.TilesZ)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tileX),
                "Тайл вне уровня.");
        }

        var rules = scene.ScatterRules
            .Where(rule => rule.Enabled)
            .OrderBy(rule => rule.Id)
            .ToArray();

        foreach (var rule in rules)
        {
            ValidateRule(rule);

            if (rule.MaterialIndex >= document.Layers.Count)
            {
                throw new InvalidOperationException(
                    $"Материал правила «{rule.Name}» не найден.");
            }

            if (document.Layers[rule.MaterialIndex].IsArchived)
            {
                throw new InvalidOperationException(
                    $"Материал правила «{rule.Name}» находится в архиве.");
            }

            if (!scene.Assets.ContainsKey(rule.Model))
            {
                var asset = SpeedTreeReader.Read(
                    assets,
                    rule.Model,
                    token);

                scene.Assets.Add(rule.Model, asset);
            }
        }

        float tileSize = 256f * descriptor.CellSize;

        float minX = tileX * tileSize;
        float minZ = tileZ * tileSize;

        float maxX = Math.Min(
            minX + tileSize,
            descriptor.WorldSizeX);

        float maxZ = Math.Min(
            minZ + tileSize,
            descriptor.WorldSizeZ);

        var surface = new TerrainSurface(level);
        var tile = document.Tile(tileX, tileZ);

        var generated = new List<GeneratedVegetationInstance>();
        var occupied = new PlacementGrid();

        float maximumCandidateRadius = rules
            .Select(rule =>
                Footprint(
                    scene.Assets[rule.Model],
                    rule.MaximumScale) +
                rule.MinimumSpacing)
            .DefaultIfEmpty(0f)
            .Max();

        foreach (var tree in scene.Instances)
        {
            if (scene.Assets.TryGetValue(tree.Model, out var asset))
            {
                float radius = Footprint(asset, tree.Scale);

                if (CanInfluenceTile(
                        tree,
                        radius + maximumCandidateRadius,
                        minX,
                        minZ,
                        maxX,
                        maxZ))
                {
                    occupied.Add(tree.X, tree.Z, radius);
                }
            }
        }

        foreach (var item in scene.Generated)
        {
            if (item.TileX == tileX && item.TileZ == tileZ)
            {
                continue;
            }

            var tree = item.Tree;

            if (scene.Assets.TryGetValue(tree.Model, out var asset))
            {
                float spacing = scene.ScatterRules
                    .FirstOrDefault(rule => rule.Id == item.RuleId)?
                    .MinimumSpacing ?? 0f;

                float radius = Footprint(asset, tree.Scale) + spacing;

                if (CanInfluenceTile(
                        tree,
                        radius + maximumCandidateRadius,
                        minX,
                        minZ,
                        maxX,
                        maxZ))
                {
                    occupied.Add(tree.X, tree.Z, radius);
                }
            }
        }

        var roads = BuildRoadSegments(level, minX, minZ, maxX, maxZ);

        foreach (var rule in rules)
        {
            token.ThrowIfCancellationRequested();

            var asset = scene.Assets[rule.Model];

            float area = (maxX - minX) * (maxZ - minZ);

            float expected = area / 100f *
                rule.DensityPer100SquareMeters;

            if (expected > MaximumCandidatesPerTile)
            {
                throw new InvalidOperationException(
                    $"Правило «{rule.Name}» создаёт слишком много " +
                    "кандидатов для одного тайла. Уменьшите плотность.");
            }

            uint random = CreateSeed(
                rule,
                tileX,
                tileZ);

            int count = (int)MathF.Floor(expected);

            if (NextFloat(ref random) < expected - count)
            {
                count++;
            }

            for (int candidate = 0; candidate < count; candidate++)
            {
                token.ThrowIfCancellationRequested();

                float x = minX +
                    NextFloat(ref random) * (maxX - minX);

                float z = minZ +
                    NextFloat(ref random) * (maxZ - minZ);

                float scale = rule.MinimumScale +
                    NextFloat(ref random) *
                    (rule.MaximumScale - rule.MinimumScale);

                float yaw = NextFloat(ref random) * 360f;

                if (!surface.TryGetHeight(x, z, out float height))
                {
                    continue;
                }

                if (!document.HasSurface(x, z))
                {
                    continue;
                }

                if (!MaterialAllowed(
                    document,
                    tile,
                    rule,
                    tileX,
                    tileZ,
                    x,
                    z))
                {
                    continue;
                }

                if (!WaterAllowed(
                    document,
                    rule,
                    x,
                    z,
                    height))
                {
                    continue;
                }

                if (!SlopeAllowed(
                    document,
                    rule,
                    x,
                    z))
                {
                    continue;
                }

                float footprint = Footprint(asset, scale);
                float spacing = footprint + rule.MinimumSpacing;

                if (occupied.Intersects(x, z, spacing))
                {
                    continue;
                }

                if (IntersectsRoad(
                    roads,
                    x,
                    z,
                    footprint + rule.RoadClearance))
                {
                    continue;
                }

                if (IntersectsObject(
                    level.Objects,
                    x,
                    z,
                    height,
                    asset,
                    scale,
                    rule.ObjectClearance))
                {
                    continue;
                }

                Guid id = DeterministicId(
                    rule.Id,
                    rule.Seed,
                    tileX,
                    tileZ,
                    candidate);

                var tree = new TreeInstance
                {
                    Id = id,
                    Model = rule.Model,
                    X = x,
                    Y = height,
                    Z = z,
                    Yaw = yaw,
                    Scale = scale,
                    FollowTerrain = true
                };

                VegetationStorage.Validate(tree);

                generated.Add(new GeneratedVegetationInstance
                {
                    RuleId = rule.Id,
                    TileX = tileX,
                    TileZ = tileZ,
                    Tree = tree
                });

                occupied.Add(x, z, spacing);
            }
        }

        int previous = scene.Generated.Count(item =>
            item.TileX == tileX && item.TileZ == tileZ);

        int resultCount =
            scene.Generated.Count - previous + generated.Count;

        if (resultCount > VegetationScatterStorage.MaximumGenerated)
        {
            throw new InvalidOperationException(
                "Превышен безопасный лимит генерации. " +
                "Уменьшите плотность растительности.");
        }

        scene.Generated.RemoveAll(item =>
            item.TileX == tileX && item.TileZ == tileZ);

        scene.Generated.AddRange(generated);

        if (previous > 0 || generated.Count > 0)
        {
            scene.IsDirty = true;
        }

        return generated.Count;
    }

    private static bool MaterialAllowed(
        TerrainDocument document,
        TerrainTileData tile,
        VegetationScatterRule rule,
        int tileX,
        int tileZ,
        float x,
        float z)
    {
        float pixelSize =
            256f / 248f * document.Descriptor.CellSize;

        int px = Math.Clamp(
            (int)MathF.Round(
                (x - tileX * 256f * document.Descriptor.CellSize)
                / pixelSize + 3.5f),
            0,
            255);

        int pz = Math.Clamp(
            (int)MathF.Round(
                (z - tileZ * 256f * document.Descriptor.CellSize)
                / pixelSize + 3.5f),
            0,
            255);

        float sum = 0f;
        float selectedWeight = 0f;
        float blockedWeight = 0f;

        foreach (byte index in tile.ActiveLayers)
        {
            if (index == 0 || index >= document.Layers.Count)
            {
                continue;
            }

            int group = (index - 1) / 3;
            int channel = (index - 1) % 3;

            var mask = tile.Masks.FirstOrDefault(
                item => item.Group == group);

            if (mask == null)
            {
                continue;
            }

            Vector3 weights = TerrainMaskCodec.Sample(
                mask.Data,
                px,
                pz);

            float weight = channel switch
            {
                0 => weights.X,
                1 => weights.Y,
                _ => weights.Z
            };

            sum += weight;
        }

        float normalization = Math.Max(1f, sum);

        foreach (byte index in tile.ActiveLayers)
        {
            if (index == 0 || index >= document.Layers.Count)
            {
                continue;
            }

            int group = (index - 1) / 3;
            int channel = (index - 1) % 3;

            var mask = tile.Masks.FirstOrDefault(
                item => item.Group == group);

            if (mask == null)
            {
                continue;
            }

            Vector3 weights = TerrainMaskCodec.Sample(
                mask.Data,
                px,
                pz);

            float weight = (channel switch
            {
                0 => weights.X,
                1 => weights.Y,
                _ => weights.Z
            }) / normalization;

            if (index == rule.MaterialIndex)
            {
                selectedWeight = weight;
            }

            var type = document.Layers[index].Settings.SurfaceType;

            if (type is TerrainSurfaceType.Water or
                TerrainSurfaceType.Rock or
                TerrainSurfaceType.Asphalt or
                TerrainSurfaceType.Glass)
            {
                blockedWeight += weight;
            }
        }

        float baseWeight = Math.Max(1f - sum, 0f);

        var baseType = document.Layers[0].Settings.SurfaceType;

        if (baseType is TerrainSurfaceType.Water or
            TerrainSurfaceType.Rock or
            TerrainSurfaceType.Asphalt or
            TerrainSurfaceType.Glass)
        {
            blockedWeight += baseWeight;
        }

        if (rule.MaterialIndex == 0)
        {
            selectedWeight = baseWeight;
        }

        return blockedWeight < 0.2f &&
            selectedWeight >= rule.MinimumMaterialWeight;
    }

    private static bool WaterAllowed(
        TerrainDocument document,
        VegetationScatterRule rule,
        float x,
        float z,
        float height)
    {
        bool nearWater = false;

        foreach (var water in document.WaterBodies)
        {
            if (water.Contains(x, z) &&
                height < water.SurfaceHeight)
            {
                return false;
            }

            float dx = x - water.CenterX;
            float dz = z - water.CenterZ;

            float distance = MathF.Sqrt(dx * dx + dz * dz);

            if (distance < 0.001f)
            {
                continue;
            }

            float normalized = MathF.Sqrt(
                dx * dx / (water.RadiusX * water.RadiusX) +
                dz * dz / (water.RadiusZ * water.RadiusZ));

            if (normalized < 0.001f)
            {
                continue;
            }

            float shorelineDistance = MathF.Abs(
                distance - distance / normalized);

            if (shorelineDistance <= rule.MaximumShoreDistance &&
                MathF.Abs(height - water.SurfaceHeight) <= 2.5f)
            {
                nearWater = true;
            }
        }

        return !rule.NearWaterOnly || nearWater;
    }

    private static bool SlopeAllowed(
        TerrainDocument document,
        VegetationScatterRule rule,
        float x,
        float z)
    {
        float step = Math.Max(
            document.Descriptor.CellSize,
            1f);

        float dx = (
            document.SampleWaterTerrainHeight(x + step, z) -
            document.SampleWaterTerrainHeight(x - step, z)
        ) / (2f * step);

        float dz = (
            document.SampleWaterTerrainHeight(x, z + step) -
            document.SampleWaterTerrainHeight(x, z - step)
        ) / (2f * step);

        float degrees = MathF.Atan(
            MathF.Sqrt(dx * dx + dz * dz)
        ) * 180f / MathF.PI;

        return degrees <= rule.MaximumSlopeDegrees;
    }

    private static float Footprint(
        TreeAsset asset,
        float scale)
    {
        float radius = Math.Max(
            Math.Max(
                MathF.Abs(asset.Minimum.X),
                MathF.Abs(asset.Maximum.X)),
            Math.Max(
                MathF.Abs(asset.Minimum.Z),
                MathF.Abs(asset.Maximum.Z)));

        return Math.Max(0.2f, radius * scale);
    }

    private static bool CanInfluenceTile(
        TreeInstance tree,
        float radius,
        float minX,
        float minZ,
        float maxX,
        float maxZ)
    {
        return tree.X + radius >= minX &&
               tree.X - radius <= maxX &&
               tree.Z + radius >= minZ &&
               tree.Z - radius <= maxZ;
    }

    private static bool IntersectsObject(
        SceneData? objects,
        float x,
        float z,
        float y,
        TreeAsset asset,
        float scale,
        float clearance)
    {
        if (objects == null)
        {
            return false;
        }

        float radius = Footprint(asset, scale) + clearance;

        float top = y + Math.Max(
            1f,
            asset.Maximum.Y * scale);

        var bounds = new SceneBounds(
            new Vector3(x - radius, y - 1f, z - radius),
            new Vector3(x + radius, top, z + radius));

        return objects.Query(bounds).Any();
    }

    private static List<RoadStrip> BuildRoadSegments(
        LoadedLevel level,
        float minX,
        float minZ,
        float maxX,
        float maxZ)
    {
        var result = new List<RoadStrip>();

        foreach (var road in level.Roads.Roads)
        {
            for (int i = 1; i < road.Nodes.Length; i++)
            {
                var a = road.Nodes[i - 1];
                var b = road.Nodes[i];

                float radius = Math.Max(a.Width, b.Width) * 0.5f;

                if (Math.Max(a.X, b.X) + radius < minX ||
                    Math.Min(a.X, b.X) - radius > maxX ||
                    Math.Max(a.Z, b.Z) + radius < minZ ||
                    Math.Min(a.Z, b.Z) - radius > maxZ)
                {
                    continue;
                }

                result.Add(new RoadStrip(
                    a.X,
                    a.Z,
                    b.X,
                    b.Z,
                    Math.Max(radius, 1f)));
            }
        }

        return result;
    }

    private static bool IntersectsRoad(
        IReadOnlyList<RoadStrip> roads,
        float x,
        float z,
        float radius)
    {
        foreach (var road in roads)
        {
            float dx = road.Bx - road.Ax;
            float dz = road.Bz - road.Az;

            float length = dx * dx + dz * dz;

            float t = length > 0.00001f
                ? Math.Clamp(
                    ((x - road.Ax) * dx +
                     (z - road.Az) * dz) / length,
                    0f,
                    1f)
                : 0f;

            float cx = road.Ax + dx * t;
            float cz = road.Az + dz * t;

            float distanceX = x - cx;
            float distanceZ = z - cz;
            float minimum = road.Radius + radius;

            if (distanceX * distanceX +
                distanceZ * distanceZ < minimum * minimum)
            {
                return true;
            }
        }

        return false;
    }

    private static uint CreateSeed(
        VegetationScatterRule rule,
        int tileX,
        int tileZ)
    {
        uint hash = 2166136261u;

        foreach (byte value in rule.Id.ToByteArray())
        {
            hash = (hash ^ value) * 16777619u;
        }

        hash = (hash ^ (uint)rule.Seed) * 16777619u;
        hash = (hash ^ (uint)tileX) * 16777619u;
        hash = (hash ^ (uint)tileZ) * 16777619u;

        return hash == 0 ? 1u : hash;
    }

    private static float NextFloat(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;

        return (state >> 8) * (1f / 16777216f);
    }

    private static Guid DeterministicId(
        Guid ruleId,
        int seed,
        int tileX,
        int tileZ,
        int candidate)
    {
        string key =
            $"{ruleId:N}:{seed}:{tileX}:{tileZ}:{candidate}";

        byte[] hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(key));

        return new Guid(hash.AsSpan(0, 16));
    }

    private static bool InRange(
        float value,
        float minimum,
        float maximum)
    {
        return float.IsFinite(value) &&
            value >= minimum &&
            value <= maximum;
    }

    private readonly record struct RoadStrip(
        float Ax,
        float Az,
        float Bx,
        float Bz,
        float Radius);

    private sealed class PlacementGrid
    {
        private const float CellSize = 8f;

        private readonly Dictionary<
            (int X, int Z),
            List<Placement>> _cells = [];

        public void Add(float x, float z, float radius)
        {
            radius = Math.Max(0f, radius);

            int minX = Cell(x - radius);
            int maxX = Cell(x + radius);

            int minZ = Cell(z - radius);
            int maxZ = Cell(z + radius);

            for (int cz = minZ; cz <= maxZ; cz++)
            {
                for (int cx = minX; cx <= maxX; cx++)
                {
                    var key = (cx, cz);

                    if (!_cells.TryGetValue(key, out var items))
                    {
                        items = [];
                        _cells.Add(key, items);
                    }

                    items.Add(new Placement(x, z, radius));
                }
            }
        }

        public bool Intersects(float x, float z, float radius)
        {
            int minX = Cell(x - radius);
            int maxX = Cell(x + radius);
            int minZ = Cell(z - radius);
            int maxZ = Cell(z + radius);

            var visited = new HashSet<Placement>();

            for (int cz = minZ; cz <= maxZ; cz++)
            {
                for (int cx = minX; cx <= maxX; cx++)
                {
                    if (!_cells.TryGetValue(
                        (cx, cz),
                        out var items))
                    {
                        continue;
                    }

                    foreach (var item in items)
                    {
                        if (!visited.Add(item))
                        {
                            continue;
                        }

                        float dx = item.X - x;
                        float dz = item.Z - z;
                        float minimum = item.Radius + radius;

                        if (dx * dx + dz * dz < minimum * minimum)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static int Cell(float value)
        {
            return (int)MathF.Floor(value / CellSize);
        }

        private readonly record struct Placement(
            float X,
            float Z,
            float Radius);
    }
}
