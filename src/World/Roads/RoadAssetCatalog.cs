using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Scene;

namespace ZoneReborn.World.Roads;

/// <summary>Metadata only: loading a catalog never opens primitive meshes or texture pixels.</summary>
public sealed class RoadAssetCatalog
{
    private static readonly ConditionalWeakTable<AssetPaths, Lazy<Task<RoadAssetCatalog>>> Cached = new();
    public const int MaximumAssets = 8192;
    public const int MaximumMaterials = 4096;
    private const int MaximumDirectoryEntries = 50000;
    private const long MaximumMetadataBytes = 64L * 1024 * 1024;
    private const long MaximumFileBytes = 2L * 1024 * 1024;
    public IReadOnlyList<RoadAssetEntry> Entries { get; }
    public IReadOnlyList<RoadMaterialOption> Materials { get; }
    public IReadOnlyList<string> Warnings { get; }

    private RoadAssetCatalog(List<RoadAssetEntry> entries, IEnumerable<RoadMaterialOption> materials, List<string> warnings)
    {
        Entries = entries.AsReadOnly();
        Materials = Array.AsReadOnly(materials.OrderBy(value => value.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value.Id, StringComparer.Ordinal).ToArray());
        Warnings = warnings.AsReadOnly();
    }

    public static RoadAssetCatalog Load(AssetPaths assets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assets);
        return new Reader(assets, cancellationToken).Load();
    }

    /// <summary>Shares one background scan per asset-root object. Cancellation cancels this caller's wait;
    /// use Load with a token when cancellation must stop an independent scan.</summary>
    public static Task<RoadAssetCatalog> LoadCachedAsync(AssetPaths assets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assets);
        cancellationToken.ThrowIfCancellationRequested();
        var task = Cached.GetValue(assets, value => new Lazy<Task<RoadAssetCatalog>>(() => Task.Run(() => Load(value)))).Value;
        return cancellationToken.CanBeCanceled ? task.WaitAsync(cancellationToken) : task;
    }

    public IEnumerable<RoadAssetEntry> Search(string? query, string? category = null) => Entries.Where(entry =>
        InCategory(entry.Category, category) && Matches(query, entry.Name, entry.File, entry.Category));

    public IEnumerable<RoadMaterialOption> SearchMaterials(string? query, string? category = null)
    {
        // A shared texture set can occur in several locations; its representative source is
        // deterministic, but category filtering must still find every location that uses it.
        HashSet<string>? ids = string.IsNullOrWhiteSpace(category) ? null : Entries
            .Where(entry => InCategory(entry.Category, category)).SelectMany(entry => entry.MaterialIds).ToHashSet(StringComparer.Ordinal);
        return Materials.Where(entry => (ids == null || ids.Contains(entry.Id)) &&
            Matches(query, entry.Name, entry.Diffuse, entry.SourceAsset, entry.Category));
    }

    private static bool InCategory(string value, string? category) => string.IsNullOrWhiteSpace(category) ||
        value.Equals(category.Trim().TrimEnd('/'), StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith(category.Trim().TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);
    private static bool Matches(string? query, params string[] values) => string.IsNullOrWhiteSpace(query) ||
        query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .All(word => values.Any(value => value.Contains(word, StringComparison.OrdinalIgnoreCase)));

    private sealed class Reader(AssetPaths assets, CancellationToken token)
    {
        private readonly List<RoadAssetEntry> _entries = [];
        private readonly Dictionary<string, RoadMaterialOption> _materials = new(StringComparer.Ordinal);
        private readonly List<string> _warnings = [];
        private readonly HashSet<string> _warningSet = new(StringComparer.Ordinal);
        private readonly Dictionary<string, BigWorldSection> _metadata = new(StringComparer.OrdinalIgnoreCase);
        private long _metadataBytes;
        private int _directoryEntries;

        public RoadAssetCatalog Load()
        {
            token.ThrowIfCancellationRequested();
            var files = new List<string>();
            // Prefer a reusable segment as the representative source for shared texture sets.
            foreach (string folder in new[] { "objects/roads_segments", "objects/roads" })
            {
                string directory = Resolve(folder);
                if (!Directory.Exists(directory)) { Warn($"Road asset directory is absent: {folder}"); continue; }
                Enumerate(directory, files, 0);
            }
            foreach (string file in files)
            {
                token.ThrowIfCancellationRequested();
                try { ReadAsset(file); }
                catch (Exception error) when (Recoverable(error)) { Warn($"{file}: {error.Message}"); }
            }
            return new(_entries, _materials.Values, _warnings);
        }

        private void Enumerate(string directory, List<string> files, int depth)
        {
            token.ThrowIfCancellationRequested();
            if (depth > 16) throw new InvalidDataException("Road asset directory nesting exceeds 16 levels.");
            string[] children = Directory.EnumerateFileSystemEntries(directory).Take(MaximumDirectoryEntries + 1)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ThenBy(path => path, StringComparer.Ordinal).ToArray();
            if (children.Length > MaximumDirectoryEntries - _directoryEntries)
                throw new InvalidDataException("Road asset catalog exceeds the directory-entry budget.");
            _directoryEntries += children.Length;
            foreach (string path in children)
            {
                token.ThrowIfCancellationRequested();
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0) { Warn($"Skipped linked asset path: {Relative(path)}"); continue; }
                if ((attributes & FileAttributes.Directory) != 0) { Enumerate(path, files, depth + 1); continue; }
                if (!Path.GetExtension(path).Equals(".model", StringComparison.OrdinalIgnoreCase)) continue;
                if (files.Count >= MaximumAssets) throw new InvalidDataException("Road asset catalog exceeds 8192 models.");
                files.Add(Relative(path));
            }
        }

        private void ReadAsset(string file)
        {
            var overrides = new Dictionary<string, BigWorldSection>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string modelFile = file, visualFile = "";
            for (int depth = 0; depth < 16; depth++)
            {
                if (!visited.Add(modelFile)) throw new InvalidDataException("Cyclic road model parent reference.");
                var model = Read(modelFile);
                foreach (var dye in model.All("dye"))
                {
                    string replaces = dye.Child("replaces")?.Text ?? "";
                    var tint = dye.All("tint").FirstOrDefault(value => value.Child("name")?.Text == "Default");
                    if (replaces.Length > 0 && tint?.Child("material") is { } material) overrides.TryAdd(replaces, material);
                }
                string visual = model.Child("nodefullVisual")?.Text ?? model.Child("nodelessVisual")?.Text ?? "";
                if (visual.Length > 0) { visualFile = Reference(WithExtension(visual, ".visual")); break; }
                string parent = model.Child("parent")?.Text ?? "";
                if (parent.Length == 0) throw new InvalidDataException("Road model has no visual or parent reference.");
                modelFile = Reference(WithExtension(parent, ".model"));
            }
            if (visualFile.Length == 0) throw new InvalidDataException("Road model parent chain exceeds 16 models.");
            var source = Read(visualFile);
            string category = Category(file);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var set in source.All("renderSet"))
            foreach (var geometry in set.All("geometry"))
            foreach (var group in geometry.All("primitiveGroup"))
            {
                token.ThrowIfCancellationRequested();
                if (group.Child("material") is not { } material) continue;
                try
                {
                    var option = ReadMaterial(material, overrides, file, visualFile, category);
                    if (option != null) ids.Add(option.Id);
                }
                catch (Exception error) when (Recoverable(error)) { Warn($"{visualFile}: {error.Message}"); }
            }
            SceneBounds? bounds = null;
            if (source.Child("boundingBox") is { } box)
            {
                try
                {
                    float[] min = box.Child("min")?.Floats() ?? [], max = box.Child("max")?.Floats() ?? [];
                    if (min.Length != 3 || max.Length != 3 || Enumerable.Range(0, 3).Any(i => min[i] > max[i]))
                        throw new InvalidDataException("Invalid road visual bounds.");
                    bounds = new(new Vector3(min[0], min[1], min[2]), new Vector3(max[0], max[1], max[2]));
                }
                catch (Exception error) when (Recoverable(error)) { Warn($"{visualFile}: {error.Message}"); }
            }
            _entries.Add(new(file, Path.GetFileNameWithoutExtension(file), category, visualFile, bounds,
                Array.AsReadOnly(ids.OrderBy(id => id, StringComparer.Ordinal).ToArray())));
        }

        private RoadMaterialOption? ReadMaterial(BigWorldSection material, Dictionary<string, BigWorldSection> overrides,
            string asset, string visual, string category)
        {
            var fields = new Dictionary<string, BigWorldSection>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Merge(material, 0);
            string identifier = material.Child("identifier")?.Text ?? "";
            if (overrides.TryGetValue(identifier, out var replacement)) Merge(replacement, 0);
            string diffuse = Texture("diffuseMap");
            if (diffuse.Length == 0) return null;
            string normal = Texture("normalMap"), specular = Texture("specularMap");
            string shader = fields.GetValueOrDefault("fx")?.Text ?? "";
            if (shader.Length > 0) shader = Reference(shader);
            bool alphaTest = Flag("alphaTestEnable") || shader.Contains("alphatest", StringComparison.OrdinalIgnoreCase);
            bool alphaBlend = Flag("alphaBlendEnable") || Flag("blendEnable") ||
                shader.Contains("_alpha", StringComparison.OrdinalIgnoreCase) || shader.Contains("_blend", StringComparison.OrdinalIgnoreCase) ||
                shader.Contains("_add", StringComparison.OrdinalIgnoreCase) || shader.Contains("transparent", StringComparison.OrdinalIgnoreCase);
            bool specialized = shader.Contains("multitex", StringComparison.OrdinalIgnoreCase) ||
                shader.Contains("uvtransform", StringComparison.OrdinalIgnoreCase) || shader.Contains("chrome", StringComparison.OrdinalIgnoreCase) ||
                shader.Contains("glow", StringComparison.OrdinalIgnoreCase) || shader.Contains("pixelup", StringComparison.OrdinalIgnoreCase) ||
                shader.EndsWith("/road.fx", StringComparison.OrdinalIgnoreCase) || fields.ContainsKey("reflectionMap") ||
                fields.ContainsKey("Mask1") || fields.ContainsKey("DiffMap1") || fields.ContainsKey("Lightmap") || fields.ContainsKey("glowMap");
            string signature = string.Join('\n', diffuse, normal, specular, shader, alphaTest.ToString(), alphaBlend.ToString(), specialized.ToString());
            string id = "source-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signature.ToLowerInvariant()))).ToLowerInvariant();
            if (_materials.TryGetValue(id, out var existing)) return existing;
            if (_materials.Count >= MaximumMaterials) throw new InvalidDataException("Road catalog exceeds 4096 unique texture sets.");
            var option = new RoadMaterialOption(id, Path.GetFileNameWithoutExtension(diffuse), category, asset, visual,
                diffuse, normal, specular, shader, alphaTest, alphaBlend, specialized);
            _materials.Add(id, option);
            return option;

            bool Flag(string key) => fields.TryGetValue(key, out var value) && value.Flag;
            string Texture(string key)
            {
                if (!fields.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value.Text)) return "";
                string reference = Reference(value.Text);
                // Matches BigWorldModelReader's authored-extension to shipped-DDS resolution.
                string dds = Path.ChangeExtension(reference, ".dds");
                if (File.Exists(Resolve(dds))) return dds;
                if (File.Exists(Resolve(reference))) return reference;
                Warn($"Missing road material texture: {reference}");
                return "";
            }
            void Merge(BigWorldSection value, int depth)
            {
                token.ThrowIfCancellationRequested();
                if (depth > 16) throw new InvalidDataException("Road material inheritance exceeds 16 levels.");
                string reference = value.Child("mfm")?.Text ?? "";
                if (reference.Length > 0)
                {
                    reference = Reference(reference);
                    if (!visited.Add(reference)) throw new InvalidDataException("Cyclic road material reference.");
                    Merge(Read(reference), depth + 1);
                    visited.Remove(reference);
                }
                foreach (var field in value.Children)
                    if (field.Name == "property")
                    {
                        if (field.Children.Length > 0) fields[field.Text] = field.Children[0];
                    }
                    else fields[field.Name] = field;
            }
        }

        private BigWorldSection Read(string file)
        {
            token.ThrowIfCancellationRequested();
            string path = Resolve(file);
            if (_metadata.TryGetValue(path, out var cached)) return cached;
            using var stream = File.OpenRead(path);
            if (stream.Length is < 1 or > MaximumFileBytes || stream.Length > MaximumMetadataBytes - _metadataBytes)
                throw new InvalidDataException("Road metadata exceeds its file or catalog byte budget.");
            _metadataBytes += stream.Length;
            byte[] bytes = new byte[checked((int)stream.Length)];
            stream.ReadExactly(bytes);
            token.ThrowIfCancellationRequested();
            var section = BigWorldSection.Read(bytes, token);
            _metadata.Add(path, section);
            return section;
        }

        private string Resolve(string file)
        {
            string reference = Reference(file);
            string result = AssetPathResolver.Resolve(assets.Root, reference);
            // Lexical containment alone does not prevent a junction in a dependency path.
            string current = assets.Root;
            foreach (string part in reference.Split('/'))
            {
                token.ThrowIfCancellationRequested();
                current = Path.Combine(current, part);
                if ((File.Exists(current) || Directory.Exists(current)) &&
                    (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Linked road asset dependencies are unsupported.");
            }
            return result;
        }

        private static string Reference(string value)
        {
            string reference = value.Trim().Replace('\\', '/');
            if (reference.StartsWith("packs/", StringComparison.OrdinalIgnoreCase)) reference = reference[6..];
            if (reference.StartsWith("models/", StringComparison.OrdinalIgnoreCase)) reference = "objects/" + reference[7..];
            if (reference.Length is 0 or > 1024 || reference.Contains(':') || reference.Any(char.IsControl) ||
                reference.Split('/').Any(part => part is ".." or "." || part.Length == 0) ||
                reference.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new InvalidDataException("Invalid road resource reference.");
            return reference;
        }
        private string Relative(string path) => Path.GetRelativePath(assets.Root, path).Replace('\\', '/');
        private static string Category(string file)
        {
            string[] parts = file.Split('/');
            return parts.Length > 3 && parts[1].Equals("roads", StringComparison.OrdinalIgnoreCase)
                ? $"roads/{parts[2]}" : "roads_segments";
        }
        private static string WithExtension(string file, string extension) => file.Trim().EndsWith(extension, StringComparison.OrdinalIgnoreCase)
            ? file.Trim() : file.Trim() + extension;
        private static bool Recoverable(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
            ArgumentException or FormatException or OverflowException or XmlException or NotSupportedException;
        private void Warn(string message)
        {
            if (_warnings.Count < 256 && _warningSet.Add(message)) _warnings.Add(message);
        }
    }
}

public sealed record RoadAssetEntry(string File, string Name, string Category, string Visual,
    SceneBounds? Bounds, IReadOnlyList<string> MaterialIds)
{
    public override string ToString() => $"{Category} / {Name}";
}

/// <summary>Verified texture references relative to packs; these are source texture sets, not converted PBR materials.</summary>
public sealed record RoadMaterialOption(string Id, string Name, string Category, string SourceAsset, string SourceVisual,
    string Diffuse, string Normal, string Specular, string Shader, bool AlphaTest, bool AlphaBlend, bool RequiresSpecializedShader)
{
    public override string ToString() => Name;
}
