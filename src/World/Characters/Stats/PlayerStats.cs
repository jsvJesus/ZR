namespace ZoneReborn.World.Characters.Stats
{
    public sealed class PlayerStats
    {
        private const float RegenerationInterval = 1f;

        private readonly PlayerStatsSettings _settings;

        private float _regenerationElapsed;

        public float HitPoints { get; private set; }
        public float Stamina { get; private set; }

        public float Satiety { get; private set; } = 1f;
        public float Hydration { get; private set; } = 1f;

        public float CarryWeight { get; private set; }

        public int BleedingStacks { get; private set; }
        public int RadiationStacks { get; private set; }
        public int IntoxicationStacks { get; private set; }

        public float MaxHitPoints => _settings.MaxHitPoints;
        public float MaxStamina => _settings.MaxStamina;
        public float MaxWeight => _settings.MaxWeight;

        public StatModifier HealthRegenerationModifier { get; } = new();
        public StatModifier StaminaRegenerationModifier { get; } = new();

        public bool IsAlive => HitPoints > 0f;

        public bool IsHungry =>
            Satiety <= _settings.HungryThreshold;

        public bool IsThirsty =>
            Hydration <= _settings.ThirstThreshold;

        public bool IsOverloaded =>
            CarryWeight > MaxWeight;

        public bool IsCriticalHealth =>
            HitPoints / MaxHitPoints <=
            _settings.CriticalHealthThreshold;

        public bool CanSprint =>
            IsAlive &&
            !IsOverloaded &&
            Stamina >= _settings.MinimumSprintStamina;

        public bool CanJump =>
            IsAlive &&
            Stamina >= _settings.JumpStaminaUsage;

        public float HitPointsRegeneration =>
            HealthRegenerationModifier.Evaluate(
                _settings.HitPointsRegeneration);

        public float StaminaRegeneration
        {
            get
            {
                float value = _settings.StaminaRegeneration;

                if (IsHungry)
                {
                    value -= _settings.HungryRegenerationPenalty;
                }

                if (IsThirsty)
                {
                    value -= _settings.ThirstRegenerationPenalty;
                }

                return StaminaRegenerationModifier.Evaluate(value);
            }
        }

        public PlayerStatsSnapshot Snapshot => new(
            HitPoints,
            MaxHitPoints,
            Stamina,
            MaxStamina,
            Satiety,
            Hydration,
            CarryWeight,
            MaxWeight,
            HitPointsRegeneration,
            StaminaRegeneration,
            BleedingStacks,
            RadiationStacks,
            IntoxicationStacks,
            IsHungry,
            IsThirsty,
            IsOverloaded,
            IsCriticalHealth,
            IsAlive,
            CanSprint);

        public PlayerStats(PlayerStatsSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            settings.Validate();

            _settings = settings;

            Reset();
        }

        public void Reset()
        {
            HitPoints = MaxHitPoints;
            Stamina = MaxStamina;

            Satiety = 1f;
            Hydration = 1f;

            CarryWeight = 0f;

            BleedingStacks = 0;
            RadiationStacks = 0;
            IntoxicationStacks = 0;

            HealthRegenerationModifier.Clear();
            StaminaRegenerationModifier.Clear();

            _regenerationElapsed = 0f;
        }

        public void Update(
            float deltaTime,
            bool sprinting,
            bool grounded)
        {
            if (!IsAlive ||
                !float.IsFinite(deltaTime) ||
                deltaTime <= 0f)
            {
                return;
            }

            Satiety = Math.Clamp(
                Satiety -
                _settings.SatietyDrainPerSecond * deltaTime,
                0f,
                1f);

            Hydration = Math.Clamp(
                Hydration -
                _settings.HydrationDrainPerSecond * deltaTime,
                0f,
                1f);

            if (sprinting && grounded)
            {
                ConsumeStamina(
                    _settings.SprintStaminaUsage * deltaTime);
            }

            _regenerationElapsed += deltaTime;

            while (_regenerationElapsed >= RegenerationInterval)
            {
                _regenerationElapsed -= RegenerationInterval;

                if (!IsAlive)
                {
                    break;
                }

                HitPoints = Math.Clamp(
                    HitPoints + HitPointsRegeneration,
                    0f,
                    MaxHitPoints);

                if (grounded && !sprinting && IsAlive)
                {
                    Stamina = Math.Clamp(
                        Stamina + StaminaRegeneration,
                        0f,
                        MaxStamina);
                }
            }
        }

        public void ConsumeJumpStamina()
        {
            ConsumeStamina(
                _settings.JumpStaminaUsage);
        }

        public void ConsumeStamina(float amount)
        {
            ValidateAmount(amount);

            Stamina = Math.Clamp(
                Stamina - amount,
                0f,
                MaxStamina);
        }

        public void RestoreStamina(float amount)
        {
            ValidateAmount(amount);

            if (!IsAlive)
            {
                return;
            }

            Stamina = Math.Clamp(
                Stamina + amount,
                0f,
                MaxStamina);
        }

        public void ApplyDamage(float amount)
        {
            ValidateAmount(amount);

            if (!IsAlive)
            {
                return;
            }

            HitPoints = Math.Clamp(
                HitPoints - amount,
                0f,
                MaxHitPoints);
        }

        public void Heal(float amount)
        {
            ValidateAmount(amount);

            if (!IsAlive)
            {
                return;
            }

            HitPoints = Math.Clamp(
                HitPoints + amount,
                0f,
                MaxHitPoints);
        }

        public void RestoreSatiety(float amount)
        {
            ValidateAmount(amount);

            if (!IsAlive)
            {
                return;
            }

            Satiety = Math.Clamp(
                Satiety + amount,
                0f,
                1f);
        }

        public void RestoreHydration(float amount)
        {
            ValidateAmount(amount);

            if (!IsAlive)
            {
                return;
            }

            Hydration = Math.Clamp(
                Hydration + amount,
                0f,
                1f);
        }

        public void SetCarryWeight(float value)
        {
            ValidateAmount(value);

            CarryWeight = value;
        }

        public void SetBleedingStacks(int stacks)
        {
            BleedingStacks = Math.Max(0, stacks);
        }

        public void SetRadiationStacks(int stacks)
        {
            RadiationStacks = Math.Max(0, stacks);
        }

        public void SetIntoxicationStacks(int stacks)
        {
            IntoxicationStacks = Math.Max(0, stacks);
        }

        private static void ValidateAmount(float amount)
        {
            if (!float.IsFinite(amount) || amount < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount));
            }
        }
    }
}
