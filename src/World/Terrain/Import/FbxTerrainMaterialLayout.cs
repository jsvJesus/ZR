using System.Numerics;
using Assimp;
using Assimp.Configs;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain.Import;

public sealed record TerrainMaterialTile(byte[] ActiveLayers, IReadOnlyList<TerrainMask> Masks);

/// <summary>Projects the FBX's material assignments onto the editable heightfield's mask grid.</summary>
public static class FbxTerrainMaterialLayout
{
    public static IReadOnlyDictionary<(int X, int Z), TerrainMaterialTile> Read(string[] files,
        TerrainDescriptor descriptor, IReadOnlyList<TerrainLayer> layers, CancellationToken token)
    {
        int width = checked(descriptor.TilesX * 248), height = checked(descriptor.TilesZ * 248);
        if ((long)width * height > 16_777_216)
            throw new InvalidDataException("Для переноса материалов поддерживается до 16 миллионов отсчётов.");
        var indices = new byte[checked(width * height)];
        var heights = new float[indices.Length]; Array.Fill(heights, float.NegativeInfinity);
        var sourceLayers = layers.Where(layer => !string.IsNullOrEmpty(layer.SourceMaterial))
            .GroupBy(layer => layer.SourceMaterial, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);
        float spacing = descriptor.CellSize * 256 / 248f;
        int triangles = 0, hits = 0;
        using var context = new AssimpContext();
        context.SetConfig(new FBXPreservePivotsConfig(false));
        context.SetConfig(new FBXImportAnimationsConfig(false));
        var flags = PostProcessSteps.Triangulate | PostProcessSteps.PreTransformVertices | PostProcessSteps.GlobalScale
            | PostProcessSteps.MakeLeftHanded | PostProcessSteps.FlipWindingOrder;
        foreach (string file in files)
        {
            token.ThrowIfCancellationRequested();
            var scene = context.ImportFile(file, flags)
                ?? throw new InvalidDataException("Не удалось прочитать распределение материалов FBX.");
            foreach (var mesh in scene.Meshes)
            {
                if (mesh.MaterialIndex < 0 || mesh.MaterialIndex >= scene.Materials.Count)
                    throw new InvalidDataException("Некорректный материал поверхности FBX.");
                string key = FbxTerrainMaterials.SourceKey(file, scene.Materials[mesh.MaterialIndex], mesh.MaterialIndex);
                if (!sourceLayers.TryGetValue(key, out int layer) || layer is < 1 or > 254)
                    throw new InvalidDataException("Сначала импортируйте материалы этого FBX в палитру Terrain.");
                foreach (var face in mesh.Faces)
                {
                    if ((triangles++ & 1023) == 0) token.ThrowIfCancellationRequested();
                    if (triangles > 4_000_000) throw new InvalidDataException("В FBX слишком много треугольников.");
                    if (face.IndexCount != 3) continue;
                    var a = Project(mesh.Vertices[face.Indices[0]]);
                    var b = Project(mesh.Vertices[face.Indices[1]]);
                    var c = Project(mesh.Vertices[face.Indices[2]]);
                    double area = (double)(b.X - a.X) * (c.Z - a.Z) - (double)(b.Z - a.Z) * (c.X - a.X);
                    if (Math.Abs(area) < 1e-10) continue;
                    int x0 = Math.Max(0, (int)MathF.Floor(Math.Min(a.X, Math.Min(b.X, c.X))));
                    int x1 = Math.Min(width - 1, (int)MathF.Ceiling(Math.Max(a.X, Math.Max(b.X, c.X))));
                    int z0 = Math.Max(0, (int)MathF.Floor(Math.Min(a.Z, Math.Min(b.Z, c.Z))));
                    int z1 = Math.Min(height - 1, (int)MathF.Ceiling(Math.Max(a.Z, Math.Max(b.Z, c.Z))));
                    for (int z = z0; z <= z1; z++)
                    {
                        if ((z & 31) == 0) token.ThrowIfCancellationRequested();
                        for (int x = x0; x <= x1; x++)
                        {
                            double u = ((double)(x - a.X) * (c.Z - a.Z) - (double)(z - a.Z) * (c.X - a.X)) / area;
                            double v = ((double)(b.X - a.X) * (z - a.Z) - (double)(b.Z - a.Z) * (x - a.X)) / area;
                            if (u < -1e-6 || v < -1e-6 || u + v > 1.000001) continue;
                            float y = (float)(a.Y + (b.Y - a.Y) * u + (c.Y - a.Y) * v);
                            int offset = z * width + x;
                            if (y <= heights[offset]) continue;
                            if (indices[offset] == 0) hits++;
                            heights[offset] = y; indices[offset] = (byte)layer;
                        }
                    }
                }
            }
        }
        if (hits == 0) throw new InvalidDataException("Материалы FBX не попали на Terrain. Проверьте исходный FBX и его положение.");
        var result = new Dictionary<(int X, int Z), TerrainMaterialTile>();
        for (int tz = 0; tz < descriptor.TilesZ; tz++)
        for (int tx = 0; tx < descriptor.TilesX; tx++)
        {
            token.ThrowIfCancellationRequested();
            var masks = new Dictionary<int, byte[]>();
            var active = new HashSet<byte>();
            for (int z = 0; z < 256; z++)
            for (int x = 0; x < 256; x++)
            {
                // Four shared border texels match Terrain.hlsl and the editor's painting grid.
                int gx = Math.Clamp(tx * 248 + x - 4, 0, width - 1);
                int gz = Math.Clamp(tz * 248 + z - 4, 0, height - 1);
                byte layer = indices[gz * width + gx];
                if (layer == 0) continue;
                int group = (layer - 1) / 3;
                if (!masks.TryGetValue(group, out var pixels)) masks.Add(group, pixels = new byte[256 * 256 * 4]);
                pixels[(z * 256 + x) * 4 + (layer - 1) % 3] = 255; active.Add(layer);
            }
            result.Add((tx, tz), new(active.Order().ToArray(), masks.OrderBy(pair => pair.Key)
                .Select(pair => new TerrainMask(pair.Key, pair.Value)).ToArray()));
        }
        return result;

        Vector3 Project(Vector3 point)
        {
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) || !float.IsFinite(point.Z))
                throw new InvalidDataException("Некорректные координаты FBX.");
            return new((point.X - descriptor.SourceOffsetX) / spacing - 0.5f, point.Y,
                (point.Z - descriptor.SourceOffsetZ) / spacing - 0.5f);
        }
    }
}
