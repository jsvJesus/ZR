namespace ZoneReborn.World.Terrain;

/// <summary>One bit per heightfield cell: set means no terrain surface or collision.</summary>
public sealed class TerrainHoles(int width, int height)
{
    private byte[]? _bits;
    private int _count;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public bool Any => _count > 0;

    public bool Contains(int x, int z)
    {
        if (x < 0 || z < 0 || x >= Width || z >= Height) return true;
        int index = z * Width + x;
        return _bits != null && (_bits[index >> 3] & (1 << (index & 7))) != 0;
    }

    public bool Set(int x, int z, bool hole)
    {
        if (x < 0 || z < 0 || x >= Width || z >= Height) return false;
        if (Contains(x, z) == hole) return false;
        _bits ??= new byte[checked((int)(((long)Width * Height + 7) / 8))];
        int index = z * Width + x, mask = 1 << (index & 7);
        if (hole) { _bits[index >> 3] |= (byte)mask; _count++; }
        else { _bits[index >> 3] &= (byte)~mask; _count--; }
        return true;
    }

    public byte[] Pixels()
    {
        var pixels = new byte[checked(Width * Height)];
        if (_bits != null)
            for (int index = 0; index < pixels.Length; index++)
                if ((_bits[index >> 3] & (1 << (index & 7))) != 0) pixels[index] = 255;
        return pixels;
    }

    public void Write(string path)
    {
        using var writer = new BinaryWriter(new FileStream(path, FileMode.CreateNew));
        writer.Write("ZRHOLE01"u8); writer.Write(Width); writer.Write(Height);
        int length = checked((int)(((long)Width * Height + 7) / 8));
        writer.Write(length); writer.Write(_bits ?? new byte[length]);
    }

    public static TerrainHoles Read(string path, int width, int height)
    {
        using var reader = new BinaryReader(File.OpenRead(path));
        if (!reader.ReadBytes(8).AsSpan().SequenceEqual("ZRHOLE01"u8)
            || reader.ReadInt32() != width || reader.ReadInt32() != height)
            throw new InvalidDataException("Некорректная маска отверстий Terrain.");
        int length = checked((int)(((long)width * height + 7) / 8));
        if (reader.ReadInt32() != length || reader.BaseStream.Length - reader.BaseStream.Position != length)
            throw new InvalidDataException("Некорректный размер маски отверстий Terrain.");
        var result = new TerrainHoles(width, height) { _bits = reader.ReadBytes(length) };
        for (int index = 0; index < (long)width * height; index++)
            if ((result._bits[index >> 3] & (1 << (index & 7))) != 0) result._count++;
        return result;
    }
}
