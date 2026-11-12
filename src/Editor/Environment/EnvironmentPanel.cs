using Avalonia.Controls;
using Avalonia.Threading;
using Editor.Viewport;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;

namespace Editor.Environment
{
    public enum EnvironmentSection
    {
        Lighting,
        Shadows,
        Clouds,
        Weather
    }

    public sealed class EnvironmentPanel : UserControl
    {
        private readonly StackPanel _content = new()
        {
            Spacing = 12
        };

        private DxViewportHost? _viewport;
        private AssetPaths? _assets;
        private LoadedLevel? _level;
        private EnvironmentSettings _settings = new();
        private EnvironmentSection _section;

        private readonly DispatcherTimer _clockTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };

        private Slider? _timeSlider;
        private TextBlock? _timeText;
        private bool _updatingTime;

        public bool HasPendingChanges { get; private set; }

        public EnvironmentPanel()
        {
            Content = _content;
            Build();

            _clockTimer.Tick += (_, _) => RefreshTime();
            _clockTimer.Start();

            DetachedFromVisualTree += (_, _) => _clockTimer.Stop();
            AttachedToVisualTree += (_, _) => _clockTimer.Start();
        }

        private void RefreshTime()
        {
            if (!IsVisible ||
                !_settings.RunTimeOfDay ||
                _timeSlider == null ||
                _timeText == null)
            {
                return;
            }

            _updatingTime = true;

            try
            {
                float time = Math.Clamp(
                    _settings.TimeOfDay,
                    0f,
                    24f);

                _timeSlider.Value = time;

                int hours = (int)time % 24;
                int minutes = Math.Clamp(
                    (int)((time % 1f) * 60f),
                    0,
                    59);

                _timeText.Text =
                    $"Время суток: {hours:00}:{minutes:00}";
            }
            finally
            {
                _updatingTime = false;
            }
        }

        public void Attach(
            DxViewportHost viewport,
            AssetPaths assets)
        {
            _viewport = viewport;
            _assets = assets;
            _viewport.SetEnvironment(_settings);
        }

        public void Bind(LoadedLevel level)
        {
            if (_assets == null)
            {
                throw new InvalidOperationException(
                    "Панель окружения не подключена к ресурсам.");
            }

            _level = level;
            _settings = EnvironmentSettings.Load(
                level.Directory,
                _assets);

            HasPendingChanges = false;
            _viewport?.SetEnvironment(_settings);
            Build();
        }

        public void ShowSection(EnvironmentSection section)
        {
            _section = section;
            Build();
        }

        public async Task SavePendingAsync(
            CancellationToken cancellationToken = default)
        {
            if (!HasPendingChanges || _level == null)
            {
                return;
            }

            await _settings.SaveAsync(
                _level.Directory,
                cancellationToken);

            HasPendingChanges = false;
        }

        private void Changed()
        {
            HasPendingChanges = _level != null;
            _viewport?.SetEnvironment(_settings);
        }

        private void Build()
        {
            _content.Children.Clear();

            _timeSlider = null;
            _timeText = null;

            switch (_section)
            {
                case EnvironmentSection.Lighting:
                    AddHeading("ОСВЕЩЕНИЕ");

                    AddSlider(
                        "Время суток",
                        0,
                        24,
                        () => _settings.TimeOfDay,
                        value => _settings.TimeOfDay = value,
                        value => $"{(int)value:00}:{(int)((value % 1) * 60):00}");

                    AddToggle(
                        "Автоматический цикл суток",
                        () => _settings.RunTimeOfDay,
                        value => _settings.RunTimeOfDay = value);

                    AddSlider(
                        "Секунд на игровой час",
                        30,
                        3600,
                        () => _settings.SecondsPerGameHour,
                        value => _settings.SecondsPerGameHour = value,
                        value => $"{value:0} сек.");

                    AddSlider(
                        "Азимут солнца",
                        0,
                        360,
                        () => _settings.SunAzimuth,
                        value => _settings.SunAzimuth = value);

                    AddSlider(
                        "Яркость солнца",
                        0,
                        3,
                        () => _settings.SunIntensity,
                        value => _settings.SunIntensity = value);

                    AddSlider(
                        "Окружающий свет",
                        0,
                        2,
                        () => _settings.AmbientIntensity,
                        value => _settings.AmbientIntensity = value);

                    AddInfo(
                        "Параметры освещения применяются ко всей сцене.");
                    break;

                case EnvironmentSection.Shadows:
                    AddHeading("ТЕНИ");

                    AddToggle(
                        "Динамические тени",
                        () => _settings.ShadowsEnabled,
                        value => _settings.ShadowsEnabled = value);

                    AddSlider(
                        "Дальность теней, м",
                        10,
                        200,
                        () => _settings.ShadowDistance,
                        value => _settings.ShadowDistance = value);

                    AddSlider(
                        "Интенсивность теней",
                        0,
                        1,
                        () => _settings.ShadowStrength,
                        value => _settings.ShadowStrength = value);
                    break;

                case EnvironmentSection.Clouds:
                    AddHeading("ОБЛАКА");

                    AddSlider(
                        "Облачность",
                        0,
                        1,
                        () => _settings.CloudCoverage,
                        value => _settings.CloudCoverage = value);

                    AddSlider(
                        "Скорость движения",
                        0,
                        5,
                        () => _settings.CloudSpeed,
                        value => _settings.CloudSpeed = value);

                    AddInfo(
                        "Облака используют погодные текстуры из packs/weather/maps.");
                    break;

                case EnvironmentSection.Weather:
                    AddHeading("ПОГОДА");

                    AddWeatherSelector();

                    AddSlider(
                        "Переход погоды",
                        0,
                        120,
                        () => _settings.WeatherTransitionSeconds,
                        value => _settings.WeatherTransitionSeconds = value,
                        value => $"{value:0} сек.");

                    AddToggle(
                        "Молнии во время грозы",
                        () => _settings.LightningEnabled,
                        value => _settings.LightningEnabled = value);

                    AddSlider(
                        "Направление ветра",
                        0,
                        360,
                        () => _settings.WindDirection,
                        value => _settings.WindDirection = value);

                    AddSlider(
                        "Скорость ветра",
                        0,
                        25,
                        () => _settings.WindSpeed,
                        value => _settings.WindSpeed = value);

                    AddSlider(
                        "Плотность тумана",
                        0,
                        0.04,
                        () => _settings.FogDensity,
                        value => _settings.FogDensity = value,
                        value => value.ToString("0.0000"));

                    AddSlider(
                        "Интенсивность осадков",
                        0,
                        2,
                        () => _settings.Precipitation,
                        value => _settings.Precipitation = value);

                    AddToggle(
                        "Равномерное намокание дорог (Roads)",
                        () => _settings.UniformRoadWetness,
                        value => _settings.UniformRoadWetness = value);

                    AddInfo(
                        "Включено — вся поверхность дорожных мешей " +
                        "постепенно намокает. Выключено — влажность " +
                        "распределяется отдельными пятнами.");
                    break;
            }
        }

        private void AddHeading(string title)
        {
            _content.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = Avalonia.Media.FontWeight.Bold
            });
        }

        private void AddInfo(string message)
        {
            _content.Children.Add(new TextBlock
            {
                Text = message,
                FontSize = 12,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });
        }

        private void AddSlider(
            string title,
            double minimum,
            double maximum,
            Func<float> getter,
            Action<float> setter,
            Func<double, string>? formatter = null)
        {
            var valueText = new TextBlock
            {
                Text = title,
                FontSize = 12
            };

            var slider = new Slider
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = Math.Clamp(getter(), minimum, maximum),
                TickFrequency = 0.1
            };

            if (title == "Время суток")
            {
                _timeSlider = slider;
                _timeText = valueText;
            }

            string Format(double value) =>
                formatter?.Invoke(value) ?? value.ToString("0.##");

            valueText.Text = $"{title}: {Format(slider.Value)}";

            slider.ValueChanged += (_, args) =>
            {
                if (_updatingTime)
                {
                    return;
                }

                setter((float)args.NewValue);
                valueText.Text = $"{title}: {Format(args.NewValue)}";
                Changed();
            };

            _content.Children.Add(valueText);
            _content.Children.Add(slider);
        }

        private void AddToggle(
            string title,
            Func<bool> getter,
            Action<bool> setter)
        {
            var checkbox = new CheckBox
            {
                Content = title,
                IsChecked = getter()
            };

            checkbox.IsCheckedChanged += (_, _) =>
            {
                setter(checkbox.IsChecked == true);
                Changed();
            };

            _content.Children.Add(checkbox);
        }

        private void AddWeatherSelector()
        {
            _content.Children.Add(new TextBlock
            {
                Text = "Тип погоды",
                FontSize = 12
            });

            var selector = new ComboBox
            {
                ItemsSource = Enum.GetValues<WeatherType>(),
                SelectedItem = _settings.Weather,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };

            selector.SelectionChanged += (_, _) =>
            {
                if (selector.SelectedItem is WeatherType weather)
                {
                    _settings.Weather = weather;
                    Changed();
                }
            };

            _content.Children.Add(selector);
        }
    }
}
