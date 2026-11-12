using Avalonia.Controls;
using Avalonia.Media;
using ZoneReborn.World.Terrain.Models;

namespace Editor.Terrain;

public sealed partial class TerrainEditorPanel
{
    private readonly TextBox _pbrNoise = new() { Text = "1" };
    private readonly TextBox _pbrHeight = new() { Text = "0" };
    private readonly TextBox _pbrSlope = new() { Text = "0" };
    private readonly TextBox _pbrMacro = new() { Text = "0" };
    private readonly TextBox _pbrMacroScale = new() { Text = "32" };
    private readonly TextBox _pbrSlopeBias = new() { Text = "0" };

    private void BuildPbrControls()
    {
        var controls = new StackPanel
        {
            Spacing = 7
        };

        controls.Children.Add(new TextBlock
        {
            Text = "Смешивание покрытий внутри материала",
            TextWrapping = TextWrapping.Wrap
        });

        AddPbrField(
            controls,
            "Noise Blend — интенсивность (0–1)",
            _pbrNoise);

        AddPbrField(
            controls,
            "Height Blend — интенсивность (0–1)",
            _pbrHeight);

        AddPbrField(
            controls,
            "Slope Blend — интенсивность (0–1)",
            _pbrSlope);

        AddPbrField(
            controls,
            "Macro Variation — интенсивность (0–1)",
            _pbrMacro);

        AddPbrField(
            controls,
            "Macro Scale — размер, м",
            _pbrMacroScale);

        controls.Children.Add(new TextBlock
        {
            Text = "Настройка выбранного текстурного покрытия",
            FontWeight = FontWeight.Bold
        });

        AddPbrField(
            controls,
            "Slope Bias (−1 ровная, +1 склон)",
            _pbrSlopeBias);

        var apply = new Button
        {
            Content = "Применить PBR-настройки"
        };

        apply.Click += (_, _) =>
        {
            if (!Active)
            {
                return;
            }

            try
            {
                ApplyPbrSettings();
            }
            catch (Exception error)
            {
                _status.Text = error.Message;
            }
        };

        controls.Children.Add(apply);

        _materialControls.Children.Add(new Expander
        {
            Header = "PBR и смешивание покрытий",
            IsExpanded = false,
            Content = controls
        });

        _palette.SelectionChanged += (_, _) =>
        {
            RefreshPbrControls();
        };

        _catalogVariants.SelectionChanged += (_, _) =>
        {
            RefreshPbrControls();
        };

        RefreshPbrControls();
    }

    private static void AddPbrField(
        StackPanel parent,
        string label,
        TextBox input)
    {
        parent.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 12
        });

        parent.Children.Add(input);
    }

    private void RefreshPbrControls()
    {
        if (_palette.SelectedItem is not TerrainLayer layer)
        {
            return;
        }

        var blend = layer.Settings.Blend;

        _pbrNoise.Text = Format(blend.NoiseIntensity);
        _pbrHeight.Text = Format(blend.HeightIntensity);
        _pbrSlope.Text = Format(blend.SlopeIntensity);
        _pbrMacro.Text = Format(blend.MacroIntensity);
        _pbrMacroScale.Text = Format(blend.MacroScaleMeters);

        var variants = layer.GetTextureVariants();
        int selected = _catalogVariants.SelectedIndex;

        if (selected >= 0 && selected < variants.Count)
        {
            _pbrSlopeBias.Text = Format(
                variants[selected].SlopeBias);
        }
    }

    private void ApplyPbrSettings()
    {
        if (_level?.TerrainDocument is not { } document ||
            _palette.SelectedItem is not TerrainLayer layer)
        {
            return;
        }

        _viewport?.EndTerrainPaint();

        var blend = new TerrainBlendProperties
        {
            NoiseIntensity = Parse(
                _pbrNoise, 0, 1, "Noise Blend"),

            HeightIntensity = Parse(
                _pbrHeight, 0, 1, "Height Blend"),

            SlopeIntensity = Parse(
                _pbrSlope, 0, 1, "Slope Blend"),

            MacroIntensity = Parse(
                _pbrMacro, 0, 1, "Macro Variation"),

            MacroScaleMeters = Parse(
                _pbrMacroScale, 1, 1024, "Macro Scale")
        };

        var variants = layer.GetTextureVariants();
        int selected = _catalogVariants.SelectedIndex;

        if (selected >= 0 && selected < variants.Count)
        {
            variants[selected] = variants[selected] with
            {
                SlopeBias = Parse(
                    _pbrSlopeBias, -1, 1, "Slope Bias")
            };
        }

        document.UpdateMaterial(layer with
        {
            Settings = layer.Settings with
            {
                Blend = blend
            },

            TextureVariants = variants
        });

        RefreshPalette(layer.Index);
        RefreshMaterialCatalog(selected >= 0 ? selected : 0);
        RefreshPbrControls();
        RefreshMode();

        _status.Text =
            "PBR-настройки сохранены в документе. " +
            "Сохраните Terrain.";
    }
}
