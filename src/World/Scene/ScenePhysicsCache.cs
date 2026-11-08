using System.Collections.Concurrent;
using System.Numerics;
using System.Xml;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene;

public sealed class ScenePhysicsCache(
    AssetPaths assets,
    Action<string> warning)
{
    private const int HeavyCollisionThreshold = 25_000;

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
            var physical = SourcePhysicsReader.Read(
                assets,
                model,
                warning: warning
            );

            if (physical == null)
            {
                return BuildVisualFallback(model);
            }

            return ApplyImportedScale(model, physical);
        }
        catch (Exception exception) when (IsLoadError(exception))
        {
            if (exception is not FileNotFoundException)
            {
                warning(
                    $"Physics {model}: {exception.Message}"
                );
            }

            return BuildVisualFallback(model);
        }
    }

    private ScenePhysicsMesh ApplyImportedScale(
        string model,
        ScenePhysicsMesh physical)
    {
        if (!model.StartsWith(
                "models2/",
                StringComparison.OrdinalIgnoreCase))
        {
            return physical;
        }

        string modelPath = SourceModelReader.Resolve(
            assets,
            model
        );

        float scale = ModelScaleStorage.Read(modelPath);

        if (scale == 1f)
        {
            return physical;
        }

        var vertices = new Vector3[physical.Vertices.Length];

        for (int index = 0; index < vertices.Length; index++)
        {
            vertices[index] = physical.Vertices[index] * scale;
        }

        var bounds = new SceneBounds(
            physical.Bounds.Minimum * scale,
            physical.Bounds.Maximum * scale
        );

        return new ScenePhysicsMesh(
            physical.File,
            vertices,
            physical.Indices,
            bounds
        )
        {
            IsVisualFallback = physical.IsVisualFallback
        };
    }

    private ScenePhysicsMesh? BuildVisualFallback(string model)
    {
        string normalized = model.Replace('\\', '/');

        bool supported =
            normalized.StartsWith(
                "objects/",
                StringComparison.OrdinalIgnoreCase
            ) ||
            normalized.StartsWith(
                "models2/",
                StringComparison.OrdinalIgnoreCase
            ) ||
            normalized.StartsWith(
                "models/",
                StringComparison.OrdinalIgnoreCase
            );

        if (!supported)
        {
            return null;
        }

        try
        {
            var visual = SourceModelReader.Read(
                assets,
                normalized
            );

            var vertices = new List<Vector3>();
            var indices = new List<int>();

            var offsets = new Dictionary<SceneVertex[], int>(
                ReferenceEqualityComparer.Instance
            );

            foreach (var mesh in visual.Meshes)
            {
                if (mesh.Vertices.Length == 0 ||
                    mesh.Indices.Length == 0)
                {
                    continue;
                }

                if (mesh.Indices.Length % 3 != 0)
                {
                    throw new InvalidDataException(
                        "Invalid visual triangle count."
                    );
                }

                if (!offsets.TryGetValue(
                        mesh.Vertices,
                        out int startVertex))
                {
                    startVertex = vertices.Count;

                    foreach (var vertex in mesh.Vertices)
                    {
                        Vector3 position = vertex.Position;

                        if (!float.IsFinite(position.X) ||
                            !float.IsFinite(position.Y) ||
                            !float.IsFinite(position.Z))
                        {
                            throw new InvalidDataException(
                                "Non-finite visual collision vertex."
                            );
                        }

                        vertices.Add(position);
                    }

                    offsets.Add(
                        mesh.Vertices,
                        startVertex
                    );
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

                    indices.Add(
                        checked(startVertex + index)
                    );
                }
            }

            if (vertices.Count == 0 || indices.Count == 0)
            {
                warning(
                    $"Physics {model}: отсутствует геометрия " +
                    "для создания коллизии."
                );

                return null;
            }

            int triangles = indices.Count / 3;

            if (triangles >= HeavyCollisionThreshold)
            {
                warning(
                    $"Physics {model}: тяжёлая коллизия, " +
                    $"{triangles:N0} треугольников. " +
                    "Для повышения производительности " +
                    "рекомендуется отдельная упрощённая коллизия."
                );
            }

            var collision = new ScenePhysicsMesh(
                normalized,
                vertices.ToArray(),
                indices.ToArray(),
                visual.Bounds
            )
            {
                IsVisualFallback = true
            };

            warning(
                $"Physics {model}: создана коллизия " +
                $"из визуальной модели, " +
                $"{triangles:N0} треугольников."
            );

            return collision;
        }
        catch (Exception exception) when (IsLoadError(exception))
        {
            warning(
                $"Physics {model}: не удалось создать " +
                $"коллизию: {exception.Message}"
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
