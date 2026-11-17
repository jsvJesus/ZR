using System.Numerics;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Roads;

public static class RoadSpline
{
    public const int MaximumSamples = 32768;
    public const float MaximumControlLength = 20000;

    public static IReadOnlyList<RoadSample> Sample(RoadDefinition road, TerrainSurface? surface = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(road);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateNodes(road);
        if (road.Nodes.Count < 2) return Array.Empty<RoadSample>();
        int count = road.Nodes.Count;
        var points = road.Nodes.Select(n => road.FollowTerrain ? new Vector3(n.X, 0, n.Z) : n.Position).ToArray();
        var tangents = new Vector3[count];
        tangents[0] = points[1] - points[0];
        tangents[^1] = points[^1] - points[^2];
        for (int i = 1; i < count - 1; i++)
        {
            Vector3 before = points[i] - points[i - 1], after = points[i + 1] - points[i];
            float previousLength = before.Length(), nextLength = after.Length();
            // Shared, length-limited bisectors produce C1 joins without long handles on short bends.
            tangents[i] = (before / previousLength + after / nextLength) * (MathF.Min(previousLength, nextLength) * 0.5f);
        }
        var samples = new List<RoadSample>();
        float distance = 0;
        for (int segment = 0; segment < count - 1; segment++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Vector3 a = points[segment], b = points[segment + 1], ta = tangents[segment], tb = tangents[segment + 1];
            // The Bezier control polygon bounds arc length; .75m steps also resolve short curves.
            float bound = ta.Length() / 3 + ((b - a) - (ta + tb) / 3).Length() + tb.Length() / 3;
            int steps = Math.Max(4, (int)MathF.Ceiling(bound / 0.75f));
            if (samples.Count + steps + (segment == 0 ? 1 : 0) > MaximumSamples)
                throw new InvalidDataException($"Road exceeds the {MaximumSamples} sample limit.");
            for (int step = segment == 0 ? 0 : 1; step <= steps; step++)
            {
                if ((step & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
                float t = step / (float)steps, t2 = t * t, t3 = t2 * t;
                // Evaluate relative to a to avoid cancellation between large world coordinates.
                Vector3 position = a + (t3 - 2 * t2 + t) * ta
                    + (-2 * t3 + 3 * t2) * (b - a) + (t3 - t2) * tb;
                if (road.FollowTerrain)
                {
                    float blend = t2 * (3 - 2 * t);
                    var nodeA = road.Nodes[segment]; var nodeB = road.Nodes[segment + 1];
                    float ground = nodeA.Y + (nodeB.Y - nodeA.Y) * blend;
                    if (surface is not null && surface.TryGetHeight(position.X, position.Z, out float height)) ground = height;
                    position.Y = ground + nodeA.HeightOffset + (nodeB.HeightOffset - nodeA.HeightOffset) * blend;
                }
                position.Y += road.SurfaceOffset;
                if (!Finite(position)) throw new InvalidDataException("Road sampling produced a non-finite point.");
                if (samples.Count > 0) distance += Vector3.Distance(samples[^1].Position, position);
                if (!float.IsFinite(distance) || distance > MaximumControlLength * 4)
                    throw new InvalidDataException("Road surface exceeds the sampled length limit.");
                samples.Add(new(position, Vector3.UnitZ, distance, segment));
            }
        }
        for (int i = 0; i < samples.Count; i++)
        {
            if ((i & 255) == 0) cancellationToken.ThrowIfCancellationRequested();
            Vector3 direction = samples[Math.Min(i + 1, samples.Count - 1)].Position - samples[Math.Max(0, i - 1)].Position;
            if (direction.LengthSquared() < 1e-10f)
            {
                int segment = samples[i].SegmentIndex;
                direction = points[segment + 1] - points[segment];
            }
            samples[i] = samples[i] with { Forward = Vector3.Normalize(direction) };
        }
        return samples;
    }

    private static void ValidateNodes(RoadDefinition road)
    {
        if (road.Nodes is null || road.Nodes.Count > 256 || !float.IsFinite(road.SurfaceOffset) || MathF.Abs(road.SurfaceOffset) > 100)
            throw new InvalidDataException("Invalid road nodes or surface offset.");
        float length = 0;
        for (int i = 0; i < road.Nodes.Count; i++)
        {
            var node = road.Nodes[i];
            if (node is null || !Finite(node.Position) || !float.IsFinite(node.HeightOffset)
                || MathF.Max(MathF.Abs(node.X), MathF.Max(MathF.Abs(node.Y), MathF.Abs(node.Z))) > 1e7f
                || MathF.Abs(node.HeightOffset) > 1e7f)
                throw new InvalidDataException("Road coordinates must be finite and within supported world bounds.");
            if (i == 0) continue;
            var previous = road.Nodes[i - 1];
            float horizontal = Vector2.Distance(new(node.X, node.Z), new(previous.X, previous.Z));
            if (horizontal < 0.25f) throw new InvalidDataException("Adjacent road points must be at least 0.25m apart in X/Z.");
            length += road.FollowTerrain ? horizontal + MathF.Abs(node.HeightOffset - previous.HeightOffset)
                : Vector3.Distance(previous.Position, node.Position);
            if (length > MaximumControlLength) throw new InvalidDataException("Road control length exceeds 20,000m.");
        }
    }

    internal static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
