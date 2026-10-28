using System.Numerics;

namespace Editor.Roads
{
    public static class RoadMeshBuilder
    {
        public static List<Vector3[]> BuildRows(RoadData road)
        {
            var rows = new List<Vector3[]>();

            if (road.Points.Count < 2 ||
                road.SamplesPerSegment < 1)
            {
                return rows;
            }

            int samples = road.SamplesPerSegment;

            int requiredRows =
                (road.Points.Count - 1) * samples + 1;

            if (road.HeightRows.Count != requiredRows)
            {
                return rows;
            }

            for (int index = 0; index < requiredRows; index++)
            {
                int segment = Math.Min(
                    index / samples,
                    road.Points.Count - 2);

                float t = index == requiredRows - 1
                    ? 1f
                    : (index - segment * samples) /
                      (float)samples;

                RoadControlPoint a = road.Points[segment];
                RoadControlPoint b = road.Points[segment + 1];

                Evaluate(
                    a,
                    b,
                    t,
                    out float x,
                    out float z,
                    out float dx,
                    out float dz);

                float length = MathF.Sqrt(
                    dx * dx + dz * dz);

                if (length < 0.0001f)
                {
                    dx = b.X - a.X;
                    dz = b.Z - a.Z;

                    length = MathF.Sqrt(
                        dx * dx + dz * dz);
                }

                if (length < 0.0001f)
                {
                    dx = 0f;
                    dz = 1f;
                    length = 1f;
                }

                float sideX = -dz / length;
                float sideZ = dx / length;

                float offsetA = a.Y - a.BaseY;
                float offsetB = b.Y - b.BaseY;

                float heightOffset =
                    offsetA * (1f - t) +
                    offsetB * t;

                RoadHeightRow original =
                    road.HeightRows[index];

                var vertices = new Vector3[4];

                for (int column = 0; column < 4; column++)
                {
                    float sideOffset =
                        (column / 3f - 0.5f) *
                        road.Width;

                    vertices[column] = new Vector3(
                        x + sideX * sideOffset,
                        original.GetHeight(column) + heightOffset,
                        z + sideZ * sideOffset);
                }

                rows.Add(vertices);
            }

            return rows;
        }

        private static void Evaluate(
            RoadControlPoint a,
            RoadControlPoint b,
            float t,
            out float x,
            out float z,
            out float dx,
            out float dz)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            float h00 = 2f * t3 - 3f * t2 + 1f;
            float h10 = t3 - 2f * t2 + t;
            float h01 = -2f * t3 + 3f * t2;
            float h11 = t3 - t2;

            float tangentAX = a.TangentX * 3f;
            float tangentAZ = a.TangentZ * 3f;

            float tangentBX = b.TangentX * 3f;
            float tangentBZ = b.TangentZ * 3f;

            x = h00 * a.X +
                h10 * tangentAX +
                h01 * b.X +
                h11 * tangentBX;

            z = h00 * a.Z +
                h10 * tangentAZ +
                h01 * b.Z +
                h11 * tangentBZ;

            float d00 = 6f * t2 - 6f * t;
            float d10 = 3f * t2 - 4f * t + 1f;
            float d01 = -6f * t2 + 6f * t;
            float d11 = 3f * t2 - 2f * t;

            dx = d00 * a.X +
                 d10 * tangentAX +
                 d01 * b.X +
                 d11 * tangentBX;

            dz = d00 * a.Z +
                 d10 * tangentAZ +
                 d01 * b.Z +
                 d11 * tangentBZ;
        }
    }
}
