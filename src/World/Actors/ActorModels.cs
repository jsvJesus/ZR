using System.Numerics;
using System.Text.Json.Serialization;

namespace ZoneReborn.World.Actors
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ActorKind
    {
        PlayerSpawn,
        NpcSpawn,
        MobSpawn,
        LootSpawn
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LootRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [Flags]
    public enum NpcCapabilities
    {
        None = 0,
        CanTalk = 1,
        QuestGiver = 2,
        NightHighlight = 4,
        CanTrade = 8
    }

    public sealed class LootSpawnEntry
    {
        public string Category { get; set; } = "";
        public int TypeId { get; set; }

        public decimal Chance { get; set; } = 100;
        public LootRarity Rarity { get; set; } = LootRarity.Common;

        public int MinCount { get; set; } = 1;
        public int MaxCount { get; set; } = 1;
    }

    public sealed class LevelActor
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public ActorKind Kind { get; init; }

        public string Name { get; set; } = "";
        public bool Enabled { get; set; } = true;

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public float Yaw { get; set; }

        public string TemplateId { get; set; } = "";

        public int NpcId { get; set; }

        public NpcCapabilities NpcFlags { get; set; } = NpcCapabilities.CanTalk;

        public string NpcHeadPreset { get; set; } = "";

        public NpcAppearancePreset NpcAppearance { get; set; } = NpcAppearancePreset.Civilian;

        public string NpcBarterList { get; set; } = "";

        public int NpcFactionId { get; set; }

        public float Radius { get; set; } = 0.65f;
        public int Count { get; set; } = 1;
        public int RespawnSeconds { get; set; } = 60;

        public List<LootSpawnEntry> Loot { get; set; } = [];

        [JsonIgnore]
        public Vector3 Position
        {
            get => new(X, Y, Z);

            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        public static LevelActor Create(
            ActorKind kind,
            Vector3 position,
            float yaw)
        {
            return new LevelActor
            {
                Kind = kind,
                Position = position,
                Yaw = MathF.IEEERemainder(yaw, MathF.Tau),

                Name = kind switch
                {
                    ActorKind.PlayerSpawn => "Player Spawn",
                    ActorKind.NpcSpawn => "NPC Spawn",
                    ActorKind.MobSpawn => "Mob Spawn",
                    ActorKind.LootSpawn => "Loot Spawn",
                    _ => "Actor"
                },

                Radius = kind == ActorKind.MobSpawn
                    ? 5f
                    : 0.65f,

                RespawnSeconds = kind switch
                {
                    ActorKind.MobSpawn => 300,
                    ActorKind.LootSpawn => 600,
                    _ => 60
                }
            };
        }
    }
}
