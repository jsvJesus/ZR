using System.Buffers.Binary;
using System.Numerics;

namespace ZoneReborn.World.Terrain;

/// <summary>Legacy BC1 masks and lossless RGBA masks share three weight channels.</summary>
public static class TerrainMaskCodec
{
    public static Vector3 Sample(byte[] data, int x, int z)
    {
        if (data.Length == 256 * 256 * 4)
        {
            int offset = (z * 256 + x) * 4;
            return new(data[offset] / 255f, data[offset + 1] / 255f, data[offset + 2] / 255f);
        }
        var block = data.AsSpan(((z / 4) * 64 + x / 4) * 8, 8);
        return Color(block, x % 4, z % 4);
    }

    internal static Vector3 Color(ReadOnlySpan<byte> block, int x, int z, bool fourColors = false)
    {
        ushort first = BinaryPrimitives.ReadUInt16LittleEndian(block);
        ushort second = BinaryPrimitives.ReadUInt16LittleEndian(block[2..]);
        uint bits = BinaryPrimitives.ReadUInt32LittleEndian(block[4..]);
        Vector3 a = Rgb(first), b = Rgb(second);
        return ((bits >> ((z * 4 + x) * 2)) & 3) switch
        {
            0 => a, 1 => b, 2 => fourColors || first > second ? (2 * a + b) / 3 : (a + b) / 2,
            _ => fourColors || first > second ? (a + 2 * b) / 3 : Vector3.Zero
        };
    }

    public static byte[] Editable(byte[] data)
    {
        if (data.Length == 256 * 256 * 4) return (byte[])data.Clone();
        var pixels = new byte[256 * 256 * 4];
        for (int z = 0; z < 256; z++)
        for (int x = 0; x < 256; x++)
        {
            Vector3 color = Sample(data, x, z);
            int offset = (z * 256 + x) * 4;
            pixels[offset] = (byte)MathF.Round(color.X * 255);
            pixels[offset + 1] = (byte)MathF.Round(color.Y * 255);
            pixels[offset + 2] = (byte)MathF.Round(color.Z * 255);
            pixels[offset + 3] = 255;
        }
        return pixels;
    }

    private static Vector3 Rgb(ushort value) => new(
        ((value >> 11) & 31) / 31f, ((value >> 5) & 63) / 63f, (value & 31) / 31f);
}
