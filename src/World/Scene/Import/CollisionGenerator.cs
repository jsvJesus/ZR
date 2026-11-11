using System.Numerics;
using MeshOptimizer;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene.Import;

public readonly record struct CollisionBuildResult(
    int SourceTriangles,
    int CollisionTriangles
);

public static class CollisionGenerator
{
    private const int MinimumTriangles = 64;
    private const int MinimumSimplificationTriangles = 256;

    public static CollisionBuildResult Generate(
        SceneModel model,
        string modelPath,
        CancellationToken token = default)
    {
        var settings = CollisionGenerationSettingsStorage.Read(
            modelPath
        );

        return GenerateInternal(
            model,
            modelPath,
            1f,
            settings,
            token
        );
    }

    public static CollisionBuildResult Generate(
        AssetPaths assets,
        string model,
        CancellationToken token = default)
    {
        string modelPath = SourceModelReader.Resolve(
            assets,
            model
        );

        var settings = CollisionGenerationSettingsStorage.Read(
            modelPath
        );

        var geometry = ImportedModelStorage.Read(
            assets,
            model,
            token
        );

        float scale = ModelScaleStorage.Read(modelPath);

        return GenerateInternal(
            geometry,
            modelPath,
            1f / scale,
            settings,
            token
        );
    }

    private static CollisionBuildResult GenerateInternal(
        SceneModel model,
        string modelPath,
        float inverseScale,
        CollisionGenerationSettings settings,
        CancellationToken token)
    {
        var vertices = new List<Vector3>();
        var indices = new List<int>();

        int sourceTriangles = 0;

        foreach (var mesh in model.Meshes)
        {
            token.ThrowIfCancellationRequested();

            if (mesh.Vertices.Length == 0 ||
                mesh.Indices.Length == 0)
            {
                continue;
            }

            if (mesh.Indices.Length % 3 != 0)
            {
                throw new InvalidDataException(
                    "Invalid source triangle count."
                );
            }

            int triangleCount = mesh.Indices.Length / 3;

            sourceTriangles = checked(
                sourceTriangles + triangleCount
            );

            float[] positions = new float[
                checked(mesh.Vertices.Length * 3)
            ];

            for (int index = 0;
                 index < mesh.Vertices.Length;
                 index++)
            {
                if ((index & 4095) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Vector3 position =
                    mesh.Vertices[index].Position * inverseScale;

                if (!float.IsFinite(position.X) ||
                    !float.IsFinite(position.Y) ||
                    !float.IsFinite(position.Z))
                {
                    throw new InvalidDataException(
                        "Invalid source vertex position."
                    );
                }

                int offset = index * 3;

                positions[offset] = position.X;
                positions[offset + 1] = position.Y;
                positions[offset + 2] = position.Z;
            }

            uint[] sourceIndices = new uint[
                mesh.Indices.Length
            ];

            for (int index = 0;
                 index < sourceIndices.Length;
                 index++)
            {
                int value = mesh.Indices[index];

                if ((uint)value >=
                    (uint)mesh.Vertices.Length)
                {
                    throw new InvalidDataException(
                        "Invalid source triangle index."
                    );
                }

                sourceIndices[index] = (uint)value;
            }

            uint[] simplifiedIndices = Simplify(
                positions,
                sourceIndices,
                mesh.Vertices.Length,
                settings,
                token
            );

            var remap = new Dictionary<uint, int>();

            foreach (uint sourceIndex in simplifiedIndices)
            {
                if (!remap.TryGetValue(
                        sourceIndex,
                        out int destinationIndex))
                {
                    destinationIndex = vertices.Count;

                    int offset = checked(
                        (int)sourceIndex * 3
                    );

                    vertices.Add(new Vector3(
                        positions[offset],
                        positions[offset + 1],
                        positions[offset + 2]
                    ));

                    remap.Add(
                        sourceIndex,
                        destinationIndex
                    );
                }

                indices.Add(destinationIndex);
            }
        }

        if (vertices.Count == 0 ||
            indices.Count == 0)
        {
            throw new InvalidDataException(
                "No triangles available for collision."
            );
        }

        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);

        foreach (Vector3 vertex in vertices)
        {
            minimum = Vector3.Min(minimum, vertex);
            maximum = Vector3.Max(maximum, vertex);
        }

        var collision = new ScenePhysicsMesh(
            model.File,
            vertices.ToArray(),
            indices.ToArray(),
            new SceneBounds(minimum, maximum)
        )
        {
            IsVisualFallback = true
        };

        CollisionStorage.Write(
            modelPath,
            collision,
            token
        );

        return new CollisionBuildResult(
            sourceTriangles,
            indices.Count / 3
        );
    }

    private static unsafe uint[] Simplify(
        float[] positions,
        uint[] sourceIndices,
        int vertexCount,
        CollisionGenerationSettings settings,
        CancellationToken token)
    {
        int triangleCount = sourceIndices.Length / 3;

        if (triangleCount < MinimumSimplificationTriangles)
        {
            return sourceIndices;
        }

        token.ThrowIfCancellationRequested();

        int targetTriangles = Math.Max(
            MinimumTriangles,
            (int)MathF.Ceiling(
                triangleCount * settings.TargetRatio
            )
        );

        if (targetTriangles >= triangleCount)
        {
            return sourceIndices;
        }

        var destination = new uint[
            sourceIndices.Length
        ];

        SimplificationOptions options =
            SimplificationOptions.meshopt_SimplifyErrorAbsolute;

        if (settings.PreserveBorders)
        {
            options |= SimplificationOptions.SimplifyLockBorder;
        }

        float error = 0f;
        nuint resultCount;

        fixed (uint* destinationPointer = destination)
        fixed (uint* indicesPointer = sourceIndices)
        fixed (float* positionsPointer = positions)
        {
            resultCount = Meshopt.Simplify(
                destinationPointer,
                indicesPointer,
                (nuint)sourceIndices.Length,
                positionsPointer,
                (nuint)vertexCount,
                (nuint)(sizeof(float) * 3),
                (nuint)(targetTriangles * 3),
                settings.MaximumError,
                options,
                &error
            );
        }

        token.ThrowIfCancellationRequested();

        if (resultCount == 0 ||
            resultCount > (nuint)sourceIndices.Length ||
            resultCount % 3 != 0)
        {
            throw new InvalidDataException(
                "MeshOptimizer returned invalid collision indices."
            );
        }

        int count = checked((int)resultCount);

        if (count == sourceIndices.Length)
        {
            return sourceIndices;
        }

        if (!float.IsFinite(error) ||
            error > settings.MaximumError + 0.00001f ||
            !ValidateSimplifiedTriangles(
                positions,
                destination.AsSpan(0, count)))
        {
            return sourceIndices;
        }

        Array.Resize(
            ref destination,
            count
        );

        return destination;
    }

    private static bool ValidateSimplifiedTriangles(
        float[] positions,
        ReadOnlySpan<uint> indices)
    {
        if (indices.Length == 0 ||
            indices.Length % 3 != 0)
        {
            return false;
        }

        int vertexCount = positions.Length / 3;

        for (int index = 0; index < indices.Length; index += 3)
        {
            uint a = indices[index];
            uint b = indices[index + 1];
            uint c = indices[index + 2];

            if (a >= vertexCount ||
                b >= vertexCount ||
                c >= vertexCount ||
                a == b ||
                b == c ||
                a == c)
            {
                return false;
            }

            int ai = (int)a * 3;
            int bi = (int)b * 3;
            int ci = (int)c * 3;

            var first = new Vector3(
                positions[bi] - positions[ai],
                positions[bi + 1] - positions[ai + 1],
                positions[bi + 2] - positions[ai + 2]
            );

            var second = new Vector3(
                positions[ci] - positions[ai],
                positions[ci + 1] - positions[ai + 1],
                positions[ci + 2] - positions[ai + 2]
            );

            float areaSquared = Vector3.Cross(
                first,
                second
            ).LengthSquared();

            if (!float.IsFinite(areaSquared) ||
                areaSquared <= 1e-16f)
            {
                return false;
            }
        }

        return true;
    }
}
