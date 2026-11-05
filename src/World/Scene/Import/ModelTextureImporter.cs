using System.Buffers.Binary;
using System.Security.Cryptography;
using Assimp;
using StbImageSharp;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Textures.Dds;

namespace ZoneReborn.World.Scene.Import;

internal sealed class ModelTextureImporter
{
    private readonly AssetPaths _assets;
    private readonly string[] _searchRoots;
    private readonly CancellationToken _token;
    private readonly Dictionary<string, string> _resolved = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _outputs = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, List<string>>? _fileIndex;

    public int TextureCount => _outputs.Count;

    public ModelTextureImporter(AssetPaths assets, IEnumerable<string> extraRoots, CancellationToken token)
    {
        _assets = assets;
        _token = token;
        _searchRoots = new[] { assets.ImportModels }.Concat(extraRoots)
            .Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public string Import(Assimp.Scene scene, string model, string reference)
    {
        _token.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(reference)) throw new InvalidDataException("Пустая ссылка на текстуру FBX.");
        string key = model + "\n" + reference;
        if (_resolved.TryGetValue(key, out string? cached)) return cached;
        EmbeddedTexture? embedded = scene.GetEmbeddedTexture(reference);
        byte[] bytes;
        string name;
        if (embedded != null)
        {
            name = string.IsNullOrWhiteSpace(embedded.Filename) ? "embedded" + reference.TrimStart('*') : embedded.Filename;
            if (embedded.IsCompressed)
                bytes = ToDds(embedded.CompressedData);
            else
            {
                var rgba = new byte[checked(embedded.Width * embedded.Height * 4)];
                if (embedded.NonCompressedData.Length != rgba.Length / 4)
                    throw new InvalidDataException("Некорректная встроенная текстура FBX.");
                for (int index = 0; index < embedded.NonCompressedData.Length; index++)
                {
                    if ((index & 4095) == 0) _token.ThrowIfCancellationRequested();
                    var pixel = embedded.NonCompressedData[index];
                    rgba[index * 4] = pixel.R; rgba[index * 4 + 1] = pixel.G;
                    rgba[index * 4 + 2] = pixel.B; rgba[index * 4 + 3] = pixel.A;
                }
                bytes = RgbaDds(embedded.Width, embedded.Height, rgba);
            }
        }
        else
        {
            string source = Resolve(model, reference);
            name = source;
            bytes = ToDds(File.ReadAllBytes(source));
        }
        _token.ThrowIfCancellationRequested();
        string basename = Path.GetFileNameWithoutExtension(name.Replace('\\', '/'));
        var invalid = Path.GetInvalidFileNameChars();
        basename = new string(basename.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        if (string.IsNullOrWhiteSpace(basename)) basename = "texture";
        if (basename.Length > 80) basename = basename[..80];
        string hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        string destination = AssetPathResolver.Resolve(Path.Combine(_assets.Models2, "Tex"), $"{basename}-{hash}.dds");
        Publish(destination, bytes);
        _outputs.Add(destination);
        _resolved.Add(key, destination);
        return destination;
    }

    private string Resolve(string model, string reference)
    {
        string path = reference.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        if (reference.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(reference, UriKind.Absolute, out var uri) && uri.IsFile) path = uri.LocalPath;
        string directory = Path.GetDirectoryName(model)!;
        string name = Path.GetFileName(path);
        var candidates = new List<string>();
        if (Path.IsPathRooted(path)) candidates.Add(path);
        else candidates.Add(Path.GetFullPath(Path.Combine(directory, path)));
        candidates.Add(Path.Combine(directory, name));
        candidates.Add(Path.Combine(directory, "Tex", name));
        candidates.Add(Path.Combine(directory, "textures", name));
        candidates.Add(Path.Combine(directory, Path.GetFileNameWithoutExtension(model) + ".fbm", name));
        foreach (string root in _searchRoots)
        {
            if (!Path.IsPathRooted(path)) candidates.Add(Path.GetFullPath(Path.Combine(root, path)));
            candidates.Add(Path.Combine(root, name));
        }
        foreach (string candidate in candidates)
        {
            _token.ThrowIfCancellationRequested();
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);
        }
        BuildIndex();
        if (_fileIndex!.TryGetValue(name, out var matches))
        {
            if (matches.Count == 1) return matches[0];
            throw new InvalidDataException($"Неоднозначная текстура {reference}: {string.Join("; ", matches.Take(5))}. " +
                "Выберите более точную папку текстур.");
        }
        throw new FileNotFoundException($"Не найдена текстура {reference}. Выберите папку текстур и повторите импорт.", reference);
    }

    private void BuildIndex()
    {
        if (_fileIndex != null) return;
        _fileIndex = new(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in _searchRoots.Where(Directory.Exists))
        {
            foreach (string file in Directory.EnumerateFiles(root, "*", new EnumerationOptions
            {
                RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint
            }))
            {
                _token.ThrowIfCancellationRequested();
                if (!seen.Add(file)) continue;
                string name = Path.GetFileName(file);
                if (!_fileIndex.TryGetValue(name, out var matches)) _fileIndex.Add(name, matches = []);
                matches.Add(file);
            }
        }
    }

    private void Publish(string destination, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (File.Exists(destination))
        {
            // Content-addressed files are immutable. Detect manually damaged assets.
            if (!SHA256.HashData(File.ReadAllBytes(destination)).AsSpan().SequenceEqual(SHA256.HashData(bytes)))
                throw new InvalidDataException($"Повреждена ранее импортированная текстура: {destination}");
            return;
        }
        string temporary = destination + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllBytes(temporary, bytes);
            _ = DdsReader.Read(temporary); // Reject images the renderer cannot load before publishing the model.
            _token.ThrowIfCancellationRequested();
            File.Move(temporary, destination);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static byte[] ToDds(byte[] bytes)
    {
        if (bytes.Length >= 4 && BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x20534444) return bytes;
        var image = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);
        return RgbaDds(image.Width, image.Height, image.Data);
    }

    private static byte[] RgbaDds(int width, int height, byte[] rgba)
    {
        if (width <= 0 || height <= 0 || width > 16384 || height > 16384 || rgba.Length != checked(width * height * 4))
            throw new InvalidDataException("Некорректный размер текстуры FBX.");
        var bytes = new byte[checked(128 + rgba.Length)];
        Put(0, 0x20534444); Put(4, 124); Put(8, 0x100F);
        Put(12, (uint)height); Put(16, (uint)width); Put(20, (uint)(width * 4));
        Put(76, 32); Put(80, 0x41); Put(88, 32);
        Put(92, 0x000000FF); Put(96, 0x0000FF00); Put(100, 0x00FF0000); Put(104, 0xFF000000);
        Put(108, 0x1000);
        rgba.CopyTo(bytes, 128);
        return bytes;

        void Put(int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, 4), value);
    }
}
