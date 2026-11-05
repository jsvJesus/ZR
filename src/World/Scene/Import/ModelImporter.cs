using System.Numerics;
using Assimp;
using Assimp.Configs;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene.Import;

public sealed record ModelImportProgress(string File, int Completed, int Total);
public sealed record ModelImportReport(int Imported, int Skipped, int Textures, string[] Models, string[] Errors);

/// <summary>Converts source FBX into self-contained static scene assets.</summary>
public sealed class ModelImporter(AssetPaths assets)
{
    public Task<ModelImportReport> ImportAsync(IEnumerable<string>? textureFolders = null,
        IProgress<ModelImportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        string[] roots = textureFolders?.ToArray() ?? [];
        return Task.Run(() => Import(roots, progress, cancellationToken), cancellationToken);
    }

    private ModelImportReport Import(string[] textureFolders, IProgress<ModelImportProgress>? progress, CancellationToken token)
    {
        if (!Directory.Exists(assets.ImportModels))
            throw new DirectoryNotFoundException($"Не найдена папка исходных моделей: {assets.ImportModels}");
        var files = Directory.EnumerateFiles(assets.ImportModels, "*", SearchOption.AllDirectories)
            .Where(file => Path.GetExtension(file).Equals(".fbx", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var outputs = new List<string>();
        var errors = new List<string>();
        if (files.Length == 0) return new(0, 0, 0, [], []);
        int skipped = 0;
        var textures = new ModelTextureImporter(assets, textureFolders, token);
        // FBX declares its own units. GlobalScale applies the importer's conversion
        // to metres; a fixed centimetre multiplier also shrinks metre-authored assets.
        using var context = new AssimpContext();
        context.SetConfig(new FBXPreservePivotsConfig(false));
        context.SetConfig(new FBXImportAnimationsConfig(false));
        context.SetConfig(new FBXImportCamerasConfig(false));
        context.SetConfig(new FBXImportLightsConfig(false));
        var flags = PostProcessSteps.Triangulate | PostProcessSteps.JoinIdenticalVertices
            | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.TransformUVCoords
            | PostProcessSteps.PreTransformVertices | PostProcessSteps.SortByPrimitiveType | PostProcessSteps.GlobalScale
            | PostProcessSteps.MakeLeftHanded | PostProcessSteps.FlipWindingOrder | PostProcessSteps.FlipUVs;
        for (int index = 0; index < files.Length; index++)
        {
            token.ThrowIfCancellationRequested();
            string source = files[index];
            string relative = Path.GetRelativePath(assets.ImportModels, source).Replace('\\', '/');
            string target = "models2/" + Path.ChangeExtension(relative, ImportedModelStorage.Extension);
            progress?.Report(new(relative, index, files.Length));
            try
            {
                string path = AssetPathResolver.Resolve(assets.Root, target);
                if (File.Exists(path) && ImportedModelStorage.HasLegacyScale(path))
                {
                    // Keep old assets stable: existing instances may compensate with scale 100.
                    target = "models2/" + Path.ChangeExtension(relative, ".meters" + ImportedModelStorage.Extension);
                    path = AssetPathResolver.Resolve(assets.Root, target);
                }
                if (File.Exists(path))
                {
                    if (ImportedModelStorage.HasLegacyScale(path))
                        throw new InvalidDataException($"Путь исправленной модели занят старым ресурсом: {target}");
                    skipped++;
                    continue;
                }
                var scene = context.ImportFile(source, flags);
                token.ThrowIfCancellationRequested();
                if (scene == null || !scene.HasMeshes) throw new InvalidDataException("В FBX нет геометрии.");
                var model = Convert(scene, source, target, textures, token);
                ImportedModelStorage.Write(assets, model, token);
                outputs.Add(target);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                errors.Add($"{relative}: {error.Message}");
            }
            finally { progress?.Report(new(relative, index + 1, files.Length)); }
        }
        return new(outputs.Count, skipped, textures.TextureCount, outputs.ToArray(), errors.ToArray());
    }

    private static SceneModel Convert(Assimp.Scene scene, string source, string target,
        ModelTextureImporter textures, CancellationToken token)
    {
        var materials = new Dictionary<int, (SceneMaterial Material, int Uv)>();
        var meshes = new List<SceneMesh>();
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        int totalVertices = 0, totalIndices = 0;
        foreach (var mesh in scene.Meshes)
        {
            token.ThrowIfCancellationRequested();
            if (mesh.VertexCount == 0 || mesh.FaceCount == 0) continue;
            if (mesh.Faces.Any(face => face.IndexCount != 3))
                throw new InvalidDataException("FBX содержит линии или точки; ожидается треугольная поверхность.");
            totalVertices = checked(totalVertices + mesh.VertexCount);
            totalIndices = checked(totalIndices + mesh.FaceCount * 3);
            if (totalVertices > ImportedModelStorage.MaximumVertices || totalIndices > ImportedModelStorage.MaximumIndices)
                throw new InvalidDataException("Геометрия FBX превышает поддерживаемый размер модели.");
            if ((uint)mesh.MaterialIndex >= (uint)scene.MaterialCount)
                throw new InvalidDataException("Некорректный индекс материала FBX.");
            if (!materials.TryGetValue(mesh.MaterialIndex, out var material))
            {
                material = ConvertMaterial(scene, scene.Materials[mesh.MaterialIndex], source, textures);
                materials.Add(mesh.MaterialIndex, material);
            }
            if ((material.Material.Diffuse.Length > 0 || material.Material.Normal.Length > 0) && !mesh.HasTextureCoords(material.Uv))
                throw new InvalidDataException($"Нет UV-канала {material.Uv} для материала {material.Material.Name}.");
            var vertices = new SceneVertex[mesh.VertexCount];
            for (int index = 0; index < vertices.Length; index++)
            {
                if ((index & 1023) == 0) token.ThrowIfCancellationRequested();
                Vector3 position = mesh.Vertices[index];
                Vector3 normal = mesh.HasNormals ? Direction(mesh.Normals[index], Vector3.UnitY) : Vector3.UnitY;
                Vector3 uv = mesh.HasTextureCoords(material.Uv) ? mesh.TextureCoordinateChannels[material.Uv][index] : Vector3.Zero;
                Vector3 fallback = Direction(Vector3.Cross(Math.Abs(normal.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX, normal), Vector3.UnitX);
                vertices[index] = new(position, normal, new(uv.X, uv.Y), new(fallback, 1));
                minimum = Vector3.Min(minimum, position); maximum = Vector3.Max(maximum, position);
            }
            var indices = mesh.Faces.SelectMany(face => face.Indices).ToArray();
            BuildTangents(vertices, indices, token);
            meshes.Add(new(vertices, indices, material.Material));
        }
        if (meshes.Count == 0) throw new InvalidDataException("В FBX нет треугольных поверхностей.");
        return new(target, new(minimum, maximum), meshes.ToArray());
    }

    private static (SceneMaterial Material, int Uv) ConvertMaterial(Assimp.Scene scene, Material source,
        string model, ModelTextureImporter textures)
    {
        // Keep every referenced texture locally, including channels the current scene shader does not render.
        foreach (var slot in source.GetAllMaterialTextures()) textures.Import(scene, model, slot.FilePath);
        TextureSlot? diffuse = Slot(TextureType.BaseColor) ?? Slot(TextureType.Diffuse);
        TextureSlot? normal = Slot(TextureType.Normals) ?? Slot(TextureType.NormalCamera);
        int uv = diffuse?.UVIndex ?? normal?.UVIndex ?? 0;
        if ((diffuse is { } d && d.Mapping != TextureMapping.FromUV) || (normal is { } n && n.Mapping != TextureMapping.FromUV))
            throw new InvalidDataException($"Материал {source.Name} использует проекцию текстур без UV.");
        if (diffuse.HasValue && normal.HasValue && diffuse.Value.UVIndex != normal.Value.UVIndex)
            throw new InvalidDataException($"Материал {source.Name} использует разные UV-каналы для цвета и нормалей.");
        string diffusePath = diffuse.HasValue ? textures.Import(scene, model, diffuse.Value.FilePath) : "";
        string normalPath = normal.HasValue ? textures.Import(scene, model, normal.Value.FilePath) : "";
        var color = source.HasColorDiffuse ? source.ColorDiffuse : Vector4.One;
        color.W = Math.Clamp(source.Opacity, 0, 1);
        bool transparent = color.W < 0.999f;
        return (new(source.Name, diffusePath, normalPath, color, source.IsTwoSided,
            !transparent && diffusePath.Length > 0, transparent, 0), uv);

        TextureSlot? Slot(TextureType type) => source.GetMaterialTexture(type, 0, out var slot) ? slot : null;
    }

    private static Vector3 Direction(Vector3 direction, Vector3 fallback) =>
        float.IsFinite(direction.X) && float.IsFinite(direction.Y) && float.IsFinite(direction.Z) && direction.LengthSquared() > 1e-12f
            ? Vector3.Normalize(direction) : fallback;

    private static void BuildTangents(SceneVertex[] vertices, int[] indices, CancellationToken token)
    {
        // Generate from the material's actual UV channel, including the FBX UV flip.
        var tangents = new Vector3[vertices.Length];
        var bitangents = new Vector3[vertices.Length];
        for (int face = 0; face < indices.Length; face += 3)
        {
            if ((face & 4095) == 0) token.ThrowIfCancellationRequested();
            int a = indices[face], b = indices[face + 1], c = indices[face + 2];
            if ((uint)a >= (uint)vertices.Length || (uint)b >= (uint)vertices.Length || (uint)c >= (uint)vertices.Length)
                throw new InvalidDataException("Некорректный индекс вершины FBX.");
            var first = vertices[b].Position - vertices[a].Position;
            var second = vertices[c].Position - vertices[a].Position;
            var firstUv = vertices[b].Uv - vertices[a].Uv;
            var secondUv = vertices[c].Uv - vertices[a].Uv;
            float determinant = firstUv.X * secondUv.Y - firstUv.Y * secondUv.X;
            if (!float.IsFinite(determinant) || Math.Abs(determinant) < 1e-12f) continue;
            var tangent = (first * secondUv.Y - second * firstUv.Y) / determinant;
            var bitangent = (second * firstUv.X - first * secondUv.X) / determinant;
            tangents[a] += tangent; tangents[b] += tangent; tangents[c] += tangent;
            bitangents[a] += bitangent; bitangents[b] += bitangent; bitangents[c] += bitangent;
        }
        for (int index = 0; index < vertices.Length; index++)
        {
            if ((index & 1023) == 0) token.ThrowIfCancellationRequested();
            var vertex = vertices[index];
            var fallback = new Vector3(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z);
            var tangent = Direction(tangents[index] - vertex.Normal * Vector3.Dot(vertex.Normal, tangents[index]), fallback);
            float sign = Vector3.Dot(Vector3.Cross(vertex.Normal, tangent), bitangents[index]) < 0 ? -1 : 1;
            vertices[index] = vertex with { Tangent = new(tangent, sign) };
        }
    }
}
