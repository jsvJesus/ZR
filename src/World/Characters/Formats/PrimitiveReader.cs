using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace ZoneReborn.World.Characters.Formats;

public readonly record struct SourceVertex(Vector3 Position, Vector3 Normal, Vector2 Uv, int Bone0, int Bone1, int Bone2, Vector3 Weights);
public readonly record struct PrimitiveGroup(int StartIndex, int TriangleCount, int StartVertex, int VertexCount);

public static class PrimitiveReader
{
    public static Dictionary<string, byte[]> Read(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length < 12 || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != 0x42a14e65)
            throw new InvalidDataException("Unsupported primitives container.");
        int size = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(bytes.Length - 4));
        int directory = bytes.Length - 4 - size;
        if (size < 24 || directory < 4) throw new InvalidDataException("Invalid primitives directory.");
        using var reader = new SourceBinary(bytes);
        var sections = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        int dataOffset = 4;
        reader.BaseStream.Position = directory;
        while (reader.BaseStream.Position < bytes.Length - 4)
        {
            if (reader.BaseStream.Position + 24 > bytes.Length - 4) throw new InvalidDataException("Truncated primitives entry.");
            int length = reader.ReadInt32() & int.MaxValue;
            reader.BaseStream.Position += 16;
            string name = reader.SourceString();
            reader.BaseStream.Position = (reader.BaseStream.Position + 3) & ~3L;
            if (length < 0 || length > directory - dataOffset || !sections.TryAdd(name, bytes.AsSpan(dataOffset, length).ToArray()))
                throw new InvalidDataException("Invalid primitives section.");
            dataOffset = checked(dataOffset + ((length + 3) & ~3));
        }
        if (reader.BaseStream.Position != bytes.Length - 4 || dataOffset != directory)
            throw new InvalidDataException("Primitives sections overlap directory.");
        return sections;
    }

    public static SourceVertex[] Vertices(byte[] bytes)
    {
        if (bytes.Length < 68) throw new InvalidDataException("Truncated vertex header.");
        using var reader = new SourceBinary(bytes);
        string format = Encoding.ASCII.GetString(reader.ReadBytes(64)).Split('\0')[0];
        int stride = format switch { "xyznuviiiwwtb" => 37, "xyznuviiiww" => 29,
            _ => throw new InvalidDataException($"Unsupported source vertex format: {format}") };
        int count = reader.Count(stride);
        var vertices = new SourceVertex[count];
        for (int i = 0; i < count; i++)
        {
            var position = reader.Vector();
            var normal = UnpackNormal(reader.ReadUInt32());
            var uv = new Vector2(reader.Scalar(), reader.Scalar());
            int a = reader.ReadByte(), b = reader.ReadByte(), c = reader.ReadByte();
            int w0 = reader.ReadByte(), w1 = reader.ReadByte();
            if (a % 3 != 0 || b % 3 != 0 || c % 3 != 0 || w0 + w1 > 255)
                throw new InvalidDataException("Invalid bone indices or weights.");
            vertices[i] = new(position, normal, uv, a / 3, b / 3, c / 3, new Vector3(w0, w1, 255 - w0 - w1) / 255);
            if (stride == 37) reader.BaseStream.Position += 8;
        }
        // .mvertices appends morph targets after the base vertex stream. Default face uses the base shape.
        return vertices;
    }

    public static (int[] Indices, PrimitiveGroup[] Groups) Indices(byte[] bytes)
    {
        if (bytes.Length < 72) throw new InvalidDataException("Truncated index header.");
        using var reader = new SourceBinary(bytes);
        string format = Encoding.ASCII.GetString(reader.ReadBytes(64)).Split('\0')[0];
        int stride = format switch { "list" => 2, "list32" => 4, _ => throw new InvalidDataException("Unsupported index format.") };
        int count = reader.Count(stride);
        int groups = reader.ReadInt32();
        if (groups < 1 || groups > 65536 || (long)count * stride + (long)groups * 16 != bytes.Length - 72)
            throw new InvalidDataException("Invalid primitive groups.");
        var indices = new int[count];
        for (int i = 0; i < count; i++) indices[i] = stride == 2 ? reader.ReadUInt16() : reader.ReadInt32();
        var result = new PrimitiveGroup[groups];
        for (int i = 0; i < groups; i++)
        {
            var g = new PrimitiveGroup(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            if (g.StartIndex < 0 || g.TriangleCount < 0 || (long)g.StartIndex + (long)g.TriangleCount * 3 > count || g.StartVertex < 0 || g.VertexCount < 0)
                throw new InvalidDataException("Invalid primitive range.");
            result[i] = g;
        }
        return (indices, result);
    }

    private static Vector3 UnpackNormal(uint packed)
    {
        int value = unchecked((int)packed);
        return Vector3.Normalize(new Vector3((value << 21) >> 21, (value << 10) >> 21, value >> 22) / new Vector3(1023, 1023, 511));
    }
}
