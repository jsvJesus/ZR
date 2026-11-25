using ZoneReborn.World.Levels.Storage;

namespace ZoneReborn.World.Actors
{
    public static class ActorStorage
    {
        public const string FileName = "actors.json";

        public static async Task<ActorDocument> LoadAsync(
            string directory,
            CancellationToken cancellationToken = default)
        {
            string path = Path.Combine(directory, FileName);

            if (!File.Exists(path))
            {
                return new ActorDocument();
            }

            var document = await LevelJson.ReadAsync<ActorDocument>(
                path,
                cancellationToken);

            Validate(document);

            return document;
        }

        public static async Task SaveAsync(
            string directory,
            ActorDocument document,
            CancellationToken cancellationToken = default)
        {
            Validate(document);

            long revision = document.Revision;

            string path = Path.Combine(directory, FileName);

            string temporary = Path.Combine(
                directory,
                $".actors-{Guid.NewGuid():N}.tmp");

            try
            {
                await LevelJson.WriteAsync(
                    temporary,
                    document,
                    cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                File.Move(temporary, path, true);

                document.MarkSaved(revision);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        private static void Validate(ActorDocument document)
        {
            if (document.Version != 1 || document.Actors == null)
            {
                throw new InvalidDataException(
                    "Неподдерживаемый формат акторов.");
            }

            var ids = new HashSet<Guid>();

            foreach (LevelActor actor in document.Actors)
            {
                if (actor == null ||
                    actor.Id == Guid.Empty ||
                    !ids.Add(actor.Id) ||
                    !Enum.IsDefined(actor.Kind) ||
                    string.IsNullOrWhiteSpace(actor.Name) ||
                    !float.IsFinite(actor.X) ||
                    !float.IsFinite(actor.Y) ||
                    !float.IsFinite(actor.Z) ||
                    !float.IsFinite(actor.Yaw) ||
                    !float.IsFinite(actor.Radius) ||
                    MathF.Abs(actor.X) > 10000000f ||
                    MathF.Abs(actor.Y) > 10000000f ||
                    MathF.Abs(actor.Z) > 10000000f ||
                    MathF.Abs(actor.Yaw) > MathF.Tau ||
                    actor.Radius < 0.1f ||
                    actor.Radius > 100f ||
                    actor.Count is < 1 or > 1000 ||
                    actor.RespawnSeconds is < 0 or > 604800 ||
                    actor.Loot == null)
                {
                    throw new InvalidDataException(
                        "Некорректные параметры актора.");
                }

                foreach (LootSpawnEntry entry in actor.Loot)
                {
                    if (entry == null ||
                        entry.TypeId <= 0 ||
                        string.IsNullOrWhiteSpace(entry.Category) ||
                        entry.Chance is < 0 or > 100 ||
                        !Enum.IsDefined(entry.Rarity) ||
                        entry.MinCount < 1 ||
                        entry.MaxCount < entry.MinCount ||
                        entry.MaxCount > 10000)
                    {
                        throw new InvalidDataException(
                            $"Некорректный лут актора {actor.Name}.");
                    }
                }
            }
        }
    }
}
