using System.Buffers.Binary;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain.Import.Binary;

public sealed class SourceTerrainReader
{
    private readonly string directory;
    private readonly SourceTerrainMetadata metadata;
    private readonly int layerCount;
    private readonly object cacheLock = new();
    private SourceTerrainChunkIndex? cachedChunk;

    public SourceTerrainReader(string terrainDirectory, TerrainDescriptor descriptor, int layerCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(terrainDirectory);
        ArgumentNullException.ThrowIfNull(descriptor);
        if (layerCount is < 1 or > 255)
            throw new ArgumentOutOfRangeException(nameof(layerCount), "Terrain supports a base layer and at most 254 painted layers.");
        directory = Path.GetFullPath(terrainDirectory);
        this.layerCount = layerCount;
        metadata = SourceTerrainMetadata.Read(directory, descriptor);
    }

    public TerrainTileData ReadTile(int x, int z)
    {
        if (x < 0 || x >= metadata.TilesX)
            throw new ArgumentOutOfRangeException(nameof(x));
        if (z < 0 || z >= metadata.TilesZ)
            throw new ArgumentOutOfRangeException(nameof(z));
        var chunkX = x / metadata.TilesPerChunk;
        var chunkZ = z / metadata.TilesPerChunk;
        var chunk = GetChunk(chunkX, chunkZ);
        var localTile = z % metadata.TilesPerChunk * metadata.TilesPerChunk + x % metadata.TilesPerChunk;
        var path = Path.Combine(directory, $"chunk_{chunkX}_{chunkZ}.bin");

        try
        {
            using var stream = File.OpenRead(path);
            var heightNormalOffset = (long)localTile * metadata.HeightNormalRecordBytes;
            EnsureRange(stream, heightNormalOffset, metadata.HeightNormalRecordBytes, "height and normal");
            stream.Position = heightNormalOffset;
            Span<byte> marker = stackalloc byte[2];
            stream.ReadExactly(marker);
            if (!marker.SequenceEqual("HN"u8))
                throw new InvalidDataException($"Terrain chunk '{path}' has an invalid height and normal marker for tile ({x}, {z}).");

            var heightBytes = new byte[metadata.HeightBytes];
            stream.ReadExactly(heightBytes);
            var heights = new ushort[metadata.TileSize * metadata.TileSize];
            for (var index = 0; index < heights.Length; index++)
                heights[index] = BinaryPrimitives.ReadUInt16LittleEndian(heightBytes.AsSpan(index * 2, 2));
            var normals = new byte[metadata.Bc1Bytes];
            stream.ReadExactly(normals);

            var masks = new List<TerrainMask>();
            foreach (var entry in chunk.Masks.Where(entry => entry.Key.X == x && entry.Key.Z == z).OrderBy(entry => entry.Key.Group))
            {
                EnsureRange(stream, entry.Value, 2L + metadata.Bc1Bytes, "mask");
                stream.Position = entry.Value;
                stream.ReadExactly(marker);
                if (BinaryPrimitives.ReadInt16LittleEndian(marker) != entry.Key.Group)
                    throw new InvalidDataException($"Terrain chunk '{path}' has an incorrect mask group tag for tile ({x}, {z}), group {entry.Key.Group}.");
                var data = new byte[metadata.Bc1Bytes];
                stream.ReadExactly(data);
                // Retain all BC1 blocks, including the four pixel sampling border.
                masks.Add(new TerrainMask(entry.Key.Group, data));
            }

            return new TerrainTileData(x, z, heights, normals, (byte[])chunk.ActiveLayers[localTile].Clone(), masks.AsReadOnly());
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException($"Terrain chunk '{path}' is truncated for tile ({x}, {z}).", exception);
        }
    }

    private SourceTerrainChunkIndex GetChunk(int x, int z)
    {
        lock (cacheLock)
        {
            if (cachedChunk is null || cachedChunk.X != x || cachedChunk.Z != z)
                cachedChunk = SourceTerrainChunkIndex.Read(directory, metadata, layerCount, x, z);
            return cachedChunk;
        }
    }

    private static void EnsureRange(Stream stream, long offset, long length, string kind)
    {
        if (offset < 0 || offset > stream.Length || length > stream.Length - offset)
            throw new InvalidDataException($"Terrain {kind} data at offset {offset} extends beyond its source chunk.");
    }
}
