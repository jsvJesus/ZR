using System.Numerics;
using System.Text;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Vegetation
{
    public static class SpeedTreeReader
    {
        public static IReadOnlyList<TreeCatalogEntry> Catalog(AssetPaths assets)
        {
            string root = Path.Combine(assets.Root, "speedtree");
            if (!Directory.Exists(root)) return [];
            return Directory.EnumerateFiles(root, "*.ctree", SearchOption.AllDirectories)
                .Select(p => new TreeCatalogEntry(Path.GetRelativePath(assets.Root, p).Replace('\\', '/'),
                    Path.GetRelativePath(root, p)[..^6].Replace('\\', '/')))
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public static TreeAsset Read(AssetPaths assets, string file, CancellationToken cancellationToken = default)
        {
            if (Path.GetExtension(file).Equals(".spt", StringComparison.OrdinalIgnoreCase)) file = Path.ChangeExtension(file, ".ctree");
            if (!file.StartsWith("speedtree/", StringComparison.OrdinalIgnoreCase) ||
                !Path.GetExtension(file).Equals(".ctree", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Ожидается дерево speedtree/*.ctree.");
            using var stream = File.OpenRead(AssetPathResolver.Resolve(assets.Root, file));
            if (stream.Length > 128 * 1024 * 1024) throw new InvalidDataException("Слишком большой CTREE.");
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            if (reader.ReadInt32() != 105) throw new InvalidDataException("Поддерживается CTREE версии 105.");
            _ = reader.ReadUInt32(); // Source checksum; CTREE105 adds it before the bounding box.
            Vector3 minimum = Vector(reader), maximum = Vector(reader);
            if (minimum.X > maximum.X || minimum.Y > maximum.Y || minimum.Z > maximum.Z)
                throw new InvalidDataException("Некорректные границы дерева.");
            // The cache stores generated geometry with Z negated, but retains the native bbox.
            float oldMinimumZ = minimum.Z;
            minimum.Z = -maximum.Z; maximum.Z = -oldMinimumZ;
            _ = Scalar(reader); _ = Scalar(reader); // Native leaf rock/rustle factors.
            var parts = new List<TreePart>();
            for (int part = 0; part < 3; part++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int count = Count(reader, 1_000_000), stride = part == 2 ? 100 : 64;
                Ensure(reader, (long)count * stride);
                var vertices = new TreeVertex[count];
                for (int i = 0; i < count; i++)
                {
                    if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                    var position = Vector(reader); var normal = Vector(reader); var uv = Pair(reader);
                    _ = Pair(reader); // Wind indices/weights, retained in the original file.
                    var card = Vector4.Zero; var leaf = new Vector2(-1, 1);
                    if (part == 2)
                    {
                        _ = Pair(reader); // Rest angle offsets for native leaf wind animation.
                        _ = Scalar(reader); float corner = Scalar(reader), dimming = Scalar(reader);
                        var size = Pair(reader); var pivot = Pair(reader);
                        if (corner < 0 || corner > 4 || corner != MathF.Truncate(corner) || size.X < 0 || size.Y < 0)
                            throw new InvalidDataException("Некорректная карточка листвы.");
                        card = new Vector4(size, pivot.X, pivot.Y);
                        leaf = new(corner < 4 ? corner : -1, Math.Clamp(dimming, 0, 1));
                    }
                    _ = Vector(reader); _ = Vector(reader); // Binormal and tangent.
                    vertices[i] = new(position, normal, uv, card, leaf);
                }
                int lodCount = Count(reader, 128);
                var lods = new int[lodCount][];
                for (int lod = 0; lod < lodCount; lod++)
                {
                    int indices = Count(reader, 10_000_000);
                    Ensure(reader, indices * 4L);
                    lods[lod] = new int[indices];
                    for (int i = 0; i < indices; i++)
                    {
                        uint index = reader.ReadUInt32();
                        if (index >= count) throw new InvalidDataException("Индекс дерева вне массива вершин.");
                        lods[lod][i] = (int)index;
                    }
                    if (part == 2 && indices % 3 != 0) throw new InvalidDataException("Неполный треугольник листвы.");
                }
                string diffuse = Texture(reader, assets), normalMap = Texture(reader, assets);
                if (vertices.Length > 0 && lods.Any(l => l.Length > 0))
                    parts.Add(new((TreePartKind)part, vertices, lods, diffuse, normalMap));
            }
            // The companion cache also contains the far billboard LODs. Read and validate them;
            // full tree geometry remains available without the native SpeedTree runtime.
            int billboardLods = Count(reader, 32);
            for (int lod = 0; lod < billboardLods; lod++)
            {
                int count = Count(reader, 100_000);
                Ensure(reader, count * 20L);
                for (int i = 0; i < count; i++) { _ = Vector(reader); _ = Pair(reader); }
                int strips = Count(reader, 1024);
                for (int strip = 0; strip < strips; strip++)
                {
                    int indices = Count(reader, 1_000_000); Ensure(reader, indices * 2L);
                    for (int i = 0; i < indices; i++)
                        if (reader.ReadUInt16() >= count) throw new InvalidDataException("Индекс billboard вне массива.");
                }
            }
            _ = Texture(reader, assets); _ = Texture(reader, assets);
            if (stream.Position != stream.Length) throw new InvalidDataException("Неизвестные данные в конце CTREE.");
            if (parts.Count == 0) throw new InvalidDataException("В CTREE отсутствует геометрия дерева.");
            foreach (var part in parts)
            foreach (var vertex in part.Vertices)
            {
                float radius = 0;
                if (vertex.Leaf.X >= 0)
                {
                    float dx = (0.5f + MathF.Abs(vertex.Card.Z)) * vertex.Card.X;
                    float dy = (0.5f + MathF.Abs(vertex.Card.W)) * vertex.Card.Y;
                    radius = MathF.Sqrt(dx * dx + dy * dy);
                }
                minimum = Vector3.Min(minimum, vertex.Position - new Vector3(radius));
                maximum = Vector3.Max(maximum, vertex.Position + new Vector3(radius));
            }
            return new(file, minimum, maximum, parts.ToArray());
        }

        private static string Texture(BinaryReader reader, AssetPaths assets)
        {
            int count = Count(reader, 4096); Ensure(reader, count);
            string path = Encoding.UTF8.GetString(reader.ReadBytes(count)).Replace('\\', '/');
            if (path.Length == 0) return "";
            if (!Path.GetExtension(path).Equals(".dds", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Неизвестная текстура дерева: {path}");
            return AssetPathResolver.Resolve(assets.Root, path);
        }
        private static int Count(BinaryReader reader, int maximum)
        {
            int value = reader.ReadInt32();
            return value >= 0 && value <= maximum ? value : throw new InvalidDataException("Некорректная длина CTREE.");
        }
        private static void Ensure(BinaryReader reader, long count)
        {
            if (count > reader.BaseStream.Length - reader.BaseStream.Position) throw new EndOfStreamException("Обрезанный CTREE.");
        }
        private static float Scalar(BinaryReader reader)
        {
            float value = reader.ReadSingle();
            return float.IsFinite(value) ? value : throw new InvalidDataException("Неконечное число CTREE.");
        }
        private static Vector2 Pair(BinaryReader reader) => new(Scalar(reader), Scalar(reader));
        private static Vector3 Vector(BinaryReader reader) => new(Scalar(reader), Scalar(reader), Scalar(reader));
    }
}
