using System.Numerics;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Vegetation;

public static class VegetationCollision
{
    private const int MaximumRoadCollisionTriangles = 2_000_000;

    public static SceneCollision Create(
        LoadedLevel level,
        AssetPaths assets,
        TerrainSurface surface,
        CancellationToken cancellationToken = default)
    {
        var instances = level.Objects?.Instances.ToList()
            ?? new List<SceneInstance>();

        var physics = level.Objects?.Physics
            ?? new ScenePhysicsCache(assets, _ => { });

        var meshes = new Dictionary<string, ScenePhysicsMesh?>(
            StringComparer.OrdinalIgnoreCase);

        var clearance = RoadClearance.Build(
            level.Roads,
            cancellationToken);

        foreach (var entry in level.Vegetation.Instances
            .Select(tree => (Tree: tree, Generated: false))
            .Concat(level.Vegetation.Generated.Select(
                item => (Tree: item.Tree, Generated: true))))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tree = entry.Tree;

            if (!level.Vegetation.Assets.TryGetValue(
                    tree.Model,
                    out var asset))
            {
                continue;
            }

            if (entry.Generated)
            {
                float footprint = MathF.Max(
                    MathF.Max(
                        MathF.Abs(asset.Minimum.X),
                        MathF.Abs(asset.Maximum.X)),
                    MathF.Max(
                        MathF.Abs(asset.Minimum.Z),
                        MathF.Abs(asset.Maximum.Z)));

                footprint = MathF.Max(
                    0.2f,
                    footprint * tree.Scale);

                if (clearance.Intersects(
                    tree.X,
                    tree.Z,
                    footprint))
                {
                    continue;
                }
            }

            if (!VegetationCategory.HasBarkCollision(asset.File))
            {
                continue;
            }

            if (!meshes.TryGetValue(asset.File, out var mesh))
            {
                mesh = Bark(asset, cancellationToken);
                meshes.Add(asset.File, mesh);

                if (mesh != null)
                {
                    physics.Store(asset.File, mesh);
                }
            }

            if (mesh == null)
            {
                continue;
            }

            float y = tree.Y;

            if (tree.FollowTerrain &&
                surface.TryGetHeight(
                    tree.X,
                    tree.Z,
                    out float ground))
            {
                y = ground + tree.HeightOffset;
            }

            var world = tree.World(y);

            if (!Matrix4x4.Invert(world, out var inverse))
            {
                continue;
            }

            instances.Add(new SceneInstance(
                asset.File,
                world,
                inverse,
                mesh.Bounds.Transform(world),
                true)
            {
                Id = "vegetation/" + tree.Id.ToString("N"),
                LocalBounds = mesh.Bounds,
                VisualLocalBounds = new SceneBounds(
                    asset.Minimum,
                    asset.Maximum)
            });
        }

        int collisionTriangles = 0;

        foreach (var road in level.Roads.Roads)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (road.Nodes.Count < 2)
            {
                continue;
            }

            RoadStorage.Validate(road);

            var sections = RoadMesh.Build(
                road,
                surface,
                cancellationToken);

            foreach (var section in sections)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (section.Vertices.Length == 0 ||
                    section.Indices.Length == 0)
                {
                    continue;
                }

                int triangleCount = section.Indices.Length / 3;

                if (triangleCount >
                    MaximumRoadCollisionTriangles - collisionTriangles)
                {
                    throw new InvalidDataException(
                        "Превышен лимит коллизии дорог уровня. " +
                        "Разделите слишком длинные дороги " +
                        "или уменьшите сложность геометрии.");
                }

                collisionTriangles += triangleCount;

                string model = "roads/generated/" +
                    road.Id + "/" + section.SegmentIndex;

                var vertices = new Vector3[
                    section.Vertices.Length];

                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = section.Vertices[i].Position;
                }

                var collision = new ScenePhysicsMesh(
                    model,
                    vertices,
                    section.Indices,
                    section.Bounds);

                physics.Store(model, collision);

                instances.Add(new SceneInstance(
                    model,
                    Matrix4x4.Identity,
                    Matrix4x4.Identity,
                    section.Bounds,
                    true)
                {
                    Id = model,
                    LocalBounds = section.Bounds,
                    VisualLocalBounds = section.Bounds
                });
            }
        }

        return new SceneData(
            assets,
            instances,
            physics: physics).Collision;
    }

    private static ScenePhysicsMesh? Bark(
        TreeAsset asset,
        CancellationToken cancellationToken)
    {
        var vertices = new List<Vector3>();
        var indices = new List<int>();

        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);

        foreach (var part in asset.Parts)
        {
            if (part.Kind != TreePartKind.Branches)
            {
                continue;
            }

            var strip = part.Lods.FirstOrDefault(
                lod => lod.Length >= 3);

            if (strip == null)
            {
                continue;
            }

            int offset = vertices.Count;

            vertices.AddRange(
                part.Vertices.Select(vertex => vertex.Position));

            for (int i = 2; i < strip.Length; i++)
            {
                if ((i & 4095) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                int a = strip[i - 2];
                int b = strip[i - 1];
                int c = strip[i];

                if ((i & 1) != 0)
                {
                    (a, b) = (b, a);
                }

                if (a == b || b == c || c == a)
                {
                    continue;
                }

                Vector3 pa = part.Vertices[a].Position;
                Vector3 pb = part.Vertices[b].Position;
                Vector3 pc = part.Vertices[c].Position;

                if (Vector3.Cross(
                        pb - pa,
                        pc - pa).LengthSquared() <= 1e-16f)
                {
                    continue;
                }

                indices.Add(offset + a);
                indices.Add(offset + b);
                indices.Add(offset + c);

                minimum = Vector3.Min(
                    minimum,
                    Vector3.Min(pa, Vector3.Min(pb, pc)));

                maximum = Vector3.Max(
                    maximum,
                    Vector3.Max(pa, Vector3.Max(pb, pc)));
            }
        }

        if (indices.Count == 0)
        {
            return null;
        }

        return new ScenePhysicsMesh(
            asset.File,
            vertices.ToArray(),
            indices.ToArray(),
            new SceneBounds(minimum, maximum))
        {
            IsVisualFallback = true
        };
    }
}
