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

        foreach (RoadPath road in document.Roads)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (road == null ||
                road.Id == Guid.Empty ||
                !ids.Add(road.Id) ||
                string.IsNullOrWhiteSpace(road.Name) ||
                !Enum.IsDefined(road.Category) ||
                road.Points == null ||
                road.Structures == null ||
                road.Points.Count > RoadNetwork.MaximumPointsPerRoad ||
                road.Structures.Count > RoadNetwork.MaximumPointsPerRoad ||
                !float.IsFinite(road.Width) ||
                road.Width is < 0.5f or > 40f ||
                !float.IsFinite(road.ShoulderWidth) ||
                road.ShoulderWidth is < 0f or > 8f)
            {
                throw new InvalidDataException(
                    "Некорректный дорожный маршрут.");
            }

            foreach (RoadPoint point in road.Points)
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

            foreach (RoadStructure structure in road.Structures)
            {
                if (structure == null ||
                    structure.Id == Guid.Empty ||
                    !Enum.IsDefined(structure.Kind) ||
                    structure.StartIndex < 0 ||
                    structure.EndIndex != structure.StartIndex + 1 ||
                    structure.EndIndex >= road.Points.Count)
                {
                    throw new InvalidDataException(
                        "Некорректные точки моста или туннеля.");
                }

                string prefix = structure.Kind == RoadStructureKind.Bridge
                    ? "objects/props/bridges/"
                    : "objects/props/tunnels/";

                if (!ValidAsset(structure.Model, prefix, ".model"))
                {
                    throw new InvalidDataException(
                        "Некорректная модель моста или туннеля.");
                }
            }

            if ((!string.IsNullOrEmpty(road.Material) &&
                 !ValidAsset(road.Material, "textures/roads/", ".dds")) ||
                (!string.IsNullOrEmpty(road.ShoulderMaterial) &&
                 !ValidAsset(road.ShoulderMaterial, "textures/roads/", ".dds")))
            {
                throw new InvalidDataException(
                    "Некорректный материал дороги.");
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
                Category = road.Category,
                Model = road.Model,
                Material = road.Material,
                ShoulderMaterial = road.ShoulderMaterial,
                Width = road.Width,
                ShoulderWidth = road.ShoulderWidth,
                Points = [.. road.Points],
                Structures = road.Structures.Select(structure =>
                    new RoadStructure
                    {
                        Id = structure.Id,
                        Kind = structure.Kind,
                        StartIndex = structure.StartIndex,
                        EndIndex = structure.EndIndex,
                        Model = structure.Model
                    }).ToList()
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

    private static bool ValidAsset(
        string file,
        string prefix,
        string extension)
    {
        return !string.IsNullOrWhiteSpace(file) &&
               file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
               file.EndsWith(extension, StringComparison.OrdinalIgnoreCase) &&
               !file.Contains("..", StringComparison.Ordinal) &&
               !file.Contains('\\') &&
               !file.Contains(':');
    }

    public sealed class RoadDocument
    {
        public string Format { get; init; } = "Roads";
        public int Version { get; init; } = 1;
        public string Level { get; init; } = "";
        public RoadPath[] Roads { get; init; } = [];
    }
}
