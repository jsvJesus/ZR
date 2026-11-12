using System.Text.Json;
using System.Text.Json.Serialization;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters.Formats;

namespace Editor.Environment
{
    public enum WeatherType
    {
        Clear,
        Cloudy,
        Rain,
        Thunderstorm,
        Snow,
        Blizzard,
        Fog
    }

    public sealed class EnvironmentSettings
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public float TimeOfDay { get; set; } = 14.38f;
        public bool RunTimeOfDay { get; set; } = false;
        public float SecondsPerGameHour { get; set; } = 3600f;
        public float SunAzimuth { get; set; } = 115.2f;
        public float SunIntensity { get; set; } = 1f;
        public float AmbientIntensity { get; set; } = 0.75f;

        public bool ShadowsEnabled { get; set; } = true;
        public float ShadowDistance { get; set; } = 35f;
        public float ShadowStrength { get; set; } = 0.72f;

        public float CloudCoverage { get; set; } = 0.45f;
        public float CloudSpeed { get; set; } = 0.3f;

        public WeatherType Weather { get; set; } = WeatherType.Clear;
        public float WeatherTransitionSeconds { get; set; } = 12f;
        public bool LightningEnabled { get; set; } = true;
        public float WindDirection { get; set; } = 30f;
        public float WindSpeed { get; set; } = 2f;
        public float FogDensity { get; set; } = 0.002f;
        public float Precipitation { get; set; } = 1f;
        public bool UniformRoadWetness { get; set; }

        public static EnvironmentSettings Load(
            string levelDirectory,
            AssetPaths assets)
        {
            string path = Path.Combine(levelDirectory, "environment.json");

            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<EnvironmentSettings>(
                    File.ReadAllText(path),
                    JsonOptions) ?? new EnvironmentSettings();
            }

            var settings = new EnvironmentSettings();

            string skyPath = Path.Combine(
                assets.Root,
                "system",
                "data",
                "sky.xml");

            if (File.Exists(skyPath))
            {
                var sky = PackedSection.Read(skyPath);
                var cycle = sky.Find("day_night_cycle");

                settings.TimeOfDay = ReadNumber(
                    cycle?.Find("starttime"),
                    settings.TimeOfDay);

                settings.SunAzimuth = ReadNumber(
                    cycle?.Find("angle"),
                    settings.SunAzimuth);
            }

            string shadowPath = Path.Combine(
                assets.Root,
                "system",
                "data",
                "dynamic_shadows.xml");

            if (File.Exists(shadowPath))
            {
                var shadows = PackedSection.Read(shadowPath);
                var shadowMap = shadows.Find("shadowMap");
                var config = shadowMap?.Find("config");

                settings.ShadowDistance = ReadNumber(
                    config?.Find("CastDistance"),
                    settings.ShadowDistance);
            }

            return settings;
        }

        public async Task SaveAsync(
            string levelDirectory,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(levelDirectory);

            string path = Path.Combine(levelDirectory, "environment.json");
            string temporary = Path.Combine(
                levelDirectory,
                $".environment-{Guid.NewGuid():N}.tmp");

            try
            {
                string json = JsonSerializer.Serialize(this, JsonOptions);

                await File.WriteAllTextAsync(
                    temporary,
                    json,
                    cancellationToken);

                File.Move(temporary, path, true);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        private static float ReadNumber(
            PackedSection? section,
            float fallback)
        {
            if (section == null)
            {
                return fallback;
            }

            if (section.Value is float[] values &&
                values.Length > 0 &&
                float.IsFinite(values[0]))
            {
                return values[0];
            }

            if (section.Value is int integer)
            {
                return integer;
            }

            return fallback;
        }
    }
}
