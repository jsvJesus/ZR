namespace ZoneReborn.World.Characters.Stats
{
    public sealed class StatModifier
    {
        public float Before { get; private set; }
        public float Multiplier { get; private set; } = 1f;
        public float Percent { get; private set; }
        public float After { get; private set; }

        public float Evaluate(float baseValue)
        {
            return (baseValue + Before) *
                   Multiplier *
                   (1f + Percent / 100f) +
                   After;
        }

        public void AddBefore(float value)
        {
            Before += value;
        }

        public void Multiply(float value)
        {
            Multiplier *= value;
        }

        public void AddPercent(float value)
        {
            Percent += value;
        }

        public void AddAfter(float value)
        {
            After += value;
        }

        public void Merge(StatModifier modifier)
        {
            ArgumentNullException.ThrowIfNull(modifier);

            Before += modifier.Before;
            Multiplier *= modifier.Multiplier;
            Percent += modifier.Percent;
            After += modifier.After;
        }

        public void Clear()
        {
            Before = 0f;
            Multiplier = 1f;
            Percent = 0f;
            After = 0f;
        }
    }
}
