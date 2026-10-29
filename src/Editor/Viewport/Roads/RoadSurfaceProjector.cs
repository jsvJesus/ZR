using System.Numerics;
using Editor.Viewport.Terrain;
using ZoneReborn.World.Roads;

namespace Editor.Viewport.Roads
{
    internal sealed record RoadSurfaceProjection(RoadSegmentMesh Mesh,
        Dictionary<(int X, int Z), TerrainSurfaceGrid> Grids)
    {
        public bool IsCurrent(TerrainRenderer terrain) =>
            Grids.All(pair => ReferenceEquals(pair.Value, terrain.GetSurfaceGrid(pair.Key.X, pair.Key.Z)));
    }

    internal static class RoadSurfaceProjector
    {
        internal const float Clearance = 0.02f;

        public static RoadSurfaceProjection Build(SourceRoad road, RoadSegment segment, RoadSegmentMesh source, TerrainRenderer terrain)
        {
            var descriptor = terrain.Descriptor;
            float tileSize = descriptor.CellSize * 256;
            var grids = new Dictionary<(int X, int Z), TerrainSurfaceGrid>();
            var vertices = new List<RoadVertex>();
            var indices = new List<int>();
            int first = segment.Index * SourceRoad.SamplesPerSegment * 4;
            var input = new Vertex[source.Vertices.Length];
            for (int i = 0; i < input.Length; i++)
            {
                float baseline = road.TerrainOffsets.Length == road.Vertices.Length ? road.TerrainOffsets[first + i] : 0;
                float editedOffset = source.Vertices[i].Position.Y - road.Vertices[first + i].Position.Y;
                input[i] = new(source.Vertices[i], baseline + editedOffset);
            }
            for (int i = 0; i < source.Indices.Length; i += 3)
            {
                Vertex a = input[source.Indices[i]], b = input[source.Indices[i + 1]], c = input[source.Indices[i + 2]];
                float minX = Math.Max(0, Math.Min(a.Value.Position.X, Math.Min(b.Value.Position.X, c.Value.Position.X)));
                float minZ = Math.Max(0, Math.Min(a.Value.Position.Z, Math.Min(b.Value.Position.Z, c.Value.Position.Z)));
                float maxX = Math.Min(descriptor.WorldSizeX, Math.Max(a.Value.Position.X, Math.Max(b.Value.Position.X, c.Value.Position.X)));
                float maxZ = Math.Min(descriptor.WorldSizeZ, Math.Max(a.Value.Position.Z, Math.Max(b.Value.Position.Z, c.Value.Position.Z)));
                if (minX >= maxX || minZ >= maxZ) continue;
                int minTileX = Math.Clamp((int)(minX / tileSize), 0, descriptor.TilesX - 1);
                int minTileZ = Math.Clamp((int)(minZ / tileSize), 0, descriptor.TilesZ - 1);
                int maxTileX = Math.Clamp((int)(maxX / tileSize), 0, descriptor.TilesX - 1);
                int maxTileZ = Math.Clamp((int)(maxZ / tileSize), 0, descriptor.TilesZ - 1);
                for (int tz = minTileZ; tz <= maxTileZ; tz++)
                for (int tx = minTileX; tx <= maxTileX; tx++)
                {
                    if (!grids.TryGetValue((tx, tz), out var grid))
                    {
                        grid = terrain.GetSurfaceGrid(tx, tz);
                        grids.Add((tx, tz), grid);
                    }
                    int x0 = Math.Clamp((int)MathF.Floor((minX - grid.MinX) / grid.StepX), 0, grid.Side - 2);
                    int z0 = Math.Clamp((int)MathF.Floor((minZ - grid.MinZ) / grid.StepZ), 0, grid.Side - 2);
                    int x1 = Math.Clamp((int)MathF.Floor((maxX - grid.MinX) / grid.StepX), 0, grid.Side - 2);
                    int z1 = Math.Clamp((int)MathF.Floor((maxZ - grid.MinZ) / grid.StepZ), 0, grid.Side - 2);
                    for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        var p = grid.Positions[z * grid.Side + x]; var q = grid.Positions[z * grid.Side + x + 1];
                        var r = grid.Positions[(z + 1) * grid.Side + x]; var s = grid.Positions[(z + 1) * grid.Side + x + 1];
                        Emit(a, b, c, p, q, r, vertices, indices);
                        Emit(a, b, c, s, r, q, vertices, indices);
                    }
                }
            }
            return new(new(vertices.ToArray(), indices.ToArray()), grids);
        }

        private static void Emit(Vertex a, Vertex b, Vertex c, Vector3 p, Vector3 q, Vector3 r,
            List<RoadVertex> vertices, List<int> indices)
        {
            var polygon = new List<Vertex> { a, b, c };
            polygon = Clip(polygon, p, q);
            polygon = Clip(polygon, q, r);
            polygon = Clip(polygon, r, p);
            if (polygon.Count < 3) return;
            float determinant = Cross(q - p, r - p);
            int start = vertices.Count;
            foreach (var vertex in polygon)
            {
                var relative = vertex.Value.Position - p;
                float u = Cross(relative, r - p) / determinant, v = Cross(q - p, relative) / determinant;
                var value = vertex.Value;
                value.Position.Y = p.Y + (q.Y - p.Y) * u + (r.Y - p.Y) * v + vertex.Offset + Clearance;
                vertices.Add(value);
            }
            for (int i = 1; i + 1 < polygon.Count; i++)
            {
                // Shared clipping boundaries can produce zero-area polygons; never submit those triangles.
                if (MathF.Abs(Cross(vertices[start + i].Position - vertices[start].Position,
                    vertices[start + i + 1].Position - vertices[start].Position)) < 1e-7f) continue;
                indices.Add(start); indices.Add(start + i); indices.Add(start + i + 1);
            }
        }

        private static List<Vertex> Clip(List<Vertex> input, Vector3 a, Vector3 b)
        {
            var output = new List<Vertex>();
            if (input.Count == 0) return output;
            Vertex previous = input[^1];
            float previousDistance = Cross(b - a, previous.Value.Position - a);
            foreach (var current in input)
            {
                float distance = Cross(b - a, current.Value.Position - a);
                if ((distance >= 0) != (previousDistance >= 0))
                {
                    float t = Math.Clamp(previousDistance / (previousDistance - distance), 0, 1);
                    output.Add(Vertex.Lerp(previous, current, t));
                }
                if (distance >= 0) output.Add(current);
                previous = current; previousDistance = distance;
            }
            return output;
        }

        private static float Cross(Vector3 a, Vector3 b) => a.X * b.Z - a.Z * b.X;

        private readonly record struct Vertex(RoadVertex Value, float Offset)
        {
            public static Vertex Lerp(Vertex a, Vertex b, float t) => new(
                new RoadVertex(Vector3.Lerp(a.Value.Position, b.Value.Position, t),
                    Vector3.Lerp(a.Value.Normal, b.Value.Normal, t), Vector2.Lerp(a.Value.Uv, b.Value.Uv, t)),
                a.Offset + (b.Offset - a.Offset) * t);
        }
    }
}
