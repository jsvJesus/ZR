using System.Text.Json.Serialization;

namespace ZoneReborn.World.Actors
{
    public sealed class ActorDocument
    {
        public int Version { get; init; } = 1;

        public List<LevelActor> Actors { get; init; } = [];

        [JsonIgnore]
        public long Revision { get; private set; }

        [JsonIgnore]
        public long SavedRevision { get; private set; }

        [JsonIgnore]
        public bool HasPendingChanges =>
            Revision != SavedRevision;

        public LevelActor? Find(Guid id)
        {
            return Actors.FirstOrDefault(actor => actor.Id == id);
        }

        public void Add(LevelActor actor)
        {
            ArgumentNullException.ThrowIfNull(actor);

            if (actor.Id == Guid.Empty || Find(actor.Id) != null)
            {
                throw new InvalidOperationException(
                    "Некорректный или повторный ID актора.");
            }

            Actors.Add(actor);
            Touch();
        }

        public bool Remove(Guid id)
        {
            LevelActor? actor = Find(id);

            if (actor == null)
            {
                return false;
            }

            Actors.Remove(actor);
            Touch();

            return true;
        }

        public void Touch()
        {
            Revision++;
        }

        internal void MarkSaved(long revision)
        {
            SavedRevision = revision;
        }
    }
}
