using System.Collections.Concurrent;
using System.Numerics;
using System.Xml;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene;

public sealed class ScenePhysicsCache(
    AssetPaths assets,
    Action<string> warning)
{
    private const int MaximumFallbackTriangles = 25_000;
    private const int MaximumFallbackVertices = 150_000;

    private readonly ConcurrentDictionary<
        string,
        Lazy<ScenePhysicsMesh?>> _meshes =
        new(StringComparer.OrdinalIgnoreCase);

    public ScenePhysicsMesh? Get(string model)
    {
        return _meshes.GetOrAdd(
            model,
            file => new Lazy<ScenePhysicsMesh?>(
                () => Load(file),
                LazyThreadSafetyMode.ExecutionAndPublication
            )
        ).Value;
    }

    public bool TryGet(
        string model,
        out ScenePhysicsMesh? mesh)
    {
        mesh = null;

        if (!_meshes.TryGetValue(model, out var entry) ||
            !entry.IsValueCreated)
        {
            return false;
        }

        mesh = entry.Value;
        return true;
    }

    private ScenePhysicsMesh? Load(string model)
    {
        try
        {
            return SourcePhysicsReader.Read(
                assets,
                model,
                warning: warning
            );
        }
        catch (Exception exception) when (IsLoadError(exception))
        {
            warning(
                $"Physics {model}: {exception.Message}"
            );

            return BuildVisualFallback(model);
        }
    }

    private ScenePhysicsMesh? BuildVisualFallback(string model)
    {
        // Reserve this fallback for the editable objects catalogue.
        // Other scene collision sources retain their existing behavior.
        string normalized = model.Replace('\\', '/');

        if (!normalized.StartsWith(
            "objects/",
            StringComparison.OrdinalIgnoreCase) && !normalized.StartsWith(
            "models2/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var visual = SourceModelReader.Read(
                assets,
                normalized
            );

            long triangleCount = visual.Meshes.Sum(
                mesh => (long)mesh.Indices.Length / 3
            );

            long vertexCount = visual.Meshes.Sum(
                mesh => (long)mesh.Vertices.Length
            );

            if (triangleCount == 0 ||
                triangleCount > MaximumFallbackTriangles ||
                vertexCount > MaximumFallbackVertices)
            {
                warning(
                    $"Physics {model}: визуальная геометрия " +
                    "слишком большая или пустая для резервной коллизии."
                );

                return null;
            }

            var vertices = new List<Vector3>(
                (int)vertexCount
            );

            var indices = new List<int>(
                checked((int)(triangleCount * 3))
            );

            foreach (var mesh in visual.Meshes)
            {
                if (mesh.Indices.Length % 3 != 0)
                {
                    throw new InvalidDataException(
                        "Invalid visual triangle count."
                    );
                }

                int startVertex = vertices.Count;

                foreach (var vertex in mesh.Vertices)
                {
                    if (!float.IsFinite(vertex.Position.X) ||
                        !float.IsFinite(vertex.Position.Y) ||
                        !float.IsFinite(vertex.Position.Z))
                    {
                        throw new InvalidDataException(
                            "Non-finite visual collision vertex."
                        );
                    }

                    vertices.Add(vertex.Position);
                }

                foreach (int index in mesh.Indices)
                {
                    if ((uint)index >=
                        (uint)mesh.Vertices.Length)
                    {
                        throw new InvalidDataException(
                            "Visual collision index is out of range."
                        );
                    }

                    indices.Add(startVertex + index);
                }
            }

            if (indices.Count == 0)
            {
                return null;
            }

            warning(
                $"Physics {model}: использована резервная " +
                $"коллизия из {indices.Count / 3:N0} " +
                "визуальных треугольников."
            );

            return new ScenePhysicsMesh(
                normalized,
                vertices.ToArray(),
                indices.ToArray(),
                visual.Bounds
            )
            {
                IsVisualFallback = true
            };
        }
        catch (Exception exception) when (IsLoadError(exception))
        {
            warning(
                $"Physics {model}: резервная коллизия " +
                $"не создана: {exception.Message}"
            );

            return null;
        }
    }

    private static bool IsLoadError(Exception exception)
    {
        return exception is
            IOException or
            UnauthorizedAccessException or
            InvalidDataException or
            FormatException or
            OverflowException or
            ArgumentException or
            XmlException;
    }
}
