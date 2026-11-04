using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace ZoneReborn.World.Scene;

// BigWorld BinSection directory and Moo VertexHeader / IndexHeader / PrimitiveGroup.
internal sealed class BigWorldPrimitives : IDisposable
{
    private readonly FileStream _stream;
    private readonly Dictionary<string, (int Offset, int Length)> _sections = new(StringComparer.Ordinal);
    public BigWorldPrimitives(string path)
    {
        _stream = File.OpenRead(path);
        try
        {
            if (_stream.Length is < 8 or > 512 * 1024 * 1024) throw new InvalidDataException("Invalid BigWorld primitives size.");
            using var reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
            if (reader.ReadUInt32() != 0x42A14E65) throw new InvalidDataException("Unsupported BigWorld primitives header.");
            _stream.Position = _stream.Length - 4;
            int directoryLength = reader.ReadInt32();
            if (directoryLength is < 0 or > 8 * 1024 * 1024 || directoryLength > _stream.Length - 8)
                throw new InvalidDataException("Invalid BigWorld primitives directory size.");
            long directoryStart = _stream.Length - 4 - directoryLength;
            _stream.Position = directoryStart;
            int offset = 4;
            while (_stream.Position < _stream.Length - 4)
            {
                if (_sections.Count >= 16384 || _stream.Length - 4 - _stream.Position < 24)
                    throw new InvalidDataException("Invalid BigWorld primitives directory.");
                int length = reader.ReadInt32();
                _stream.Position += 16; // preload length, version, modification timestamp
                int nameLength = reader.ReadInt32();
                if (length < 0 || length > directoryStart - offset || nameLength is < 1 or > 4096
                    || Align(nameLength) > _stream.Length - 4 - _stream.Position)
                    throw new InvalidDataException("Invalid BigWorld primitive section range.");
                string name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
                _stream.Position += Align(nameLength) - nameLength;
                if (!_sections.TryAdd(name, (offset, length))) throw new InvalidDataException("Duplicate BigWorld primitive section.");
                offset = checked(offset + Align(length));
            }
            if (_stream.Position != _stream.Length - 4 || offset != directoryStart)
                throw new InvalidDataException("Invalid BigWorld primitives directory boundary.");
        }
        catch { _stream.Dispose(); throw; }
    }
    public bool Contains(string name) => _sections.ContainsKey(name);
    public byte[] Read(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_sections.TryGetValue(name, out var section)) throw new InvalidDataException($"Missing BigWorld primitive section '{name}'.");
        byte[] bytes = new byte[section.Length];
        _stream.Position = section.Offset;
        // Bounded reads make cancellation responsive even for large source files.
        for (int offset = 0; offset < bytes.Length;)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int length = Math.Min(65536, bytes.Length - offset);
            _stream.ReadExactly(bytes.AsSpan(offset, length));
            offset += length;
        }
        return bytes;
    }
    public void Dispose() => _stream.Dispose();
    private static int Align(int size) => checked((size + 3) & ~3);

    internal readonly record struct Group(int StartIndex, int TriangleCount, int StartVertex, int VertexCount);
    public static (int[] Indices, Group[] Groups) ReadIndices(byte[] bytes, int vertexCount, CancellationToken cancellationToken)
    {
        Require(bytes, 72);
        string format = Format(bytes);
        int width = format switch { "list" => 2, "list32" => 4, _ => throw new InvalidDataException($"Unsupported BigWorld index format '{format}'.") };
        int count = Int(bytes, 64), groupCount = Int(bytes, 68);
        if (count is < 3 or > 12_000_000 || count % 3 != 0 || groupCount is < 1 or > 16384
            || 72L + count * (long)width + groupCount * 16L != bytes.Length)
            throw new InvalidDataException("Invalid BigWorld index/group counts.");
        var indices = new int[count];
        for (int i = 0; i < count; i++)
        {
            if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            int index = width == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(72 + i * 2)) : Int(bytes, 72 + i * 4);
            if ((uint)index >= (uint)vertexCount) throw new InvalidDataException("BigWorld vertex index is out of range.");
            indices[i] = index;
        }
        var groups = new Group[groupCount];
        for (int i = 0; i < groupCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int at = 72 + count * width + i * 16;
            int start = Int(bytes, at), triangles = Int(bytes, at + 4), vertexStart = Int(bytes, at + 8), vertices = Int(bytes, at + 12);
            if (start < 0 || triangles < 0 || start + triangles * 3L > count || vertexStart < 0 || vertices < 0
                || vertexStart + (long)vertices > vertexCount)
                throw new InvalidDataException("Invalid BigWorld primitive group range.");
            groups[i] = new(start, triangles, vertexStart, vertices);
        }
        return (indices, groups);
    }

    public static SceneVertex[] ReadVertices(byte[] bytes, Matrix4x4[] palette, bool skinned,
        bool morph, CancellationToken cancellationToken, Action<string>? warning)
    {
        Require(bytes, 68);
        string format = Format(bytes);
        int stride = format switch
        {
            "xyznuvtb" => 32, "xyznuviiiwwtb" => 37, "xyznuvi" => 36, "xyznuv" => 32,
            _ => throw new InvalidDataException($"Unsupported BigWorld vertex format '{format}'.")
        };
        int count = Int(bytes, 64);
        long expected = 68L + count * (long)stride;
        if (count is < 1 or > 2_000_000 || expected > bytes.Length || (!morph && expected != bytes.Length))
            throw new InvalidDataException("Invalid BigWorld vertex count or stride.");
        if (morph && expected != bytes.Length) warning?.Invoke("BigWorld morph targets are displayed in their authored base pose; animation is unavailable.");
        var result = new SceneVertex[count];
        for (int i = 0; i < count; i++)
        {
            if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            int at = 68 + i * stride;
            var position = Vector(bytes, at);
            bool packed = format is "xyznuvtb" or "xyznuviiiwwtb";
            var normal = packed ? PackedNormal(bytes, at + 12) : Vector(bytes, at + 12);
            int uvOffset = at + (packed ? 16 : 24);
            var uv = new Vector2(Scalar(bytes, uvOffset), Scalar(bytes, uvOffset + 4));
            int tangentOffset = at + (format == "xyznuviiiwwtb" ? 29 : 24);
            var tangent = packed ? PackedNormal(bytes, tangentOffset) : Orthogonal(normal);
            var binormal = packed ? PackedNormal(bytes, tangentOffset + 4) : Vector3.Cross(normal, tangent);
            Matrix4x4 transform = palette[0];
            if (skinned && format == "xyznuviiiwwtb")
            {
                int a = Bone(bytes[at + 24]), b = Bone(bytes[at + 25]), c = Bone(bytes[at + 26]);
                int weightA = bytes[at + 27], weightB = bytes[at + 28];
                if (weightA + weightB > 255) throw new InvalidDataException("Invalid BigWorld skin weights.");
                transform = palette[a] * (weightA / 255f) + palette[b] * (weightB / 255f)
                    + palette[c] * ((255 - weightA - weightB) / 255f);
            }
            else if (skinned && format == "xyznuvi")
            {
                float raw = Scalar(bytes, at + 32);
                if (raw < 0 || raw > 255 || raw != MathF.Truncate(raw)) throw new InvalidDataException("Invalid BigWorld rigid bone index.");
                transform = palette[Bone((int)raw)];
            }
            position = Vector3.Transform(position, transform);
            if (!Matrix4x4.Invert(transform, out var inverse)) throw new InvalidDataException("Singular BigWorld node transform.");
            normal = Unit(Vector3.TransformNormal(normal, Matrix4x4.Transpose(inverse)), Vector3.UnitY);
            tangent = Vector3.TransformNormal(tangent, transform);
            tangent = Unit(tangent - normal * Vector3.Dot(normal, tangent), Orthogonal(normal));
            binormal = Vector3.TransformNormal(binormal, transform);
            Finite(position); Finite(normal); Finite(tangent); Finite(binormal);
            float sign = Vector3.Dot(Vector3.Cross(normal, tangent), binormal) < 0 ? -1 : 1;
            result[i] = new(position, normal, uv, new(tangent, sign));
        }
        return result;

        int Bone(int raw)
        {
            if (raw % 3 != 0 || (uint)(raw / 3) >= (uint)palette.Length)
                throw new InvalidDataException("BigWorld bone index is outside the render-set palette.");
            return raw / 3;
        }
    }

    private static string Format(byte[] bytes)
    {
        int end = Array.IndexOf(bytes, (byte)0, 0, 64);
        if (end < 1) throw new InvalidDataException("Invalid BigWorld primitive format name.");
        return Encoding.ASCII.GetString(bytes, 0, end);
    }
    private static Vector3 PackedNormal(byte[] bytes, int offset)
    {
        uint word = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
        return new(((int)(word << 21) >> 21) / 1023f, ((int)(word << 10) >> 21) / 1023f, ((int)word >> 22) / 511f);
    }
    internal static int Int(byte[] bytes, int offset) => BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset));
    internal static float Scalar(byte[] bytes, int offset)
    {
        float value = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(offset));
        return float.IsFinite(value) ? value : throw new InvalidDataException("Non-finite BigWorld coordinate.");
    }
    internal static Vector3 Vector(byte[] bytes, int offset) => new(Scalar(bytes, offset), Scalar(bytes, offset + 4), Scalar(bytes, offset + 8));
    internal static void Finite(Vector3 value)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) || !float.IsFinite(value.Z))
            throw new InvalidDataException("Non-finite transformed BigWorld coordinate.");
    }
    internal static Vector3 Unit(Vector3 value, Vector3 fallback)
    {
        Finite(value);
        float length = value.Length();
        return float.IsFinite(length) && length > 1e-12f ? value / length : fallback;
    }
    private static Vector3 Orthogonal(Vector3 normal) => Unit(Vector3.Cross(normal,
        Math.Abs(normal.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitZ), Vector3.UnitX);
    internal static void Require(byte[] bytes, int size)
    {
        if (bytes.Length < size) throw new InvalidDataException("Truncated BigWorld primitive data.");
    }
}
