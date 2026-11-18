using System.Numerics;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Roads;

public static class RoadSurfaceBuilder
{
    private const int MaximumVertices = 750000;
    private const float StepLength = 0.65f;
    private const float StepWidth = 0.45f;

    private sealed record Corridor(
        RoadCurve Curve,
        float Radius,
        Vector2 Minimum,
        Vector2 Maximum);

    private readonly record struct SurfacePoint(
        SceneVertex Vertex,
        float Clearance);

    public static RoadBuildResult Build(
        RoadNetwork network,
        RoadMaterialCatalog catalog,
        TerrainSurface surface)
    {
        var models = new List<SceneModel>();
        var instances = new List<SceneInstance>();

        var automobileCorridors = network.Roads
            .Where(road =>
                road.Category == RoadCategory.Automobile &&
                road.Points.Count >= 2)
            .Select(road => CreateCorridor(road))
            .Where(corridor => corridor != null)
            .Cast<Corridor>()
            .ToArray();

        foreach (RoadPath road in network.Roads)
        {
            if (road.Category == RoadCategory.Railway ||
                road.Points.Count < 2 ||
                !catalog.TryGet(road.Material, out RoadMaterialInfo material))
            {
                continue;
            }

            RoadCurve curve = RoadCurve.Create(
                road.Points,
                road.Width);

            if (curve.Length < 0.05f)
            {
                continue;
            }

            bool isTrail = road.Category == RoadCategory.Dirt;
            float half = road.Width * 0.5f;

            var meshes = new List<SceneMesh>();

            SceneMesh? center = CreateStrip(
                road,
                curve,
                surface,
                material,
                -half,
                half,
                false,
                isTrail ? automobileCorridors : []);

            if (center != null)
            {
                meshes.Add(center);
            }

            if (!isTrail &&
                road.ShoulderWidth > 0f &&
                catalog.TryGet(
                    road.ShoulderMaterial,
                    out RoadMaterialInfo shoulder))
            {
                SceneMesh? left = CreateStrip(
                    road,
                    curve,
                    surface,
                    shoulder,
                    -half - road.ShoulderWidth,
                    -half,
                    true,
                    []);

                SceneMesh? right = CreateStrip(
                    road,
                    curve,
                    surface,
                    shoulder,
                    half,
                    half + road.ShoulderWidth,
                    true,
                    []);

                if (left != null)
                {
                    meshes.Add(left);
                }

                if (right != null)
                {
                    meshes.Add(right);
                }
            }

            if (meshes.Count == 0)
            {
                continue;
            }

            var minimum = new Vector3(float.PositiveInfinity);
            var maximum = new Vector3(float.NegativeInfinity);

            foreach (SceneVertex vertex in meshes
                         .SelectMany(mesh => mesh.Vertices))
            {
                minimum = Vector3.Min(minimum, vertex.Position);
                maximum = Vector3.Max(maximum, vertex.Position);
            }

            string file =
                $"objects/roads_generated/surface_{road.Id:N}.model";

            var bounds = new SceneBounds(minimum, maximum);

            models.Add(new SceneModel(
                file,
                bounds,
                meshes.ToArray()));

            instances.Add(new SceneInstance(
                file,
                Matrix4x4.Identity,
                Matrix4x4.Identity,
                bounds,
                false)
            {
                Id = $"road/surface/{road.Id:N}",
                LocalBounds = bounds,
                VisualLocalBounds = bounds,
                FollowTerrain = false
            });
        }

        return new RoadBuildResult(
            instances.ToArray(),
            models.ToArray());
    }

    private static Corridor? CreateCorridor(RoadPath road)
    {
        RoadCurve curve = RoadCurve.Create(
            road.Points,
            road.Width);

        if (curve.Samples.Count < 2)
        {
            return null;
        }

        float radius =
            road.Width * 0.5f + road.ShoulderWidth;

        var minimum = new Vector2(float.PositiveInfinity);
        var maximum = new Vector2(float.NegativeInfinity);

        foreach (RoadCurveSample sample in curve.Samples)
        {
            minimum = Vector2.Min(minimum, sample.Position);
            maximum = Vector2.Max(maximum, sample.Position);
        }

        return new Corridor(
            curve,
            radius,
            minimum - new Vector2(radius),
            maximum + new Vector2(radius));
    }

    private static SceneMesh? CreateStrip(
        RoadPath road,
        RoadCurve curve,
        TerrainSurface surface,
        RoadMaterialInfo material,
        float left,
        float right,
        bool shoulder,
        IReadOnlyList<Corridor> blockers)
    {
        int along = Math.Max(
            1,
            (int)MathF.Ceiling(curve.Length / StepLength));

        int across = Math.Max(
            1,
            (int)MathF.Ceiling((right - left) / StepWidth));

        if ((long)along * across * 12 > MaximumVertices)
        {
            throw new InvalidOperationException(
                "Слишком много вершин дорожного покрытия.");
        }

        var vertices = new List<SceneVertex>();
        var indices = new List<int>();

        var sceneMaterial = new SceneMaterial(
            shoulder ? "Road shoulder" : "Road surface",
            material.Diffuse,
            material.Normal,
            Vector4.One,
            true,
            false,
            false,
            0.15f);

        for (int row = 0; row < along; row++)
        {
            float s0 = curve.Length * row / along;
            float s1 = curve.Length * (row + 1f) / along;

            for (int column = 0; column < across; column++)
            {
                float l0 = left + (right - left) * column / across;
                float l1 = left + (right - left) * (column + 1f) / across;

                if (!TrySample(s0, l0, out SurfacePoint a) ||
                    !TrySample(s1, l0, out SurfacePoint b) ||
                    !TrySample(s1, l1, out SurfacePoint c) ||
                    !TrySample(s0, l1, out SurfacePoint d))
                {
                    continue;
                }

                AddTriangle(a, c, b);
                AddTriangle(a, d, c);
            }
        }

        if (vertices.Count == 0)
        {
            return null;
        }

        return new SceneMesh(
            vertices.ToArray(),
            indices.ToArray(),
            sceneMaterial);

        bool TrySample(
            float distance,
            float lateral,
            out SurfacePoint result)
        {
            result = default;

            RoadCurveSample sample = curve.Evaluate(distance);
            Vector2 forward = sample.Direction;
            Vector2 sideways = new(-forward.Y, forward.X);

            Vector2 planar = sample.Position +
                             sideways * lateral;

            if (!surface.TryGetHeight(
                    planar.X,
                    planar.Y,
                    out float height))
            {
                return false;
            }

            float edgeRatio = road.Width > 0f
                ? Math.Clamp(MathF.Abs(lateral) /
                             (road.Width * 0.5f), 0f, 1f)
                : 1f;

            float lift;

            if (road.Category == RoadCategory.Dirt)
            {
                lift = 0.026f;
            }
            else if (shoulder)
            {
                float shoulderDistance =
                    (MathF.Abs(lateral) - road.Width * 0.5f) /
                    MathF.Max(road.ShoulderWidth, 0.001f);

                lift = 0.06f -
                       0.04f * Math.Clamp(
                           shoulderDistance, 0f, 1f);
            }
            else
            {
                lift = 0.095f - 0.035f * edgeRatio;
            }

            var position = new Vector3(
                planar.X,
                height + lift,
                planar.Y);

            Vector3 tangent = new(
                forward.X,
                0f,
                forward.Y);

            const float shoulderTileSize = 1.0f;

            float u;
            float v;

            if (shoulder)
            {
                // Одинаковый масштаб текстуры по обеим осям.
                // Один повтор соответствует 1 метру поверхности.
                u = distance / shoulderTileSize;
                v = lateral / shoulderTileSize;
            }
            else
            {
                // Основное полотно: квадратный масштаб текстуры.
                // Сохраняем полный поперечный рисунок дороги.
                float textureSize = MathF.Max(road.Width, 0.5f);

                u = distance / textureSize;
                v = (lateral - left) / (right - left);
            }

            float clearance = blockers.Count == 0
                ? float.PositiveInfinity
                : DistanceOutsideCorridors(planar, blockers);

            result = new SurfacePoint(
                new SceneVertex(
                    position,
                    Vector3.UnitY,
                    new Vector2(u, v),
                    new Vector4(tangent, -1f)),
                clearance);

            return true;
        }

        void AddTriangle(
            SurfacePoint first,
            SurfacePoint second,
            SurfacePoint third)
        {
            var polygon = new List<SurfacePoint>
            {
                first,
                second,
                third
            };

            if (blockers.Count > 0)
            {
                polygon = ClipAgainstRoads(polygon);
            }

            for (int i = 1; i < polygon.Count - 1; i++)
            {
                AddVertex(polygon[0].Vertex);
                AddVertex(polygon[i].Vertex);
                AddVertex(polygon[i + 1].Vertex);
            }
        }

        void AddVertex(SceneVertex vertex)
        {
            indices.Add(vertices.Count);
            vertices.Add(vertex);
        }
    }

    private static List<SurfacePoint> ClipAgainstRoads(
        List<SurfacePoint> polygon)
    {
        var output = new List<SurfacePoint>();

        if (polygon.Count == 0)
        {
            return output;
        }

        SurfacePoint previous = polygon[^1];

        foreach (SurfacePoint current in polygon)
        {
            bool insidePrevious = previous.Clearance >= 0f;
            bool insideCurrent = current.Clearance >= 0f;

            if (insidePrevious != insideCurrent)
            {
                float divisor =
                    previous.Clearance - current.Clearance;

                float t = MathF.Abs(divisor) > 1e-7f
                    ? Math.Clamp(previous.Clearance / divisor, 0f, 1f)
                    : 0f;

                SceneVertex a = previous.Vertex;
                SceneVertex b = current.Vertex;

                output.Add(new SurfacePoint(
                    new SceneVertex(
                        Vector3.Lerp(a.Position, b.Position, t),
                        Vector3.UnitY,
                        Vector2.Lerp(a.Uv, b.Uv, t),
                        Vector4.Lerp(a.Tangent, b.Tangent, t)),
                    0f));
            }

            if (insideCurrent)
            {
                output.Add(current);
            }

            previous = current;
        }

        return output;
    }

    private static float DistanceOutsideCorridors(
        Vector2 position,
        IReadOnlyList<Corridor> corridors)
    {
        float best = 4f;

        foreach (Corridor corridor in corridors)
        {
            if (position.X < corridor.Minimum.X - 4f ||
                position.X > corridor.Maximum.X + 4f ||
                position.Y < corridor.Minimum.Y - 4f ||
                position.Y > corridor.Maximum.Y + 4f)
            {
                continue;
            }

            var samples = corridor.Curve.Samples;

            for (int index = 1; index < samples.Count; index++)
            {
                Vector2 a = samples[index - 1].Position;
                Vector2 b = samples[index].Position;
                Vector2 ab = b - a;

                float lengthSquared = ab.LengthSquared();

                if (lengthSquared < 1e-8f)
                {
                    continue;
                }

                float t = Math.Clamp(
                    Vector2.Dot(position - a, ab) /
                    lengthSquared,
                    0f,
                    1f);

                float distance =
                    Vector2.Distance(position, a + t * ab) -
                    corridor.Radius;

                best = MathF.Min(best, distance);
            }
        }

        return best;
    }
}
