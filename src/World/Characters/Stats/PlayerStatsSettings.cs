using System.Text.Json;

namespace ZoneReborn.World.Characters.Stats
{
    public sealed class PlayerStatsSettings
    {
        public float MaxHitPoints { get; init; } = 275f;
        public float MaxStamina { get; init; } = 200f;
        public float MaxWeight { get; init; } = 38f;

        public float HitPointsRegeneration { get; init; } = 0.2f;
        public float StaminaRegeneration { get; init; } = 5f;

        public float SprintStaminaUsage { get; init; } = 5f;
        public float JumpStaminaUsage { get; init; } = 20f;
        public float MinimumSprintStamina { get; init; } = 20f;

        public float SatietyDrainPerSecond { get; init; } = 0.0005f;
        public float HydrationDrainPerSecond { get; init; } = 0.00075f;

        public float HungryThreshold { get; init; } = 0.1f;
        public float ThirstThreshold { get; init; } = 0.1f;

        public float HungryRegenerationPenalty { get; init; } = 2f;
        public float ThirstRegenerationPenalty { get; init; } = 2f;

        public float CriticalHealthThreshold { get; init; } = 0.15f;

        public static PlayerStatsSettings Load(string path)
        {
            var settings = File.Exists(path)
                ? JsonSerializer.Deserialize<PlayerStatsSettings>(
                    File.ReadAllText(path),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                : new PlayerStatsSettings();

            if (settings == null)
            {
                throw new InvalidDataException(
                    "Player Stats settings are empty.");
            }

            settings.Validate();

            return settings;
        }

        public void Validate()
        {
            float[] values =
            [
                MaxHitPoints,
                MaxStamina,
                MaxWeight,
                HitPointsRegeneration,
                StaminaRegeneration,
                SprintStaminaUsage,
                JumpStaminaUsage,
                MinimumSprintStamina,
                SatietyDrainPerSecond,
                HydrationDrainPerSecond,
                HungryThreshold,
                ThirstThreshold,
                HungryRegenerationPenalty,
                ThirstRegenerationPenalty,
                CriticalHealthThreshold
            ];

            if (values.Any(value =>
                    !float.IsFinite(value) || value < 0f) ||
                MaxHitPoints <= 0f ||
                MaxStamina <= 0f ||
                MaxWeight <= 0f ||
                MinimumSprintStamina > MaxStamina ||
                JumpStaminaUsage > MaxStamina ||
                HungryThreshold > 1f ||
                ThirstThreshold > 1f ||
                CriticalHealthThreshold > 1f)
            {
                throw new InvalidDataException(
                    "Invalid Player Stats settings.");
            }
        }
    }
}
