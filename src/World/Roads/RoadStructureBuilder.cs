using System.Numerics;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Roads;

public static class RoadStructureBuilder
{
    public static RoadBuildResult Build(
        RoadNetwork network,
        SceneModelCache cache,
        TerrainSurface surface)
    {
        var instances = new List<SceneInstance>();

        foreach (RoadPath road in network.Roads)
        {
            foreach (RoadStructure structure in road.Structures)
            {
                if (structure.StartIndex < 0 ||
                    structure.EndIndex >= road.Points.Count ||
                    structure.EndIndex != structure.StartIndex + 1)
                {
                    continue;
                }

                SceneModel? model = cache.Get(structure.Model);

                if (model == null)
                {
                    continue;
                }

                RoadPoint a = road.Points[structure.StartIndex];
                RoadPoint b = road.Points[structure.EndIndex];

                Vector2 from = new(a.X, a.Z);
                Vector2 to = new(b.X, b.Z);
                Vector2 delta = to - from;

                float distance = delta.Length();

                if (distance < 0.1f)
                {
                    continue;
                }

                if (!surface.TryGetHeight(
                        a.X,
                        a.Z,
                        out float heightA) ||
                    !surface.TryGetHeight(
                        b.X,
                        b.Z,
                        out float heightB))
                {
                    continue;
                }

                SceneBounds bounds = model.Bounds;

                float sizeX = bounds.Maximum.X - bounds.Minimum.X;
                float sizeZ = bounds.Maximum.Z - bounds.Minimum.Z;

                bool alongX = sizeX >= sizeZ;
                float sourceLength = alongX ? sizeX : sizeZ;

                if (sourceLength < 0.05f)
                {
                    continue;
                }

                float scale = distance / sourceLength;
                Vector2 direction = delta / distance;

                float yaw = alongX
                    ? MathF.Atan2(-direction.Y, direction.X)
                    : MathF.Atan2(direction.X, direction.Y);

                float slope = MathF.Atan2(
                    heightB - heightA,
                    distance);

                Matrix4x4 scaling = alongX
                    ? Matrix4x4.CreateScale(scale, 1f, 1f)
                    : Matrix4x4.CreateScale(1f, 1f, scale);

                Matrix4x4 inclination = alongX
                    ? Matrix4x4.CreateRotationZ(slope)
                    : Matrix4x4.CreateRotationX(-slope);

                Matrix4x4 transform =
                    scaling *
                    inclination *
                    Matrix4x4.CreateRotationY(yaw);

                Vector3 localCenter = bounds.Center;
                Vector3 centerOffset = Vector3.Transform(
                    localCenter,
                    transform);

                float anchorY = structure.Kind == RoadStructureKind.Bridge
                    ? bounds.Maximum.Y
                    : bounds.Minimum.Y;

                Vector2 midpoint = (from + to) * 0.5f;

                float targetY =
                    (heightA + heightB) * 0.5f + 0.05f;

                Vector3 position = new(
                    midpoint.X,
                    targetY + localCenter.Y - anchorY,
                    midpoint.Y);

                Matrix4x4 world =
                    transform *
                    Matrix4x4.CreateTranslation(
                        position - centerOffset);

                if (!Matrix4x4.Invert(world, out Matrix4x4 inverse))
                {
                    continue;
                }

                instances.Add(new SceneInstance(
                    structure.Model,
                    world,
                    inverse,
                    bounds.Transform(world),
                    false)
                {
                    Id = $"road/structure/{structure.Id:N}",
                    LocalBounds = bounds,
                    VisualLocalBounds = bounds,
                    FollowTerrain = false
                });
            }
        }

        return new RoadBuildResult(
            instances.ToArray(),
            []);
    }
}
