using System.Buffers.Binary;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain.Import.Binary;

internal sealed class SourceTerrainMetadata
{
    public int TileSize { get; private init; }
    public int TilesX { get; private init; }
    public int TilesZ { get; private init; }
    public int LodCount { get; private init; }
    public int TilesPerChunk { get; private init; }
    public int ChunksX => TilesX / TilesPerChunk;
    public int ChunksZ => TilesZ / TilesPerChunk;
    public int HeightBytes => checked(TileSize * TileSize * 2);
    public int Bc1Bytes => checked(TileSize / 4 * (TileSize / 4) * 8);
    public int HeightNormalRecordBytes => checked(2 + HeightBytes + Bc1Bytes);

    public static SourceTerrainMetadata Read(string directory, TerrainDescriptor descriptor)
    {
        var path = Path.Combine(directory, "megatexinfo.bin");
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length != 55 || !bytes.AsSpan(0, 15).SequenceEqual("MEGATEXINFO100\0"u8))
            throw new InvalidDataException($"Terrain metadata '{path}' has an invalid signature or size.");

        var values = new uint[10];
        for (var index = 0; index < values.Length; index++)
            values[index] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(15 + index * 4, 4));

        const uint bc1 = 0x31545844;
        const uint l16 = 81;
        if (values[0] != 256 || values[1] != 256 || values[2] != 256 || descriptor.TileSize != 256)
            throw new InvalidDataException("Terrain source requires 256 by 256 height, normal, and mask tiles.");
        if (values[5] != bc1 || values[6] != l16 || values[7] != bc1 || values[8] != bc1)
            throw new InvalidDataException("Terrain source requires L16 heights and BC1 normal, mask, and modulation formats.");
        if (!IsTileCount(values[3]) || !IsTileCount(values[4]))
            throw new InvalidDataException("Terrain source tile counts must be powers of two between 1 and 128.");
        if (descriptor.SamplesX != values[3] * 256 || descriptor.SamplesZ != values[4] * 256)
            throw new InvalidDataException("Terrain metadata tile counts do not match the configured sample dimensions.");

        var tilesX = (int)values[3];
        var tilesZ = (int)values[4];
        var maximumLods = System.Numerics.BitOperations.Log2((uint)Math.Min(tilesX, tilesZ)) + 1;
        if (values[9] == 0 || values[9] > maximumLods)
            throw new InvalidDataException("Terrain source has an invalid detail level count.");

        var tilesPerChunk = Math.Max(Math.Max(tilesX, tilesZ) / 8, 1);
        if (tilesX % tilesPerChunk != 0 || tilesZ % tilesPerChunk != 0)
            throw new InvalidDataException("Terrain source dimensions do not form a complete chunk grid.");

        return new SourceTerrainMetadata
        {
            TileSize = 256,
            TilesX = tilesX,
            TilesZ = tilesZ,
            LodCount = (int)values[9],
            TilesPerChunk = tilesPerChunk
        };
    }

    private static bool IsTileCount(uint value) => value is >= 1 and <= 128 && (value & (value - 1)) == 0;
}
