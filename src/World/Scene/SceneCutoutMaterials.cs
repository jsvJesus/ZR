using System.Buffers.Binary;
using System.Collections.Concurrent;
using ZoneReborn.World.Textures.Dds;

namespace ZoneReborn.World.Scene;

internal static class SceneCutoutMaterials
{
    private const byte AlphaCutoff = 102;
    private const byte OpaqueCutoff = 204;

    private static readonly ConcurrentDictionary<string, bool> Cache =
        new(StringComparer.OrdinalIgnoreCase);

    public static bool HasCutoutAlpha(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return Cache.GetOrAdd(path, Inspect);
    }

    private static bool Inspect(string path)
    {
        try
        {
            DdsImage image = DdsReader.Read(path);
            DdsMipLevel mip = image.MipLevels[0];

            bool cutout = false;
            bool opaque = false;

            void Check(byte alpha)
            {
                cutout |= alpha < AlphaCutoff;
                opaque |= alpha >= OpaqueCutoff;
            }

            if (image.Format == DdsPixelFormat.Rgba8)
            {
                for (int y = 0; y < mip.Height; y++)
                {
                    for (int x = 0; x < mip.Width; x++)
                    {
                        Check(mip.Data[y * mip.RowPitch + x * 4 + 3]);

                        if (cutout && opaque)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            int blockSize = image.Format == DdsPixelFormat.Bc1 ? 8 : 16;
            int blocksX = (mip.Width + 3) / 4;
            int blocksY = (mip.Height + 3) / 4;

            for (int by = 0; by < blocksY; by++)
            {
                for (int bx = 0; bx < blocksX; bx++)
                {
                    int offset = by * mip.RowPitch + bx * blockSize;
                    ReadOnlySpan<byte> block = mip.Data.AsSpan(offset, blockSize);

                    uint colorIndices = 0;
                    bool bc1Transparent = false;
                    ulong alphaBits = 0;
                    Span<byte> palette = stackalloc byte[8];

                    if (image.Format == DdsPixelFormat.Bc1)
                    {
                        ushort color0 = BinaryPrimitives.ReadUInt16LittleEndian(block);
                        ushort color1 = BinaryPrimitives.ReadUInt16LittleEndian(block[2..]);

                        bc1Transparent = color0 <= color1;
                        colorIndices = BinaryPrimitives.ReadUInt32LittleEndian(block[4..]);
                    }
                    else if (image.Format == DdsPixelFormat.Bc2)
                    {
                        alphaBits = BinaryPrimitives.ReadUInt64LittleEndian(block);
                    }
                    else
                    {
                        byte a0 = block[0];
                        byte a1 = block[1];

                        palette[0] = a0;
                        palette[1] = a1;

                        if (a0 > a1)
                        {
                            for (int i = 1; i < 7; i++)
                            {
                                palette[i + 1] = (byte)(
                                    ((7 - i) * a0 + i * a1) / 7
                                );
                            }
                        }
                        else
                        {
                            for (int i = 1; i < 5; i++)
                            {
                                palette[i + 1] = (byte)(
                                    ((5 - i) * a0 + i * a1) / 5
                                );
                            }

                            palette[6] = 0;
                            palette[7] = 255;
                        }

                        for (int i = 0; i < 6; i++)
                        {
                            alphaBits |= (ulong)block[i + 2] << (i * 8);
                        }
                    }

                    for (int py = 0; py < 4; py++)
                    {
                        int y = by * 4 + py;

                        if (y >= mip.Height)
                        {
                            break;
                        }

                        for (int px = 0; px < 4; px++)
                        {
                            int x = bx * 4 + px;

                            if (x >= mip.Width)
                            {
                                break;
                            }

                            int pixel = py * 4 + px;
                            byte alpha;

                            switch (image.Format)
                            {
                                case DdsPixelFormat.Bc1:
                                    int index = (int)(
                                        (colorIndices >> (pixel * 2)) & 3
                                    );

                                    alpha = bc1Transparent && index == 3
                                        ? (byte)0
                                        : (byte)255;
                                    break;

                                case DdsPixelFormat.Bc2:
                                    alpha = (byte)(
                                        ((alphaBits >> (pixel * 4)) & 15) * 17
                                    );
                                    break;

                                default:
                                    int alphaIndex = (int)(
                                        (alphaBits >> (pixel * 3)) & 7
                                    );

                                    alpha = palette[alphaIndex];
                                    break;
                            }

                            Check(alpha);

                            if (cutout && opaque)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
        catch (Exception error) when (
            error is IOException or
            UnauthorizedAccessException or
            InvalidDataException or
            OverflowException)
        {
            return false;
        }
    }
}
