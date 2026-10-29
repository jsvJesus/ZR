using System.Numerics;

namespace ZoneReborn.World.Roads
{
    public sealed record RoadSegmentMesh(RoadVertex[] Vertices, int[] Indices);

    public static class RoadMesh
    {
        public static RoadSegmentMesh Build(SourceRoad road, RoadSegment segment)
        {
            int steps = SourceRoad.SamplesPerSegment;
            int first = segment.Index * steps * 4;
            var vertices = new RoadVertex[(steps + 1) * 4];
            var indices = new int[steps * 18];
            Array.Copy(road.Vertices, first, vertices, 0, vertices.Length);
            for (int i = 0; i < indices.Length; i++)
                indices[i] = road.Indices[segment.Index * indices.Length + i] - first;

            if (road.GeometryEdited)
            {
                for (int row = 0; row <= steps; row++)
                {
                    var positions = DeformRow(road, segment.Index * steps + row);
                    for (int column = 0; column < 4; column++)
                        vertices[row * 4 + column].Position = positions[column];
                }
                // Recompute normals only after geometry edits. Original mesh normals remain exact on load.
                var normals = new Vector3[vertices.Length];
                for (int i = 0; i < indices.Length; i += 3)
                {
                    int ia = indices[i], ib = indices[i + 1], ic = indices[i + 2];
                    var normal = FaceNormal(vertices[ia].Position, vertices[ib].Position, vertices[ic].Position);
                    normals[ia] += normal; normals[ib] += normal; normals[ic] += normal;
                }
                // Boundary vertices include triangles on both sides of their shared global row.
                if (segment.Index > 0) AddBoundaryNormals(road, segment.Index * steps - 1, segment.Index * steps, normals, 0);
                if (segment.Index + 1 < road.Segments.Length)
                    AddBoundaryNormals(road, (segment.Index + 1) * steps, (segment.Index + 1) * steps, normals, steps * 4);
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i].Normal = normals[i].LengthSquared() > 1e-10f ? Vector3.Normalize(normals[i]) : Vector3.UnitY;
            }
            foreach (ref var vertex in vertices.AsSpan()) vertex.Uv.X *= segment.UvScale;
            return new(vertices, indices);
        }

        private static Vector3[] DeformRow(SourceRoad road, int globalRow)
        {
            int steps = SourceRoad.SamplesPerSegment;
            int segment = Math.Min(globalRow / steps, road.Segments.Length - 1);
            float t = (globalRow - segment * steps) / (float)steps, t2 = t * t, t3 = t2 * t;
            var a = road.Nodes[segment]; var b = road.Nodes[segment + 1];
            var originalA = road.OriginalNodes[segment]; var originalB = road.OriginalNodes[segment + 1];
            Vector3 da = a.Position - originalA.Position, db = b.Position - originalB.Position;
            // DAT stores Bézier handle offsets; Hermite endpoint derivatives are three times the handles.
            Vector3 ta = new(3 * (a.TangentX - originalA.TangentX), 0, 3 * (a.TangentZ - originalA.TangentZ));
            Vector3 tb = new(3 * (b.TangentX - originalB.TangentX), 0, 3 * (b.TangentZ - originalB.TangentZ));
            Vector3 delta = (2 * t3 - 3 * t2 + 1) * da + (t3 - 2 * t2 + t) * ta
                + (-2 * t3 + 3 * t2) * db + (t3 - t2) * tb;
            Vector3 derivative = (6 * t2 - 6 * t) * da + (3 * t2 - 4 * t + 1) * ta
                + (-6 * t2 + 6 * t) * db + (3 * t2 - 2 * t) * tb;
            int before = Math.Max(0, globalRow - 1), after = Math.Min(road.Vertices.Length / 4 - 1, globalRow + 1);
            Vector3 oldDirection = (RowCenter(road, after) - RowCenter(road, before)) * (steps / (float)(after - before));
            Vector3 newDirection = oldDirection + derivative;
            float angle = oldDirection.X * oldDirection.X + oldDirection.Z * oldDirection.Z > 1e-8f
                && newDirection.X * newDirection.X + newDirection.Z * newDirection.Z > 1e-8f
                ? MathF.Atan2(newDirection.X, newDirection.Z) - MathF.Atan2(oldDirection.X, oldDirection.Z) : 0;
            var rotation = Matrix4x4.CreateRotationY(angle);
            float width = a.Width / originalA.Width * (1 - t) + b.Width / originalB.Width * t;
            var center = RowCenter(road, globalRow);
            var result = new Vector3[4];
            for (int column = 0; column < 4; column++)
            {
                var lateral = road.Vertices[globalRow * 4 + column].Position - center;
                var rotated = Vector3.TransformNormal(new Vector3(lateral.X, 0, lateral.Z) * width, rotation);
                result[column] = center + delta + rotated + new Vector3(0, lateral.Y, 0);
            }
            return result;
        }

        private static Vector3 RowCenter(SourceRoad road, int row)
        {
            Vector3 center = Vector3.Zero;
            for (int column = 0; column < 4; column++) center += road.Vertices[row * 4 + column].Position * 0.25f;
            return center;
        }

        private static Vector3 FaceNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            var normal = Vector3.Cross(b - a, c - a);
            return normal.Y < 0 ? -normal : normal;
        }

        private static void AddBoundaryNormals(SourceRoad road, int stripRow, int boundaryRow, Vector3[] normals, int localOffset)
        {
            var a = DeformRow(road, stripRow); var b = DeformRow(road, stripRow + 1);
            Vector3 Position(int index) => index / 4 == stripRow ? a[index % 4] : b[index % 4];
            for (int i = stripRow * 18; i < (stripRow + 1) * 18; i += 3)
            {
                int ia = road.Indices[i], ib = road.Indices[i + 1], ic = road.Indices[i + 2];
                var normal = FaceNormal(Position(ia), Position(ib), Position(ic));
                foreach (int index in new[] { ia, ib, ic })
                    if (index / 4 == boundaryRow) normals[localOffset + index % 4] += normal;
            }
        }

        public static Vector3 Center(SourceRoad road, RoadSegment segment)
        {
            var mesh = Build(road, segment);
            return mesh.Vertices.Aggregate(Vector3.Zero, (sum, v) => sum + v.Position) / mesh.Vertices.Length;
        }
    }
}
