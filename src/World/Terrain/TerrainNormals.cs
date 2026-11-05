using System.Buffers.Binary;
using System.Numerics;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain;

internal static class TerrainNormals
{
    public static byte[] Build(TerrainDescriptor d, int tileX, int tileZ, Func<int, int, float> height)
    {
        var bytes = new byte[32768];
        for (int z = 0; z < 64; z++)
        for (int x = 0; x < 64; x++)
        {
            // The existing shader expects a four-texel neighbouring border.
            int gx = Math.Clamp(tileX * 256 + (int)((x * 4 + 2 - 4) * 256f / 248), 0, d.SamplesX - 1);
            int gz = Math.Clamp(tileZ * 256 + (int)((z * 4 + 2 - 4) * 256f / 248), 0, d.SamplesZ - 1);
            int left = Math.Max(gx - 1, 0), right = Math.Min(gx + 1, d.SamplesX - 1);
            int top = Math.Max(gz - 1, 0), bottom = Math.Min(gz + 1, d.SamplesZ - 1);
            var normal = Vector3.Normalize(new Vector3(
                -(height(right, gz) - height(left, gz)) / (Math.Max(1, right - left) * d.CellSize), 1,
                -(height(gx, bottom) - height(gx, top)) / (Math.Max(1, bottom - top) * d.CellSize)));
            var rgb = Vector3.Clamp(normal * 0.5f + new Vector3(0.5f), Vector3.Zero, Vector3.One);
            ushort color = (ushort)(((int)MathF.Round(rgb.X * 31) << 11)
                | ((int)MathF.Round(rgb.Y * 63) << 5) | (int)MathF.Round(rgb.Z * 31));
            int offset = (z * 64 + x) * 8;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset, 2), color);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset + 2, 2), color);
        }
        return bytes;
    }
}
