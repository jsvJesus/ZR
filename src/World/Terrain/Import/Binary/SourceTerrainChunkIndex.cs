using System.Buffers.Binary;

namespace ZoneReborn.World.Terrain.Import.Binary;

internal sealed class SourceTerrainChunkIndex
{
    public required int X { get; init; }
    public required int Z { get; init; }
    public required byte[][] ActiveLayers { get; init; }
    public required Dictionary<(int X, int Z, int Group), long> Masks { get; init; }

    public static SourceTerrainChunkIndex Read(string directory, SourceTerrainMetadata metadata, int layerCount, int chunkX, int chunkZ)
    {
        var mapPath = Path.Combine(directory, $"megatexmap_{chunkX}_{chunkZ}.bin");
        var bytes = File.ReadAllBytes(mapPath);
        if (bytes.Length < 19 || !bytes.AsSpan(0, 15).SequenceEqual("MEGAMAPCHNK200\0"u8))
            throw new InvalidDataException($"Terrain map '{mapPath}' has an invalid signature or size.");
        var count = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(15, 4));
        if (19L + count * 16L != bytes.Length)
            throw new InvalidDataException($"Terrain map '{mapPath}' has an invalid record count.");

        var masks = new Dictionary<(int X, int Z, int Group), long>();
        var identifiers = new HashSet<ulong>();
        var groupCount = (layerCount - 1 + 2) / 3;
        for (var offset = 19; offset < bytes.Length; offset += 16)
        {
            var record = bytes.AsSpan(offset, 16);
            var identifier = BinaryPrimitives.ReadUInt64LittleEndian(record);
            var tileX = BinaryPrimitives.ReadUInt16LittleEndian(record);
            var tileZ = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(2));
            var lod = record[4];
            var group = record[5];
            var locator = BinaryPrimitives.ReadUInt64LittleEndian(record.Slice(8));
            var grid = (byte)(locator >> 56);
            var payloadOffset = (long)(locator & 0x00ffffffffffffffUL);
            if (BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(6)) != 0 || !identifiers.Add(identifier))
                throw new InvalidDataException($"Terrain map '{mapPath}' has reserved bits or duplicate tile identifiers.");
            if (lod >= metadata.LodCount || tileX >= metadata.TilesX >> lod || tileZ >= metadata.TilesZ >> lod || group >= groupCount)
                throw new InvalidDataException($"Terrain map '{mapPath}' has a tile, detail level, or mask group outside the configured bounds.");
            if (tileX / metadata.TilesPerChunk != chunkX || tileZ / metadata.TilesPerChunk != chunkZ)
                throw new InvalidDataException($"Terrain map '{mapPath}' contains a tile assigned to a different chunk.");
            if (grid != 255 && ((grid & 15) >= metadata.ChunksX || (grid >> 4) >= metadata.ChunksZ))
                throw new InvalidDataException($"Terrain map '{mapPath}' references a chunk outside the source grid.");
            if (lod != 0)
                continue;
            if (grid == 255 || (grid & 15) != chunkX || (grid >> 4) != chunkZ)
                throw new InvalidDataException($"Terrain map '{mapPath}' references an invalid source chunk for a full detail mask.");
            if (payloadOffset < (long)metadata.TilesPerChunk * metadata.TilesPerChunk * metadata.HeightNormalRecordBytes)
                throw new InvalidDataException($"Terrain map '{mapPath}' points a mask into height or normal data.");
            masks.Add((tileX, tileZ, group), payloadOffset);
        }

        var infoPath = Path.Combine(directory, $"tileinfos_{chunkX}_{chunkZ}.bin");
        bytes = File.ReadAllBytes(infoPath);
        if (bytes.Length < 22 || !bytes.AsSpan(0, 22).SequenceEqual("MEGATILEINFOS_CHNK100\0"u8))
            throw new InvalidDataException($"Terrain tile information '{infoPath}' has an invalid signature or size.");
        var position = 22;
        var layers = new byte[metadata.TilesPerChunk * metadata.TilesPerChunk][];
        for (var tile = 0; tile < layers.Length; tile++)
        {
            if (position >= bytes.Length)
                throw new InvalidDataException($"Terrain tile information '{infoPath}' is truncated.");
            var length = bytes[position++];
            if (position + length > bytes.Length)
                throw new InvalidDataException($"Terrain tile information '{infoPath}' has a truncated layer list.");
            var active = new byte[length];
            var seen = new HashSet<byte>();
            for (var index = 0; index < length; index++)
            {
                var nativeIndex = bytes[position++];
                if (nativeIndex >= layerCount - 1 || !seen.Add(nativeIndex))
                    throw new InvalidDataException($"Terrain tile information '{infoPath}' has an invalid or repeated layer index.");
                var tileX = chunkX * metadata.TilesPerChunk + tile % metadata.TilesPerChunk;
                var tileZ = chunkZ * metadata.TilesPerChunk + tile / metadata.TilesPerChunk;
                if (!masks.ContainsKey((tileX, tileZ, nativeIndex / 3)))
                    throw new InvalidDataException($"Terrain tile information '{infoPath}' references a missing mask for tile ({tileX}, {tileZ}).");
                active[index] = checked((byte)(nativeIndex + 1));
            }
            layers[tile] = active;
        }
        if (position != bytes.Length)
            throw new InvalidDataException($"Terrain tile information '{infoPath}' has unexpected trailing bytes.");

        return new SourceTerrainChunkIndex { X = chunkX, Z = chunkZ, Masks = masks, ActiveLayers = layers };
    }
}
