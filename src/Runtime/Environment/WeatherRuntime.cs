namespace Runtime.Environment
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
        private static readonly WeatherType[] ScheduledTypes =
        [
            WeatherType.Clear,
            WeatherType.Cloudy,
            WeatherType.Overcast,
            WeatherType.Rain,
            WeatherType.Thunderstorm,
            WeatherType.Fog
        ];

        private Random _random = new();

        private WeatherType _requested;
        private WeatherType _active;

        private WeatherFrame _start;
        private WeatherFrame _target;
        private WeatherFrame _current;

        private bool _initialized;
        private bool _transitioning;
        private bool _automaticTransition;

        private float _hoursRemaining;
        private float _transitionElapsed;
        private float _nextLightning;
        private float _lightningTime;

        public WeatherType ActiveWeather => _active;
        public float HoursRemaining => MathF.Max(_hoursRemaining, 0f);

        public void Reset()
        {
            _initialized = false;
            _transitioning = false;
            _transitionElapsed = 0f;
            _nextLightning = 0f;
            _lightningTime = 0f;
        }

        public WeatherFrame Advance(
            EnvironmentSettings settings,
            float deltaTime)
        {
            deltaTime = Math.Clamp(deltaTime, 0f, 0.1f);

            float gameHours = settings.RunTimeOfDay
                ? deltaTime / Math.Clamp(
                    settings.SecondsPerGameHour,
                    1f,
                    3600f)
                : 0f;

            if (!_initialized)
            {
                _random = new Random(settings.WeatherSeed);
                _requested = settings.Weather;
                _active = settings.Weather;
                _start = CreateTarget(settings, _active);
                _target = _start;
                _current = _start;
                _hoursRemaining = RandomDuration(settings, _active);
                _nextLightning = RandomLightningDelay();
                _initialized = true;
            }

            if (_requested != settings.Weather)
            {
                _requested = settings.Weather;
                BeginTransition(settings, _requested, false);
                _hoursRemaining = RandomDuration(settings, _active);
            }
            else if (!settings.AutoWeather && _active != _requested)
            {
                BeginTransition(settings, _requested, false);
            }
            else if (settings.AutoWeather && settings.RunTimeOfDay)
            {
                _hoursRemaining -= gameHours;

                if (_hoursRemaining <= 0f)
                {
                    WeatherType next = SelectNext(settings);
                    BeginTransition(settings, next, true);
                    _hoursRemaining = RandomDuration(settings, next);
                }
            }

            _target = CreateTarget(settings, _active);

            if (_transitioning)
            {
                float duration = _automaticTransition
                    ? Math.Clamp(
                        settings.AutoWeatherTransitionHours,
                        0.01f,
                        2f)
                    : Math.Clamp(
                        settings.WeatherTransitionSeconds,
                        0.01f,
                        120f);

                _transitionElapsed += _automaticTransition
                    ? gameHours
                    : deltaTime;

                float blend = Math.Clamp(
                    _transitionElapsed / duration,
                    0f,
                    1f);

                blend = blend * blend * (3f - 2f * blend);

                _current = Blend(_start, _target, blend);

                if (blend >= 1f)
                {
                    _transitioning = false;
                }
            }
            else
            {
                _current = _target;
            }

            float lightning = UpdateLightning(
                settings,
                deltaTime);

            return _current with
            {
                Lightning = lightning
            };
        }

        private void BeginTransition(
            EnvironmentSettings settings,
            WeatherType next,
            bool automatic)
        {
            _start = _current;
            _active = next;
            _target = CreateTarget(settings, next);
            _transitionElapsed = 0f;
            _transitioning = true;
            _automaticTransition = automatic;
        }

        private WeatherType SelectNext(EnvironmentSettings settings)
        {
            int totalWeight = 0;

            foreach (WeatherType type in ScheduledTypes)
            {
                if (type == _active)
                {
                    continue;
                }

                totalWeight += (int)GetRule(
                    settings,
                    type).Frequency;
            }

            if (totalWeight <= 0)
            {
                return _active;
            }

            int selection = _random.Next(totalWeight);

            foreach (WeatherType type in ScheduledTypes)
            {
                if (type == _active)
                {
                    continue;
                }

                int weight = (int)GetRule(
                    settings,
                    type).Frequency;

                if (selection < weight)
                {
                    return type;
                }

                selection -= weight;
            }

            return _active;
        }

        private float RandomDuration(
            EnvironmentSettings settings,
            WeatherType type)
        {
            WeatherPresetRule rule = GetRule(settings, type);

            float minimum = Math.Clamp(
                rule.MinHours,
                0.5f,
                24f);

            float maximum = Math.Clamp(
                rule.MaxHours,
                minimum,
                24f);

            return minimum +
                (float)_random.NextDouble() *
                (maximum - minimum);
        }

        private static WeatherPresetRule GetRule(
            EnvironmentSettings settings,
            WeatherType type)
        {
            if (settings.WeatherSchedule != null &&
                settings.WeatherSchedule.TryGetValue(
                    type,
                    out WeatherPresetRule? rule) &&
                rule != null)
            {
                return rule;
            }

            return new WeatherPresetRule
            {
                MinHours = type is WeatherType.Rain or
                    WeatherType.Thunderstorm or WeatherType.Fog
                        ? 2f
                        : 4f,

                MaxHours = 12f,
                Frequency = WeatherFrequency.Normal
            };
        }

        private float UpdateLightning(
            EnvironmentSettings settings,
            float deltaTime)
        {
            if (!settings.LightningEnabled ||
                _current.Thunder <= 0.05f)
            {
                _lightningTime = 0f;
                _nextLightning = RandomLightningDelay();
                return 0f;
            }

            _nextLightning -= deltaTime;

            if (_nextLightning <= 0f)
            {
                _lightningTime = 0.24f;
                _nextLightning = RandomLightningDelay() /
                    MathF.Max(_current.Thunder, 0.2f);
            }

            if (_lightningTime <= 0f)
            {
                return 0f;
            }

            _lightningTime = MathF.Max(
                0f,
                _lightningTime - deltaTime);

            float phase = 1f - _lightningTime / 0.24f;

            float first = MathF.Exp(
                -MathF.Pow((phase - 0.16f) * 15f, 2f));

            float second = MathF.Exp(
                -MathF.Pow((phase - 0.56f) * 13f, 2f));

            return Math.Clamp(
                (first + second * 0.75f) * _current.Thunder,
                0f,
                1f);
        }

        private float RandomLightningDelay()
        {
            return 5f + (float)_random.NextDouble() * 13f;
        }

        private static WeatherFrame CreateTarget(
            EnvironmentSettings settings,
            WeatherType type)
        {
            float clouds = Math.Clamp(
                settings.CloudCoverage, 0f, 1f);

            float fog = Math.Clamp(
                settings.FogDensity, 0f, 0.04f);

            return type switch
            {
                WeatherType.Clear => new(
                    MathF.Min(clouds, 0.25f),
                    0f, 0f, 0f,
                    fog, 1f, 0f, 0f),

                WeatherType.Cloudy => new(
                    MathF.Max(clouds, 0.58f),
                    0.08f, 0f, 0f,
                    MathF.Max(fog, 0.0015f),
                    0.62f, 0f, 0f),

                WeatherType.Overcast => new(
                    MathF.Max(clouds, 0.9f),
                    0.6f, 0f, 0f,
                    MathF.Max(fog, 0.003f),
                    0.23f, 0f, 0f),

                WeatherType.Rain => new(
                    MathF.Max(clouds, 0.87f),
                    0.55f, 0.9f, 0f,
                    MathF.Max(fog, 0.005f),
                    0.3f, 0f, 0f),

                WeatherType.Thunderstorm => new(
                    MathF.Max(clouds, 0.97f),
                    1f, 1f, 0f,
                    MathF.Max(fog, 0.008f),
                    0.12f, 1f, 0f),

                WeatherType.Fog => new(
                    MathF.Max(clouds, 0.55f),
                    0.2f, 0f, 0f,
                    MathF.Max(fog, 0.018f),
                    0.38f, 0f, 0f),

                WeatherType.Snow => new(
                    MathF.Max(clouds, 0.78f),
                    0.3f, 0f, 1f,
                    MathF.Max(fog, 0.003f),
                    0.55f, 0f, 0f),

                WeatherType.Blizzard => new(
                    MathF.Max(clouds, 0.95f),
                    1f, 0f, 1f,
                    MathF.Max(fog, 0.008f),
                    0.22f, 0f, 0f),

                _ => new(
                    clouds, 0f, 0f, 0f,
                    fog, 1f, 0f, 0f)
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
