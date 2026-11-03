using System.Buffers.Binary;
using System.IO;
using ZoneReborn.World.Textures.Dds;

namespace Editor.Viewport.Scene;

/// <summary>Supplies filtering levels for source DDS files exported without mipmaps.</summary>
internal static class SceneTextureMips
{
    public static DdsImage Complete(DdsImage image, CancellationToken token)
    {
        if (image.MipLevels.Count != 1 || (image.Width == 1 && image.Height == 1)) return image;
        var top = image.MipLevels[0];
        byte[] pixels = image.Format == DdsPixelFormat.Rgba8 ? top.Data : Decode(top, image.Format, token);
        var mips = new List<DdsMipLevel> { new(image.Width, image.Height, image.Width * 4, pixels) };
        int width = image.Width, height = image.Height;
        while (width > 1 || height > 1)
        {
            token.ThrowIfCancellationRequested();
            int nextWidth = Math.Max(1, width / 2), nextHeight = Math.Max(1, height / 2);
            var next = new byte[checked(nextWidth * nextHeight * 4)];
            for (int y = 0; y < nextHeight; y++)
            {
                token.ThrowIfCancellationRequested();
                int y0 = y * height / nextHeight, y1 = (y + 1) * height / nextHeight;
                for (int x = 0; x < nextWidth; x++)
                {
                    int x0 = x * width / nextWidth, x1 = (x + 1) * width / nextWidth;
                    int samples = (x1 - x0) * (y1 - y0);
                    for (int channel = 0; channel < 4; channel++)
                    {
                        int sum = 0;
                        for (int sy = y0; sy < y1; sy++)
                            for (int sx = x0; sx < x1; sx++) sum += pixels[(sy * width + sx) * 4 + channel];
                        next[(y * nextWidth + x) * 4 + channel] = (byte)((sum + samples / 2) / samples);
                    }
                }
            }
            mips.Add(new(nextWidth, nextHeight, nextWidth * 4, next));
            pixels = next; width = nextWidth; height = nextHeight;
        }
        return new(image.Width, image.Height, DdsPixelFormat.Rgba8, mips);
    }

    private static byte[] Decode(DdsMipLevel mip, DdsPixelFormat format, CancellationToken token)
    {
        if (format is not (DdsPixelFormat.Bc1 or DdsPixelFormat.Bc2 or DdsPixelFormat.Bc3))
            throw new InvalidDataException("Unsupported scene texture compression.");
        var pixels = new byte[checked(mip.Width * mip.Height * 4)];
        int blockBytes = format == DdsPixelFormat.Bc1 ? 8 : 16;
        Span<byte> colors = stackalloc byte[16];
        Span<byte> alpha = stackalloc byte[8];
        for (int by = 0; by < (mip.Height + 3) / 4; by++)
        {
            token.ThrowIfCancellationRequested();
            for (int bx = 0; bx < (mip.Width + 3) / 4; bx++)
            {
                var block = mip.Data.AsSpan(by * mip.RowPitch + bx * blockBytes, blockBytes);
                var colorBlock = block[(blockBytes - 8)..];
                ushort c0 = BinaryPrimitives.ReadUInt16LittleEndian(colorBlock);
                ushort c1 = BinaryPrimitives.ReadUInt16LittleEndian(colorBlock[2..]);
                Color565(c0, colors); Color565(c1, colors[4..]);
                bool fourColors = format != DdsPixelFormat.Bc1 || c0 > c1;
                for (int channel = 0; channel < 3; channel++)
                {
                    int a = colors[channel], b = colors[4 + channel];
                    colors[8 + channel] = (byte)(fourColors ? (2 * a + b) / 3 : (a + b) / 2);
                    colors[12 + channel] = (byte)(fourColors ? (a + 2 * b) / 3 : 0);
                }
                colors[11] = 255; colors[15] = fourColors ? (byte)255 : (byte)0;
                uint colorIndices = BinaryPrimitives.ReadUInt32LittleEndian(colorBlock[4..]);
                ulong alphaIndices = 0;
                if (format == DdsPixelFormat.Bc3)
                {
                    alpha[0] = block[0]; alpha[1] = block[1];
                    int denominator = alpha[0] > alpha[1] ? 7 : 5;
                    for (int i = 1; i < denominator; i++)
                        alpha[i + 1] = (byte)(((denominator - i) * alpha[0] + i * alpha[1]) / denominator);
                    if (denominator == 5) { alpha[6] = 0; alpha[7] = 255; }
                    for (int i = 0; i < 6; i++) alphaIndices |= (ulong)block[i + 2] << (8 * i);
                }
                else if (format == DdsPixelFormat.Bc2)
                    alphaIndices = BinaryPrimitives.ReadUInt64LittleEndian(block);
                for (int i = 0; i < 16; i++)
                {
                    int x = bx * 4 + i % 4, y = by * 4 + i / 4;
                    if (x >= mip.Width || y >= mip.Height) continue;
                    int destination = (y * mip.Width + x) * 4;
                    int index = (int)((colorIndices >> (i * 2)) & 3);
                    colors.Slice(index * 4, 4).CopyTo(pixels.AsSpan(destination, 4));
                    if (format == DdsPixelFormat.Bc3)
                        pixels[destination + 3] = alpha[(int)((alphaIndices >> (i * 3)) & 7)];
                    else if (format == DdsPixelFormat.Bc2)
                        pixels[destination + 3] = (byte)(((alphaIndices >> (i * 4)) & 15) * 17);
                }
            }
        }
        return pixels;
    }

    private static void Color565(ushort value, Span<byte> color)
    {
        int r = value >> 11, g = (value >> 5) & 63, b = value & 31;
        color[0] = (byte)((r << 3) | (r >> 2));
        color[1] = (byte)((g << 2) | (g >> 4));
        color[2] = (byte)((b << 3) | (b >> 2));
        color[3] = 255;
    }
}
