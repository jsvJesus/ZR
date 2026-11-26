namespace Runtime.Environment
{
    internal readonly record struct NightState(
        MoonPhase Phase,
        NightBrightness Brightness,
        float Illumination,
        float BrightnessFactor);

    internal static class NightCycle
    {
        private const float LunarMonth = 29.53059f;

        public static NightState Evaluate(
            EnvironmentSettings settings)
        {
            int day = Math.Max(settings.GameDay, 0);

            uint phaseSeed = Hash(
                settings.NightSeed,
                17);

            float phaseOffset =
                phaseSeed % 2953 / 100f;

            float lunarAge = (
                day + phaseOffset) % LunarMonth;

            int phaseIndex = Math.Clamp(
                (int)(lunarAge / LunarMonth * 8f),
                0,
                7);

            MoonPhase phase = settings.RandomMoonPhases
                ? (MoonPhase)phaseIndex
                : settings.FixedMoonPhase;

            uint brightnessSeed = Hash(
                settings.NightSeed,
                day + 193);

            int selection = (int)(
                brightnessSeed % 100);

            NightBrightness brightness =
                settings.RandomNightBrightness
                    ? selection < 25
                        ? NightBrightness.Dark
                        : selection < 75
                            ? NightBrightness.Dim
                            : NightBrightness.Bright
                    : settings.FixedNightBrightness;

            float illumination = phase switch
            {
                MoonPhase.NewMoon => 0f,
                MoonPhase.WaxingCrescent => 0.15f,
                MoonPhase.FirstQuarter => 0.5f,
                MoonPhase.WaxingGibbous => 0.85f,
                MoonPhase.FullMoon => 1f,
                MoonPhase.WaningGibbous => 0.85f,
                MoonPhase.LastQuarter => 0.5f,
                MoonPhase.WaningCrescent => 0.15f,
                _ => 0f
            };

            float brightnessFactor = brightness switch
            {
                NightBrightness.Dark => 0.25f,
                NightBrightness.Dim => 0.6f,
                NightBrightness.Bright => 1f,
                _ => 0.6f
            };

            return new NightState(
                phase,
                brightness,
                illumination,
                brightnessFactor);
        }

        private static uint Hash(
            int seed,
            int salt)
        {
            unchecked
            {
                uint value = (uint)seed ^
                    ((uint)salt * 0x9E3779B9u);

                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;

                return value;
            }
        }
    }
}
