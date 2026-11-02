using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using ZoneReborn.World.Terrain.Models;

namespace Editor.Viewport.Terrain;

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct TerrainVertex(Vector3 Position, Vector2 TileUv);

internal static class TerrainMeshBuilder
{
    // Every tile uses the same sampling step, including its shared border vertices.
    internal const int Step = 4;
    internal const int Side = 256 / Step + 1;

    public static (TerrainVertex[] Vertices, int[] Indices, int MainIndexCount, int EdgeIndexCount) Build(TerrainDescriptor descriptor,
        TerrainTileData tile, TerrainTileData? right, TerrainTileData? bottom, TerrainTileData? diagonal,
        Func<int, int, float> overviewHeight)
    {
        var vertices = new TerrainVertex[Side * Side];
        for (int z = 0; z < Side; z++)
        for (int x = 0; x < Side; x++)
        {
            int localX = x * Step, localZ = z * Step;
            int globalX = Math.Min(tile.X * 256 + localX, descriptor.SamplesX - 1);
            int globalZ = Math.Min(tile.Z * 256 + localZ, descriptor.SamplesZ - 1);
            var source = localX == 256 && localZ == 256 ? diagonal
                : localX == 256 ? right : localZ == 256 ? bottom : tile;
            int sampleX = localX == 256 && right is not null ? 0 : Math.Min(localX, 255);
            int sampleZ = localZ == 256 && bottom is not null ? 0 : Math.Min(localZ, 255);
            // On one outer edge the adjacent tile on the other axis still owns the corner.
            source ??= localX == 256 && localZ == 256 ? right ?? bottom ?? tile : tile;
            ushort height = source.Heights[sampleZ * 256 + sampleX];
            vertices[z * Side + x] = new TerrainVertex(new Vector3(globalX * descriptor.CellSize,
                descriptor.DecodeHeight(height), globalZ * descriptor.CellSize), new Vector2(localX / 256f, localZ / 256f));
        }
        var indices = new int[(Side - 1) * (Side - 1) * 6];
        int offset = 0;
        for (int z = 0; z < Side - 1; z++)
        for (int x = 0; x < Side - 1; x++)
        {
            int a = z * Side + x, b = a + 1, c = a + Side, d = c + 1;
            indices[offset++] = a; indices[offset++] = c; indices[offset++] = b;
            indices[offset++] = b; indices[offset++] = c; indices[offset++] = d;
        }
        int mainIndexCount = indices.Length;
        var allVertices = vertices.ToList();
        var allIndices = indices.ToList();
        // Each edge has its own draw range. Only edges touching the overview are rendered.
        for (int edge = 0; edge < 4; edge++)
        {
            int first = allVertices.Count;
            for (int index = 0; index < Side; index++)
            {
                int topIndex = edge switch
                {
                    0 => index,
                    1 => (Side - 1) * Side + index,
                    2 => index * Side,
                    _ => index * Side + Side - 1
                };
                var vertex = vertices[topIndex];
                int sampleX = tile.X * 256 + (int)(vertex.TileUv.X * 256);
                int sampleZ = tile.Z * 256 + (int)(vertex.TileUv.Y * 256);
                float coarseHeight = overviewHeight(sampleX, sampleZ);
                var upper = vertex.Position;
                var lower = vertex.Position;
                // Span either sign of LOD height error; the lower margin covers rasterization gaps.
                upper.Y = Math.Max(upper.Y, coarseHeight);
                lower.Y = Math.Min(lower.Y, coarseHeight) - 2 * descriptor.CellSize;
                allVertices.Add(new TerrainVertex(upper, vertex.TileUv));
                allVertices.Add(new TerrainVertex(lower, vertex.TileUv));
            }
            for (int index = 0; index < Side - 1; index++)
            {
                int a = first + index * 2, b = a + 2, c = a + 1, d = a + 3;
                allIndices.Add(a); allIndices.Add(c); allIndices.Add(b);
                allIndices.Add(b); allIndices.Add(c); allIndices.Add(d);
            }
        }
        return (allVertices.ToArray(), allIndices.ToArray(), mainIndexCount, (Side - 1) * 6);
    }

    public static float[] BuildWeightSum(TerrainTileData tile, int layerCount)
    {
        var sum = new float[256 * 256];
        var active = tile.ActiveLayers.ToHashSet();
        foreach (var mask in tile.Masks)
        {
            for (int blockZ = 0; blockZ < 64; blockZ++)
            for (int blockX = 0; blockX < 64; blockX++)
            {
                var block = mask.Data.AsSpan((blockZ * 64 + blockX) * 8, 8);
                ushort first = BinaryPrimitives.ReadUInt16LittleEndian(block);
                ushort second = BinaryPrimitives.ReadUInt16LittleEndian(block[2..]);
                uint bits = BinaryPrimitives.ReadUInt32LittleEndian(block[4..]);
                Vector3 a = DecodeRgb(first), b = DecodeRgb(second);
                Vector3 c = first > second ? (2 * a + b) / 3 : (a + b) / 2;
                Vector3 d = first > second ? (a + 2 * b) / 3 : Vector3.Zero;
                for (int z = 0; z < 4; z++)
                for (int x = 0; x < 4; x++)
                {
                    var value = ((bits >> ((z * 4 + x) * 2)) & 3) switch { 0 => a, 1 => b, 2 => c, _ => d };
                    int firstLayer = mask.Group * 3 + 1;
                    float weight = 0;
                    if (firstLayer < layerCount && active.Contains((byte)firstLayer)) weight += value.X;
                    if (firstLayer + 1 < layerCount && active.Contains((byte)(firstLayer + 1))) weight += value.Y;
                    if (firstLayer + 2 < layerCount && active.Contains((byte)(firstLayer + 2))) weight += value.Z;
                    sum[(blockZ * 4 + z) * 256 + blockX * 4 + x] += weight;
                }
            }
        }
        return sum;
    }

    private static Vector3 DecodeRgb(ushort value) => new(
        ((value >> 11) & 31) / 31f, ((value >> 5) & 63) / 63f, (value & 31) / 31f);
}
