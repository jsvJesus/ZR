using System.Numerics;
using ZoneReborn.World.Roads;

namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainDocument
{
    private const int MaximumRoadTerrainCells = 2_000_000;
    private const float MaximumRoadTerrainChange = 6f;
    private const float RoadTerrainClearance = 0.025f;

    public int ConformRoad(
        RoadDefinition road,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(road);
        RoadStorage.Validate(road);

        if (road.Nodes.Count < 2)
        {
            return 0;
        }

        if (!road.FollowTerrain)
        {
            throw new InvalidOperationException(
                "Выравнивание Terrain доступно только для дорог " +
                "с включённой привязкой к рельефу.");
        }

        if (road.SurfaceOffset < 0.02f)
        {
            throw new InvalidOperationException(
                "Установите смещение поверхности дороги не менее 0,02 м.");
        }

        if (road.Nodes.Any(node =>
                MathF.Abs(node.HeightOffset) > 0.01f))
        {
            throw new InvalidOperationException(
                "Для выравнивания Terrain смещения опорных точек " +
                "должны быть равны нулю.");
        }

        var descriptor = Descriptor;

        if (!float.IsFinite(descriptor.CellSize) ||
            descriptor.CellSize <= 0f ||
            !float.IsFinite(descriptor.HeightScale) ||
            descriptor.HeightScale <= 0f)
        {
            throw new InvalidDataException(
                "Некорректные параметры высот Terrain.");
        }

        var samples = RoadSpline.Sample(
            road,
            cancellationToken: cancellationToken);

        if (samples.Count < 2)
        {
            return 0;
        }

        float halfWidth = road.Width * 0.5f;
        float shoulder = road.ShoulderWidth;
        float innerRadius = halfWidth + shoulder;
        float transition = MathF.Max(2f, road.Width * 0.6f);
        float outerRadius = innerRadius + transition;

        var heights = new float[samples.Count];

        for (int i = 0; i < samples.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            float total = 0f;
            float weight = 0f;
            float center = samples[i].Distance;
            float smoothingDistance = Math.Clamp(
                road.Width,
                2f,
                10f);

            for (int j = i; j >= 0; j--)
            {
                float distance = center - samples[j].Distance;

                if (distance > smoothingDistance)
                {
                    break;
                }

                float factor = 1f -
                    distance / (smoothingDistance + 0.001f);

                total += samples[j].Position.Y * factor;
                weight += factor;
            }

            for (int j = i + 1; j < samples.Count; j++)
            {
                float distance = samples[j].Distance - center;

                if (distance > smoothingDistance)
                {
                    break;
                }

                float factor = 1f -
                    distance / (smoothingDistance + 0.001f);

                total += samples[j].Position.Y * factor;
                weight += factor;
            }

            heights[i] = total / weight -
                road.SurfaceOffset -
                RoadTerrainClearance;
        }

        var targets = new Dictionary<
            (int X, int Z),
            RoadTerrainTarget>();

        for (int i = 1; i < samples.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Vector2 a = new(
                samples[i - 1].Position.X,
                samples[i - 1].Position.Z);

            Vector2 b = new(
                samples[i].Position.X,
                samples[i].Position.Z);

            Vector2 segment = b - a;
            float lengthSquared = segment.LengthSquared();

            if (lengthSquared < 1e-8f)
            {
                continue;
            }

            int x0 = Math.Clamp(
                (int)MathF.Floor(
                    (MathF.Min(a.X, b.X) -
                     outerRadius -
                     descriptor.OriginX) /
                    descriptor.CellSize),
                0,
                descriptor.SamplesX - 1);

            int x1 = Math.Clamp(
                (int)MathF.Ceiling(
                    (MathF.Max(a.X, b.X) +
                     outerRadius -
                     descriptor.OriginX) /
                    descriptor.CellSize),
                0,
                descriptor.SamplesX - 1);

            int z0 = Math.Clamp(
                (int)MathF.Floor(
                    (MathF.Min(a.Y, b.Y) -
                     outerRadius -
                     descriptor.OriginZ) /
                    descriptor.CellSize),
                0,
                descriptor.SamplesZ - 1);

            int z1 = Math.Clamp(
                (int)MathF.Ceiling(
                    (MathF.Max(a.Y, b.Y) +
                     outerRadius -
                     descriptor.OriginZ) /
                    descriptor.CellSize),
                0,
                descriptor.SamplesZ - 1);

            for (int gz = z0; gz <= z1; gz++)
            {
                if ((gz & 31) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                float worldZ = descriptor.OriginZ +
                    gz * descriptor.CellSize;

                for (int gx = x0; gx <= x1; gx++)
                {
                    float worldX = descriptor.OriginX +
                        gx * descriptor.CellSize;

                    if (!HasSurface(worldX, worldZ))
                    {
                        continue;
                    }

                    Vector2 point = new(worldX, worldZ);

                    float t = Math.Clamp(
                        Vector2.Dot(
                            point - a,
                            segment) / lengthSquared,
                        0f,
                        1f);

                    Vector2 closest = a + segment * t;
                    float distanceSquared =
                        Vector2.DistanceSquared(point, closest);

                    if (distanceSquared >
                        outerRadius * outerRadius)
                    {
                        continue;
                    }

                    float distance = MathF.Sqrt(distanceSquared);

                    float blend = 1f;

                    if (distance > innerRadius)
                    {
                        float fade = Math.Clamp(
                            (outerRadius - distance) / transition,
                            0f,
                            1f);

                        blend = fade * fade * (3f - 2f * fade);
                    }

                    float desiredHeight = MathF.Lerp(
                        heights[i - 1],
                        heights[i],
                        t);

                    var key = (gx, gz);

                    if (targets.TryGetValue(
                            key,
                            out var existing))
                    {
                        if (distanceSquared >=
                            existing.DistanceSquared)
                        {
                            continue;
                        }
                    }
                    else if (targets.Count >=
                             MaximumRoadTerrainCells)
                    {
                        throw new InvalidDataException(
                            "Превышен лимит изменения Terrain. " +
                            "Разделите дорогу на несколько участков.");
                    }

                    targets[key] = new RoadTerrainTarget(
                        desiredHeight,
                        blend,
                        distanceSquared);
                }
            }
        }

        var pending = new List<
            (int X, int Z, ushort Value)>();

        foreach (var pair in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int gx = pair.Key.X;
            int gz = pair.Key.Z;

            float originalHeight = Height(gx, gz);
            var target = pair.Value;

            float newHeight = MathF.Lerp(
                originalHeight,
                target.Height,
                target.Weight);

            if (!float.IsFinite(newHeight))
            {
                throw new InvalidDataException(
                    "Вычислена некорректная высота дороги.");
            }

            if (MathF.Abs(newHeight - originalHeight) >
                MaximumRoadTerrainChange)
            {
                throw new InvalidOperationException(
                    "Для размещения дороги требуется изменить " +
                    "рельеф более чем на 6 метров. " +
                    "На этом участке нужен мост, насыпь " +
                    "или корректировка опорных точек.");
            }

            float encodedHeight =
                (newHeight - descriptor.HeightOffset) /
                descriptor.HeightScale * 65535f;

            if (!float.IsFinite(encodedHeight) ||
                encodedHeight < 0f ||
                encodedHeight > 65535f)
            {
                throw new InvalidDataException(
                    "Высота дорожного коридора выходит " +
                    "за диапазон Terrain.");
            }

            ushort encoded = (ushort)Math.Clamp(
                (int)MathF.Round(encodedHeight),
                0,
                65535);

            ushort previous = Raw(
                gx / 256,
                gz / 256).Heights[
                    (gz % 256) * 256 + gx % 256];

            if (encoded == previous)
            {
                continue;
            }

            pending.Add((gx, gz, encoded));
        }

        if (pending.Count == 0)
        {
            return 0;
        }

        cancellationToken.ThrowIfCancellationRequested();

        EndSculptStroke();

        var change = new Change();

        WriteHeightChanges(pending, change);

        if (_undo.Count >= 64)
        {
            _undo.RemoveAt(0);
        }

        _undo.Add(change);

        Revision++;
        HeightRevision++;

        return pending.Count;
    }

    private readonly record struct RoadTerrainTarget(
        float Height,
        float Weight,
        float DistanceSquared);
}
