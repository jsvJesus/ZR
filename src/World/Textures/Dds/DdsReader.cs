using System.Buffers.Binary;

namespace ZoneReborn.World.Textures.Dds
{
    public static class DdsReader
    {
        public static DdsImage Read(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            try
            {
                using var stream = File.OpenRead(path);
                Span<byte> headerBytes = stackalloc byte[128];
                stream.ReadExactly(headerBytes);
                Span<byte> extendedHeader = stackalloc byte[20];
                var hasExtendedHeader =
                    (BinaryPrimitives.ReadUInt32LittleEndian(headerBytes.Slice(80, 4)) & 4) != 0 &&
                    BinaryPrimitives.ReadUInt32LittleEndian(headerBytes.Slice(84, 4)) == DdsHeader.Dx10FourCc;
                if (hasExtendedHeader)
                {
                    stream.ReadExactly(extendedHeader);
                }

                var header = DdsHeader.Parse(headerBytes, hasExtendedHeader ? extendedHeader : ReadOnlySpan<byte>.Empty);
                var payloadLength = stream.Length - stream.Position;
                var alignRows = SelectRowAlignment(header, payloadLength);
                var mipLevels = new DdsMipLevel[header.MipCount];
                var width = header.Width;
                var height = header.Height;

                for (var level = 0; level < mipLevels.Length; level++)
                {
                    var (sourceRowPitch, sourceLength) = GetMipLayout(header, width, height, alignRows);
                    var data = new byte[(int)sourceLength];
                    stream.ReadExactly(data);
                    var rowPitch = (int)sourceRowPitch;
                    if (header.UncompressedLayout is { } layout)
                    {
                        data = layout.ConvertToRgba8(data, width, height, rowPitch);
                        rowPitch = checked(width * 4);
                    }

                    mipLevels[level] = new DdsMipLevel(width, height, rowPitch, data);
                    width = Math.Max(1, width / 2);
                    height = Math.Max(1, height / 2);
                }

                return new DdsImage(header.Width, header.Height, header.Format, Array.AsReadOnly(mipLevels));
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException($"DDS file '{path}' is truncated.", exception);
            }
            catch (OverflowException exception)
            {
                throw new InvalidDataException($"DDS file '{path}' has dimensions or data sizes that exceed supported limits.", exception);
            }
            catch (InvalidDataException exception)
            {
                throw new InvalidDataException($"Invalid DDS file '{path}': {exception.Message}", exception);
            }
        }

        private static bool SelectRowAlignment(DdsHeader header, long payloadLength)
        {
            var packedLength = GetPayloadLength(header, false);
            var alignRows = false;
            var expectedLength = packedLength;

            // Some RGB writers pad each row to a DWORD; the complete mip chain disambiguates 24-bit rows.
            if (header.UncompressedLayout?.BytesPerPixel == 3 && payloadLength != packedLength)
            {
                var alignedLength = GetPayloadLength(header, true);
                if (payloadLength == alignedLength)
                {
                    alignRows = true;
                    expectedLength = alignedLength;
                }
            }

            if (payloadLength < expectedLength)
            {
                throw new InvalidDataException($"DDS mip data is truncated: expected {expectedLength} bytes, found {payloadLength}.");
            }
            if (payloadLength != expectedLength)
            {
                throw new InvalidDataException($"DDS has {payloadLength - expectedLength} unexpected bytes after its mip chain or unsupported row padding.");
            }

            var (topRowPitch, _) = GetMipLayout(header, header.Width, header.Height, alignRows);
            if (header.DeclaredRowPitch is { } declaredPitch && declaredPitch != topRowPitch)
            {
                throw new InvalidDataException($"DDS row pitch {declaredPitch} does not match the supported row pitch {topRowPitch}.");
            }

            return alignRows;
        }

        private static long GetPayloadLength(DdsHeader header, bool alignRows)
        {
            var width = header.Width;
            var height = header.Height;
            long totalLength = 0;
            for (var level = 0; level < header.MipCount; level++)
            {
                var (_, length) = GetMipLayout(header, width, height, alignRows);
                totalLength = checked(totalLength + length);
                width = Math.Max(1, width / 2);
                height = Math.Max(1, height / 2);
            }

            return totalLength;
        }

        private static (long RowPitch, long Length) GetMipLayout(DdsHeader header, int width, int height, bool alignRows)
        {
            long rowPitch;
            long rowCount;
            if (header.UncompressedLayout is { } layout)
            {
                rowPitch = (long)width * layout.BytesPerPixel;
                if (alignRows)
                {
                    rowPitch = (rowPitch + 3) & ~3L;
                }
                rowCount = height;
                if (checked((long)width * height * 4) > Array.MaxLength)
                {
                    throw new InvalidDataException("DDS RGBA8 mip data exceeds the supported array size.");
                }
            }
            else
            {
                var blockSize = header.Format == DdsPixelFormat.Bc1 ? 8 : 16;
                rowPitch = Math.Max(1L, ((long)width + 3) / 4) * blockSize;
                rowCount = Math.Max(1L, ((long)height + 3) / 4);
            }

            var length = checked(rowPitch * rowCount);
            if (rowPitch > int.MaxValue || length > Array.MaxLength)
            {
                throw new InvalidDataException("DDS mip data exceeds the supported row pitch or array size.");
            }

            return (rowPitch, length);
        }
    }
}
