using System.Numerics;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Roads;

public sealed record RoadBuildResult(
    SceneInstance[] Instances,
    SceneModel[] Models);

public static class RoadBuilder
{
    public const int MaximumInstances = 50000;
    private const long MaximumVerticesPerRoad = 750000;
    private const long MaximumIndicesPerRoad = 2250000;

    public static RoadBuildResult Build(
        RoadNetwork network,
        RoadCatalog catalog,
        TerrainSurface surface,
        SceneModelCache cache)
    {
        var instances = new List<SceneInstance>();
        var generatedModels = new List<SceneModel>();

        foreach (var road in network.Roads)
        {
            if (road.Category != RoadCategory.Railway ||
                road.Points.Count < 2 ||
                string.IsNullOrWhiteSpace(road.Model) ||
                !catalog.TryGet(road.Model, out var info) ||
                info.Category != RoadCategory.Railway)
            {
                continue;
            }

            SceneModel? source = cache.Get(info.File);

            if (source == null)
            {
                continue;
            }

            RoadCurve curve = RoadCurve.Create(
                road.Points,
                info.Width);

            if (curve.Length < 0.05f)
            {
                continue;
            }

            string generatedFile =
                $"objects/roads_generated/{road.Id:N}.model";

            SceneModel? generated = GenerateModel(
                generatedFile,
                source,
                info,
                curve,
                surface);

            if (generated == null)
            {
                continue;
            }

            var bounds = generated.Bounds;

            var instance = new SceneInstance(
                generatedFile,
                Matrix4x4.Identity,
                Matrix4x4.Identity,
                bounds,
                false)
            {
                Id = $"road/{road.Id:N}",
                LocalBounds = bounds,
                VisualLocalBounds = bounds,
                FollowTerrain = false
            };

            generatedModels.Add(generated);
            instances.Add(instance);
        }

        return new RoadBuildResult(
            instances.ToArray(),
            generatedModels.ToArray());
    }

    private static SceneModel? GenerateModel(
        string file,
        SceneModel source,
        RoadModelInfo info,
        RoadCurve curve,
        TerrainSurface surface)
    {
        if (info.Length <= 0.05f)
        {
            return null;
        }

        int tileCount = Math.Max(
            1,
            (int)MathF.Ceiling(curve.Length / info.Length));

        if (tileCount > MaximumInstances)
        {
            throw new InvalidOperationException(
                "Превышено допустимое количество дорожных секций.");
        }

        long vertexCount = source.Meshes.Sum(
            mesh => (long)mesh.Vertices.Length) * tileCount;

        long indexCount = source.Meshes.Sum(
            mesh => (long)mesh.Indices.Length) * tileCount;

        if (vertexCount > MaximumVerticesPerRoad ||
            indexCount > MaximumIndicesPerRoad)
        {
            throw new InvalidOperationException(
                "Слишком много дорожной геометрии. " +
                "Уменьшите длину маршрута или используйте более простую модель.");
        }

        float tileLength = curve.Length / tileCount;

        float minimumLong = info.Axis == RoadModelAxis.X
            ? info.Bounds.Minimum.X
            : info.Bounds.Minimum.Z;

        float minimumLateral = info.Axis == RoadModelAxis.X
            ? info.Bounds.Minimum.Z
            : info.Bounds.Minimum.X;

        float maximumLateral = info.Axis == RoadModelAxis.X
            ? info.Bounds.Maximum.Z
            : info.Bounds.Maximum.X;

        float lateralCenter =
            (minimumLateral + maximumLateral) * 0.5f;

        float anchorY = GetVerticalAnchor(info);

        float clearance = info.Category == RoadCategory.Railway
            ? 0.035f
            : 0.05f;

        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);

        var outputMeshes = new List<SceneMesh>();

        foreach (var mesh in source.Meshes)
        {
            if (mesh.Vertices.Length == 0 ||
                mesh.Indices.Length == 0)
            {
                continue;
            }

            var vertices = new List<SceneVertex>(
                checked(mesh.Vertices.Length * tileCount));

            var indices = new List<int>(
                checked(mesh.Indices.Length * tileCount));

            for (int tile = 0; tile < tileCount; tile++)
            {
                float startDistance = tile * tileLength;
                float middleDistance = startDistance + tileLength * 0.5f;

                RoadCurveSample middle = curve.Evaluate(
                    middleDistance);

                if (!surface.TryGetHeight(
                        middle.Position.X,
                        middle.Position.Y,
                        out float fallbackHeight))
                {
                    continue;
                }

                int vertexOffset = vertices.Count;

                foreach (var vertex in mesh.Vertices)
                {
                    Vector3 local = vertex.Position;

                    float longitudinal = info.Axis == RoadModelAxis.X
                        ? local.X
                        : local.Z;

                    float lateral = info.Axis == RoadModelAxis.X
                        ? local.Z
                        : local.X;

                    float t = Math.Clamp(
                        (longitudinal - minimumLong) / info.Length,
                        0f,
                        1f);

                    RoadCurveSample sample = curve.Evaluate(
                        startDistance + tileLength * t);

                    Vector2 forward = sample.Direction;

                    Vector2 sideways = info.Axis == RoadModelAxis.X
                        ? new Vector2(-forward.Y, forward.X)
                        : new Vector2(forward.Y, -forward.X);

                    float offset = lateral - lateralCenter;

                    Vector2 planarPosition =
                        sample.Position + sideways * offset;

                    float height = surface.TryGetHeight(
                        sample.Position.X,
                        sample.Position.Y,
                        out float sampledHeight)
                            ? sampledHeight
                            : fallbackHeight;

                    var position = new Vector3(
                        planarPosition.X,
                        height + clearance + local.Y - anchorY,
                        planarPosition.Y);

                    Vector3 normal = RotateDirection(
                        vertex.Normal,
                        forward,
                        sideways,
                        info.Axis);

                    Vector3 tangent = RotateDirection(
                        new Vector3(
                            vertex.Tangent.X,
                            vertex.Tangent.Y,
                            vertex.Tangent.Z),
                        forward,
                        sideways,
                        info.Axis);

                    vertices.Add(new SceneVertex(
                        position,
                        normal,
                        vertex.Uv,
                        new Vector4(tangent, vertex.Tangent.W)));

                    minimum = Vector3.Min(minimum, position);
                    maximum = Vector3.Max(maximum, position);
                }

                foreach (int sourceIndex in mesh.Indices)
                {
                    indices.Add(vertexOffset + sourceIndex);
                }
            }

            if (vertices.Count == 0 || indices.Count == 0)
            {
                continue;
            }

            outputMeshes.Add(new SceneMesh(
                vertices.ToArray(),
                indices.ToArray(),
                mesh.Material));
        }

        if (outputMeshes.Count == 0)
        {
            return null;
        }

        var bounds = new SceneBounds(minimum, maximum);

        return new SceneModel(
            file,
            bounds,
            outputMeshes.ToArray());
    }

    private static Vector3 RotateDirection(
        Vector3 direction,
        Vector2 forward,
        Vector2 sideways,
        RoadModelAxis axis)
    {
        Vector2 xAxis = axis == RoadModelAxis.X
            ? forward
            : sideways;

        Vector2 zAxis = axis == RoadModelAxis.X
            ? sideways
            : forward;

        Vector3 result = new(
            xAxis.X * direction.X + zAxis.X * direction.Z,
            direction.Y,
            xAxis.Y * direction.X + zAxis.Y * direction.Z);

        float lengthSquared = result.LengthSquared();

        if (lengthSquared < 0.000001f)
        {
            return Vector3.UnitY;
        }

        return Vector3.Normalize(result);
    }

    private static float GetVerticalAnchor(RoadModelInfo model)
    {
        if (model.Category != RoadCategory.Railway)
        {
            return 0f;
        }

        return model.HasDeepFoundation
            ? model.Bounds.Maximum.Y
            : model.Bounds.Minimum.Y;
    }
}
