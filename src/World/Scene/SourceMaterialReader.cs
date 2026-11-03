using System.Collections.Concurrent;
using System.Globalization;
using System.Numerics;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene;

internal sealed class SourceMaterialReader
{
    private readonly AssetPaths _assets;
    private readonly Action<string>? _warning;
    private readonly Dictionary<string, Dictionary<string, string>> _room = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Lazy<SceneMaterial>> _materials = new(StringComparer.OrdinalIgnoreCase);

    public SourceMaterialReader(AssetPaths assets, string? sourceDirectory, Action<string>? warning)
    {
        _assets = assets;
        _warning = warning;
        if (sourceDirectory == null) return;
        string path = Path.Combine(sourceDirectory, "room.mat");
        try
        {
            if (File.Exists(path)) foreach (var material in ReadDefinitions(path)) _room[material.Key] = material.Value;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
        { _warning?.Invoke($"{path}: {error.Message}"); }
    }

    public SceneMaterial Read(string modelPath, string name)
    {
        string directory = Path.GetDirectoryName(modelPath)!;
        return _materials.GetOrAdd(directory + "\n" + name, _ => new Lazy<SceneMaterial>(() => Load(directory, name),
            LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    private SceneMaterial Load(string directory, string name)
    {
        try
        {
            // Confinement also rejects material names containing traversal components.
            string local = AssetPathResolver.Resolve(directory, "Materials/" + name + ".mat");
            Dictionary<string, string>? fields = null;
            if (File.Exists(local))
            {
                var definitions = ReadDefinitions(local);
                if (!definitions.TryGetValue(name, out fields)) fields = definitions.Values.FirstOrDefault();
            }
            if (fields == null) _room.TryGetValue(name, out fields);
            if (fields == null)
            {
                _warning?.Invoke($"Missing material {name} for {directory}.");
                return Fallback(name);
            }
            string diffuse = Texture(fields.GetValueOrDefault("Texture", ""), fields, directory);
            string normal = Texture(fields.GetValueOrDefault("NormalMap", ""), fields, directory);
            string[] rgb = fields.GetValueOrDefault("Color24", "255 255 255").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            float Color(int index) => index < rgb.Length && float.TryParse(rgb[index], NumberStyles.Float,
                CultureInfo.InvariantCulture, out float value) && float.IsFinite(value) ? Math.Clamp(value / 255, 0, 1) : 1;
            bool transparent = Amount(fields, "ForceTransparent") > 0 || Amount(fields, "AlphaTransparent") > 0;
            bool doubleSided = Amount(fields, "DoubleSided") > 0;
            // Source foliage commonly has no explicit AlphaRef; its DDS stores the cutout alpha.
            bool alphaTest = !transparent && (Amount(fields, "AlphaTest") > 0 || Amount(fields, "AlphaRef") > 0 || HasAlpha(diffuse));
            return new(name, diffuse, normal, new(Color(0), Color(1), Color(2), 1), doubleSided, alphaTest,
                transparent, Math.Clamp(Amount(fields, "SpecularPower"), 0, 1));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            _warning?.Invoke($"Material {name}: {error.Message}");
            return Fallback(name);
        }
    }

    private string Texture(string texture, Dictionary<string, string> fields, string directory)
    {
        if (string.IsNullOrWhiteSpace(texture)) return "";
        texture = texture.Replace('\\', '/');
        // Original object-local textures have priority over the level-wide ImagesDir.
        foreach (string candidate in Candidates())
            if (File.Exists(candidate)) return candidate;
        _warning?.Invoke($"Missing texture {texture} for {directory}.");
        return "";

        IEnumerable<string> Candidates()
        {
            yield return AssetPathResolver.Resolve(directory, "Textures/" + Path.GetFileName(texture));
            if (texture.Contains('/')) yield return ResolveAsset(texture);
            if (fields.TryGetValue("ImagesDir", out string? images) && !string.IsNullOrWhiteSpace(images))
                yield return ResolveAsset(images.TrimEnd('/', '\\') + "/" + Path.GetFileName(texture));
            yield return AssetPathResolver.Resolve(_assets.Root, texture);
        }
    }

    private string ResolveAsset(string path)
    {
        path = path.Replace('\\', '/');
        if (path.StartsWith("packs/", StringComparison.OrdinalIgnoreCase)) path = path[6..];
        if (path.StartsWith("data/objectsdepot/", StringComparison.OrdinalIgnoreCase)) path = "models/" + path[18..];
        return AssetPathResolver.Resolve(_assets.Root, path);
    }

    private static Dictionary<string, Dictionary<string, string>> ReadDefinitions(string path)
    {
        var definitions = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string>? current = null;
        foreach (string line in File.ReadLines(path))
        {
            string text = line.Trim();
            if (text == "[MaterialBegin]") current = new(StringComparer.OrdinalIgnoreCase);
            else if (text == "[MaterialEnd]" && current != null)
            {
                if (current.TryGetValue("Name", out string? name)) definitions[name] = current;
                current = null;
            }
            else if (current != null)
            {
                int equals = text.IndexOf('=');
                if (equals > 0) current[text[..equals].Trim()] = text[(equals + 1)..].Trim();
            }
        }
        return definitions;
    }

    private static bool HasAlpha(string path)
    {
        if (path.Length == 0) return false;
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[128];
        if (stream.Read(header) != header.Length || BitConverter.ToUInt32(header) != 0x20534444) return false;
        uint flags = BitConverter.ToUInt32(header[80..]);
        uint format = BitConverter.ToUInt32(header[84..]);
        if ((flags & 0x3) != 0 || format is 0x33545844 or 0x35545844) return true;
        if (format != 0x31545844) return false;
        // BC1 can encode cutouts without DDPF_ALPHAPIXELS. Check the top mip's
        // transparent palette entry, rather than treating every BC1 image as alpha.
        uint height = BitConverter.ToUInt32(header[12..]), width = BitConverter.ToUInt32(header[16..]);
        if (width is < 1 or > 16384 || height is < 1 or > 16384) return false;
        long remaining = ((width + 3L) / 4) * ((height + 3L) / 4) * 8;
        if (stream.Length - stream.Position < remaining) return false;
        Span<byte> blocks = stackalloc byte[8192];
        while (remaining > 0)
        {
            int length = (int)Math.Min(remaining, blocks.Length);
            stream.ReadExactly(blocks[..length]);
            for (int offset = 0; offset < length; offset += 8)
            {
                if (BitConverter.ToUInt16(blocks[offset..]) > BitConverter.ToUInt16(blocks[(offset + 2)..])) continue;
                uint indices = BitConverter.ToUInt32(blocks[(offset + 4)..]);
                if ((indices & (indices >> 1) & 0x55555555) != 0) return true;
            }
            remaining -= length;
        }
        return false;
    }

    private static float Amount(Dictionary<string, string> fields, string key) =>
        float.TryParse(fields.GetValueOrDefault(key), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
        && float.IsFinite(value) ? value : 0;

    private static SceneMaterial Fallback(string name) => new(name, "", "", Vector4.One, false, false, false, 0);
}
