using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain.Storage;

public static class TerrainTileReader
{
    public static TerrainTileData Read(string path)
    {
        using var reader = new BinaryReader(File.OpenRead(path));
        if (!reader.ReadBytes(8).AsSpan().SequenceEqual("ZRTERR01"u8))
            throw new InvalidDataException("Неподдерживаемая версия тайла Terrain.");
        int x = reader.ReadInt32(), z = reader.ReadInt32();
        if (x < 0 || z < 0 || x >= 128 || z >= 128)
            throw new InvalidDataException("Некорректные координаты тайла.");
        if (reader.ReadInt32() != 256 * 256)
            throw new InvalidDataException("Некорректный размер высот Terrain.");
        var heights = new ushort[256 * 256];
        for (int i = 0; i < heights.Length; i++) heights[i] = reader.ReadUInt16();
        byte[] normals = ReadBlock(reader, 32768, 32768);
        byte[] layers = ReadBlock(reader, 0, 254);
        int count = reader.ReadInt32();
        if (count < 0 || count > 85) throw new InvalidDataException("Некорректное количество масок.");
        var masks = new List<TerrainMask>(count);
        var groups = new HashSet<int>();
        for (int i = 0; i < count; i++)
        {
            int group = reader.ReadInt32();
            if (group < 0 || group >= 85 || !groups.Add(group))
                throw new InvalidDataException("Некорректная группа маски.");
            masks.Add(new TerrainMask(group, ReadBlock(reader, 32768, 32768)));
        }
        if (reader.BaseStream.Position != reader.BaseStream.Length)
            throw new InvalidDataException("Лишние данные тайла Terrain.");
        return new TerrainTileData(x, z, heights, normals, layers, masks);
    }

    private static byte[] ReadBlock(BinaryReader reader, int minimum, int maximum)
    {
        int length = reader.ReadInt32();
        if (length < minimum || length > maximum)
            throw new InvalidDataException("Некорректный размер блока Terrain.");
        byte[] data = reader.ReadBytes(length);
        if (data.Length != length) throw new EndOfStreamException("Неполный блок Terrain.");
        return data;
    }
}

