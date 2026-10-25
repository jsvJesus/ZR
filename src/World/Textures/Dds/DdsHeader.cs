using System.Buffers.Binary;

namespace ZoneReborn.World.Textures.Dds
{
    internal sealed record DdsHeader(
        int Width,
        int Height,
        int MipCount,
        DdsPixelFormat Format,
        DdsUncompressedLayout? UncompressedLayout,
        uint? DeclaredRowPitch)
    {
        internal const uint Dx10FourCc = 0x30315844;

        public static DdsHeader Parse(ReadOnlySpan<byte> header, ReadOnlySpan<byte> extendedHeader)
        {
            if (ReadUInt32(header, 0) != 0x20534444)
            {
                throw new InvalidDataException("Missing DDS file signature.");
            }
            if (ReadUInt32(header, 4) != 124)
            {
                throw new InvalidDataException("DDS header size must be 124 bytes.");
            }
            if (ReadUInt32(header, 76) != 32)
            {
                throw new InvalidDataException("DDS pixel format header size must be 32 bytes.");
            }

            var flags = ReadUInt32(header, 8);
            var width = ReadDimension(ReadUInt32(header, 16), "width");
            var height = ReadDimension(ReadUInt32(header, 12), "height");
            if ((flags & 0x6) != 0x6)
            {
                throw new InvalidDataException("DDS header must declare width and height.");
            }
            if ((ReadUInt32(header, 112) & 0x20fe00) != 0 ||
                (flags & 0x800000) != 0 || ReadUInt32(header, 24) > 1)
            {
                throw new InvalidDataException("DDS cube maps and volume textures are unsupported; expected one 2D texture.");
            }

            var mipCountValue = ReadUInt32(header, 28);
            if (mipCountValue == 0 && (flags & 0x20000) != 0)
            {
                throw new InvalidDataException("DDS header declares mipmaps but its mip count is zero.");
            }

            var maximumMipCount = 1;
            for (var size = Math.Max(width, height); size > 1; size /= 2)
            {
                maximumMipCount++;
            }
            if (mipCountValue > maximumMipCount)
            {
                throw new InvalidDataException($"DDS mip count {mipCountValue} exceeds the {maximumMipCount} levels allowed by its dimensions.");
            }
            var mipCount = mipCountValue == 0 ? 1 : (int)mipCountValue;

            var pixelFlags = ReadUInt32(header, 80);
            DdsPixelFormat format;
            DdsUncompressedLayout? layout = null;
            if ((pixelFlags & 0x4) != 0)
            {
                var fourCc = ReadUInt32(header, 84);
                if (fourCc == Dx10FourCc)
                {
                    (format, layout) = ReadExtendedFormat(extendedHeader);
                }
                else
                {
                    format = fourCc switch
                    {
                        0x31545844 => DdsPixelFormat.Bc1,
                        0x35545844 => DdsPixelFormat.Bc3,
                        _ => throw new InvalidDataException($"Unsupported DDS FourCC 0x{fourCc:X8}; expected DXT1, DXT5, or DX10.")
                    };
                }
            }
            else if ((pixelFlags & 0x40) != 0 && (pixelFlags & ~0x41u) == 0)
            {
                var alphaMask = (pixelFlags & 1) != 0 ? ReadUInt32(header, 104) : 0;
                if ((pixelFlags & 1) != 0 && alphaMask == 0)
                {
                    throw new InvalidDataException("DDS alpha pixels flag requires a nonzero alpha channel mask.");
                }

                layout = DdsUncompressedLayout.Create(
                    ReadUInt32(header, 88),
                    ReadUInt32(header, 92),
                    ReadUInt32(header, 96),
                    ReadUInt32(header, 100),
                    alphaMask);
                format = DdsPixelFormat.Rgba8;
            }
            else
            {
                throw new InvalidDataException($"Unsupported DDS pixel format flags 0x{pixelFlags:X8}.");
            }

            uint? declaredRowPitch = layout is not null && (flags & 8) != 0
                ? ReadUInt32(header, 20)
                : null;
            return new DdsHeader(width, height, mipCount, format, layout, declaredRowPitch);
        }

        private static (DdsPixelFormat, DdsUncompressedLayout?) ReadExtendedFormat(ReadOnlySpan<byte> header)
        {
            if (header.Length != 20)
            {
                throw new InvalidDataException("DDS DX10 header is truncated.");
            }
            if (ReadUInt32(header, 4) != 3 || ReadUInt32(header, 12) != 1 || (ReadUInt32(header, 8) & 4) != 0)
            {
                throw new InvalidDataException("DDS DX10 resource must be a single 2D texture; arrays, cube maps, and other dimensions are unsupported.");
            }

            var dxgiFormat = ReadUInt32(header, 0);
            return dxgiFormat switch
            {
                71 => (DdsPixelFormat.Bc1, null),
                77 => (DdsPixelFormat.Bc3, null),
                28 => (DdsPixelFormat.Rgba8, DdsUncompressedLayout.Create(32, 0xff, 0xff00, 0xff0000, 0xff000000)),
                87 => (DdsPixelFormat.Rgba8, DdsUncompressedLayout.Create(32, 0xff0000, 0xff00, 0xff, 0xff000000)),
                88 => (DdsPixelFormat.Rgba8, DdsUncompressedLayout.Create(32, 0xff0000, 0xff00, 0xff, 0)),
                _ => throw new InvalidDataException($"Unsupported DDS DXGI format {dxgiFormat}; expected BC1_UNORM, BC3_UNORM, or an 8-bit UNORM RGB format.")
            };
        }

        private static int ReadDimension(uint value, string name)
        {
            if (value == 0 || value > int.MaxValue)
            {
                throw new InvalidDataException($"DDS {name} {value} is outside the supported positive integer range.");
            }
            return (int)value;
        }

        private static uint ReadUInt32(ReadOnlySpan<byte> bytes, int offset) =>
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset, 4));
    }
}
