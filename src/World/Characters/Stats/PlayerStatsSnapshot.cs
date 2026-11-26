namespace ZoneReborn.World.Characters.Stats
{
    public readonly record struct PlayerStatsSnapshot(
        float HitPoints,
        float MaxHitPoints,
        float Stamina,
        float MaxStamina,
        float Satiety,
        float Hydration,
        float CarryWeight,
        float MaxWeight,
        float HitPointsRegeneration,
        float StaminaRegeneration,
        int BleedingStacks,
        int RadiationStacks,
        int IntoxicationStacks,
        bool IsHungry,
        bool IsThirsty,
        bool IsOverloaded,
        bool IsCriticalHealth,
        bool IsAlive,
        bool CanSprint);
}
