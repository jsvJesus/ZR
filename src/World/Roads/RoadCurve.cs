using System.Numerics;

namespace ZoneReborn.World.Roads;

public readonly record struct RoadCurveSample(
    float Distance,
    Vector2 Position,
    Vector2 Direction);

public sealed class RoadCurve
{
    private readonly List<RoadCurveSample> _samples = [];

    public IReadOnlyList<RoadCurveSample> Samples => _samples;

    public float Length => _samples.Count == 0
        ? 0f
        : _samples[^1].Distance;

    private RoadCurve()
    {
    }

    public static RoadCurve Create(
        IReadOnlyList<RoadPoint> points,
        float roadWidth)
    {
        var curve = new RoadCurve();
        var positions = new List<Vector2>();

        foreach (var point in points)
        {
            var position = new Vector2(point.X, point.Z);

            if (positions.Count == 0 ||
                Vector2.DistanceSquared(positions[^1], position) > 0.0001f)
            {
                positions.Add(position);
            }
        }

        if (positions.Count < 2)
        {
            return curve;
        }

        Vector2 cursor = positions[0];

        for (int index = 1; index < positions.Count - 1; index++)
        {
            Vector2 previous = positions[index - 1];
            Vector2 current = positions[index];
            Vector2 next = positions[index + 1];

            Vector2 incoming = current - previous;
            Vector2 outgoing = next - current;

            float previousLength = incoming.Length();
            float nextLength = outgoing.Length();

            if (previousLength < 0.01f || nextLength < 0.01f)
            {
                continue;
            }

            incoming /= previousLength;
            outgoing /= nextLength;

            float alignment = Vector2.Dot(incoming, outgoing);

            if (alignment > 0.997f || alignment < -0.995f)
            {
                continue;
            }

            float radius = MathF.Max(roadWidth * 1.5f, 1f);

            float cut = MathF.Min(
                radius,
                MathF.Min(previousLength, nextLength) * 0.45f);

            if (cut < 0.05f)
            {
                continue;
            }

            Vector2 entry = current - incoming * cut;
            Vector2 exit = current + outgoing * cut;

            curve.AddLine(cursor, entry);
            curve.AddQuadratic(entry, current, exit);

            cursor = exit;
        }

        curve.AddLine(cursor, positions[^1]);

        return curve;
    }

    public RoadCurveSample Evaluate(float distance)
    {
        if (_samples.Count == 0)
        {
            return default;
        }

        if (_samples.Count == 1 || distance <= 0f)
        {
            return _samples[0];
        }

        if (distance >= Length)
        {
            return _samples[^1];
        }

        int low = 0;
        int high = _samples.Count - 1;

        while (high - low > 1)
        {
            int middle = (low + high) / 2;

            if (_samples[middle].Distance <= distance)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        RoadCurveSample a = _samples[low];
        RoadCurveSample b = _samples[high];

        float length = b.Distance - a.Distance;

        float t = length > 0.00001f
            ? (distance - a.Distance) / length
            : 0f;

        Vector2 position = Vector2.Lerp(
            a.Position,
            b.Position,
            t);

        Vector2 direction = Vector2.Lerp(
            a.Direction,
            b.Direction,
            t);

        if (direction.LengthSquared() < 0.000001f)
        {
            direction = b.Direction;
        }
        else
        {
            direction = Vector2.Normalize(direction);
        }

        return new RoadCurveSample(
            distance,
            position,
            direction);
    }

    private void AddLine(Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;

        if (delta.LengthSquared() < 0.000001f)
        {
            return;
        }

        Vector2 direction = Vector2.Normalize(delta);

        Append(start, direction);
        Append(end, direction);
    }

    private void AddQuadratic(
        Vector2 start,
        Vector2 control,
        Vector2 end)
    {
        float approximateLength =
            Vector2.Distance(start, control) +
            Vector2.Distance(control, end);

        int steps = Math.Clamp(
            (int)MathF.Ceiling(approximateLength / 0.35f),
            8,
            256);

        for (int index = 0; index <= steps; index++)
        {
            float t = index / (float)steps;
            float inverse = 1f - t;

            Vector2 position =
                inverse * inverse * start +
                2f * inverse * t * control +
                t * t * end;

            Vector2 tangent =
                2f * inverse * (control - start) +
                2f * t * (end - control);

            if (tangent.LengthSquared() < 0.000001f)
            {
                continue;
            }

            Append(
                position,
                Vector2.Normalize(tangent));
        }
    }

    private void Append(Vector2 position, Vector2 direction)
    {
        if (_samples.Count == 0)
        {
            _samples.Add(new RoadCurveSample(
                0f,
                position,
                direction));

            return;
        }

        RoadCurveSample last = _samples[^1];

        float distance = Vector2.Distance(
            last.Position,
            position);

        if (distance < 0.00001f)
        {
            _samples[^1] = last with
            {
                Direction = direction
            };

            return;
        }

        _samples.Add(new RoadCurveSample(
            last.Distance + distance,
            position,
            direction));
    }
}
