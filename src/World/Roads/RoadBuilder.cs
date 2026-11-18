using System.Numerics;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Roads;

public static class RoadBuilder
{
    public const int MaximumInstances = 50000;

    public static SceneInstance[] Build(
        RoadNetwork network,
        RoadCatalog catalog,
        TerrainSurface surface)
    {
        var instances = new List<SceneInstance>();

        foreach (var road in network.Roads)
        {
            if (road.Points.Count < 2 ||
                string.IsNullOrWhiteSpace(road.Model) ||
                !catalog.TryGet(road.Model, out var model) ||
                model.Category != road.Category)
            {
                continue;
            }

            for (int index = 1; index < road.Points.Count; index++)
            {
                BuildSection(
                    road,
                    index,
                    road.Points[index - 1],
                    road.Points[index],
                    model,
                    surface,
                    instances);
            }
        }

        return instances.ToArray();
    }

    private static void BuildSection(
        RoadPath road,
        int sectionIndex,
        RoadPoint start,
        RoadPoint end,
        RoadModelInfo model,
        TerrainSurface surface,
        List<SceneInstance> instances)
    {
        var from = new Vector2(start.X, start.Z);
        var to = new Vector2(end.X, end.Z);

        Vector2 delta = to - from;
        float distance = delta.Length();

        if (!float.IsFinite(distance) || distance < 0.05f)
        {
            return;
        }

        Vector2 direction = delta / distance;

        int count = checked((int)MathF.Ceiling(
            distance / model.Length));

        if (count > MaximumInstances - instances.Count)
        {
            throw new InvalidOperationException(
                "Превышен лимит дорожных сегментов уровня.");
        }

        float segmentLength = distance / count;
        float lengthScale = segmentLength / model.Length;

        float yaw = model.Axis == RoadModelAxis.X
            ? MathF.Atan2(-direction.Y, direction.X)
            : MathF.Atan2(direction.X, direction.Y);

        var bounds = model.Bounds;

        var localCenter = new Vector3(
            (bounds.Minimum.X + bounds.Maximum.X) * 0.5f,
            0f,
            (bounds.Minimum.Z + bounds.Maximum.Z) * 0.5f);

        for (int index = 0; index < count; index++)
        {
            float t0 = index / (float)count;
            float t1 = (index + 1f) / count;

            Vector2 a = Vector2.Lerp(from, to, t0);
            Vector2 b = Vector2.Lerp(from, to, t1);
            Vector2 middle = (a + b) * 0.5f;

            if (!surface.TryGetHeight(a.X, a.Y, out float heightA) ||
                !surface.TryGetHeight(b.X, b.Y, out float heightB) ||
                !surface.TryGetHeight(
                    middle.X,
                    middle.Y,
                    out float middleHeight))
            {
                continue;
            }

            float slope = MathF.Atan2(
                heightB - heightA,
                segmentLength);

            Matrix4x4 scale = model.Axis == RoadModelAxis.X
                ? Matrix4x4.CreateScale(lengthScale, 1f, 1f)
                : Matrix4x4.CreateScale(1f, 1f, lengthScale);

            Matrix4x4 inclination = model.Axis == RoadModelAxis.X
                ? Matrix4x4.CreateRotationZ(slope)
                : Matrix4x4.CreateRotationX(-slope);

            Matrix4x4 rotation = Matrix4x4.CreateRotationY(yaw);

            Matrix4x4 transform =
                scale * inclination * rotation;

            Vector3 centerOffset = Vector3.Transform(
                localCenter,
                transform);

            Vector3 position = new(
                middle.X,
                middleHeight + 0.05f,
                middle.Y);

            Matrix4x4 world =
                transform *
                Matrix4x4.CreateTranslation(position - centerOffset);

            if (!Matrix4x4.Invert(world, out Matrix4x4 inverse))
            {
                continue;
            }

            var instance = new SceneInstance(
                model.File,
                world,
                inverse,
                bounds.Transform(world),
                false)
            {
                Id = $"road/{road.Id:N}/{sectionIndex}/{index}",
                LocalBounds = bounds,
                VisualLocalBounds = bounds,
                FollowTerrain = false
            };

            instances.Add(instance);
        }
    }
}
