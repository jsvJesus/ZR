using ZoneReborn.World.Levels.Storage;

namespace ZoneReborn.World.Roads;

public static class RoadStorage
{
    public const string FileName = "roads.json";

    public static async Task<RoadNetwork> LoadAsync(
        string directory,
        string levelName,
        CancellationToken cancellationToken = default)
    {
        var network = new RoadNetwork();
        string path = Path.Combine(directory, FileName);

        if (!File.Exists(path))
        {
            return network;
        }

        if (new FileInfo(path).Length > 32L * 1024 * 1024)
        {
            throw new InvalidDataException(
                "Файл дорог превышает допустимый размер.");
        }

        var document = await LevelJson.ReadAsync<RoadDocument>(
            path,
            cancellationToken);

        if (document.Format != "Roads" ||
            document.Version != 1 ||
            document.Level != levelName ||
            document.Roads == null ||
            document.Roads.Length > RoadNetwork.MaximumRoads)
        {
            throw new InvalidDataException(
                "Некорректный документ дорожной сети.");
        }

        var ids = new HashSet<Guid>();

        foreach (var road in document.Roads)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (road == null ||
                road.Id == Guid.Empty ||
                !ids.Add(road.Id) ||
                string.IsNullOrWhiteSpace(road.Name) ||
                road.Points == null ||
                road.Points.Count > RoadNetwork.MaximumPointsPerRoad)
            {
                throw new InvalidDataException(
                    "Некорректный дорожный маршрут.");
            }

            foreach (var point in road.Points)
            {
                if (!float.IsFinite(point.X) ||
                    !float.IsFinite(point.Z) ||
                    MathF.Abs(point.X) > 1e7f ||
                    MathF.Abs(point.Z) > 1e7f)
                {
                    throw new InvalidDataException(
                        "Некорректные координаты дорожной точки.");
                }
            }
        }

        network.Load(document.Roads);
        return network;
    }

    public static async Task SaveAsync(
        string directory,
        string levelName,
        RoadNetwork network)
    {
        if (!network.IsDirty)
        {
            return;
        }

        long revision = network.Revision;

        var document = new RoadDocument
        {
            Level = levelName,
            Roads = network.Roads.Select(road => new RoadPath
            {
                Id = road.Id,
                Name = road.Name,
                Points = [.. road.Points]
            }).ToArray()
        };

        string temporary = Path.Combine(
            directory,
            $".roads-{Guid.NewGuid():N}.tmp");

        try
        {
            await LevelJson.WriteAsync(temporary, document);

            File.Move(
                temporary,
                Path.Combine(directory, FileName),
                true);

            network.MarkSaved(revision);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    public sealed class RoadDocument
    {
        public string Format { get; init; } = "Roads";
        public int Version { get; init; } = 1;
        public string Level { get; init; } = "";
        public RoadPath[] Roads { get; init; } = [];
    }
}
