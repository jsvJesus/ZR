using System.Text.Json;

namespace Editor.Roads
{
    public sealed class RoadDocument
    {
        public string Format { get; set; } = "RoadNetwork";
        public int Version { get; set; } = 1;
        public string Level { get; set; } = "";
        public string SourceLevel { get; set; } = "";

        public List<RoadData> Roads { get; set; } = [];
    }

    public sealed class RoadData
    {
        public string Id { get; set; } = "";
        public string Material { get; set; } = "";

        public float Width { get; set; }
        public float OriginalParameter { get; set; }
        public int DrawPriority { get; set; }

        public int SamplesPerSegment { get; set; } = 50;

        public float SourceX { get; set; }
        public float SourceY { get; set; }
        public float SourceZ { get; set; }

        public float SourceScaleXZ { get; set; } = 1f;
        public float SourceHeightOffset { get; set; }

        public bool NeedsReview { get; set; }

        public List<RoadControlPoint> Points { get; set; } = [];
        public List<RoadHeightRow> HeightRows { get; set; } = [];

        public override string ToString()
        {
            return NeedsReview
                ? $"{Id} [Проверить]"
                : Id;
        }
    }

    public sealed class RoadControlPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public float TangentX { get; set; }
        public float TangentZ { get; set; }

        public float BaseY { get; set; }

        public override string ToString()
        {
            return $"X {X:F1} / Y {Y:F1} / Z {Z:F1}";
        }
    }

    public sealed class RoadHeightRow
    {
        public float Y0 { get; set; }
        public float Y1 { get; set; }
        public float Y2 { get; set; }
        public float Y3 { get; set; }

        public float GetHeight(int index)
        {
            return index switch
            {
                0 => Y0,
                1 => Y1,
                2 => Y2,
                3 => Y3,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }

        public float CenterHeight =>
            (Y0 + Y1 + Y2 + Y3) * 0.25f;

        public void Move(float offset)
        {
            Y0 += offset;
            Y1 += offset;
            Y2 += offset;
            Y3 += offset;
        }
    }

    public static class RoadStorage
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        public static async Task SaveAsync(
            Stream stream,
            RoadDocument document)
        {
            if (stream.CanSeek)
            {
                stream.SetLength(0);
            }

            await JsonSerializer.SerializeAsync(
                stream,
                document,
                Options);
        }

        public static async Task<RoadDocument> LoadAsync(
            Stream stream)
        {
            RoadDocument? document =
                await JsonSerializer.DeserializeAsync<RoadDocument>(
                    stream,
                    Options);

            if (document == null ||
                document.Format != "RoadNetwork" ||
                document.Version != 1 ||
                document.Roads == null)
            {
                throw new InvalidDataException(
                    "Неподдерживаемый формат дорожной сети.");
            }

            foreach (RoadData road in document.Roads)
            {
                if (road.Points == null ||
                    road.HeightRows == null ||
                    road.SamplesPerSegment < 1 ||
                    road.SamplesPerSegment > 200 ||
                    road.Points.Count < 2)
                {
                    throw new InvalidDataException(
                        $"Некорректная дорога {road.Id}.");
                }

                int expectedRows =
                    (road.Points.Count - 1) *
                    road.SamplesPerSegment + 1;

                if (road.HeightRows.Count != expectedRows)
                {
                    throw new InvalidDataException(
                        $"Повреждён профиль дороги {road.Id}.");
                }
            }

            return document;
        }
    }
}
