using Assimp;
using Assimp.Configs;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Scene.Import;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain.Import;

public sealed record TerrainMaterialImport(IReadOnlyList<TerrainLayer> Layers, IReadOnlyList<string> Warnings);

public static class FbxTerrainMaterials
{
    public static TerrainMaterialImport Read(AssetPaths assets, IEnumerable<string> files,
        TerrainDescriptor descriptor, TerrainLayer fallback, string? textureFolder, CancellationToken token)
    {
        var layers = new List<TerrainLayer>();
        var warnings = new List<string>();
        foreach (string file in files)
        {
            token.ThrowIfCancellationRequested();
            using var context = new AssimpContext();
            context.SetConfig(new FBXImportAnimationsConfig(false));
            var scene = context.ImportFile(file, PostProcessSteps.None)
                ?? throw new InvalidDataException("Не удалось прочитать материалы FBX.");
            var roots = new List<string> { Path.GetDirectoryName(file)! };
            if (!string.IsNullOrWhiteSpace(textureFolder)) roots.Insert(0, textureFolder);
            var textures = new ModelTextureImporter(assets, roots, token, Path.Combine(assets.Materials, "terrain-import"));
            for (int materialIndex = 0; materialIndex < scene.Materials.Count; materialIndex++)
            {
                var material = scene.Materials[materialIndex];
                token.ThrowIfCancellationRequested();
                string name = MaterialName(material, materialIndex);
                string diffuse = "", normal = fallback.NormalTexture;
                string? reference = Reference(material, TextureType.BaseColor, TextureType.Diffuse);
                try
                {
                    // Some exports keep material names but omit image connections. Try the exact name locally.
                    if (reference != null) diffuse = Relative(textures.Import(scene, file, reference));
                    else
                    {
                        Exception? missing = null;
                        foreach (string extension in new[] { ".dds", ".tga", ".png", ".jpg", ".jpeg", ".bmp" })
                        {
                            try { diffuse = Relative(textures.Import(scene, file, name + extension)); break; }
                            catch (FileNotFoundException error) { missing = error; }
                        }
                        if (diffuse.Length == 0) throw missing ?? new FileNotFoundException($"Не найдена текстура {name}.");
                    }
                }
                catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
                { warnings.Add($"{name}: {error.Message}"); }
                string? normalReference = Reference(material, TextureType.Normals, TextureType.NormalCamera);
                if (normalReference != null)
                {
                    try { normal = Relative(textures.Import(scene, file, normalReference)); }
                    catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
                    { warnings.Add($"{name}, normal: {error.Message}"); }
                }
                layers.Add(new TerrainLayer
                {
                    Name = name, SourceMaterial = SourceKey(file, material, materialIndex),
                    DiffuseTexture = diffuse.Length == 0 ? fallback.DiffuseTexture : diffuse,
                    NormalTexture = normal, NeedsTexture = diffuse.Length == 0,
                    AverageColor = diffuse.Length == 0 ? [] : TerrainMaterialColor.Read(AssetPathResolver.Resolve(assets.Root, diffuse)),
                    ScaleU = descriptor.SamplesX * descriptor.CellSize / 4,
                    ScaleV = descriptor.SamplesZ * descriptor.CellSize / 4
                });
            }
        }
        return new(layers, warnings);

        string Relative(string path) => Path.GetRelativePath(assets.Root, path).Replace('\\', '/');
    }

    internal static string MaterialName(Material material, int index) =>
        string.IsNullOrWhiteSpace(material.Name) ? $"Материал {index + 1}" : material.Name;

    internal static string SourceKey(string file, Material material, int index) =>
        Path.GetFullPath(file) + "|" + MaterialName(material, index);

    public static string CopyTexture(AssetPaths assets, string file, CancellationToken token)
    {
        var textures = new ModelTextureImporter(assets, [Path.GetDirectoryName(file)!], token,
            Path.Combine(assets.Materials, "terrain-import"));
        string result = textures.Import(new Assimp.Scene(), file, Path.GetFullPath(file));
        return Path.GetRelativePath(assets.Root, result).Replace('\\', '/');
    }

    private static string? Reference(Material material, params TextureType[] types)
    {
        foreach (var type in types)
            if (material.GetMaterialTexture(type, 0, out var slot) && !string.IsNullOrWhiteSpace(slot.FilePath)) return slot.FilePath;
        return null;
    }
}
