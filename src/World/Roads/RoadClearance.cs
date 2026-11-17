using System.Numerics;

namespace ZoneReborn.World.Roads;

public sealed class RoadClearance
{
    private const float CellSize = 32f;

    private readonly List<RoadStrip> _segments = [];
    private readonly Dictionary<(int X, int Z), List<int>> _cells = [];

    public static RoadClearance Build(
        RoadNetwork network,
        CancellationToken cancellationToken = default)
    {
        return Build(
            network,
            float.MinValue,
            float.MinValue,
            float.MaxValue,
            float.MaxValue,
            0f,
            cancellationToken);
    }

    public static RoadClearance Build(
        RoadNetwork network,
        float minX,
        float minZ,
        float maxX,
        float maxZ,
        float queryPadding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(network);

        if (!float.IsFinite(queryPadding) || queryPadding < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(queryPadding));
        }

        if (minX > maxX || minZ > maxZ)
        {
            throw new ArgumentException("Некорректные границы дорожной зоны.");
        }

        var result = new RoadClearance();

        foreach (var road in network.Roads)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (road.Nodes.Count < 2)
            {
                continue;
            }

            float radius = road.Width * 0.5f + road.ShoulderWidth;

            if (!float.IsFinite(radius) || radius < 0f)
            {
                throw new InvalidDataException("Некорректная ширина дороги.");
            }

            float roadMinX = float.MaxValue;
            float roadMinZ = float.MaxValue;
            float roadMaxX = float.MinValue;
            float roadMaxZ = float.MinValue;
            float maximumSpan = 0f;

            for (int i = 0; i < road.Nodes.Count; i++)
            {
                var node = road.Nodes[i];

                roadMinX = MathF.Min(roadMinX, node.X);
                roadMinZ = MathF.Min(roadMinZ, node.Z);
                roadMaxX = MathF.Max(roadMaxX, node.X);
                roadMaxZ = MathF.Max(roadMaxZ, node.Z);

                if (i == 0)
                {
                    continue;
                }

                var previous = road.Nodes[i - 1];

                float span = Vector2.Distance(
                    new Vector2(node.X, node.Z),
                    new Vector2(previous.X, previous.Z));

                maximumSpan = MathF.Max(maximumSpan, span);
            }

            float margin = radius + queryPadding + maximumSpan;

            if (roadMaxX + margin < minX ||
                roadMinX - margin > maxX ||
                roadMaxZ + margin < minZ ||
                roadMinZ - margin > maxZ)
            {
                continue;
            }

            var samples = RoadSpline.Sample(
                road,
                cancellationToken: cancellationToken);

            for (int i = 1; i < samples.Count; i++)
            {
                if ((i & 255) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var a = samples[i - 1].Position;
                var b = samples[i].Position;

                float padding = radius + queryPadding;

                if (MathF.Max(a.X, b.X) + padding < minX ||
                    MathF.Min(a.X, b.X) - padding > maxX ||
                    MathF.Max(a.Z, b.Z) + padding < minZ ||
                    MathF.Min(a.Z, b.Z) - padding > maxZ)
                {
                    continue;
                }

                result.Add(new RoadStrip(
                    a.X,
                    a.Z,
                    b.X,
                    b.Z,
                    radius));
            }
        }

        return result;
    }

    public bool Intersects(
        float x,
        float z,
        float clearance = 0f)
    {
        if (_segments.Count == 0 ||
            !float.IsFinite(x) ||
            !float.IsFinite(z) ||
            !float.IsFinite(clearance))
        {
            return false;
        }

        clearance = MathF.Max(0f, clearance);

        if (clearance > 96f)
        {
            foreach (var segment in _segments)
            {
                if (Intersects(segment, x, z, clearance))
                {
                    return true;
                }
            }

            return false;
        }

        int minX = Cell(x - clearance);
        int maxX = Cell(x + clearance);
        int minZ = Cell(z - clearance);
        int maxZ = Cell(z + clearance);

        for (int cz = minZ; cz <= maxZ; cz++)
        {
            for (int cx = minX; cx <= maxX; cx++)
            {
                if (!_cells.TryGetValue((cx, cz), out var indices))
                {
                    continue;
                }

                foreach (int index in indices)
                {
                    if (Intersects(_segments[index], x, z, clearance))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private void Add(RoadStrip segment)
    {
        int index = _segments.Count;
        _segments.Add(segment);

        int minX = Cell(MathF.Min(segment.Ax, segment.Bx) - segment.Radius);
        int maxX = Cell(MathF.Max(segment.Ax, segment.Bx) + segment.Radius);
        int minZ = Cell(MathF.Min(segment.Az, segment.Bz) - segment.Radius);
        int maxZ = Cell(MathF.Max(segment.Az, segment.Bz) + segment.Radius);

        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (!_cells.TryGetValue((x, z), out var indices))
                {
                    indices = [];
                    _cells.Add((x, z), indices);
                }

                indices.Add(index);
            }
        }
    }

    private static bool Intersects(
        RoadStrip segment,
        float x,
        float z,
        float clearance)
    {
        float dx = segment.Bx - segment.Ax;
        float dz = segment.Bz - segment.Az;
        float lengthSquared = dx * dx + dz * dz;

        float t = lengthSquared > 1e-8f
            ? Math.Clamp(
                ((x - segment.Ax) * dx + (z - segment.Az) * dz) /
                lengthSquared,
                0f,
                1f)
            : 0f;

        float closestX = segment.Ax + dx * t;
        float closestZ = segment.Az + dz * t;

        float distanceX = x - closestX;
        float distanceZ = z - closestZ;
        float radius = segment.Radius + clearance;

        return distanceX * distanceX + distanceZ * distanceZ <
            radius * radius;
    }

    private static int Cell(float coordinate)
    {
        return (int)MathF.Floor(coordinate / CellSize);
    }

    private readonly record struct RoadStrip(
        float Ax,
        float Az,
        float Bx,
        float Bz,
        float Radius);
}
