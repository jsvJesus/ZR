using System.Numerics;
using System.Runtime.CompilerServices;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Roads;

namespace ZoneReborn.World.Vegetation;

internal sealed class VegetationRoadExclusion
{
    private static readonly ConditionalWeakTable<RoadNetwork, Snapshot> Snapshots = new();
    private readonly Segment[] _segments;

    private VegetationRoadExclusion(Segment[] segments) => _segments = segments;

    public static VegetationRoadExclusion ForTile(
        RoadNetwork network, AssetPaths assets,
        float minX, float minZ, float maxX, float maxZ,
        float maximumPlantRadius, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var snapshot = Snapshots.GetValue(network, static _ => new Snapshot());
        lock (snapshot)
        {
            if (snapshot.Revision != network.Revision ||
                !string.Equals(snapshot.AssetRoot, assets.Root, StringComparison.OrdinalIgnoreCase))
            {
                // Publish only complete geometry; cancelled generation cannot cache a partial mask.
                Segment[] segments = Build(network, assets, token);
                snapshot.Segments = segments;
                snapshot.AssetRoot = assets.Root;
                snapshot.Revision = network.Revision;
            }

            var minimum = new Vector2(minX - maximumPlantRadius, minZ - maximumPlantRadius);
            var maximum = new Vector2(maxX + maximumPlantRadius, maxZ + maximumPlantRadius);
            return new VegetationRoadExclusion(snapshot.Segments.Where(segment =>
                segment.Maximum.X >= minimum.X && segment.Minimum.X <= maximum.X &&
                segment.Maximum.Y >= minimum.Y && segment.Minimum.Y <= maximum.Y).ToArray());
        }
    }

    public bool Intersects(float x, float z, float plantRadius, float clearance)
    {
        var position = new Vector2(x, z);
        float margin = plantRadius + clearance;
        foreach (Segment segment in _segments)
        {
            if (x < segment.Minimum.X - margin || x > segment.Maximum.X + margin ||
                z < segment.Minimum.Y - margin || z > segment.Maximum.Y + margin)
            {
                continue;
            }

            Vector2 direction = segment.End - segment.Start;
            float lengthSquared = direction.LengthSquared();
            float t = lengthSquared > 1e-8f
                ? Math.Clamp(Vector2.Dot(position - segment.Start, direction) / lengthSquared, 0f, 1f)
                : 0f;
            float radius = segment.Radius + margin;
            if (Vector2.DistanceSquared(position, segment.Start + direction * t) <= radius * radius)
            {
                return true;
            }
        }

        return false;
    }

    private static Segment[] Build(RoadNetwork network, AssetPaths assets, CancellationToken token)
    {
        RoadCatalog? railwayModels = null;
        var segments = new List<Segment>();
        foreach (RoadPath road in network.Roads)
        {
            token.ThrowIfCancellationRequested();
            if (road.Points.Count < 2) continue;

            float width = road.Width;
            if (road.Category == RoadCategory.Railway)
            {
                railwayModels ??= RoadCatalog.Scan(assets);
                if (railwayModels.TryGet(road.Model, out var model)) width = model.Width;
            }

            // Use the same rounded centreline as RoadSurfaceBuilder/RoadBuilder.
            RoadCurve curve = RoadCurve.Create(road.Points, width);
            float shoulder = road.Category == RoadCategory.Automobile ? road.ShoulderWidth : 0f;
            float radius = width * 0.5f + shoulder + 0.01f;
            var extent = new Vector2(radius);
            for (int index = 1; index < curve.Samples.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                Vector2 start = curve.Samples[index - 1].Position;
                Vector2 end = curve.Samples[index].Position;
                segments.Add(new Segment(start, end, radius,
                    Vector2.Min(start, end) - extent, Vector2.Max(start, end) + extent));
            }

            // Structures use raw control-point segments rather than the rounded curve.
            foreach (RoadStructure structure in road.Structures)
            {
                token.ThrowIfCancellationRequested();
                if (structure.StartIndex < 0 || structure.EndIndex >= road.Points.Count ||
                    structure.EndIndex != structure.StartIndex + 1)
                {
                    continue;
                }

                float structureRadius = radius;
                try
                {
                    var bounds = RoadCatalog.ReadModelBounds(assets, structure.Model);
                    Vector3 size = bounds.Maximum - bounds.Minimum;
                    // The long axis is stretched between the points. Half the model
                    // height also bounds its horizontal displacement at any road slope.
                    float modelRadius = (Math.Min(size.X, size.Z) + size.Y) * 0.5f;
                    if (float.IsFinite(modelRadius))
                        structureRadius = Math.Max(structureRadius, modelRadius + 0.01f);
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or
                    InvalidDataException or FormatException or OverflowException or ArgumentException or
                    System.Xml.XmlException)
                {
                    // Missing structures are also skipped by the renderer. Still
                    // reserve the configured road corridor along their raw segment.
                }

                RoadPoint first = road.Points[structure.StartIndex];
                RoadPoint last = road.Points[structure.EndIndex];
                var start = new Vector2(first.X, first.Z);
                var end = new Vector2(last.X, last.Z);
                var structureExtent = new Vector2(structureRadius);
                segments.Add(new Segment(start, end, structureRadius,
                    Vector2.Min(start, end) - structureExtent,
                    Vector2.Max(start, end) + structureExtent));
            }
        }

        return segments.ToArray();
    }

    private sealed class Snapshot
    {
        public long Revision = -1;
        public string AssetRoot = "";
        public Segment[] Segments = [];
    }

    private readonly record struct Segment(
        Vector2 Start, Vector2 End, float Radius, Vector2 Minimum, Vector2 Maximum);
}
