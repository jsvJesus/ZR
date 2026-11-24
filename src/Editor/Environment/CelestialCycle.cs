using System.Numerics;

namespace Editor.Environment
{
    internal readonly record struct CelestialFrame(
        Vector3 SunDirection,
        Vector3 MoonDirection,
        Vector3 LightDirection,
        Vector3 LightColor,
        Vector3 Ambient,
        Vector3 FogColor,
        float FogDensity,
        float LightIntensity,
        float Daylight,
        float MoonStrength,
        bool MoonActive,
        NightState Night);

    internal static class CelestialCycle
    {
        public static CelestialFrame Evaluate(
            EnvironmentSettings settings,
            WeatherFrame weather,
            SkyCycle? skyCycle)
        {
            float hour = (
                settings.TimeOfDay % 24f + 24f) % 24f;

            float hourAngle =
                (hour - 12f) * MathF.PI / 12f;

            float elevation = MathF.Cos(hourAngle);
            float horizontal = MathF.Sin(hourAngle);

            float azimuth =
                settings.SunAzimuth * MathF.PI / 180f;

            Vector3 sunDirection = Vector3.Normalize(
                new Vector3(
                    MathF.Cos(azimuth) * horizontal,
                    elevation,
                    MathF.Sin(azimuth) * horizontal));

            Vector3 moonDirection = -sunDirection;

            float daylight = Smooth(
                -0.15f,
                0.18f,
                elevation);

            float sunVisibility = Smooth(
                0.0f,
                0.12f,
                elevation);

            NightState night = NightCycle.Evaluate(settings);

            float cloudOcclusion = Math.Clamp(
                weather.CloudCoverage * 0.65f +
                weather.StormCover * 0.25f,
                0f,
                0.95f);

            float moonVisibility =
                Smooth(0.02f, 0.14f, moonDirection.Y) *
                (1f - daylight);

            float moonStrength =
                night.Illumination *
                night.BrightnessFactor *
                moonVisibility *
                Math.Clamp(settings.MoonIntensity, 0f, 3f) *
                (1f - cloudOcclusion);

            float sunlight =
                Math.Clamp(settings.SunIntensity, 0f, 3f) *
                weather.SunMultiplier *
                sunVisibility;

            float lunarLight = moonStrength * 0.14f;

            bool moonActive =
                sunlight < 0.005f &&
                lunarLight > 0.0005f;

            Vector3 lightDirection = moonActive
                ? moonDirection
                : sunDirection;

            Vector3 sunColor = skyCycle?.SunColor(hour) ??
                new Vector3(1f, 0.94f, 0.84f);

            Vector3 lightColor = moonActive
                ? new Vector3(0.65f, 0.75f, 0.95f)
                : sunColor;

            float cloudAmbient = Math.Clamp(
                1f - weather.CloudCoverage * 0.18f -
                weather.StormCover * 0.18f,
                0.5f,
                1f);

            Vector3 dayAmbient =
                (skyCycle?.AmbientColor(hour) ??
                new Vector3(0.44f, 0.48f, 0.5f)) *
                cloudAmbient;

            Vector3 nightAmbient =
                new Vector3(0.012f, 0.018f, 0.028f) *
                (0.45f + night.BrightnessFactor * 0.55f);

            nightAmbient += new Vector3(
                0.013f,
                0.019f,
                0.031f) * moonStrength;

            Vector3 ambient = Vector3.Lerp(
                nightAmbient,
                dayAmbient,
                daylight);

            ambient *= Math.Clamp(
                settings.AmbientIntensity,
                0f,
                2f);

            ambient += new Vector3(
                0.6f,
                0.72f,
                0.9f) * weather.Lightning * 0.7f;

            Vector3 dayFog = Vector3.Lerp(
                new Vector3(0.43f, 0.49f, 0.53f),
                new Vector3(0.29f, 0.32f, 0.33f),
                Math.Clamp(
                    weather.CloudCoverage * 0.6f +
                    weather.StormCover * 0.4f,
                    0f,
                    1f));

            Vector3 nightFog =
                new Vector3(0.009f, 0.015f, 0.025f) *
                (0.5f + night.BrightnessFactor * 0.7f);

            nightFog += new Vector3(
                0.008f,
                0.012f,
                0.021f) * moonStrength;

            Vector3 fogColor = Vector3.Lerp(
                nightFog,
                dayFog,
                daylight);

            fogColor += new Vector3(
                0.24f,
                0.31f,
                0.42f) * weather.Lightning * 0.3f;

            return new CelestialFrame(
                sunDirection,
                moonDirection,
                lightDirection,
                lightColor,
                ambient,
                fogColor,
                Math.Clamp(weather.FogDensity, 0f, 0.04f),
                moonActive ? lunarLight : sunlight,
                daylight,
                moonStrength,
                moonActive,
                night);
        }

        private static float Smooth(
            float start,
            float end,
            float value)
        {
            float t = Math.Clamp(
                (value - start) / (end - start),
                0f,
                1f);

            return t * t * (3f - 2f * t);
        }
    }
}
