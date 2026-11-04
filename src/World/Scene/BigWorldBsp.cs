using System.Buffers.Binary;
using System.Numerics;

namespace ZoneReborn.World.Scene;

internal static class BigWorldBsp
{
    // BSP v0 stores 40-byte WorldTriangles: three float3 positions, uint16 flags,
    // uint16 padding. In bsp2 the flags are material-table indices, not collision bits.
    public static ScenePhysicsMesh? Read(string file, byte[] bytes, int[]? materialFlags, CancellationToken cancellationToken)
    {
        BigWorldPrimitives.Require(bytes, 16);
        if (BigWorldPrimitives.Int(bytes, 0) != 0x00505342)
            throw new InvalidDataException("Unsupported authored BigWorld BSP version; collision is unavailable.");
        int triangleCount = BigWorldPrimitives.Int(bytes, 4), maxNodeTriangles = BigWorldPrimitives.Int(bytes, 8),
            nodeCount = BigWorldPrimitives.Int(bytes, 12);
        if (triangleCount is < 0 or > 2_000_000 || maxNodeTriangles < 0 || maxNodeTriangles > triangleCount
            || nodeCount is < 0 or > 4_000_000 || 16L + triangleCount * 40L + nodeCount * 19L > bytes.Length)
            throw new InvalidDataException("Invalid BigWorld BSP counts.");
        int at = checked(16 + triangleCount * 40);
        // Validate the serialized tree as well as the triangle payload before accepting it.
        int pending = nodeCount == 0 ? 0 : 1;
        for (int i = 0; i < nodeCount; i++)
        {
            if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            if (pending-- <= 0 || at > bytes.Length - 19) throw new InvalidDataException("Truncated BigWorld BSP tree.");
            byte flags = bytes[at];
            if ((flags & 0xF8) != 0xA0) throw new InvalidDataException("Invalid BigWorld BSP node flags.");
            if ((flags & 2) != 0) pending++;
            if ((flags & 1) != 0) pending++;
            for (int n = 0; n < 4; n++) _ = BigWorldPrimitives.Scalar(bytes, at + 1 + n * 4);
            int references = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(at + 17));
            at += 19;
            if (references > maxNodeTriangles || references * 2L > bytes.Length - at)
                throw new InvalidDataException("Invalid BigWorld BSP triangle-reference count.");
            for (int n = 0; n < references; n++)
                if (BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(at + n * 2)) >= triangleCount)
                    throw new InvalidDataException("Invalid BigWorld BSP triangle reference.");
            at += references * 2;
        }
        if (pending != 0) throw new InvalidDataException("Incomplete BigWorld BSP tree.");
        while (at < bytes.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (at > bytes.Length - 8) throw new InvalidDataException("Truncated BigWorld BSP user data.");
            int length = BigWorldPrimitives.Int(bytes, at + 4);
            if (length < 0 || length > bytes.Length - at - 8) throw new InvalidDataException("Invalid BigWorld BSP user-data length.");
            at += 8 + length;
        }
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        for (int i = 0; i < triangleCount; i++)
        {
            if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
            at = 16 + i * 40;
            int flags = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(at + 36));
            if (materialFlags != null) flags = materialFlags[flags % materialFlags.Length];
            var a = BigWorldPrimitives.Vector(bytes, at);
            var b = BigWorldPrimitives.Vector(bytes, at + 12);
            var c = BigWorldPrimitives.Vector(bytes, at + 24);
            // Bit 4 excludes player collision; 0xff excludes all collision.
            if ((flags & 0x10) != 0 || (flags & 0xFF) == 0xFF) continue;
            if (vertices.Count > 2_000_000 - 3) throw new InvalidDataException("BigWorld collision geometry exceeds the vertex limit.");
            Add(a); Add(b); Add(c);
        }
        return indices.Count == 0 ? null : new(file, vertices.ToArray(), indices.ToArray(), new(minimum, maximum));
        void Add(Vector3 point)
        {
            indices.Add(vertices.Count);
            vertices.Add(point);
            minimum = Vector3.Min(minimum, point);
            maximum = Vector3.Max(maximum, point);
        }
    }
}
