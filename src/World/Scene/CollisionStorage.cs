using System.Numerics;
using System.Text;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene;

public static class CollisionStorage
{
    public const string Extension = ".zrcollision";

    private const uint Signature = 0x3143525A;
    private const int Version = 1;
    private const int MaximumVertices = 2_000_000;
    private const int MaximumIndices = 12_000_000;

    public static string GetPath(string modelPath)
    {
        return Path.ChangeExtension(
            modelPath,
            Extension
        );
    }

    public static bool IsCurrent(string modelPath)
    {
        string path = GetPath(modelPath);

        if (!File.Exists(path))
        {
            return false;
        }

        DateTime collisionTime = File.GetLastWriteTimeUtc(path);

        if (collisionTime < File.GetLastWriteTimeUtc(modelPath))
        {
            return false;
        }

        string settingsPath =
            ZoneReborn.World.Scene.Import
                .CollisionGenerationSettingsStorage.GetPath(modelPath);

        return !File.Exists(settingsPath) ||
               collisionTime >= File.GetLastWriteTimeUtc(settingsPath);
    }

    public static ScenePhysicsMesh Read(
        AssetPaths assets,
        string model,
        CancellationToken token = default)
    {
        string normalized = SourceModelReader.Normalize(model);
        string modelPath = SourceModelReader.Resolve(
            assets,
            normalized
        );

        string path = GetPath(modelPath);
        float scale = ModelScaleStorage.Read(modelPath);

        using var reader = new BinaryReader(
            File.OpenRead(path),
            Encoding.UTF8
        );

        if (reader.ReadUInt32() != Signature ||
            reader.ReadInt32() != Version)
        {
            throw new InvalidDataException(
                "Invalid collision file header."
            );
        }

        int vertexCount = reader.ReadInt32();
        int indexCount = reader.ReadInt32();

        if (vertexCount <= 0 ||
            vertexCount > MaximumVertices ||
            indexCount <= 0 ||
            indexCount > MaximumIndices ||
            indexCount % 3 != 0)
        {
            throw new InvalidDataException(
                "Invalid collision geometry size."
            );
        }

        long required =
            vertexCount * 12L +
            indexCount * 4L;

        if (reader.BaseStream.Length -
            reader.BaseStream.Position != required)
        {
            throw new InvalidDataException(
                "Invalid collision file length."
            );
        }

        var vertices = new Vector3[vertexCount];
        var indices = new int[indexCount];

        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);

        for (int index = 0; index < vertexCount; index++)
        {
            if ((index & 1023) == 0)
            {
                token.ThrowIfCancellationRequested();
            }

            var position = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            ) * scale;

            if (!float.IsFinite(position.X) ||
                !float.IsFinite(position.Y) ||
                !float.IsFinite(position.Z))
            {
                throw new InvalidDataException(
                    "Invalid collision vertex."
                );
            }

            vertices[index] = position;

            minimum = Vector3.Min(minimum, position);
            maximum = Vector3.Max(maximum, position);
        }

        for (int index = 0; index < indexCount; index++)
        {
            if ((index & 4095) == 0)
            {
                token.ThrowIfCancellationRequested();
            }

            int value = reader.ReadInt32();

            if ((uint)value >= (uint)vertexCount)
            {
                throw new InvalidDataException(
                    "Collision triangle index is out of range."
                );
            }

            indices[index] = value;
        }

        return new ScenePhysicsMesh(
            normalized,
            vertices,
            indices,
            new SceneBounds(minimum, maximum)
        )
        {
            IsVisualFallback = true
        };
    }

    public static void Write(
        string modelPath,
        ScenePhysicsMesh mesh,
        CancellationToken token = default)
    {
        string path = GetPath(modelPath);

        if (mesh.Vertices.Length <= 0 ||
            mesh.Vertices.Length > MaximumVertices ||
            mesh.Indices.Length <= 0 ||
            mesh.Indices.Length > MaximumIndices ||
            mesh.Indices.Length % 3 != 0)
        {
            throw new InvalidDataException(
                "Invalid generated collision geometry."
            );
        }

        Directory.CreateDirectory(
            Path.GetDirectoryName(path)!
        );

        string temporary =
            path + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            using (var writer = new BinaryWriter(
                new FileStream(
                    temporary,
                    FileMode.CreateNew
                ),
                Encoding.UTF8))
            {
                writer.Write(Signature);
                writer.Write(Version);

                writer.Write(mesh.Vertices.Length);
                writer.Write(mesh.Indices.Length);

                foreach (Vector3 vertex in mesh.Vertices)
                {
                    token.ThrowIfCancellationRequested();

                    if (!float.IsFinite(vertex.X) ||
                        !float.IsFinite(vertex.Y) ||
                        !float.IsFinite(vertex.Z))
                    {
                        throw new InvalidDataException(
                            "Non-finite collision vertex."
                        );
                    }

                    writer.Write(vertex.X);
                    writer.Write(vertex.Y);
                    writer.Write(vertex.Z);
                }

                foreach (int index in mesh.Indices)
                {
                    token.ThrowIfCancellationRequested();

                    if ((uint)index >=
                        (uint)mesh.Vertices.Length)
                    {
                        throw new InvalidDataException(
                            "Invalid collision index."
                        );
                    }

                    writer.Write(index);
                }
            }

            token.ThrowIfCancellationRequested();
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
