using System.Numerics;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Roads;

public static class RoadMesh
{
    public const int MaximumVertices = 2_000_000;
    private readonly record struct Frame(Vector3 Position, Vector3 Right, Vector3 Forward, float Distance, int Segment);
    private readonly record struct Profile(float Lateral, float Height);

    public static IReadOnlyList<RoadSectionMesh> Build(RoadDefinition road, TerrainSurface surface,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(road);
        ValidateDimensions(road);
        var samples = RoadSpline.Sample(road, surface, cancellationToken);
        if (samples.Count < 2) return Array.Empty<RoadSectionMesh>();
        var frames = new Frame[samples.Count];
        float half = road.Width / 2, outer = half + road.ShoulderWidth;
        float edgeDrop = road.Type == RoadType.Automobile ? MathF.Min(0.04f, road.Thickness * 0.25f) : road.Thickness;
        float distance = 0;
        for (int i = 0; i < samples.Count; i++)
        {
            if ((i & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            var sample = samples[i];
            Vector3 horizontal = new(sample.Forward.X, 0, sample.Forward.Z);
            if (horizontal.LengthSquared() < 1e-10f)
            {
                var from = road.Nodes[sample.SegmentIndex]; var to = road.Nodes[sample.SegmentIndex + 1];
                horizontal = new(to.X - from.X, 0, to.Z - from.Z);
            }
            horizontal = Vector3.Normalize(horizontal);
            Vector3 right = new(horizontal.Z, 0, -horizontal.X);
            Vector3 position = sample.Position;
            if (road.FollowTerrain && surface.TryGetHeight(position.X, position.Z, out float centerGround))
            {
                float offset = position.Y - centerGround;
                int probes = Math.Max(2, (int)MathF.Ceiling(outer * 2 / 0.75f));
                // Lift whole rows to preserve level crossfall, including the sloped edges.
                // Holes/missing tiles retain fallback height; negative user offsets remain intentional.
                for (int p = 0; p <= probes; p++)
                {
                    float lateral = -outer + outer * 2 * p / probes;
                    Vector3 probe = sample.Position + right * lateral;
                    float drop = road.ShoulderWidth > 0 ? edgeDrop * Math.Clamp((MathF.Abs(lateral) - half) / road.ShoulderWidth, 0, 1) : 0;
                    if (surface.TryGetHeight(probe.X, probe.Z, out float ground))
                        position.Y = MathF.Max(position.Y, ground + offset + drop);
                }
            }
            if (i > 0) distance += Vector3.Distance(frames[i - 1].Position, position);
            if (!RoadSpline.Finite(position) || !float.IsFinite(distance) || distance > RoadSpline.MaximumControlLength * 4)
                throw new InvalidDataException("Road terrain profile exceeds the supported length limit.");
            frames[i] = new(position, right, sample.Forward, distance, sample.SegmentIndex);
        }
        for (int i = 0; i < frames.Length; i++)
        {
            if ((i & 255) == 0) cancellationToken.ThrowIfCancellationRequested();
            Vector3 forward = frames[Math.Min(i + 1, frames.Length - 1)].Position - frames[Math.Max(0, i - 1)].Position;
            if (forward.LengthSquared() > 1e-10f) frames[i] = frames[i] with { Forward = Vector3.Normalize(forward) };
        }
        var budget = new Budget();
        var sections = new Dictionary<int, Builder>();
        Builder Section(int index)
        {
            if (!sections.TryGetValue(index, out var builder)) sections.Add(index, builder = new(budget));
            return builder;
        }
        var top = road.ShoulderWidth > 0
            ? new[] { new Profile(-outer, -edgeDrop), new Profile(-half, 0), new Profile(half, 0), new Profile(outer, -edgeDrop) }
            : new[] { new Profile(-half, 0), new Profile(half, 0) };
        float baseHeight = -road.Thickness - (road.Type == RoadType.Automobile ? 0 : edgeDrop);
        float surfaceId = road.Type == RoadType.Automobile ? 0 : road.Type == RoadType.Railway ? 3 : 2;
        for (int i = 1; i < frames.Length; i++)
        {
            if ((i & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            Frame a = frames[i - 1], b = frames[i];
            var builder = Section(b.Segment);
            for (int p = 0; p < top.Length - 1; p++)
            {
                float material = road.Type == RoadType.Automobile && top.Length == 4 && p != 1 ? 1 : surfaceId;
                Ribbon(builder, a, b, top[p], top[p + 1], material, Vector3.UnitY);
            }
            var leftBase = new Profile(-outer, baseHeight); var rightBase = new Profile(outer, baseHeight);
            Ribbon(builder, a, b, leftBase, top[0], road.Type == RoadType.Automobile ? 1 : surfaceId, -a.Right - b.Right);
            Ribbon(builder, a, b, top[^1], rightBase, road.Type == RoadType.Automobile ? 1 : surfaceId, a.Right + b.Right);
            Ribbon(builder, a, b, rightBase, leftBase, surfaceId, -Vector3.UnitY);
            if (i == 1) CapDeck(builder, a, top, baseHeight, surfaceId, -a.Forward);
            if (i == frames.Length - 1) CapDeck(builder, b, top, baseHeight, surfaceId, b.Forward);
            if (road.Type == RoadType.Railway)
                for (int rail = -1; rail <= 1; rail += 2)
                    BuildRail(builder, a, b, rail * (road.RailGauge / 2 + 0.038f), i == 1, i == frames.Length - 1);
        }
        if (road.Type == RoadType.Railway)
        {
            int interval = 1;
            int sleeperCount = (int)MathF.Floor(frames[^1].Distance / road.SleeperSpacing);
            for (int n = 0; n < sleeperCount; n++)
            {
                if ((n & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
                float at = (n + 0.5f) * road.SleeperSpacing;
                while (interval < frames.Length - 1 && frames[interval].Distance < at) interval++;
                Frame a = frames[interval - 1], b = frames[interval];
                float span = b.Distance - a.Distance;
                float t = span > 1e-6f ? Math.Clamp((at - a.Distance) / span, 0, 1) : 0;
                Vector3 right = Vector3.Lerp(a.Right, b.Right, t);
                if (right.LengthSquared() < 1e-8f) right = b.Right;
                else right = Vector3.Normalize(right);
                Vector3 forward = b.Position - a.Position;
                if (forward.LengthSquared() < 1e-8f) forward = b.Forward;
                else forward = Vector3.Normalize(forward);
                var frame = new Frame(Vector3.Lerp(a.Position, b.Position, t), right, forward, at, b.Segment);
                BuildSleeper(Section(b.Segment), frame, road.RailGauge / 2 + 0.3f, MathF.Min(0.24f, road.SleeperSpacing * 0.65f));
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        return sections.OrderBy(pair => pair.Key).Select(pair => pair.Value.Finish(pair.Key)).ToArray();
    }

    private static Vector3 At(Frame frame, Profile profile) => frame.Position + frame.Right * profile.Lateral + Vector3.UnitY * profile.Height;
    private static Vector2 Uv(Frame frame, Profile profile) => new(frame.Distance, profile.Lateral);

    private static void Ribbon(Builder builder, Frame a, Frame b, Profile left, Profile right, float surface, Vector3 outward)
        => builder.Quad(At(a, left), At(a, right), At(b, right), At(b, left), Uv(a, left), Uv(a, right), Uv(b, right), Uv(b, left), surface, outward);

    private static void CapDeck(Builder builder, Frame frame, Profile[] top, float bottom, float surface, Vector3 outward)
    {
        for (int i = 0; i < top.Length - 1; i++)
        {
            var left = top[i]; var right = top[i + 1];
            builder.Quad(At(frame, left), At(frame, right), At(frame, right with { Height = bottom }), At(frame, left with { Height = bottom }),
                Uv(frame, left), Uv(frame, right), Uv(frame, right), Uv(frame, left), surface, outward);
        }
    }

    // Counterclockwise in lateral/height space: broad foot, narrow web and chamfered head.
    private static readonly Profile[] RailProfile =
    [
        new(-0.075f, 0.16f), new(0.075f, 0.16f), new(0.075f, 0.185f), new(0.018f, 0.20f),
        new(0.018f, 0.28f), new(0.038f, 0.29f), new(0.038f, 0.325f), new(0.027f, 0.335f),
        new(-0.027f, 0.335f), new(-0.038f, 0.325f), new(-0.038f, 0.29f), new(-0.018f, 0.28f),
        new(-0.018f, 0.20f), new(-0.075f, 0.185f)
    ];

    private static void BuildRail(Builder builder, Frame a, Frame b, float lateral, bool start, bool end)
    {
        for (int i = 0; i < RailProfile.Length; i++)
        {
            var p = RailProfile[i]; var q = RailProfile[(i + 1) % RailProfile.Length];
            Vector3 outward = (a.Right + b.Right) * (q.Height - p.Height) - Vector3.UnitY * (q.Lateral - p.Lateral) * 2;
            Ribbon(builder, a, b, p with { Lateral = p.Lateral + lateral }, q with { Lateral = q.Lateral + lateral }, i is 6 or 7 or 8 ? 6 : 5, outward);
        }
        // The concave profile is capped in horizontal bands, avoiding fans across the web recesses.
        if (start) CapRail(builder, a, lateral, -a.Forward);
        if (end) CapRail(builder, b, lateral, b.Forward);
    }

    private static void CapRail(Builder builder, Frame frame, float lateral, Vector3 outward)
    {
        int[] polygon = [0, 1, 2, 13, 13, 2, 3, 12, 12, 3, 4, 11, 11, 4, 5, 10, 10, 5, 6, 9, 9, 6, 7, 8];
        for (int i = 0; i < polygon.Length; i += 4)
        {
            Profile P(int index) { var p = RailProfile[polygon[i + index]]; return p with { Lateral = p.Lateral + lateral }; }
            var p0 = P(0); var p1 = P(1); var p2 = P(2); var p3 = P(3);
            builder.Quad(At(frame, p0), At(frame, p1), At(frame, p2), At(frame, p3),
                Uv(frame, p0), Uv(frame, p1), Uv(frame, p2), Uv(frame, p3), 5, outward);
        }
    }

    private static void BuildSleeper(Builder builder, Frame frame, float halfWidth, float length)
    {
        Frame a = frame with { Position = frame.Position - frame.Forward * (length / 2), Distance = frame.Distance - length / 2 };
        Frame b = frame with { Position = frame.Position + frame.Forward * (length / 2), Distance = frame.Distance + length / 2 };
        var left = new Profile(-halfWidth, 0.16f); var right = new Profile(halfWidth, 0.16f);
        var leftBase = left with { Height = 0.02f }; var rightBase = right with { Height = 0.02f };
        Ribbon(builder, a, b, left, right, 4, Vector3.UnitY);
        Ribbon(builder, a, b, leftBase, left, 4, -frame.Right);
        Ribbon(builder, a, b, right, rightBase, 4, frame.Right);
        Ribbon(builder, a, b, rightBase, leftBase, 4, -Vector3.UnitY);
        CapDeck(builder, a, [left, right], 0.02f, 4, -frame.Forward);
        CapDeck(builder, b, [left, right], 0.02f, 4, frame.Forward);
    }

    private static void ValidateDimensions(RoadDefinition road)
    {
        static bool Range(float value, float min, float max) => float.IsFinite(value) && value >= min && value <= max;
        if (!Enum.IsDefined(road.Type) || !Range(road.Width, 0.2f, 50) || !Range(road.ShoulderWidth, 0, 10)
            || !Range(road.Thickness, 0.02f, 2) || !Range(road.RailGauge, 0.5f, 3)
            || !Range(road.SleeperSpacing, 0.3f, 3) || road.LaneCount is < 1 or > 8
            || road.Type == RoadType.Railway && road.Width < road.RailGauge + 0.6f)
            throw new InvalidDataException("Road profile dimensions are outside supported limits.");
    }

    private sealed class Budget { public int Vertices; }

    private sealed class Builder(Budget budget)
    {
        private readonly List<RoadVertex> _vertices = [];
        private readonly List<int> _indices = [];
        private Vector3 _min = new(float.PositiveInfinity), _max = new(float.NegativeInfinity);

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, float surface, Vector3 outward)
        {
            Triangle(a, b, c, ua, ub, uc, surface, outward);
            Triangle(a, c, d, ua, uc, ud, surface, outward);
        }

        private void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, float surface, Vector3 outward)
        {
            if (!RoadSpline.Finite(a) || !RoadSpline.Finite(b) || !RoadSpline.Finite(c))
                throw new InvalidDataException("Road geometry contains a non-finite vertex.");
            Vector3 normal = Vector3.Cross(b - a, c - a);
            float area = normal.LengthSquared();
            if (!float.IsFinite(area)) throw new InvalidDataException("Road geometry exceeds supported dimensions.");
            if (area < 1e-12f) return;
            if (Vector3.Dot(normal, outward) < 0) { (b, c) = (c, b); (ub, uc) = (uc, ub); normal = -normal; }
            normal /= MathF.Sqrt(area);
            if (budget.Vertices > MaximumVertices - 3) throw new InvalidDataException($"Road exceeds the {MaximumVertices} vertex limit.");
            budget.Vertices += 3;
            int first = _vertices.Count;
            _vertices.Add(new(a, normal, ua, surface)); _vertices.Add(new(b, normal, ub, surface)); _vertices.Add(new(c, normal, uc, surface));
            _indices.Add(first); _indices.Add(first + 1); _indices.Add(first + 2);
            _min = Vector3.Min(_min, Vector3.Min(a, Vector3.Min(b, c)));
            _max = Vector3.Max(_max, Vector3.Max(a, Vector3.Max(b, c)));
        }

        public RoadSectionMesh Finish(int segment) => new(segment, _vertices.ToArray(), _indices.ToArray(),
            _vertices.Count > 0 ? new SceneBounds(_min, _max) : new SceneBounds(Vector3.Zero, Vector3.Zero));
    }
}
