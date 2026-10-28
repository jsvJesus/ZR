using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Editor.Roads
{
    public sealed class RoadImportResult
    {
        public RoadDocument Document { get; } = new();
        public List<string> Warnings { get; } = [];
    }

    public static class LegacyRoadImporter
    {
        private const int V4SamplesPerSegment = 50;

        private static readonly float[] CoordinateScales =
        [
            1f,
            0.5f,
            2f
        ];

        public static RoadImportResult Import(
            string sourceDirectory,
            string targetDirectory)
        {
            LevelInfo level = ReadLevelInfo(targetDirectory);

            string xmlPath = Path.Combine(
                sourceDirectory,
                "LevelData.xml");

            string roadsPath = Path.Combine(
                sourceDirectory,
                "roads");

            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException(
                    "В оригинальном уровне нет LevelData.xml.",
                    xmlPath);
            }

            if (!Directory.Exists(roadsPath))
            {
                throw new DirectoryNotFoundException(
                    $"В оригинальном уровне нет папки roads: {roadsPath}");
            }

            var result = new RoadImportResult();

            result.Document.Level = level.Name;
            result.Document.SourceLevel = Path.GetFileName(
                Path.TrimEndingDirectorySeparator(sourceDirectory));

            var roadFiles = Directory.EnumerateFiles(
                roadsPath,
                "*.dat",
                SearchOption.TopDirectoryOnly)
                .ToDictionary(
                    Path.GetFileNameWithoutExtension,
                    path => path,
                    StringComparer.OrdinalIgnoreCase);

            XDocument xml = XDocument.Load(xmlPath);

            var objects = xml.Descendants("object")
                .Where(element =>
                    (string?)element.Attribute("className") ==
                    "obj_Road");

            foreach (XElement element in objects)
            {
                string? id =
                    (string?)element.Attribute("fileName");

                if (string.IsNullOrWhiteSpace(id))
                {
                    result.Warnings.Add(
                        "Пропущена дорога без fileName.");
                    continue;
                }

                if (!roadFiles.TryGetValue(id, out string? path))
                {
                    result.Warnings.Add(
                        $"{id}: оригинальный .dat не найден.");
                    continue;
                }

                try
                {
                    RoadData road = ReadRoad(
                        path,
                        id,
                        element,
                        level);

                    result.Document.Roads.Add(road);

                    if (road.NeedsReview)
                    {
                        result.Warnings.Add(
                            $"{id}: требуется проверить " +
                            "положение или высоту дороги.");
                    }
                }
                catch (Exception exception)
                {
                    result.Warnings.Add(
                        $"{id}: {exception.Message}");
                }
            }

            return result;
        }

        private static LevelInfo ReadLevelInfo(
            string targetDirectory)
        {
            string path = Path.Combine(
                targetDirectory,
                "level.json");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "В целевом уровне не найден level.json.",
                    path);
            }

            using JsonDocument json = JsonDocument.Parse(
                File.ReadAllText(path));

            JsonElement root = json.RootElement;

            string? format =
                root.GetProperty("format").GetString();

            string? name =
                root.GetProperty("name").GetString();

            if (format != "ZoneReborn.Level" ||
                string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidDataException(
                    "Выбран неверный формат игрового уровня.");
            }

            JsonElement terrain = root.GetProperty("terrain");

            float sizeX = terrain
                .GetProperty("worldSizeX")
                .GetSingle();

            float sizeZ = terrain
                .GetProperty("worldSizeZ")
                .GetSingle();

            if (!float.IsFinite(sizeX) ||
                !float.IsFinite(sizeZ) ||
                sizeX <= 0f ||
                sizeZ <= 0f)
            {
                throw new InvalidDataException(
                    "Некорректные размеры уровня.");
            }

            return new LevelInfo(name, sizeX, sizeZ);
        }

        private static RoadData ReadRoad(
            string path,
            string id,
            XElement element,
            LevelInfo level)
        {
            XElement? position = element.Element("position");

            if (position == null)
            {
                throw new InvalidDataException(
                    "Нет координат объекта в LevelData.xml.");
            }

            float sourceX = ReadCoordinate(position, "x");
            float sourceY = ReadCoordinate(position, "y");
            float sourceZ = ReadCoordinate(position, "z");

            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(
                stream,
                Encoding.ASCII);

            ushort version = reader.ReadUInt16();

            return version switch
            {
                4 => ReadVersion4(
                    reader,
                    id,
                    element,
                    sourceX,
                    sourceY,
                    sourceZ,
                    level),

                _ => throw new InvalidDataException(
                    $"Формат исходной дороги версии {version} " +
                    "пока не поддерживается.")
            };
        }

        private static RoadData ReadVersion4(
            BinaryReader reader,
            string id,
            XElement element,
            float sourceX,
            float sourceY,
            float sourceZ,
            LevelInfo level)
        {
            int headerLength = reader.ReadInt32();
            int controlCount = reader.ReadInt32();

            float width = reader.ReadSingle();
            float originalParameter = reader.ReadSingle();

            int vertexCount = reader.ReadInt32();
            int indexCount = reader.ReadInt32();

            _ = reader.ReadInt32();

            if (headerLength < 30 ||
                headerLength > 4096 ||
                controlCount < 2 ||
                controlCount > 10000 ||
                vertexCount < 8 ||
                vertexCount > 10000000 ||
                indexCount < 18 ||
                indexCount > 30000000 ||
                !float.IsFinite(width) ||
                width <= 0f ||
                width > 200f)
            {
                throw new InvalidDataException(
                    "Повреждён заголовок исходной дороги.");
            }

            int expectedRows = checked(
                (controlCount - 1) *
                V4SamplesPerSegment + 1);

            if (vertexCount != expectedRows * 4 ||
                indexCount != (expectedRows - 1) * 18)
            {
                throw new InvalidDataException(
                    "Неподдерживаемая сетка дорожных сегментов.");
            }

            int materialLength = headerLength - 28;

            byte[] materialBytes =
                reader.ReadBytes(materialLength);

            if (materialBytes.Length != materialLength ||
                materialBytes[^1] != 0)
            {
                throw new InvalidDataException(
                    "Повреждено имя дорожного материала.");
            }

            string material = Encoding.ASCII.GetString(
                materialBytes,
                0,
                materialBytes.Length - 1);

            long expectedLength = checked(
                reader.BaseStream.Position +
                (long)controlCount * 28 +
                (long)vertexCount * 36 +
                (long)indexCount * 4);

            if (reader.BaseStream.Length != expectedLength)
            {
                throw new InvalidDataException(
                    "Размер файла не соответствует заголовку.");
            }

            XElement? settings = element.Element("road");

            var road = new RoadData
            {
                Id = id,
                Material = material,
                Width = width,
                OriginalParameter = originalParameter,
                SamplesPerSegment = V4SamplesPerSegment,
                DrawPriority = ReadPriority(settings),
                SourceX = sourceX,
                SourceY = sourceY,
                SourceZ = sourceZ
            };

            for (int i = 0; i < controlCount; i++)
            {
                float x = reader.ReadSingle();
                _ = reader.ReadSingle();
                float z = reader.ReadSingle();

                float tangentX = reader.ReadSingle();
                _ = reader.ReadSingle();
                float tangentZ = reader.ReadSingle();
                _ = reader.ReadSingle();

                if (!float.IsFinite(x) ||
                    !float.IsFinite(z) ||
                    !float.IsFinite(tangentX) ||
                    !float.IsFinite(tangentZ))
                {
                    throw new InvalidDataException(
                        "Некорректные контрольные точки.");
                }

                road.Points.Add(new RoadControlPoint
                {
                    X = x,
                    Z = z,
                    TangentX = tangentX,
                    TangentZ = tangentZ
                });
            }

            var centers = new List<GeometryCenter>(
                expectedRows);

            for (int row = 0; row < expectedRows; row++)
            {
                float centerX = 0f;
                float centerY = 0f;
                float centerZ = 0f;

                var heights = new float[4];

                for (int column = 0; column < 4; column++)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();

                    for (int value = 0; value < 5; value++)
                    {
                        _ = reader.ReadSingle();
                    }

                    _ = reader.ReadInt32();

                    if (!float.IsFinite(x) ||
                        !float.IsFinite(y) ||
                        !float.IsFinite(z))
                    {
                        throw new InvalidDataException(
                            "Некорректная вершина дорожной поверхности.");
                    }

                    centerX += x * 0.25f;
                    centerY += y * 0.25f;
                    centerZ += z * 0.25f;

                    heights[column] = y;
                }

                centers.Add(new GeometryCenter(
                    centerX,
                    centerY,
                    centerZ));

                road.HeightRows.Add(new RoadHeightRow
                {
                    Y0 = heights[0],
                    Y1 = heights[1],
                    Y2 = heights[2],
                    Y3 = heights[3]
                });
            }

            for (int i = 0; i < indexCount; i++)
            {
                uint index = reader.ReadUInt32();

                if (index >= vertexCount)
                {
                    throw new InvalidDataException(
                        "Повреждены индексы дорожной сетки.");
                }
            }

            RoadAlignment alignment = FindAlignment(
                centers,
                sourceX,
                sourceZ);

            const float minimumTolerance = 40f;

            float tolerance = MathF.Max(
                minimumTolerance,
                width * 6f);

            bool alignmentFailed =
                alignment.Distance > tolerance;

            float scale = alignmentFailed
                ? 1f
                : alignment.Scale;

            int closestRow = alignmentFailed
                ? FindClosestRow(
                    centers,
                    sourceX,
                    sourceZ,
                    1f).RowIndex
                : alignment.RowIndex;

            road.SourceScaleXZ = scale;
            road.Width *= scale;

            foreach (RoadControlPoint point in road.Points)
            {
                point.X *= scale;
                point.Z *= scale;

                point.TangentX *= scale;
                point.TangentZ *= scale;
            }

            float heightOffset =
                sourceY - centers[closestRow].Y;

            road.SourceHeightOffset = heightOffset;

            foreach (RoadHeightRow row in road.HeightRows)
            {
                row.Move(heightOffset);
            }

            for (int i = 0; i < road.Points.Count; i++)
            {
                float height = road.HeightRows[
                    i * road.SamplesPerSegment].CenterHeight;

                road.Points[i].Y = height;
                road.Points[i].BaseY = height;
            }

            const float boundsTolerance = 10f;

            bool outsideLevel = road.Points.Any(point =>
                point.X < -boundsTolerance ||
                point.Z < -boundsTolerance ||
                point.X > level.WorldSizeX + boundsTolerance ||
                point.Z > level.WorldSizeZ + boundsTolerance);

            road.NeedsReview =
                alignmentFailed ||
                MathF.Abs(heightOffset) > 5f ||
                outsideLevel;

            return road;
        }

        private static RoadAlignment FindAlignment(
            IReadOnlyList<GeometryCenter> centers,
            float sourceX,
            float sourceZ)
        {
            RoadAlignment best = new(
                1f,
                0,
                float.MaxValue);

            foreach (float scale in CoordinateScales)
            {
                RoadAlignment candidate = FindClosestRow(
                    centers,
                    sourceX,
                    sourceZ,
                    scale);

                if (candidate.Distance < best.Distance)
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static RoadAlignment FindClosestRow(
            IReadOnlyList<GeometryCenter> centers,
            float sourceX,
            float sourceZ,
            float scale)
        {
            float bestDistanceSquared = float.MaxValue;
            int bestIndex = 0;

            for (int i = 0; i < centers.Count; i++)
            {
                float dx = centers[i].X * scale - sourceX;
                float dz = centers[i].Z * scale - sourceZ;

                float distanceSquared = dx * dx + dz * dz;

                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestIndex = i;
                }
            }

            return new RoadAlignment(
                scale,
                bestIndex,
                MathF.Sqrt(bestDistanceSquared));
        }

        private static float ReadCoordinate(
            XElement element,
            string name)
        {
            string? value =
                (string?)element.Attribute(name);

            if (value == null ||
                !float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float result) ||
                !float.IsFinite(result))
            {
                throw new InvalidDataException(
                    $"Некорректная координата {name}.");
            }

            return result;
        }

        private static int ReadPriority(XElement? element)
        {
            string? value = (string?)element?
                .Attribute("draw_priority");

            return int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int priority)
                ? priority
                : 0;
        }

        private readonly record struct LevelInfo(
            string Name,
            float WorldSizeX,
            float WorldSizeZ);

        private readonly record struct GeometryCenter(
            float X,
            float Y,
            float Z);

        private readonly record struct RoadAlignment(
            float Scale,
            int RowIndex,
            float Distance);
    }
}
