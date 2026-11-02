using System.Buffers.Binary;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain;

// Holes use the same four-sample cells as the detailed terrain mesh and collision surface.
public sealed class TerrainHoleMask
{
    public const int Step = 4;
    private const int Side = 256 / Step;
    private readonly bool[] _cells = new bool[Side * Side];

    private TerrainHoleMask() { }

    public static TerrainHoleMask? Create(TerrainTileData tile, IReadOnlyList<TerrainLayer> layers)
    {
        var holes = tile.ActiveLayers.Distinct()
            .Where(index => index > 0 && index < layers.Count && layers[index].IsHole)
            .Select(index => (Source: tile.Masks.FirstOrDefault(mask => mask.Group == (index - 1) / 3),
                Channel: (index - 1) % 3)).Where(hole => hole.Source != null).ToArray();
        if (holes.Length == 0) return null;
        var mask = new TerrainHoleMask();
        bool any = false;
        for (int z = 0; z < Side; z++)
        for (int x = 0; x < Side; x++)
        {
            float weight = 0;
            // Match (4 + tileUv * 248) / 256 in Terrain.hlsl, including texel centers.
            float u = 3.5f + (x * Step + Step * 0.5f) * (248f / 256);
            float v = 3.5f + (z * Step + Step * 0.5f) * (248f / 256);
            int left = (int)u, top = (int)v;
            foreach (var hole in holes)
            {
                var data = hole.Source!.Data;
                float a = Channel(data, left, top, hole.Channel);
                float b = Channel(data, left + 1, top, hole.Channel);
                float c = Channel(data, left, top + 1, hole.Channel);
                float d = Channel(data, left + 1, top + 1, hole.Channel);
                float upper = a + (b - a) * (u - left), lower = c + (d - c) * (u - left);
                weight += upper + (lower - upper) * (v - top);
            }
            any |= mask._cells[z * Side + x] = weight >= 0.5f;
        }
        return any ? mask : null;
    }

    public bool Intersects(int localX, int localZ, int size = Step)
    {
        int right = Math.Min((localX + size - 1) / Step, Side - 1);
        int bottom = Math.Min((localZ + size - 1) / Step, Side - 1);
        for (int z = Math.Clamp(localZ / Step, 0, Side - 1); z <= bottom; z++)
        for (int x = Math.Clamp(localX / Step, 0, Side - 1); x <= right; x++)
            if (_cells[z * Side + x]) return true;
        return false;
    }

    private static float Channel(byte[] data, int x, int z, int channel)
    {
        var block = data.AsSpan(((z / 4) * 64 + x / 4) * 8, 8);
        ushort first = BinaryPrimitives.ReadUInt16LittleEndian(block);
        ushort second = BinaryPrimitives.ReadUInt16LittleEndian(block[2..]);
        uint bits = BinaryPrimitives.ReadUInt32LittleEndian(block[4..]);
        int code = (int)((bits >> (((z % 4) * 4 + x % 4) * 2)) & 3);
        float a = Component(first, channel), b = Component(second, channel);
        return code switch
        {
            0 => a, 1 => b,
            2 => first > second ? (2 * a + b) / 3 : (a + b) / 2,
            _ => first > second ? (a + 2 * b) / 3 : 0
        };
    }

    private static float Component(ushort value, int channel) => channel switch
    {
        0 => ((value >> 11) & 31) / 31f,
        1 => ((value >> 5) & 63) / 63f,
        _ => (value & 31) / 31f
    };
}
