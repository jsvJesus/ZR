using System.Buffers.Binary;

namespace ZoneReborn.World.Terrain;

public sealed class TerrainHoles(int width, int height)
{
    public const int Detail = 4;

    private ushort[]? _masks;
    private int _count;

    public int Width { get; } = width;
    public int Height { get; } = height;
    public bool Any => _count > 0;

    public ushort Mask(int x, int z)
    {
        if (x < 0 || z < 0 || x >= Width || z >= Height)
        {
            return ushort.MaxValue;
        }

        return _masks == null ? (ushort)0 : _masks[z * Width + x];
    }

    public bool Contains(int x, int z)
    {
        return Mask(x, z) == ushort.MaxValue;
    }

    public bool ContainsWorld(
        float x,
        float z,
        float originX,
        float originZ,
        float cellSize)
    {
        if (!float.IsFinite(x) ||
            !float.IsFinite(z) ||
            !float.IsFinite(cellSize) ||
            cellSize <= 0)
        {
            return true;
        }

        float gx = (x - originX) / cellSize;
        float gz = (z - originZ) / cellSize;

        if (gx < 0 || gz < 0 || gx > Width || gz > Height)
        {
            return true;
        }

        int cellX = Math.Clamp(
            (int)MathF.Floor(gx), 0, Width - 1
        );

        int cellZ = Math.Clamp(
            (int)MathF.Floor(gz), 0, Height - 1
        );

        int subX = Math.Clamp(
            (int)MathF.Floor((gx - cellX) * Detail),
            0,
            Detail - 1
        );

        int subZ = Math.Clamp(
            (int)MathF.Floor((gz - cellZ) * Detail),
            0,
            Detail - 1
        );

        int bit = subZ * Detail + subX;

        return (Mask(cellX, cellZ) & (1 << bit)) != 0;
    }

    public bool Set(int x, int z, bool hole)
    {
        return SetMask(
            x,
            z,
            hole ? ushort.MaxValue : (ushort)0
        );
    }

    public bool SetMask(int x, int z, ushort mask)
    {
        if (x < 0 || z < 0 || x >= Width || z >= Height)
        {
            return false;
        }

        int index = z * Width + x;
        ushort previous = Mask(x, z);

        if (previous == mask)
        {
            return false;
        }

        _masks ??= new ushort[checked(Width * Height)];
        _masks[index] = mask;

        if (previous == 0 && mask != 0)
        {
            _count++;
        }
        else if (previous != 0 && mask == 0)
        {
            _count--;
        }

        return true;
    }

    public byte[] Pixels16()
    {
        var pixels = new byte[checked(Width * Height * 2)];

        if (_masks == null)
        {
            return pixels;
        }

        for (int index = 0; index < _masks.Length; index++)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(
                pixels.AsSpan(index * 2, 2),
                _masks[index]
            );
        }

        return pixels;
    }

    public byte[] Pixels()
    {
        var pixels = new byte[checked(Width * Height)];

        if (_masks == null)
        {
            return pixels;
        }

        for (int index = 0; index < _masks.Length; index++)
        {
            pixels[index] = _masks[index] != 0
                ? (byte)255
                : (byte)0;
        }

        return pixels;
    }

    public void Write(string path)
    {
        byte[] pixels = Pixels16();

        using var writer = new BinaryWriter(
            new FileStream(path, FileMode.CreateNew)
        );

        writer.Write("ZRHOLE02"u8);
        writer.Write(Width);
        writer.Write(Height);
        writer.Write(pixels.Length);
        writer.Write(pixels);
    }

    public static TerrainHoles Read(
        string path,
        int width,
        int height)
    {
        using var reader = new BinaryReader(File.OpenRead(path));

        byte[] signature = reader.ReadBytes(8);

        bool legacy = signature.AsSpan().SequenceEqual(
            "ZRHOLE01"u8
        );

        bool current = signature.AsSpan().SequenceEqual(
            "ZRHOLE02"u8
        );

        if ((!legacy && !current) ||
            reader.ReadInt32() != width ||
            reader.ReadInt32() != height)
        {
            throw new InvalidDataException(
                "Некорректная маска отверстий Terrain."
            );
        }

        int count = checked(width * height);

        int length = legacy
            ? checked((count + 7) / 8)
            : checked(count * 2);

        if (reader.ReadInt32() != length ||
            reader.BaseStream.Length -
            reader.BaseStream.Position != length)
        {
            throw new InvalidDataException(
                "Некорректный размер маски отверстий Terrain."
            );
        }

        byte[] pixels = reader.ReadBytes(length);
        var result = new TerrainHoles(width, height);

        ushort[] masks = new ushort[count];
        int used = 0;

        for (int index = 0; index < count; index++)
        {
            ushort mask;

            if (legacy)
            {
                bool hole = (
                    pixels[index >> 3] &
                    (1 << (index & 7))
                ) != 0;

                mask = hole ? ushort.MaxValue : (ushort)0;
            }
            else
            {
                mask = BinaryPrimitives.ReadUInt16LittleEndian(
                    pixels.AsSpan(index * 2, 2)
                );
            }

            masks[index] = mask;

            if (mask != 0)
            {
                used++;
            }
        }

        if (used > 0)
        {
            result._masks = masks;
        }

        result._count = used;

        return result;
    }
}
