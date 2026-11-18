using System.Numerics;
using ZoneReborn.World.Roads;

namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainSurface
{
    public bool RoadSectionSupported(RoadDefinition road, int section) =>
        _level.TerrainDocument?.IsRoadTerrainSupported(road, section) ?? true;
    private readonly Dictionary<(int X, int Z), List<WalkRailSegment>> _railWalking = [];
    private long _railWalkingRevision = -1, _railWalkingTerrainRevision = -1;
    private const float WalkCellSize = 16;

    /// <summary>The .335m authored rail head needs a slightly higher local step than ordinary scene stairs.</summary>
    public float CharacterStepHeight(Vector3 position)
    {
        long heights = _level.TerrainDocument?.HeightRevision ?? 0;
        if (_railWalkingRevision != _level.Roads.Revision || _railWalkingTerrainRevision != heights)
        {
            _railWalking.Clear();
            foreach (var road in _level.Roads.Roads)
            {
                if (road.Type != RoadType.Railway || road.Nodes.Count < 2) continue;
                var samples = RoadSpline.Sample(road, this);
                float half = road.RailGauge * 0.5f + 0.7f;
                for (int i = 1; i < samples.Count; i++)
                {
                    if (!RoadSectionSupported(road, samples[i].SegmentIndex)) continue;
                    var a = samples[i - 1].Position; var b = samples[i].Position;
                    var segment = new WalkRailSegment(a, b, half);
                    int x0 = Cell(Math.Min(a.X, b.X) - half), x1 = Cell(Math.Max(a.X, b.X) + half);
                    int z0 = Cell(Math.Min(a.Z, b.Z) - half), z1 = Cell(Math.Max(a.Z, b.Z) + half);
                    for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        if (!_railWalking.TryGetValue((x, z), out var list)) _railWalking.Add((x, z), list = []);
                        list.Add(segment);
                    }
                }
            }
            _railWalkingRevision = _level.Roads.Revision;
            _railWalkingTerrainRevision = heights;
        }
        if (!_railWalking.TryGetValue((Cell(position.X), Cell(position.Z)), out var segments)) return 0.3f;
        foreach (var segment in segments)
        {
            var delta = new Vector2(segment.B.X - segment.A.X, segment.B.Z - segment.A.Z);
            var offset = new Vector2(position.X - segment.A.X, position.Z - segment.A.Z);
            float length = delta.LengthSquared();
            float t = length > 1e-8f ? Math.Clamp(Vector2.Dot(offset, delta) / length, 0, 1) : 0;
            float y = segment.A.Y + (segment.B.Y - segment.A.Y) * t;
            if ((offset - delta * t).LengthSquared() <= segment.HalfWidth * segment.HalfWidth &&
                position.Y >= y - 0.1f && position.Y <= y + 0.5f) return 0.36f;
        }
        return 0.3f;
    }

    private static int Cell(float coordinate) => (int)MathF.Floor(coordinate / WalkCellSize);
    private readonly record struct WalkRailSegment(Vector3 A, Vector3 B, float HalfWidth);
}
