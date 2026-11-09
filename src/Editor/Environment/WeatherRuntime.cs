namespace Editor.Environment
{
    internal readonly record struct WeatherFrame(
        float CloudCoverage,
        float StormCover,
        float Rain,
        float Snow,
        float FogDensity,
        float SunMultiplier,
        float Thunder,
        float Lightning);

    internal sealed class WeatherRuntime
    {
        private readonly Random _random = new();

        private WeatherType _weather;
        private WeatherFrame _start;
        private WeatherFrame _target;
        private WeatherFrame _current;

        private bool _initialized;
        private float _elapsed;
        private float _nextLightning;
        private float _lightningTime;

        public void Reset()
        {
            _initialized = false;
            _elapsed = 0f;
            _nextLightning = 0f;
            _lightningTime = 0f;
        }

        public WeatherFrame Advance(
            EnvironmentSettings settings,
            float deltaTime)
        {
            deltaTime = Math.Clamp(deltaTime, 0f, 0.1f);

            WeatherFrame desired = CreateTarget(settings);

            if (!_initialized)
            {
                _initialized = true;
                _weather = settings.Weather;
                _start = desired;
                _target = desired;
                _current = desired;
                _elapsed = settings.WeatherTransitionSeconds;
                _nextLightning = RandomLightningDelay();
            }
            else if (_weather != settings.Weather)
            {
                _weather = settings.Weather;
                _start = _current;
                _target = desired;
                _elapsed = 0f;
            }
            else
            {
                _target = desired;
            }

            float duration = Math.Clamp(
                settings.WeatherTransitionSeconds,
                0f,
                120f);

            if (duration <= 0f)
            {
                _current = _target;
            }
            else
            {
                _elapsed = MathF.Min(
                    _elapsed + deltaTime,
                    duration);

                float blend = Math.Clamp(
                    _elapsed / duration,
                    0f,
                    1f);

                blend = blend * blend * (3f - 2f * blend);

                _current = Blend(
                    _start,
                    _target,
                    blend);
            }

            float lightning = 0f;

            if (settings.LightningEnabled &&
                _current.Thunder > 0.05f)
            {
                _nextLightning -= deltaTime;

                if (_nextLightning <= 0f)
                {
                    _lightningTime = 0.24f;

                    _nextLightning = RandomLightningDelay() /
                        MathF.Max(0.2f, _current.Thunder);
                }

                if (_lightningTime > 0f)
                {
                    _lightningTime = MathF.Max(
                        0f,
                        _lightningTime - deltaTime);

                    float phase = 1f -
                        _lightningTime / 0.24f;

                    float firstFlash = MathF.Exp(
                        -MathF.Pow((phase - 0.16f) * 15f, 2f));

                    float secondFlash = MathF.Exp(
                        -MathF.Pow((phase - 0.56f) * 13f, 2f));

                    lightning = Math.Clamp(
                        firstFlash + secondFlash * 0.75f,
                        0f,
                        1f);
                }
            }
            else
            {
                _lightningTime = 0f;
            }

            return _current with
            {
                Lightning = lightning * _current.Thunder
            };
        }

        private float RandomLightningDelay()
        {
            return 5f + (float)_random.NextDouble() * 13f;
        }

        private static WeatherFrame CreateTarget(
            EnvironmentSettings settings)
        {
            float clouds = Math.Clamp(
                settings.CloudCoverage,
                0f,
                1f);

            float fog = Math.Clamp(
                settings.FogDensity,
                0f,
                0.04f);

            return settings.Weather switch
            {
                WeatherType.Clear => new WeatherFrame(
                    clouds, 0f, 0f, 0f, fog, 1f, 0f, 0f),

                WeatherType.Cloudy => new WeatherFrame(
                    MathF.Max(clouds, 0.65f),
                    0f, 0f, 0f, fog, 0.7f, 0f, 0f),

                WeatherType.Rain => new WeatherFrame(
                    MathF.Max(clouds, 0.78f),
                    0.3f, 1f, 0f,
                    MathF.Max(fog, 0.003f),
                    0.55f, 0f, 0f),

                WeatherType.Thunderstorm => new WeatherFrame(
                    MathF.Max(clouds, 0.95f),
                    1f, 1f, 0f,
                    MathF.Max(fog, 0.008f),
                    0.22f, 1f, 0f),

                WeatherType.Snow => new WeatherFrame(
                    MathF.Max(clouds, 0.78f),
                    0.3f, 0f, 1f,
                    MathF.Max(fog, 0.003f),
                    0.55f, 0f, 0f),

                WeatherType.Blizzard => new WeatherFrame(
                    MathF.Max(clouds, 0.95f),
                    1f, 0f, 1f,
                    MathF.Max(fog, 0.008f),
                    0.22f, 0f, 0f),

                WeatherType.Fog => new WeatherFrame(
                    MathF.Max(clouds, 0.78f),
                    0.15f, 0f, 0f,
                    MathF.Max(fog, 0.018f),
                    0.65f, 0f, 0f),

                _ => new WeatherFrame(
                    clouds, 0f, 0f, 0f, fog, 1f, 0f, 0f)
            };
        }

        private static WeatherFrame Blend(
            WeatherFrame a,
            WeatherFrame b,
            float amount)
        {
            static float Mix(float start, float end, float t)
            {
                return start + (end - start) * t;
            }

            return new WeatherFrame(
                Mix(a.CloudCoverage, b.CloudCoverage, amount),
                Mix(a.StormCover, b.StormCover, amount),
                Mix(a.Rain, b.Rain, amount),
                Mix(a.Snow, b.Snow, amount),
                Mix(a.FogDensity, b.FogDensity, amount),
                Mix(a.SunMultiplier, b.SunMultiplier, amount),
                Mix(a.Thunder, b.Thunder, amount),
                0f);
        }
    }
}
