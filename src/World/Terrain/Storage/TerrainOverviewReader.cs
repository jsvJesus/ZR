using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain.Storage;

public static class TerrainOverviewReader
{
    public static TerrainOverviewData Read(string path)
    {
        using var reader = new BinaryReader(File.OpenRead(path));
        if (!reader.ReadBytes(8).AsSpan().SequenceEqual("ZROVER01"u8))
            throw new InvalidDataException("Неподдерживаемая версия обзорной геометрии Terrain.");
        int width = reader.ReadInt32(), height = reader.ReadInt32(), step = reader.ReadInt32();
        if (width is < 2 or > 1025 || height is < 2 or > 1025 || step != 32 ||
            reader.BaseStream.Length != 20L + width * (long)height * 2)
            throw new InvalidDataException("Некорректные размеры обзорной геометрии Terrain.");
        var heights = new ushort[width * height];
        for (int index = 0; index < heights.Length; index++) heights[index] = reader.ReadUInt16();
        return new(width, height, step, heights);
    }
}

