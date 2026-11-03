using System.Globalization;
using System.Numerics;
using System.Text;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene;

public static class SourceModelReader
{
    private const int MaximumVertices = 2_000_000;
    private const int MaximumFaces = 4_000_000;

    public static SceneModel Read(AssetPaths assets, string file, CancellationToken cancellationToken = default) =>
        Read(assets, file, null, null, cancellationToken);

    internal static SceneModel Read(AssetPaths assets, string file, SourceMaterialReader? materials,
        Action<string>? warning, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string path = Resolve(assets, file);
        if (Path.GetExtension(path).Equals(".model", StringComparison.OrdinalIgnoreCase))
            return BigWorldModelReader.Read(assets, file, cancellationToken, warning);
        using var stream = File.OpenRead(path);
        Span<byte> signature = stackalloc byte[4];
        stream.ReadExactly(signature);
        stream.Position = 0;
        if (BitConverter.ToUInt32(signature) == 0xFADC0038)
            return ReadBinaryModel(assets, file, path, stream, materials, warning, cancellationToken);
        using var reader = new StreamReader(stream);
        var header = ReadHeader(reader);
        var positions = new Vector3[header.Count];
        var normals = new Vector3[header.Count];
        var tangents = new Vector4[header.Count];
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        for (int i = 0; i < header.Count; i++)
        {
            if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            var tokens = new Tokens(RequiredLine(reader));
            positions[i] = Offset(tokens.Vector3(), header.Center);
            normals[i] = tokens.Vector3();
            var tangent = tokens.Vector3();
            tangents[i] = new(tangent, tokens.Float());
            minimum = Vector3.Min(minimum, positions[i]);
            maximum = Vector3.Max(maximum, positions[i]);
        }
        int faceCount = Count(RequiredLine(reader), "Faces", MaximumFaces);
        var vertices = new List<SceneVertex>();
        var seams = new Dictionary<(int Source, Vector2 Uv), int>();
        var groups = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        for (int face = 0; face < faceCount; face++)
        {
            if ((face & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            var tokens = new Tokens(RequiredLine(reader));
            if (tokens.Int() != 3) throw new InvalidDataException("SCO contains a non-triangle face.");
            int a = tokens.Int(), b = tokens.Int(), c = tokens.Int();
            string material = tokens.Next().ToString();
            if (!groups.TryGetValue(material, out var indices)) groups.Add(material, indices = []);
            Add(a, new(tokens.Float(), tokens.Float()), indices);
            Add(b, new(tokens.Float(), tokens.Float()), indices);
            Add(c, new(tokens.Float(), tokens.Float()), indices);
        }
        if (RequiredLine(reader) != "[ObjectEnd]") throw new InvalidDataException("Missing SCO object terminator.");
        materials ??= new SourceMaterialReader(assets, null, warning);
        // Every material group references the same seam-corrected vertex array.
        SceneVertex[] sharedVertices = vertices.ToArray();
        var meshes = groups.Select(group => new SceneMesh(sharedVertices, group.Value.ToArray(),
            materials.Read(path, group.Key))).ToArray();
        return new(Normalize(file), new(minimum, maximum), meshes);

        void Add(int source, Vector2 uv, List<int> indices)
        {
            if ((uint)source >= (uint)positions.Length) throw new InvalidDataException("SCO vertex index is out of range.");
            if (!seams.TryGetValue((source, uv), out int index))
            {
                index = vertices.Count;
                if (index >= MaximumFaces * 3) throw new InvalidDataException("Too many SCO seam vertices.");
                seams.Add((source, uv), index);
                vertices.Add(new(positions[source], normals[source], uv, tangents[source]));
            }
            indices.Add(index);
        }
    }

    public static SceneBounds ReadBounds(AssetPaths assets, string file, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string path = Resolve(assets, file);
        if (Path.GetExtension(path).Equals(".model", StringComparison.OrdinalIgnoreCase))
            return BigWorldModelReader.ReadBounds(assets, file, cancellationToken);
        if (!File.Exists(path)) throw new FileNotFoundException("SCO model was not found.", path);
        string binary = Path.ChangeExtension(path, ".scb");
        if (File.Exists(binary))
        {
            try { return ReadBinaryBounds(binary, cancellationToken); }
            catch (Exception error) when (error is IOException or InvalidDataException or OverflowException)
            {
                // Only the verified SCB position layout is supported. Other versions use SCO.
            }
        }
        using var stream = File.OpenRead(path);
        Span<byte> signature = stackalloc byte[4];
        stream.ReadExactly(signature);
        if (BitConverter.ToUInt32(signature) == 0xFADC0038) return ReadBinaryBounds(path, cancellationToken);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var header = ReadHeader(reader);
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        for (int i = 0; i < header.Count; i++)
        {
            if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            var tokens = new Tokens(RequiredLine(reader));
            Vector3 point = Offset(tokens.Vector3(), header.Center);
            minimum = Vector3.Min(minimum, point);
            maximum = Vector3.Max(maximum, point);
        }
        return new(minimum, maximum);
    }

    private static SceneBounds ReadBinaryBounds(string path, CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        var header = ReadBinaryHeader(reader);
        var center = header.Center;
        int count = header.Count;
        if (stream.Length - stream.Position < count * 12L)
            throw new InvalidDataException("Invalid SCB position count.");
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        for (int i = 0; i < count; i++)
        {
            if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            var point = Offset(new Vector3(Scalar(reader), Scalar(reader), Scalar(reader)), center);
            minimum = Vector3.Min(minimum, point);
            maximum = Vector3.Max(maximum, point);
        }
        return new(minimum, maximum);
    }

    private static (Vector3 Center, int Count) ReadBinaryHeader(BinaryReader reader)
    {
        if (reader.ReadUInt32() != 0xFADC0038 || reader.ReadUInt32() != 0)
            throw new InvalidDataException("Unsupported SCB header.");
        int nameLength = reader.ReadInt32();
        if (nameLength is < 1 or > 4096) throw new InvalidDataException("Invalid SCB name length.");
        if (reader.ReadBytes(nameLength).Length != nameLength) throw new EndOfStreamException();
        var center = new Vector3(Scalar(reader), Scalar(reader), Scalar(reader));
        int count = reader.ReadInt32();
        if (count is < 1 or > MaximumVertices)
            throw new InvalidDataException("Invalid SCB position count.");
        return (center, count);
    }

    private static SceneModel ReadBinaryModel(AssetPaths assets, string file, string path, Stream stream,
        SourceMaterialReader? materials, Action<string>? warning, CancellationToken cancellationToken)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        var header = ReadBinaryHeader(reader);
        int count = header.Count;
        // V0 layout was matched against all vertices/faces of paired textual models:
        // float3 positions, float2 UVs, float3 normals, float3 tangents, byte signs,
        // int indexCount, int indices[], int materialCount, (start,end,nameLength,name) groups.
        if (stream.Length - stream.Position < count * 45L + 8)
            throw new InvalidDataException("Truncated SCB vertex sections.");
        var positions = new Vector3[count];
        var uv = new Vector2[count];
        var normals = new Vector3[count];
        var tangents = new Vector3[count];
        var vertices = new SceneVertex[count];
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        for (int i = 0; i < count; i++)
        {
            if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            positions[i] = Offset(new Vector3(Scalar(reader), Scalar(reader), Scalar(reader)), header.Center);
            minimum = Vector3.Min(minimum, positions[i]);
            maximum = Vector3.Max(maximum, positions[i]);
        }
        for (int i = 0; i < count; i++) { Cancel(i); uv[i] = new(Scalar(reader), Scalar(reader)); }
        for (int i = 0; i < count; i++) { Cancel(i); normals[i] = new(Scalar(reader), Scalar(reader), Scalar(reader)); }
        for (int i = 0; i < count; i++) { Cancel(i); tangents[i] = new(Scalar(reader), Scalar(reader), Scalar(reader)); }
        for (int i = 0; i < count; i++)
        {
            Cancel(i);
            byte sign = reader.ReadByte();
            if (sign is not (0 or 255)) throw new InvalidDataException("Unsupported SCB tangent sign.");
            vertices[i] = new(positions[i], normals[i], uv[i], new(tangents[i], sign == 255 ? 1 : -1));
        }
        int indexCount = reader.ReadInt32();
        if (indexCount is < 3 or > MaximumFaces * 3 || indexCount % 3 != 0 || stream.Length - stream.Position < indexCount * 4L)
            throw new InvalidDataException("Invalid SCB index count.");
        var indices = new int[indexCount];
        for (int i = 0; i < indexCount; i++)
        {
            Cancel(i);
            indices[i] = reader.ReadInt32();
            if ((uint)indices[i] >= (uint)count) throw new InvalidDataException("SCB index is out of range.");
        }
        int materialCount = reader.ReadInt32();
        if (materialCount is < 1 or > 4096) throw new InvalidDataException("Invalid SCB material count.");
        var meshes = new SceneMesh[materialCount];
        materials ??= new SourceMaterialReader(assets, null, warning);
        int previousEnd = 0;
        for (int i = 0; i < materialCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int start = reader.ReadInt32(), end = reader.ReadInt32(), nameLength = reader.ReadInt32();
            if (start != previousEnd || end <= start || end > indexCount || start % 3 != 0 || end % 3 != 0
                || nameLength is < 1 or > 4096) throw new InvalidDataException("Invalid SCB material range.");
            byte[] bytes = reader.ReadBytes(nameLength);
            if (bytes.Length != nameLength) throw new EndOfStreamException();
            string name = Encoding.ASCII.GetString(bytes);
            meshes[i] = new(vertices, indices[start..end], materials.Read(path, name));
            previousEnd = end;
        }
        if (previousEnd != indexCount || stream.Position != stream.Length)
            throw new InvalidDataException("Unsupported SCB trailing sections.");
        return new(Normalize(file), new(minimum, maximum), meshes);

        void Cancel(int index) { if ((index & 1023) == 0) cancellationToken.ThrowIfCancellationRequested(); }
    }

    internal static string Normalize(string file)
    {
        string normalized = file.Trim().Replace('\\', '/');
        if (normalized.StartsWith("packs/", StringComparison.OrdinalIgnoreCase)) normalized = normalized[6..];
        if (normalized.StartsWith("data/objectsdepot/", StringComparison.OrdinalIgnoreCase)) normalized = "models/" + normalized[18..];
        if (!normalized.StartsWith("models/", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("objects/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Expected a models/, objects/ or data/objectsdepot/ asset path.");
        if (normalized.Split('/').Any(part => part is ".." or "." || part.Length == 0) || normalized.Contains(':'))
            throw new InvalidDataException("Invalid scene model asset path.");
        return normalized;
    }

    internal static string Resolve(AssetPaths assets, string file)
    {
        string normalized = Normalize(file);
        bool source = normalized.StartsWith("models/", StringComparison.OrdinalIgnoreCase)
            && Path.GetExtension(normalized).Equals(".sco", StringComparison.OrdinalIgnoreCase);
        bool bigWorld = normalized.StartsWith("objects/", StringComparison.OrdinalIgnoreCase)
            && Path.GetExtension(normalized).Equals(".model", StringComparison.OrdinalIgnoreCase);
        if (!source && !bigWorld)
            throw new InvalidDataException("Expected a models/*.sco or objects/*.model scene asset.");
        return AssetPathResolver.Resolve(assets.Root, normalized);
    }

    private static (Vector3 Center, int Count) ReadHeader(StreamReader reader)
    {
        if (RequiredLine(reader) != "[ObjectBegin]" || !RequiredLine(reader).StartsWith("Name=", StringComparison.Ordinal))
            throw new InvalidDataException("Invalid SCO object header.");
        string line = RequiredLine(reader);
        int equals = line.IndexOf('=');
        if (equals < 0 || line[..equals].Trim() != "CentralPoint") throw new InvalidDataException("Missing SCO CentralPoint.");
        var tokens = new Tokens(line.AsSpan(equals + 1));
        var center = tokens.Vector3();
        return (center, Count(RequiredLine(reader), "Verts", MaximumVertices));
    }

    private static int Count(string line, string label, int maximum)
    {
        int equals = line.IndexOf('=');
        if (equals < 0 || line[..equals].Trim() != label || !int.TryParse(line.AsSpan(equals + 1),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) || count < 1 || count > maximum)
            throw new InvalidDataException($"Invalid SCO {label} count.");
        return count;
    }

    private static float Scalar(BinaryReader reader)
    {
        float number = reader.ReadSingle();
        return float.IsFinite(number) ? number : throw new InvalidDataException("Non-finite SCB coordinate.");
    }

    private static Vector3 Offset(Vector3 position, Vector3 center)
    {
        Vector3 value = position - center;
        return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z)
            ? value : throw new InvalidDataException("Non-finite model position relative to CentralPoint.");
    }

    private static string RequiredLine(StreamReader reader)
    {
        string? line;
        do { line = reader.ReadLine(); } while (line != null && string.IsNullOrWhiteSpace(line));
        if (line == null) throw new EndOfStreamException("Unexpected end of SCO file.");
        if (line.Length > 65536) throw new InvalidDataException("SCO line exceeds the supported length.");
        return line.Trim();
    }

    private ref struct Tokens(ReadOnlySpan<char> remaining)
    {
        private ReadOnlySpan<char> _remaining = remaining;
        public ReadOnlySpan<char> Next()
        {
            _remaining = _remaining.TrimStart();
            if (_remaining.IsEmpty) throw new InvalidDataException("Incomplete SCO record.");
            int length = 0;
            while (length < _remaining.Length && !char.IsWhiteSpace(_remaining[length])) length++;
            var value = _remaining[..length];
            _remaining = _remaining[length..];
            return value;
        }
        public int Int() => int.TryParse(Next(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value : throw new InvalidDataException("Invalid SCO integer.");
        public float Float() => float.TryParse(Next(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            && float.IsFinite(value) ? value : throw new InvalidDataException("Invalid SCO coordinate.");
        public Vector3 Vector3() => new(Float(), Float(), Float());
    }
}
