using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene;

/// <summary>Reads authored, cooked collision geometry independently of the visual SCO/SCB mesh.</summary>
public static class SourcePhysicsReader
{
    private const int MaximumVertices = 2_000_000;
    private const int MaximumTriangles = 4_000_000;
    private const uint Materials = 1, FaceRemap = 2, Indices8 = 4, Indices16 = 8, Adjacencies = 16;

    public static ScenePhysicsMesh? Read(AssetPaths assets, string model,
        CancellationToken cancellationToken = default, Action<string>? warning = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string path = SourceModelReader.Resolve(assets, model);
        if (Path.GetExtension(path).Equals(".model", StringComparison.OrdinalIgnoreCase))
            return BigWorldModelReader.ReadPhysics(assets, model, cancellationToken, warning);
        string configuration = Path.ChangeExtension(path, ".phx");
        string? configuredType = File.Exists(configuration) ? ReadConfiguration(configuration, warning) : null;
        cancellationToken.ThrowIfCancellationRequested();

        // PHX type numbers are engine-specific, not PhysX geometry enum values. For the
        // current static scene queries, prefer the authored triangle proxy when present.
        // Some type=4 assets have only MPX, while the dynamic bandana has only CPX.
        string triangles = Path.ChangeExtension(path, ".mpx");
        if (File.Exists(triangles))
        {
            if (configuredType != null && configuredType != "3")
                warning?.Invoke($"{configuration}: PHX type={configuredType} uses the available MPX proxy for static collision queries.");
            return ReadTriangles(triangles, cancellationToken);
        }
        string convex = Path.ChangeExtension(path, ".cpx");
        if (File.Exists(convex)) return ReadConvex(convex, cancellationToken);
        throw new FileNotFoundException("No authored MPX or CPX collision geometry exists for this model.", path);
    }

    private static string? ReadConfiguration(string path, Action<string>? warning)
    {
        using var xml = XmlReader.Create(path, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1024 * 1024
        });
        XElement physics = XDocument.Load(xml).Root ?? throw new InvalidDataException("Missing PHX root.");
        if (physics.Name != "physics") throw new InvalidDataException("Invalid PHX root.");
        string? type = (string?)physics.Attribute("type");
        if (type != null && (!uint.TryParse(type, NumberStyles.None, CultureInfo.InvariantCulture, out _)))
            throw new InvalidDataException("Invalid PHX type.");
        string? group = (string?)physics.Attribute("collision_group");
        if (group != null && !uint.TryParse(group, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            throw new InvalidDataException("Invalid PHX collision group.");
        if (ReadFlag("dynamic"))
            warning?.Invoke($"{path}: dynamic PHX object uses its authored proxy as static collision; rigid-body simulation is unavailable.");
        if (ReadFlag("fastMoving") || ReadFlag("can_modify_contacts") || ReadFlag("isVehicle"))
            warning?.Invoke($"{path}: PHX simulation flags are unavailable to static collision queries.");
        // explMesh and isMovableBuilding_TEST describe source-engine behavior; neither
        // replaces the serialized collision surface or changes its local coordinates.
        return type;

        bool ReadFlag(string name)
        {
            string? value = (string?)physics.Attribute(name);
            return value?.ToLowerInvariant() switch
            {
                null or "false" or "0" => false,
                "true" or "1" => true,
                _ => throw new InvalidDataException($"Invalid PHX {name} flag.")
            };
        }
    }

    private static ScenePhysicsMesh ReadTriangles(string path, CancellationToken cancellationToken)
    {
        using var input = new CookedReader(path);
        uint version = input.Header("NXS", "MESH");
        if (version is not (7 or 9)) throw new InvalidDataException($"Unsupported MPX mesh version {version}.");
        uint flags = input.UInt32();
        if ((flags & ~(Materials | FaceRemap | Indices8 | Indices16 | Adjacencies)) != 0
            || (flags & (Indices8 | Indices16)) == (Indices8 | Indices16))
            throw new InvalidDataException($"Unsupported MPX serialization flags {flags}.");
        // PhysX GuMeshFactory/GuMeshData: versions before 13 retain this float;
        // before version 14 there is no midphase ID preceding the flags.
        float convexEdgeThreshold = input.Scalar();
        if (convexEdgeThreshold < 0) throw new InvalidDataException("Invalid MPX convex edge threshold.");
        int vertexCount = Count(input.UInt32(), MaximumVertices, "MPX vertex");
        int triangleCount = Count(input.UInt32(), MaximumTriangles, "MPX triangle");
        int width = (flags & Indices8) != 0 ? 1 : (flags & Indices16) != 0 ? 2 : 4;
        input.Require(vertexCount * 12L + triangleCount * 3L * width);
        Vector3[] vertices = ReadVertices(input, vertexCount, cancellationToken, out var bounds);
        var indices = new int[triangleCount * 3];
        for (int index = 0; index < indices.Length; index++)
        {
            if ((index & 4095) == 0) cancellationToken.ThrowIfCancellationRequested();
            uint value = width == 1 ? input.Byte() : width == 2 ? input.UInt16() : input.UInt32();
            if (value >= vertexCount) throw new InvalidDataException("MPX triangle index is out of range.");
            indices[index] = (int)value;
        }
        // The remaining cooked stream contains remapping and acceleration data, not
        // additional surfaces. SceneTriangleTree builds its own shared acceleration.
        return new(path, vertices, indices, bounds);
    }

    private static ScenePhysicsMesh ReadConvex(string path, CancellationToken cancellationToken)
    {
        using var input = new CookedReader(path);
        uint version = input.Header("NXS", "CVXM");
        if (version != 13) throw new InvalidDataException($"Unsupported CPX mesh version {version}.");
        if (input.UInt32() != 0) throw new InvalidDataException("Unsupported CPX serialization flags.");
        uint collisionVersion = input.Header("ICE", "CLHL");
        uint hullVersion = input.Header("ICE", "CVHL");
        if (collisionVersion != hullVersion || hullVersion is not (6 or 7))
            throw new InvalidDataException($"Unsupported CPX hull versions {collisionVersion}/{hullVersion}.");

        // NVIDIA GuConvexMesh::convexHullLoad and GuConvexMeshData::HullPolygonData:
        // four u32 counts, float3 vertices, optional v6 u16, 20-byte polygons,
        // then the byte-sized polygon vertex references.
        int vertexCount = Count(input.UInt32(), 255, "CPX vertex");
        uint edgeCount = input.UInt32();
        int polygonCount = Count(input.UInt32(), 255, "CPX polygon");
        int referenceCount = Count(input.UInt32(), ushort.MaxValue, "CPX vertex reference");
        if (edgeCount > ushort.MaxValue) throw new InvalidDataException("Invalid CPX edge count.");
        input.Require(vertexCount * 12L + (hullVersion <= 6 ? 2 : 0) + polygonCount * 20L + referenceCount);
        Vector3[] vertices = ReadVertices(input, vertexCount, cancellationToken, out var bounds);
        if (hullVersion <= 6) input.UInt16(); // obsolete useUnquantizedNormals field
        var polygons = new HullPolygon[polygonCount];
        int indexCount = 0;
        for (int index = 0; index < polygonCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var normal = new Vector3(input.Scalar(), input.Scalar(), input.Scalar());
            input.Scalar(); // plane distance
            int offset = input.UInt16(), count = input.Byte(), minimumIndex = input.Byte();
            if (count < 3 || offset + count > referenceCount || minimumIndex >= vertexCount || normal.LengthSquared() == 0)
                throw new InvalidDataException("Invalid CPX hull polygon.");
            polygons[index] = new(normal, offset, count);
            indexCount = checked(indexCount + (count - 2) * 3);
        }
        byte[] references = input.Bytes(referenceCount);
        foreach (byte reference in references)
            if (reference >= vertexCount) throw new InvalidDataException("CPX hull vertex reference is out of range.");
        var indices = new int[indexCount];
        int destination = 0;
        foreach (var polygon in polygons)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int a = references[polygon.Offset];
            for (int index = 1; index < polygon.Count - 1; index++)
            {
                int b = references[polygon.Offset + index], c = references[polygon.Offset + index + 1];
                if (Vector3.Dot(Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]), polygon.Normal) < 0)
                    (b, c) = (c, b);
                indices[destination++] = a; indices[destination++] = b; indices[destination++] = c;
            }
        }
        return new(path, vertices, indices, bounds);
    }

    private static Vector3[] ReadVertices(CookedReader input, int count, CancellationToken cancellationToken,
        out SceneBounds bounds)
    {
        var vertices = new Vector3[count];
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        for (int index = 0; index < count; index++)
        {
            if ((index & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            // Cooked vertices already use model-local coordinates. SCO CentralPoint
            // has already been removed during cooking and must not be applied again.
            Vector3 point = new(input.Scalar(), input.Scalar(), input.Scalar());
            vertices[index] = point;
            minimum = Vector3.Min(minimum, point); maximum = Vector3.Max(maximum, point);
        }
        bounds = new(minimum, maximum);
        return vertices;
    }

    private static int Count(uint value, int maximum, string label) => value is > 0 && value <= maximum
        ? (int)value : throw new InvalidDataException($"Invalid {label} count {value}.");

    private readonly record struct HullPolygon(Vector3 Normal, int Offset, int Count);

    private sealed class CookedReader : IDisposable
    {
        private readonly BinaryReader reader;
        private bool reverse;

        public CookedReader(string path) => reader = new BinaryReader(File.OpenRead(path), Encoding.ASCII);

        public uint Header(string signature, string chunk)
        {
            byte[] header = Bytes(8);
            if (header[0] != signature[0] || header[1] != signature[1] || header[2] != signature[2]
                || header[3] > 1 || Encoding.ASCII.GetString(header, 4, 4) != chunk)
                throw new InvalidDataException($"Invalid {signature} {chunk} cooked physics header.");
            reverse = header[3] == 0; // BinaryReader is little endian; the header bit describes file endianness.
            return UInt32();
        }

        public byte Byte() => reader.ReadByte();
        public ushort UInt16()
        {
            ushort value = reader.ReadUInt16();
            return reverse ? BinaryPrimitives.ReverseEndianness(value) : value;
        }
        public uint UInt32()
        {
            uint value = reader.ReadUInt32();
            return reverse ? BinaryPrimitives.ReverseEndianness(value) : value;
        }
        public float Scalar()
        {
            float value = BitConverter.Int32BitsToSingle(unchecked((int)UInt32()));
            return float.IsFinite(value) ? value : throw new InvalidDataException("Non-finite cooked physics coordinate.");
        }
        public byte[] Bytes(int count)
        {
            byte[] bytes = reader.ReadBytes(count);
            return bytes.Length == count ? bytes : throw new EndOfStreamException("Truncated cooked physics data.");
        }
        public void Require(long count)
        {
            if (reader.BaseStream.Length - reader.BaseStream.Position < count)
                throw new EndOfStreamException("Truncated cooked physics geometry.");
        }
        public void Dispose() => reader.Dispose();
    }
}
