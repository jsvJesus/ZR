using System.Numerics;
using ZoneReborn.World.Roads;

namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainSurface
{
    private const float WalkCellSize = 16f;

    private readonly Dictionary<
        (int X, int Z),
        List<WalkRailSegment>> _railWalking = [];

    private long _railWalkingRevision = -1;
    private long _railWalkingTerrainRevision = -1;

    public bool RoadSectionSupported(
        RoadDefinition road,
        int section)
    {
        return _level.TerrainDocument?
            .IsRoadTerrainSupported(road, section) ?? true;
    }

    public float CharacterStepHeight(Vector3 position)
    {
        long terrainRevision =
            _level.TerrainDocument?.HeightRevision ?? 0;

        if (_railWalkingRevision != _level.Roads.Revision ||
            _railWalkingTerrainRevision != terrainRevision)
        {
            RebuildRailWalking();

            _railWalkingRevision = _level.Roads.Revision;
            _railWalkingTerrainRevision = terrainRevision;
        }

        var key = (
            Cell(position.X),
            Cell(position.Z));

        if (!_railWalking.TryGetValue(key, out var segments))
        {
            return 0.3f;
        }

        foreach (var segment in segments)
        {
            Vector2 a = new(
                segment.A.X,
                segment.A.Z);

            Vector2 b = new(
                segment.B.X,
                segment.B.Z);

            Vector2 point = new(
                position.X,
                position.Z);

            Vector2 delta = b - a;
            float lengthSquared = delta.LengthSquared();

            float t = lengthSquared > 1e-8f
                ? Math.Clamp(
                    Vector2.Dot(
                        point - a,
                        delta) / lengthSquared,
                    0f,
                    1f)
                : 0f;

            float surfaceY =
                segment.A.Y +
                (segment.B.Y - segment.A.Y) * t;

            float distanceSquared =
                Vector2.DistanceSquared(
                    point,
                    a + delta * t);

            if (distanceSquared <=
                    segment.HalfWidth * segment.HalfWidth &&
                position.Y >= surfaceY - 0.1f &&
                position.Y <= surfaceY + 0.5f)
            {
                return 0.48f;
            }
        }

        return 0.3f;
    }

    private void RebuildRailWalking()
    {
        _railWalking.Clear();

        foreach (var road in _level.Roads.Roads)
        {
            if (road.Type != RoadType.Railway ||
                road.Nodes.Count < 2)
            {
                continue;
            }

            var samples = RoadSpline.Sample(road, this);

            float halfWidth =
                road.RailGauge * 0.5f + 0.7f;

            for (int i = 1; i < samples.Count; i++)
            {
                int section = samples[i].SegmentIndex;

                if (!RoadSectionSupported(road, section))
                {
                    continue;
                }

                Vector3 a = samples[i - 1].Position;
                Vector3 b = samples[i].Position;

                Vector3 direction = new(
                    b.X - a.X,
                    0f,
                    b.Z - a.Z);

                if (direction.LengthSquared() < 1e-8f)
                {
                    continue;
                }

                direction = Vector3.Normalize(direction);

                Vector3 right = new(
                    direction.Z,
                    0f,
                    -direction.X);

                for (int track = 0;
                     track < road.TrackCount;
                     track++)
                {
                    float trackOffset = road.TrackCount == 1
                        ? 0f
                        : (track == 0 ? -0.5f : 0.5f) *
                          road.TrackSpacing;

                    Vector3 offset = right * trackOffset;

                    var segment = new WalkRailSegment(
                        a + offset,
                        b + offset,
                        halfWidth);

                    AddRailSegment(segment);
                }
            }
        }
    }

    private void AddRailSegment(WalkRailSegment segment)
    {
        int x0 = Cell(
            MathF.Min(
                segment.A.X,
                segment.B.X) - segment.HalfWidth);

        int x1 = Cell(
            MathF.Max(
                segment.A.X,
                segment.B.X) + segment.HalfWidth);

        int z0 = Cell(
            MathF.Min(
                segment.A.Z,
                segment.B.Z) - segment.HalfWidth);

        int z1 = Cell(
            MathF.Max(
                segment.A.Z,
                segment.B.Z) + segment.HalfWidth);

        for (int z = z0; z <= z1; z++)
        {
            for (int x = x0; x <= x1; x++)
            {
                var key = (x, z);

                if (!_railWalking.TryGetValue(
                        key,
                        out var list))
                {
                    list = [];
                    _railWalking.Add(key, list);
                }

                list.Add(segment);
            }
        }
    }

    private static int Cell(float coordinate)
    {
        return (int)MathF.Floor(
            coordinate / WalkCellSize);
    }

    private readonly record struct WalkRailSegment(
        Vector3 A,
        Vector3 B,
        float HalfWidth);
}
