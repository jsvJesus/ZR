using System.Numerics;

namespace ZoneReborn.World.Textures.Dds
{
    internal sealed record DdsUncompressedLayout(
        int BytesPerPixel,
        int RedIndex,
        int GreenIndex,
        int BlueIndex,
        int AlphaIndex)
    {
        public static DdsUncompressedLayout Create(
            uint bitCount,
            uint redMask,
            uint greenMask,
            uint blueMask,
            uint alphaMask)
        {
            if (bitCount is not (24 or 32))
            {
                throw new InvalidDataException($"Unsupported DDS RGB bit count {bitCount}; expected 24 or 32.");
            }

            var bytesPerPixel = (int)(bitCount / 8);
            var redIndex = GetByteIndex(redMask, bytesPerPixel);
            var greenIndex = GetByteIndex(greenMask, bytesPerPixel);
            var blueIndex = GetByteIndex(blueMask, bytesPerPixel);
            var alphaIndex = alphaMask == 0 ? -1 : GetByteIndex(alphaMask, bytesPerPixel);

            if ((redMask & greenMask) != 0 || (redMask & blueMask) != 0 ||
                (greenMask & blueMask) != 0 ||
                (alphaMask & (redMask | greenMask | blueMask)) != 0)
            {
                throw new InvalidDataException("DDS RGB channel masks overlap.");
            }

            return new DdsUncompressedLayout(bytesPerPixel, redIndex, greenIndex, blueIndex, alphaIndex);
        }

        public byte[] ConvertToRgba8(byte[] source, int width, int height, int sourceRowPitch)
        {
            var destination = new byte[checked(width * height * 4)];
            for (var y = 0; y < height; y++)
            {
                var sourceOffset = y * sourceRowPitch;
                var destinationOffset = y * width * 4;
                for (var x = 0; x < width; x++)
                {
                    destination[destinationOffset] = source[sourceOffset + RedIndex];
                    destination[destinationOffset + 1] = source[sourceOffset + GreenIndex];
                    destination[destinationOffset + 2] = source[sourceOffset + BlueIndex];
                    destination[destinationOffset + 3] = AlphaIndex < 0
                        ? (byte)255
                        : source[sourceOffset + AlphaIndex];
                    sourceOffset += BytesPerPixel;
                    destinationOffset += 4;
                }
            }

            return destination;
        }

        private static int GetByteIndex(uint mask, int bytesPerPixel)
        {
            var shift = BitOperations.TrailingZeroCount(mask);
            if (mask == 0 || shift % 8 != 0 || shift / 8 >= bytesPerPixel || (mask >> shift) != 0xff)
            {
                throw new InvalidDataException($"Unsupported DDS RGB channel mask 0x{mask:X8}; expected one complete byte.");
            }

            return shift / 8;
        }
    }
}
