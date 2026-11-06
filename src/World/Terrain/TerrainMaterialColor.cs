using System.Numerics;
using ZoneReborn.World.Textures.Dds;

namespace ZoneReborn.World.Terrain;

public static class TerrainMaterialColor
{
    public static float[] Read(string path)
    {
        var image = DdsReader.Read(path);
        var mip = image.MipLevels[^1];
        Vector3 sum = Vector3.Zero;
        int count = 0;
        for (int z = 0; z < mip.Height; z += Math.Max(1, mip.Height / 16))
        for (int x = 0; x < mip.Width; x += Math.Max(1, mip.Width / 16))
        {
            if (image.Format == DdsPixelFormat.Rgba8)
            {
                int offset = z * mip.RowPitch + x * 4;
                sum += new Vector3(mip.Data[offset], mip.Data[offset + 1], mip.Data[offset + 2]) / 255;
            }
            else
            {
                int stride = image.Format == DdsPixelFormat.Bc1 ? 8 : 16;
                int offset = (z / 4) * mip.RowPitch + (x / 4) * stride + (stride == 16 ? 8 : 0);
                sum += TerrainMaskCodec.Color(mip.Data.AsSpan(offset, 8), x % 4, z % 4, image.Format != DdsPixelFormat.Bc1);
            }
            count++;
        }
        sum /= count; return [sum.X, sum.Y, sum.Z];
    }
}
