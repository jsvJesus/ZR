using System.Numerics;
using System.Text;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene;

public static class ImportedModelStorage
{
    public const string Extension = ".zrmodel";

    internal const int MaximumVertices = 2_000_000;
    internal const int MaximumIndices = 12_000_000;

    private const uint Signature = 0x314D525A;
    private const int Version = 2;
    private const int MaximumMeshes = 4096;

    internal static bool HasLegacyScale(string path)
    {
        using var reader = new BinaryReader(
            File.OpenRead(path)
        );

        return ReadVersion(reader) == 1;
    }

    public static SceneBounds ReadBounds(string path)
    {
        float scale = ModelScaleStorage.Read(path);

        using var reader = new BinaryReader(
            File.OpenRead(path)
        );

        return ScaleBounds(
            Header(reader),
            scale
        );
    }

    public static SceneModel Read(
        AssetPaths assets,
        string file,
        CancellationToken token = default)
    {
        string normalized = SourceModelReader.Normalize(file);

        string path = AssetPathResolver.Resolve(
            assets.Root,
            normalized
        );

        float scale = ModelScaleStorage.Read(path);

        using var reader = new BinaryReader(
            File.OpenRead(path)
        );

        var bounds = ScaleBounds(
            Header(reader),
            scale
        );

        int count = Count(
            reader.ReadInt32(),
            MaximumMeshes,
            "mesh"
        );

        var meshes = new SceneMesh[count];

        int totalVertices = 0;
        int totalIndices = 0;

        for (int meshIndex = 0; meshIndex < count; meshIndex++)
        {
            token.ThrowIfCancellationRequested();

            string name = Text(reader);
            string diffuse = TexturePath(
                assets,
                Text(reader)
            );
            string normal = TexturePath(
                assets,
                Text(reader)
            );

            var color = new Vector4(
                Scalar(reader),
                Scalar(reader),
                Scalar(reader),
                Scalar(reader)
            );

            var material = new SceneMaterial(
                name,
                diffuse,
                normal,
                color,
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                Scalar(reader)
            );

            int vertexCount = Count(
                reader.ReadInt32(),
                MaximumVertices - totalVertices,
                "vertex"
            );

            int indexCount = Count(
                reader.ReadInt32(),
                MaximumIndices - totalIndices,
                "index"
            );

            if (indexCount % 3 != 0 ||
                reader.BaseStream.Length -
                reader.BaseStream.Position <
                vertexCount * 48L + indexCount * 4L)
            {
                throw new InvalidDataException(
                    "Invalid imported model geometry size."
                );
            }

            totalVertices += vertexCount;
            totalIndices += indexCount;

            var vertices = new SceneVertex[vertexCount];
            var indices = new int[indexCount];

            for (int index = 0; index < vertices.Length; index++)
            {
                if ((index & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                var position = Vector(reader) * scale;
                var vertexNormal = Vector(reader);

                var uv = new Vector2(
                    Scalar(reader),
                    Scalar(reader)
                );

                var tangent = new Vector4(
                    Scalar(reader),
                    Scalar(reader),
                    Scalar(reader),
                    Scalar(reader)
                );

                vertices[index] = new SceneVertex(
                    position,
                    vertexNormal,
                    uv,
                    tangent
                );
            }

            for (int index = 0; index < indices.Length; index++)
            {
                if ((index & 4095) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                int value = reader.ReadInt32();

                if ((uint)value >= (uint)vertexCount)
                {
                    throw new InvalidDataException(
                        "Imported model index is out of range."
                    );
                }

                indices[index] = value;
            }

            meshes[meshIndex] = new SceneMesh(
                vertices,
                indices,
                material
            );
        }

        if (reader.BaseStream.Position != reader.BaseStream.Length)
        {
            throw new InvalidDataException(
                "Unexpected imported model trailing data."
            );
        }

        return new SceneModel(
            normalized,
            bounds,
            meshes
        );
    }

    internal static void Write(
        AssetPaths assets,
        SceneModel model,
        CancellationToken token)
    {
        string path = SourceModelReader.Resolve(
            assets,
            model.File
        );

        Directory.CreateDirectory(
            Path.GetDirectoryName(path)!
        );

        string temporary = path + $".{Guid.NewGuid():N}.tmp";

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

                WriteVector(
                    writer,
                    model.Bounds.Minimum
                );

                WriteVector(
                    writer,
                    model.Bounds.Maximum
                );

                writer.Write(Count(
                    model.Meshes.Length,
                    MaximumMeshes,
                    "mesh"
                ));

                int totalVertices = 0;
                int totalIndices = 0;

                foreach (var mesh in model.Meshes)
                {
                    token.ThrowIfCancellationRequested();

                    var material = mesh.Material;

                    WriteText(writer, material.Name);

                    WriteText(
                        writer,
                        RelativeTexture(
                            assets,
                            material.Diffuse
                        )
                    );

                    WriteText(
                        writer,
                        RelativeTexture(
                            assets,
                            material.Normal
                        )
                    );

                    WriteScalar(writer, material.Color.X);
                    WriteScalar(writer, material.Color.Y);
                    WriteScalar(writer, material.Color.Z);
                    WriteScalar(writer, material.Color.W);

                    writer.Write(material.DoubleSided);
                    writer.Write(material.AlphaTest);
                    writer.Write(material.Transparent);

                    WriteScalar(writer, material.Specular);

                    writer.Write(Count(
                        mesh.Vertices.Length,
                        MaximumVertices - totalVertices,
                        "vertex"
                    ));

                    writer.Write(Count(
                        mesh.Indices.Length,
                        MaximumIndices - totalIndices,
                        "index"
                    ));

                    if (mesh.Indices.Length % 3 != 0)
                    {
                        throw new InvalidDataException(
                            "Expected imported triangles."
                        );
                    }

                    totalVertices += mesh.Vertices.Length;
                    totalIndices += mesh.Indices.Length;

                    for (int index = 0; index < mesh.Vertices.Length; index++)
                    {
                        if ((index & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var vertex = mesh.Vertices[index];

                        WriteVector(writer, vertex.Position);
                        WriteVector(writer, vertex.Normal);

                        WriteScalar(writer, vertex.Uv.X);
                        WriteScalar(writer, vertex.Uv.Y);

                        WriteScalar(writer, vertex.Tangent.X);
                        WriteScalar(writer, vertex.Tangent.Y);
                        WriteScalar(writer, vertex.Tangent.Z);
                        WriteScalar(writer, vertex.Tangent.W);
                    }

                    for (int index = 0; index < mesh.Indices.Length; index++)
                    {
                        if ((index & 4095) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        int value = mesh.Indices[index];

                        if ((uint)value >=
                            (uint)mesh.Vertices.Length)
                        {
                            throw new InvalidDataException(
                                "Invalid imported index."
                            );
                        }

                        writer.Write(value);
                    }
                }
            }

            token.ThrowIfCancellationRequested();

            File.Move(
                temporary,
                path,
                true
            );
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private static SceneBounds ScaleBounds(
        SceneBounds bounds,
        float scale)
    {
        if (scale == 1f)
        {
            return bounds;
        }

        return new SceneBounds(
            bounds.Minimum * scale,
            bounds.Maximum * scale
        );
    }

    private static SceneBounds Header(BinaryReader reader)
    {
        _ = ReadVersion(reader);

        var bounds = new SceneBounds(
            Vector(reader),
            Vector(reader)
        );

        if (bounds.Minimum.X > bounds.Maximum.X ||
            bounds.Minimum.Y > bounds.Maximum.Y ||
            bounds.Minimum.Z > bounds.Maximum.Z)
        {
            throw new InvalidDataException(
                "Invalid imported model bounds."
            );
        }

        return bounds;
    }

    private static int ReadVersion(BinaryReader reader)
    {
        if (reader.ReadUInt32() != Signature)
        {
            throw new InvalidDataException(
                "Invalid imported model signature."
            );
        }

        int version = reader.ReadInt32();

        return version is 1 or Version
            ? version
            : throw new InvalidDataException(
                "Unsupported imported model version."
            );
    }

    private static string RelativeTexture(
        AssetPaths assets,
        string path)
    {
        if (path.Length == 0)
        {
            return "";
        }

        string relative = Path.GetRelativePath(
            assets.Root,
            path
        ).Replace('\\', '/');

        _ = TexturePath(assets, relative);

        return relative;
    }

    private static string TexturePath(
        AssetPaths assets,
        string relative)
    {
        if (relative.Length == 0)
        {
            return "";
        }

        if (!relative.StartsWith(
                "models2/Tex/",
                StringComparison.OrdinalIgnoreCase) ||
            !Path.GetExtension(relative).Equals(
                ".dds",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Expected an imported texture in models2/Tex."
            );
        }

        string root = Path.Combine(
            assets.Models2,
            "Tex"
        );

        return AssetPathResolver.Resolve(
            root,
            relative["models2/Tex/".Length..]
        );
    }

    private static string Text(BinaryReader reader)
    {
        int length = reader.ReadInt32();

        if (length is < 0 or > 16384)
        {
            throw new InvalidDataException(
                "Invalid imported model string size."
            );
        }

        var bytes = reader.ReadBytes(length);

        if (bytes.Length != length)
        {
            throw new EndOfStreamException();
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private static void WriteText(
        BinaryWriter writer,
        string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);

        if (bytes.Length > 16384)
        {
            throw new InvalidDataException(
                "Imported model string is too long."
            );
        }

        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static int Count(
        int count,
        int maximum,
        string field)
    {
        return count >= 1 && count <= maximum
            ? count
            : throw new InvalidDataException(
                $"Invalid imported model {field} count."
            );
    }

    private static Vector3 Vector(BinaryReader reader)
    {
        return new Vector3(
            Scalar(reader),
            Scalar(reader),
            Scalar(reader)
        );
    }

    private static float Scalar(BinaryReader reader)
    {
        return Finite(reader.ReadSingle());
    }

    private static float Finite(float value)
    {
        return float.IsFinite(value)
            ? value
            : throw new InvalidDataException(
                "Non-finite imported model value."
            );
    }

    private static void WriteScalar(
        BinaryWriter writer,
        float value)
    {
        writer.Write(Finite(value));
    }

    private static void WriteVector(
        BinaryWriter writer,
        Vector3 value)
    {
        WriteScalar(writer, value.X);
        WriteScalar(writer, value.Y);
        WriteScalar(writer, value.Z);
    }
}
