using Avalonia.Controls;
using Avalonia.Media;
using ZoneReborn.World.Terrain.Models;

namespace Editor.Terrain;

public sealed partial class TerrainEditorPanel
{
    private readonly CheckBox _weatherEnabled = new()
    {
        Content = "Реагирует на дождь"
    };

    private readonly CheckBox _weatherUniform = new()
    {
        Content = "Равномерное намокание материала"
    };

    private readonly TextBox _weatherSoak = new();
    private readonly TextBox _weatherDry = new();
    private readonly TextBox _weatherDarkening = new();
    private readonly TextBox _weatherRoughness = new();
    private readonly TextBox _weatherSpecular = new();
    private readonly TextBox _weatherPatchiness = new();
    private readonly TextBox _weatherPatchScale = new();
    private readonly TextBox _weatherPuddles = new();
    private readonly TextBox _weatherFriction = new();
    private readonly TextBox _weatherFootsteps = new();

    private void BuildWeatherMaterialControls()
    {
        var controls = new StackPanel
        {
            Spacing = 7
        };

        controls.Children.Add(_weatherEnabled);
        controls.Children.Add(_weatherUniform);

        AddWeatherField(
            controls,
            "Время намокания, секунд",
            _weatherSoak);

        AddWeatherField(
            controls,
            "Время высыхания, секунд",
            _weatherDry);

        AddWeatherField(
            controls,
            "Потемнение (0–1)",
            _weatherDarkening);

        AddWeatherField(
            controls,
            "Roughness мокрой поверхности",
            _weatherRoughness);

        AddWeatherField(
            controls,
            "Specular мокрой поверхности",
            _weatherSpecular);

        AddWeatherField(
            controls,
            "Влажные пятна — интенсивность",
            _weatherPatchiness);

        AddWeatherField(
            controls,
            "Размер влажных пятен, м",
            _weatherPatchScale);

        AddWeatherField(
            controls,
            "Лужицы — интенсивность",
            _weatherPuddles);

        controls.Children.Add(new TextBlock
        {
            Text = "Физические свойства",
            FontWeight = FontWeight.Bold
        });

        AddWeatherField(
            controls,
            "Трение мокрой поверхности (0–3)",
            _weatherFriction);

        AddWeatherField(
            controls,
            "Звук шагов по мокрой поверхности",
            _weatherFootsteps);

        var apply = new Button
        {
            Content = "Сохранить погодные свойства"
        };

        apply.Click += (_, _) =>
        {
            if (!Active)
            {
                return;
            }

            try
            {
                ApplyWeatherMaterialSettings();
            }
            catch (Exception error)
            {
                _status.Text = error.Message;
            }
        };

        controls.Children.Add(apply);

        _materialControls.Children.Add(new Expander
        {
            Header = "Влажность и погодные свойства",
            IsExpanded = false,
            Content = controls
        });

        _palette.SelectionChanged += (_, _) =>
        {
            RefreshWeatherMaterialControls();
        };

        RefreshWeatherMaterialControls();
    }

    private static void AddWeatherField(
        StackPanel parent,
        string name,
        TextBox input)
    {
        parent.Children.Add(new TextBlock
        {
            Text = name,
            FontSize = 12
        });

        parent.Children.Add(input);
    }

    private void RefreshWeatherMaterialControls()
    {
        if (_palette.SelectedItem is not TerrainLayer layer)
        {
            return;
        }

        var weather = layer.Settings.Weather;

        _weatherEnabled.IsChecked = weather.ReactToRain;
        _weatherUniform.IsChecked = weather.UniformWetness;
        _weatherSoak.Text = Format(weather.SoakSeconds);
        _weatherDry.Text = Format(weather.DrySeconds);
        _weatherDarkening.Text = Format(weather.Darkening);
        _weatherRoughness.Text = Format(weather.WetRoughness);
        _weatherSpecular.Text = Format(weather.WetSpecular);
        _weatherPatchiness.Text = Format(weather.Patchiness);
        _weatherPatchScale.Text = Format(weather.PatchScaleMeters);
        _weatherPuddles.Text = Format(weather.PuddleStrength);
        _weatherFriction.Text = Format(weather.WetFriction);
        _weatherFootsteps.Text = weather.WetFootstepSound;
    }

    private void ApplyWeatherMaterialSettings()
    {
        if (_level?.TerrainDocument is not { } document ||
            _palette.SelectedItem is not TerrainLayer layer)
        {
            return;
        }

        _viewport?.EndTerrainPaint();

        var weather = new TerrainWeatherProperties
        {
            ReactToRain = _weatherEnabled.IsChecked == true,
            UniformWetness = _weatherUniform.IsChecked == true,

            SoakSeconds = Parse(
                _weatherSoak, 1, 3600, "Время намокания"),

            DrySeconds = Parse(
                _weatherDry, 1, 7200, "Время высыхания"),

            Darkening = Parse(
                _weatherDarkening, 0, 1, "Потемнение"),

            WetRoughness = Parse(
                _weatherRoughness, 0.045f, 1, "Roughness"),

            WetSpecular = Parse(
                _weatherSpecular, 0, 1, "Specular"),

            Patchiness = Parse(
                _weatherPatchiness, 0, 1, "Влажные пятна"),

            PatchScaleMeters = Parse(
                _weatherPatchScale, 1, 512, "Масштаб пятен"),

            PuddleStrength = Parse(
                _weatherPuddles, 0, 1, "Лужицы"),

            WetFriction = Parse(
                _weatherFriction, 0, 3, "Мокрое трение"),

            WetFootstepSound =
                (_weatherFootsteps.Text ?? "").Trim()
        };

        document.UpdateMaterial(layer with
        {
            Settings = layer.Settings with
            {
                Weather = weather
            }
        });

        RefreshPalette(layer.Index);
        RefreshWeatherMaterialControls();
        RefreshMode();

        _status.Text =
            "Погодные свойства материала сохранены в документе.";
    }
}
