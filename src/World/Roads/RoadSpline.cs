using System.Numerics;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Roads;

public static class RoadSpline
{
    public const int MaximumSamples = 32768;
    public const float MaximumControlLength = 20000;

    private const float AutomobileMaximumGrade = 0.12f;
    private const float RailwayMaximumGrade = 0.04f;

    public static IReadOnlyList<RoadSample> Sample(
        RoadDefinition road,
        TerrainSurface? surface = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(road);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateNodes(road);

        if (road.Nodes.Count < 2)
        {
            return Array.Empty<RoadSample>();
        }

        int count = road.Nodes.Count;

        var points = road.Nodes
            .Select(node => road.FollowTerrain
                ? new Vector3(node.X, 0f, node.Z)
                : node.Position)
            .ToArray();

        var tangents = new Vector3[count];

        tangents[0] = points[1] - points[0];
        tangents[^1] = points[^1] - points[^2];

        for (int i = 1; i < count - 1; i++)
        {
            Vector3 before = points[i] - points[i - 1];
            Vector3 after = points[i + 1] - points[i];

            float previousLength = before.Length();
            float nextLength = after.Length();

            tangents[i] =
                (before / previousLength + after / nextLength) *
                (MathF.Min(previousLength, nextLength) * 0.5f);
        }

        var samples = new List<RoadSample>();
        float distance = 0f;

        for (int segment = 0; segment < count - 1; segment++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Vector3 a = points[segment];
            Vector3 b = points[segment + 1];
            Vector3 tangentA = tangents[segment];
            Vector3 tangentB = tangents[segment + 1];

            float bound =
                tangentA.Length() / 3f +
                ((b - a) - (tangentA + tangentB) / 3f).Length() +
                tangentB.Length() / 3f;

            int steps = Math.Max(
                4,
                (int)MathF.Ceiling(bound / 0.75f));

            if (samples.Count + steps +
                (segment == 0 ? 1 : 0) > MaximumSamples)
            {
                throw new InvalidDataException(
                    "Превышен лимит точек дорожного сплайна.");
            }

            for (int step = segment == 0 ? 0 : 1; step <= steps; step++)
            {
                if ((step & 63) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                float t = step / (float)steps;
                float t2 = t * t;
                float t3 = t2 * t;

                Vector3 position =
                    a +
                    (t3 - 2f * t2 + t) * tangentA +
                    (-2f * t3 + 3f * t2) * (b - a) +
                    (t3 - t2) * tangentB;

                if (road.FollowTerrain)
                {
                    float blend = t2 * (3f - 2f * t);

                    var nodeA = road.Nodes[segment];
                    var nodeB = road.Nodes[segment + 1];

                    float ground = nodeA.Y +
                        (nodeB.Y - nodeA.Y) * blend;

                    if (!road.PreserveSourceElevation && surface != null &&
                        surface.TryGetBaseHeight(
                            position.X,
                            position.Z,
                            out float terrainHeight))
                    {
                        ground = terrainHeight;
                    }

                    float offset = nodeA.HeightOffset +
                        (nodeB.HeightOffset - nodeA.HeightOffset) * blend;

                    position.Y = ground + offset;
                }

                position.Y += road.SurfaceOffset;

                if (!Finite(position))
                {
                    throw new InvalidDataException(
                        "Некорректная координата дорожного сплайна.");
                }

                if (samples.Count > 0)
                {
                    distance += Vector3.Distance(
                        samples[^1].Position,
                        position);
                }

                if (!float.IsFinite(distance) ||
                    distance > MaximumControlLength * 4f)
                {
                    throw new InvalidDataException(
                        "Превышена длина дорожного сплайна.");
                }

                samples.Add(new RoadSample(
                    position,
                    Vector3.UnitZ,
                    distance,
                    segment));
            }
        }

        if (road.FollowTerrain)
        {
            switch (road.Type)
            {
                case RoadType.Automobile:
                    LimitRoadGrade(
                        samples,
                        AutomobileMaximumGrade,
                        road.PreserveSourceElevation,
                        cancellationToken
                    );
                    break;

                case RoadType.Railway:
                    LimitRoadGrade(
                        samples,
                        RailwayMaximumGrade,
                        road.PreserveSourceElevation,
                        cancellationToken
                    );
                    break;
            }
        }

        for (int i = 0; i < samples.Count; i++)
        {
            if ((i & 255) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            Vector3 direction =
                samples[Math.Min(i + 1, samples.Count - 1)].Position -
                samples[Math.Max(0, i - 1)].Position;

            if (direction.LengthSquared() < 1e-10f)
            {
                int segment = samples[i].SegmentIndex;
                direction = points[segment + 1] - points[segment];
            }

            if (direction.LengthSquared() < 1e-10f)
            {
                direction = Vector3.UnitZ;
            }

            samples[i] = samples[i] with
            {
                Forward = Vector3.Normalize(direction)
            };
        }

        return samples;
    }

    private static void LimitRoadGrade(
        List<RoadSample> samples,
        float maximumGrade,
        bool preserveLegacy,
        CancellationToken cancellationToken)
    {
        if (samples.Count < 2)
        {
            return;
        }

        int count = samples.Count;

        var original = new float[count];
        var upper = new float[count];
        var lower = new float[count];
        var horizontalDistances = new float[count - 1];

        for (int i = 0; i < count; i++)
        {
            float height = samples[i].Position.Y;

            original[i] = height;
            upper[i] = height;
            lower[i] = height;

            if (i == 0)
            {
                continue;
            }

            Vector3 previous = samples[i - 1].Position;
            Vector3 current = samples[i].Position;

            horizontalDistances[i - 1] = Vector2.Distance(
                new Vector2(previous.X, previous.Z),
                new Vector2(current.X, current.Z)
            );
        }

        for (int i = 1; i < count; i++)
        {
            if ((i & 255) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            float allowed =
                horizontalDistances[i - 1] * maximumGrade;

            upper[i] = MathF.Max(
                upper[i],
                upper[i - 1] - allowed
            );

            lower[i] = MathF.Min(
                lower[i],
                lower[i - 1] + allowed
            );
        }

        for (int i = count - 2; i >= 0; i--)
        {
            if ((i & 255) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            float allowed =
                horizontalDistances[i] * maximumGrade;

            upper[i] = MathF.Max(
                upper[i],
                upper[i + 1] - allowed
            );

            lower[i] = MathF.Min(
                lower[i],
                lower[i + 1] + allowed
            );
        }

        float distance = 0f;
        Vector3 previousPosition = Vector3.Zero;

        for (int i = 0; i < count; i++)
        {
            if ((i & 255) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            float targetHeight = preserveLegacy
                ? upper[i]
                : (lower[i] + upper[i]) * 0.5f;

            Vector3 position = samples[i].Position;

            position.Y = targetHeight;

            if (!Finite(position))
            {
                throw new InvalidDataException(
                    "Некорректный высотный профиль дороги."
                );
            }

            if (i > 0)
            {
                distance += Vector3.Distance(
                    previousPosition,
                    position
                );
            }

            if (!float.IsFinite(distance) ||
                distance > MaximumControlLength * 4f)
            {
                throw new InvalidDataException(
                    "Превышена допустимая длина дороги."
                );
            }

            samples[i] = samples[i] with
            {
                Position = position,
                Distance = distance
            };

            previousPosition = position;
        }
    }

    /// <summary>Shared cross-section offset for road geometry and its derived Terrain corridor.</summary>
    public static float CrossSectionHeight(RoadDefinition road, float lateral)
    {
        if (road.ShoulderWidth <= 0) return 0;
        float blend = Math.Clamp((MathF.Abs(lateral) - road.Width * 0.5f) / road.ShoulderWidth, 0, 1);
        float drop = road.Type == RoadType.Automobile ? MathF.Min(0.04f, road.Thickness * 0.25f) : road.Thickness;
        return -drop * blend;
    }

    private static void ValidateNodes(RoadDefinition road)
    {
        if (road.Nodes == null ||
            road.Nodes.Count > 256 ||
            !float.IsFinite(road.SurfaceOffset) ||
            MathF.Abs(road.SurfaceOffset) > 100f)
        {
            throw new InvalidDataException(
                "Некорректные опорные точки дороги.");
        }

        float length = 0f;

        for (int i = 0; i < road.Nodes.Count; i++)
        {
            var node = road.Nodes[i];

            if (node == null ||
                !Finite(node.Position) ||
                !float.IsFinite(node.HeightOffset) ||
                MathF.Max(
                    MathF.Abs(node.X),
                    MathF.Max(
                        MathF.Abs(node.Y),
                        MathF.Abs(node.Z))) > 1e7f ||
                MathF.Abs(node.HeightOffset) > 1e7f)
            {
                throw new InvalidDataException(
                    "Некорректные координаты опорной точки.");
            }

            if (i == 0)
            {
                continue;
            }

            var previous = road.Nodes[i - 1];

            float horizontal = Vector2.Distance(
                new Vector2(node.X, node.Z),
                new Vector2(previous.X, previous.Z));

            if (horizontal < 0.25f)
            {
                throw new InvalidDataException(
                    "Опорные точки расположены слишком близко.");
            }

            length += road.FollowTerrain
                ? horizontal + MathF.Abs(
                    node.HeightOffset - previous.HeightOffset)
                : Vector3.Distance(
                    previous.Position,
                    node.Position);

            if (length > MaximumControlLength)
            {
                throw new InvalidDataException(
                    "Длина дороги превышает 20 километров.");
            }
        }
    }

    internal static bool Finite(Vector3 value)
    {
        return float.IsFinite(value.X) &&
               float.IsFinite(value.Y) &&
               float.IsFinite(value.Z);
    }
}
