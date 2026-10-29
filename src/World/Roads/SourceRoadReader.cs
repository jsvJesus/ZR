using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Roads
{
    public static class SourceRoadReader
    {
        public static RoadNetwork Read(string directory, CancellationToken cancellationToken = default)
        {
            var network = new RoadNetwork();
            string xmlPath = Path.Combine(directory, "LevelData.xml");
            if (!File.Exists(xmlPath))
            {
                network.Warnings.Add($"Не найден источник дорог: {xmlPath}");
                return network;
            }
            network.Materials.AddRange(RoadMaterials.Read(Path.Combine(directory, "room.mat")));
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in XDocument.Load(xmlPath).Descendants("object")
                .Where(e => (string?)e.Attribute("className") == "obj_Road"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string id = (string?)entry.Attribute("fileName") ?? "";
                if (!seen.Add(id))
                {
                    network.Warnings.Add($"{id}: повторная ссылка XML; DAT отображается один раз.");
                    continue;
                }
                try
                {
                    var position = entry.Element("position") ?? throw new InvalidDataException("Нет позиции XML.");
                    var anchor = new Vector3(Number(position, "x"), Number(position, "y"), Number(position, "z"));
                    int priority = int.TryParse((string?)entry.Element("road")?.Attribute("draw_priority"),
                        NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
                    var road = ReadDat(AssetPathResolver.Resolve(Path.Combine(directory, "roads"), id + ".dat"), id, anchor, priority);
                    network.Roads.Add(road);
                    if (Vector3.Distance(road.Nodes[0].Position, anchor) > 50)
                        network.Warnings.Add($"{id}: позиция XML отличается от запечённой геометрии; сохранены координаты DAT.");
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or FormatException or OverflowException)
                {
                    network.Warnings.Add($"{id}: {exception.Message}");
                }
            }
            return network;
        }

        private static SourceRoad ReadDat(string path, string id, Vector3 anchor, int priority)
        {
            byte[] data = File.ReadAllBytes(path);
            using var stream = new MemoryStream(data, false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadUInt16() != 4) throw new InvalidDataException("Поддерживается DAT версии 4.");
            int header = reader.ReadInt32(), count = reader.ReadInt32();
            float width = Scalar(reader);
            _ = Scalar(reader); // Longitudinal texture length; original UVs are already baked.
            int vertexCount = reader.ReadInt32(), indexCount = reader.ReadInt32();
            _ = reader.ReadInt32();
            if (header is < 29 or > 4096 || count is < 2 or > 10000 || width <= 0 || width > 1000)
                throw new InvalidDataException("Некорректный заголовок дороги.");
            int rows = checked((count - 1) * SourceRoad.SamplesPerSegment + 1);
            long expected = 2L + header + count * 28L + vertexCount * 36L + indexCount * 4L;
            if (vertexCount != rows * 4 || indexCount != (rows - 1) * 18 || expected != data.Length)
                throw new InvalidDataException("Размер DAT не соответствует сетке сегментов.");
            byte[] name = reader.ReadBytes(header - 28);
            if (name[^1] != 0) throw new InvalidDataException("Нет терминатора материала.");
            string material = Encoding.ASCII.GetString(name, 0, name.Length - 1);
            var nodes = new RoadNode[count];
            for (int i = 0; i < count; i++)
            {
                float x = Scalar(reader); _ = Scalar(reader); float z = Scalar(reader);
                float tx = Scalar(reader); _ = Scalar(reader); float tz = Scalar(reader); _ = Scalar(reader);
                nodes[i] = new() { X = x, Z = z, TangentX = tx, TangentZ = tz, Width = width };
            }
            var vertices = new RoadVertex[vertexCount];
            for (int i = 0; i < vertices.Length; i++)
            {
                var p = new Vector3(Scalar(reader), Scalar(reader), Scalar(reader));
                var uv = new Vector2(Scalar(reader), Scalar(reader));
                var normal = new Vector3(Scalar(reader), Scalar(reader), Scalar(reader));
                _ = reader.ReadUInt32(); // Source edge/selection flags.
                vertices[i] = new(p, normal, uv);
            }
            var indices = new int[indexCount];
            for (int i = 0; i < indices.Length; i++)
            {
                uint index = reader.ReadUInt32();
                if (index >= vertexCount) throw new InvalidDataException("Индекс дороги за пределами меша.");
                // Segments must share only their boundary row, never reference unrelated vertices.
                int segment = i / (SourceRoad.SamplesPerSegment * 18);
                int local = (int)index - segment * SourceRoad.SamplesPerSegment * 4;
                if (local < 0 || local >= (SourceRoad.SamplesPerSegment + 1) * 4)
                    throw new InvalidDataException("Индекс выходит за пределы сегмента.");
                indices[i] = (int)index;
            }
            // Control points contain Y=0; obtain editing heights from the baked cross sections.
            for (int i = 0; i < count; i++)
                nodes[i].Y = Enumerable.Range(0, 4).Sum(c => vertices[(i * SourceRoad.SamplesPerSegment) * 4 + c].Position.Y) / 4;
            return new SourceRoad
            {
                Id = id, Fingerprint = Convert.ToHexString(SHA256.HashData(data)), Material = material,
                Vertices = vertices, Indices = indices, OriginalNodes = nodes.Select(n => n.Copy()).ToArray(),
                Nodes = nodes, XmlPosition = anchor, DrawPriority = priority,
                Segments = Enumerable.Range(0, count - 1).Select(i => new RoadSegment(i) { Material = material }).ToArray()
            };
        }

        private static float Scalar(BinaryReader reader)
        {
            float value = reader.ReadSingle();
            return float.IsFinite(value) ? value : throw new InvalidDataException("Неконечное число в DAT.");
        }

        private static float Number(XElement e, string name) =>
            float.TryParse((string?)e.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                && float.IsFinite(value) ? value : throw new InvalidDataException($"Некорректная координата XML {name}.");
    }
}
