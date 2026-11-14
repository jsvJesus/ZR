using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;
using ZoneReborn.World.Terrain.Models;

namespace Editor.Terrain;

public sealed partial class TerrainEditorPanel
{
    private readonly ComboBox _waterList = new();
    private readonly ComboBox _waterSand = new();

    private readonly TextBox _waterName = new();
    private readonly TextBox _waterX = new();
    private readonly TextBox _waterZ = new();
    private readonly TextBox _waterHeight = new();
    private readonly TextBox _waterRadiusX = new();
    private readonly TextBox _waterRadiusZ = new();
    private readonly TextBox _waterShoreWidth = new();
    private readonly TextBox _waterShoreStrength = new();
    private readonly TextBox _waterMaxRise = new();
    private readonly TextBox _waterMaxSlope = new();

    private readonly TextBlock _waterInfo = new()
    {
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap
    };

    private bool _updatingWaterControls;

    private void BuildWaterControls()
    {
        var controls = new StackPanel
        {
            Spacing = 7
        };

        controls.Children.Add(new TextBlock
        {
            Text = "Водоёмы уровня",
            FontWeight = FontWeight.Bold
        });

        controls.Children.Add(_waterList);

        AddWaterButton(
            controls,
            "Новый водоём возле камеры",
            PrepareNewWater);

        AddField(controls, "Название", _waterName);
        AddField(controls, "Центр X", _waterX);
        AddField(controls, "Центр Z", _waterZ);
        AddField(controls, "Высота поверхности воды", _waterHeight);
        AddField(controls, "Радиус X, м", _waterRadiusX);
        AddField(controls, "Радиус Z, м", _waterRadiusZ);

        controls.Children.Add(new TextBlock
        {
            Text = "Автоматическая береговая полоса",
            FontWeight = FontWeight.Bold
        });

        AddField(controls, "Материал песка", _waterSand);
        AddField(controls, "Ширина берега, м", _waterShoreWidth);
        AddField(controls, "Сила покрытия песком (0–1)", _waterShoreStrength);
        AddField(controls, "Максимальная высота берега, м", _waterMaxRise);
        AddField(controls, "Максимальный уклон", _waterMaxSlope);

        AddWaterButton(
            controls,
            "Создать / сохранить водоём",
            SaveWaterBody);

        AddWaterButton(
            controls,
            "Сгенерировать песчаный берег",
            GenerateWaterShore);

        AddWaterButton(
            controls,
            "Удалить выбранный водоём",
            RemoveSelectedWater);

        controls.Children.Add(_waterInfo);

        _materialControls.Children.Add(new Expander
        {
            Header = "Вода и автоматические берега",
            IsExpanded = false,
            Content = controls
        });

        _waterList.SelectionChanged += (_, _) =>
        {
            if (!_updatingWaterControls)
            {
                FillWaterControls();
            }
        };

        RefreshWaterControls();
    }

    private void AddWaterButton(
        StackPanel parent,
        string title,
        Action action)
    {
        var button = new Button
        {
            Content = title
        };

        button.Click += (_, _) =>
        {
            if (!Active)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception error)
            {
                _status.Text = error.Message;
            }
        };

        parent.Children.Add(button);
    }

    private void RefreshWaterControls(string? selectedId = null)
    {
        _updatingWaterControls = true;

        try
        {
            var document = _level?.TerrainDocument;

            selectedId ??=
                (_waterList.SelectedItem as TerrainWaterBody)?.Id;

            var waters = document?.WaterBodies.ToArray()
                ?? [];

            _waterList.ItemsSource = waters;

            _waterList.SelectedItem = waters.FirstOrDefault(
                water => water.Id == selectedId);

            var sands = document?.Layers
                .Where(layer =>
                    !layer.IsArchived &&
                    layer.Settings.SurfaceType == TerrainSurfaceType.Sand)
                .ToArray() ?? [];

            _waterSand.ItemsSource = sands;

            _waterInfo.Text =
                $"Водоёмов: {waters.Length}\n" +
                $"Доступных материалов песка: {sands.Length}";
        }
        finally
        {
            _updatingWaterControls = false;
        }

        FillWaterControls();
    }

    private void FillWaterControls()
    {
        if (_waterList.SelectedItem is not TerrainWaterBody water)
        {
            return;
        }

        _waterName.Text = water.Name;

        _waterX.Text = WaterFormat(water.CenterX);
        _waterZ.Text = WaterFormat(water.CenterZ);

        _waterHeight.Text = WaterFormat(water.SurfaceHeight);
        _waterRadiusX.Text = WaterFormat(water.RadiusX);
        _waterRadiusZ.Text = WaterFormat(water.RadiusZ);

        _waterShoreWidth.Text = WaterFormat(water.ShoreWidth);
        _waterShoreStrength.Text = WaterFormat(water.ShoreStrength);
        _waterMaxRise.Text = WaterFormat(water.MaxShoreRise);
        _waterMaxSlope.Text = WaterFormat(water.MaxShoreSlope);

        _waterSand.SelectedItem =
            (_waterSand.ItemsSource as IEnumerable<TerrainLayer>)?
            .FirstOrDefault(
                layer => layer.Index == water.SandLayerIndex);
    }

    private void PrepareNewWater()
    {
        if (_level?.TerrainDocument is not { } document)
        {
            return;
        }

        var pose = _viewport?.CameraPose;

        float x = Math.Clamp(
            pose?.X ?? document.Descriptor.OriginX + document.Descriptor.WorldSizeX * 0.5f,
            document.Descriptor.OriginX,
            document.Descriptor.MaxX);

        float z = Math.Clamp(
            pose?.Z ?? document.Descriptor.OriginZ + document.Descriptor.WorldSizeZ * 0.5f,
            document.Descriptor.OriginZ,
            document.Descriptor.MaxZ);

        _updatingWaterControls = true;

        try
        {
            _waterList.SelectedItem = null;
        }
        finally
        {
            _updatingWaterControls = false;
        }

        _waterName.Text = "Новый водоём";

        _waterX.Text = WaterFormat(x);
        _waterZ.Text = WaterFormat(z);

        _waterHeight.Text = WaterFormat(
            document.SampleWaterTerrainHeight(x, z) + 0.5f);

        _waterRadiusX.Text = "30";
        _waterRadiusZ.Text = "20";

        _waterShoreWidth.Text = "6";
        _waterShoreStrength.Text = "0.9";
        _waterMaxRise.Text = "2";
        _waterMaxSlope.Text = "0.65";

        _status.Text =
            "Параметры нового водоёма подготовлены. " +
            "Настройте высоту и размеры, затем создайте его.";
    }

    private void SaveWaterBody()
    {
        if (_level?.TerrainDocument is not { } document)
        {
            return;
        }

        _viewport?.EndTerrainPaint();

        var previous = _waterList.SelectedItem as TerrainWaterBody;

        var water = new TerrainWaterBody
        {
            Id = previous?.Id ??
                Guid.NewGuid().ToString("N"),

            Name = (_waterName.Text ?? "").Trim(),

            CenterX = WaterRead(_waterX, "Центр X"),
            CenterZ = WaterRead(_waterZ, "Центр Z"),

            SurfaceHeight = WaterRead(
                _waterHeight,
                "Высота воды"),

            RadiusX = WaterRead(_waterRadiusX, "Радиус X"),
            RadiusZ = WaterRead(_waterRadiusZ, "Радиус Z"),

            ShoreWidth = WaterRead(
                _waterShoreWidth,
                "Ширина берега"),

            ShoreStrength = WaterRead(
                _waterShoreStrength,
                "Сила покрытия"),

            MaxShoreRise = WaterRead(
                _waterMaxRise,
                "Высота берега"),

            MaxShoreSlope = WaterRead(
                _waterMaxSlope,
                "Уклон берега"),

            SandLayerIndex =
                (_waterSand.SelectedItem as TerrainLayer)?.Index ?? -1
        };

        document.SetWaterBody(water);

        RefreshWaterControls(water.Id);
        RefreshMode();

        _status.Text =
            "Водоём обновлён. " +
            "Сохраните Terrain для записи в файл уровня.";
    }

    private void GenerateWaterShore()
    {
        if (_level?.TerrainDocument is not { } document ||
            _waterList.SelectedItem is not TerrainWaterBody water)
        {
            throw new InvalidOperationException(
                "Сначала создайте и сохраните водоём.");
        }

        _viewport?.EndTerrainPaint();

        int tiles = document.GenerateShoreline(
            water.Id,
            _level.Roads);

        RefreshMode();

        _status.Text = tiles > 0
            ? $"Берег сгенерирован. Изменено тайлов: {tiles}. " +
              "Сохраните Terrain."
            : "Изменений нет. Проверьте уровень воды, " +
              "материал песка и приоритеты материалов.";
    }

    private void RemoveSelectedWater()
    {
        if (_level?.TerrainDocument is not { } document ||
            _waterList.SelectedItem is not TerrainWaterBody water)
        {
            return;
        }

        _viewport?.EndTerrainPaint();

        document.RemoveWaterBody(water.Id);

        RefreshWaterControls();
        RefreshMode();

        _status.Text =
            "Водоём удалён. " +
            "Ранее сгенерированный песок сохранён.";
    }

    private static string WaterFormat(float value)
    {
        return value.ToString(
            "G9",
            CultureInfo.InvariantCulture);
    }

    private static float WaterRead(
        TextBox input,
        string label)
    {
        string value = (input.Text ?? "")
            .Trim()
            .Replace(',', '.');

        if (!float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float result) ||
            !float.IsFinite(result))
        {
            throw new InvalidOperationException(
                $"{label}: укажите корректное число.");
        }

        return result;
    }
}
