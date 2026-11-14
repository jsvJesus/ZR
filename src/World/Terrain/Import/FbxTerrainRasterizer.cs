using System.Numerics;
using Assimp;
using Assimp.Configs;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain.Import;

internal sealed record RasterizedTerrain(int Width, int Height, float CellSize, Vector2 Offset,
    float[] Heights, TerrainHoles Holes, int Triangles, int OverlappingSamples);

internal static class FbxTerrainRasterizer
{
    public static RasterizedTerrain Read(string[] files, float cellSize, Action<string> progress, CancellationToken token,
        TerrainDescriptor? existing = null)
    {
        if (!float.IsFinite(cellSize) || cellSize < 0.25f || cellSize > 4)
            throw new InvalidDataException("Шаг сетки должен быть от 0,25 до 4 метров.");
        var triangles = new List<(Vector3 A, Vector3 B, Vector3 C)>();
        var minimum = new Vector3(float.PositiveInfinity); var maximum = new Vector3(float.NegativeInfinity);
        using var context = new AssimpContext();
        context.SetConfig(new FBXPreservePivotsConfig(false));
        context.SetConfig(new FBXImportAnimationsConfig(false));
        var flags = PostProcessSteps.Triangulate | PostProcessSteps.PreTransformVertices | PostProcessSteps.GlobalScale
            | PostProcessSteps.MakeLeftHanded | PostProcessSteps.FlipWindingOrder;
        foreach (string file in files)
        {
            token.ThrowIfCancellationRequested(); progress("Чтение " + Path.GetFileName(file));
            var scene = context.ImportFile(file, flags);
            if (scene == null || !scene.HasMeshes) throw new InvalidDataException("В FBX нет поверхности земли.");
            foreach (var mesh in scene.Meshes)
            foreach (var face in mesh.Faces)
            {
                if ((triangles.Count & 1023) == 0) token.ThrowIfCancellationRequested();
                if (face.IndexCount != 3) continue;
                var a = mesh.Vertices[face.Indices[0]]; var b = mesh.Vertices[face.Indices[1]]; var c = mesh.Vertices[face.Indices[2]];
                if (!Finite(a) || !Finite(b) || !Finite(c)) throw new InvalidDataException("Некорректные координаты FBX.");
                if (triangles.Count >= 4_000_000) throw new InvalidDataException("В FBX слишком много треугольников.");
                triangles.Add((a, b, c)); minimum = Vector3.Min(minimum, Vector3.Min(a, Vector3.Min(b, c)));
                maximum = Vector3.Max(maximum, Vector3.Max(a, Vector3.Max(b, c)));
            }
        }
        if (triangles.Count == 0) throw new InvalidDataException("В FBX нет треугольных поверхностей.");
        var offset = new Vector2(minimum.X, minimum.Z);
        if (existing != null)
        {
            if (cellSize != existing.CellSize) throw new InvalidDataException("При обновлении шаг Terrain должен сохраняться.");
            float span = 256 * cellSize;
            offset.X = existing.SourceOffsetX + Math.Min(0, MathF.Floor((minimum.X - existing.SourceOffsetX + cellSize * 1e-4f) / span)) * span;
            offset.Y = existing.SourceOffsetZ + Math.Min(0, MathF.Floor((minimum.Z - existing.SourceOffsetZ + cellSize * 1e-4f) / span)) * span;
            maximum.X = Math.Max(maximum.X, existing.SourceOffsetX + (existing.SamplesX - 1) * cellSize);
            maximum.Z = Math.Max(maximum.Z, existing.SourceOffsetZ + (existing.SamplesZ - 1) * cellSize);
        }
        double sizeX = Math.Ceiling((maximum.X - offset.X) / cellSize) + 1;
        double sizeZ = Math.Ceiling((maximum.Z - offset.Y) / cellSize) + 1;
        if (sizeX <= 1 || sizeZ <= 1 || sizeX > 8192 || sizeZ > 8192)
            throw new InvalidDataException("Размер земли не поддерживается. Проверьте единицы FBX или увеличьте шаг сетки.");
        int width = Math.Max(256, ((int)sizeX + 255) / 256 * 256), height = Math.Max(256, ((int)sizeZ + 255) / 256 * 256);
        if ((long)width * height > 16_777_216) throw new InvalidDataException("Более 16 миллионов отсчётов. Увеличьте шаг сетки.");
        var values = new float[width * height]; Array.Fill(values, float.NaN);
        var coverage = new byte[(width - 1) * (height - 1)];
        int overlaps = 0;
        byte[]? overlapFlags = null;
        for (int index = 0; index < triangles.Count; index++)
        {
            if ((index & 255) == 0) { token.ThrowIfCancellationRequested(); progress($"Перенос рельефа: {index:N0}/{triangles.Count:N0}"); }
            var triangle = triangles[index];
            var a = Project(triangle.A); var b = Project(triangle.B); var c = Project(triangle.C);
            double area = (double)(b.X - a.X) * (c.Z - a.Z) - (double)(b.Z - a.Z) * (c.X - a.X);
            if (Math.Abs(area) < 1e-10) continue; // Vertical walls cannot be represented by a heightfield.
            int x0 = Math.Clamp((int)MathF.Floor(Math.Min(a.X, Math.Min(b.X, c.X))), 0, width - 1);
            int x1 = Math.Clamp((int)MathF.Ceiling(Math.Max(a.X, Math.Max(b.X, c.X))), 0, width - 1);
            int z0 = Math.Clamp((int)MathF.Floor(Math.Min(a.Z, Math.Min(b.Z, c.Z))), 0, height - 1);
            int z1 = Math.Clamp((int)MathF.Ceiling(Math.Max(a.Z, Math.Max(b.Z, c.Z))), 0, height - 1);
            for (int z = z0; z <= z1; z++)
            {
                if ((z & 31) == 0) token.ThrowIfCancellationRequested();
                for (int x = x0; x <= x1; x++)
                {
                    if (Sample(x, z, out float y))
                    {
                        int position = z * width + x;
                        if (float.IsFinite(values[position]) && Math.Abs(values[position] - y) > 0.5f)
                        {
                            overlapFlags ??= new byte[(values.Length + 7) / 8];
                            int flag = 1 << (position & 7);
                            if ((overlapFlags[position >> 3] & flag) == 0)
                            {
                                overlapFlags[position >> 3] |= (byte)flag;
                                overlaps++;
                            }
                        }
                        // A folded ground mesh may intersect the same vertical ray
                        // more than once. Its visible upper surface defines the heightfield.
                        if (!float.IsFinite(values[position]) || y > values[position]) values[position] = y;
                    }
                    if (x < width - 1 && z < height - 1 && Sample(x + 0.5f, z + 0.5f, out _))
                        coverage[z * (width - 1) + x] = 1;
                }
            }

            bool Sample(float x, float z, out float y)
            {
                double u = ((double)(x - a.X) * (c.Z - a.Z) - (double)(z - a.Z) * (c.X - a.X)) / area;
                double v = ((double)(b.X - a.X) * (z - a.Z) - (double)(b.Z - a.Z) * (x - a.X)) / area;
                y = (float)(a.Y + (b.Y - a.Y) * u + (c.Y - a.Y) * v);
                return u >= -1e-6 && v >= -1e-6 && u + v <= 1.000001 && float.IsFinite(y);
            }
        }
        var holes = new TerrainHoles(width - 1, height - 1);
        bool hasSolidCell = false;
        for (int z = 0; z < height - 1; z++)
        {
            token.ThrowIfCancellationRequested();
            for (int x = 0; x < width - 1; x++)
                if (coverage[z * (width - 1) + x] == 0 || !float.IsFinite(values[z * width + x])
                    || !float.IsFinite(values[z * width + x + 1]) || !float.IsFinite(values[(z + 1) * width + x])
                    || !float.IsFinite(values[(z + 1) * width + x + 1])) holes.Set(x, z, true);
                else hasSolidCell = true;
        }
        if (!hasSolidCell) throw new InvalidDataException("После переноса не осталось целых ячеек земли. Уменьшите шаг сетки.");
        // Hidden heights beneath holes remain editable and fillable. Never render them
        // until the user explicitly fills the corresponding hole mask.
        var queue = new int[values.Length]; int head = 0, tail = 0;
        for (int i = 0; i < values.Length; i++) if (float.IsFinite(values[i])) queue[tail++] = i;
        if (tail == 0) throw new InvalidDataException("Поверхность не попала на сетку. Уменьшите шаг или проверьте ориентацию FBX.");
        while (head < tail)
        {
            if ((head & 4095) == 0) token.ThrowIfCancellationRequested();
            int current = queue[head++], x = current % width, z = current / width;
            if (x > 0) Visit(current - 1); if (x + 1 < width) Visit(current + 1);
            if (z > 0) Visit(current - width); if (z + 1 < height) Visit(current + width);
            void Visit(int next) { if (float.IsFinite(values[next])) return; values[next] = values[current]; queue[tail++] = next; }
        }
        return new(width, height, cellSize, offset, values, holes, triangles.Count, overlaps);

        Vector3 Project(Vector3 point) => new((point.X - offset.X) / cellSize, point.Y, (point.Z - offset.Y) / cellSize);
    }

    private static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}
