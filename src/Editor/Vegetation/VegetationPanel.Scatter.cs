using Avalonia.Controls;
using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Vegetation;

namespace Editor.Vegetation;

public sealed partial class VegetationPanel
{
    private readonly ListBox _scatterRules = new()
    {
        MaxHeight = 160
    };

    private readonly ComboBox _scatterModels = new();
    private readonly ComboBox _scatterMaterials = new();

    private readonly ComboBox _scatterKind = new()
    {
        ItemsSource = Enum.GetValues<VegetationScatterKind>(),
        SelectedIndex = 0
    };

    private readonly CheckBox _scatterEnabled = new()
    {
        Content = "Правило включено",
        IsChecked = true
    };

    private readonly CheckBox _scatterNearWater = new()
    {
        Content = "Только рядом с водой"
    };

    private readonly TextBox _scatterName = new();
    private readonly TextBox _scatterSeed = new() { Text = "12345" };
    private readonly TextBox _scatterDensity = new() { Text = "0.5" };
    private readonly TextBox _scatterScaleMin = new() { Text = "0.8" };
    private readonly TextBox _scatterScaleMax = new() { Text = "1.2" };
    private readonly TextBox _scatterWeight = new() { Text = "0.55" };
    private readonly TextBox _scatterSlope = new() { Text = "35" };
    private readonly TextBox _scatterObjectDistance = new() { Text = "2" };
    private readonly TextBox _scatterRoadDistance = new() { Text = "3" };
    private readonly TextBox _scatterSpacing = new() { Text = "2" };
    private readonly TextBox _scatterShoreDistance = new() { Text = "5" };

    private readonly TextBox _scatterTileX = new() { Text = "0" };
    private readonly TextBox _scatterTileZ = new() { Text = "0" };

    private readonly TextBlock _scatterInfo = Text("");

    private bool _scatterBinding;

    private void BuildScatterControls(StackPanel root)
    {
        var controls = new StackPanel
        {
            Spacing = 8
        };

        controls.Children.Add(Title("ПРАВИЛА ГЕНЕРАЦИИ"));
        controls.Children.Add(_scatterRules);
        BuildScatterBatchControls(controls);

        Button(
            controls,
            "Новое правило",
            NewScatterRule);

        ScatterField(controls, "Название правила", _scatterName);
        ScatterField(controls, "Категория", _scatterKind);
        ScatterField(controls, "Модель SpeedTree", _scatterModels);
        ScatterField(controls, "Материал Terrain", _scatterMaterials);

        controls.Children.Add(_scatterEnabled);

        ScatterField(controls, "Seed", _scatterSeed);
        ScatterField(controls, "Плотность на 100 м²", _scatterDensity);
        ScatterField(controls, "Минимальный масштаб", _scatterScaleMin);
        ScatterField(controls, "Максимальный масштаб", _scatterScaleMax);
        ScatterField(controls, "Минимальный вес материала", _scatterWeight);
        ScatterField(controls, "Максимальный уклон, градусы", _scatterSlope);

        controls.Children.Add(Title("ЗАЩИТНЫЕ РАССТОЯНИЯ"));

        ScatterField(
            controls,
            "Отступ от объектов, м",
            _scatterObjectDistance);

        ScatterField(
            controls,
            "Отступ от дорог, м",
            _scatterRoadDistance);

        ScatterField(
            controls,
            "Расстояние между растениями, м",
            _scatterSpacing);

        controls.Children.Add(_scatterNearWater);

        ScatterField(
            controls,
            "Максимальное расстояние до берега, м",
            _scatterShoreDistance);

        Button(
            controls,
            "Сохранить правило",
            SaveScatterRule);

        Button(
            controls,
            "Удалить правило",
            DeleteScatterRule);

        BuildScatterBatchActions(controls);
        controls.Children.Add(Title("ГЕНЕРАЦИЯ ПО ТАЙЛАМ"));

        ScatterField(controls, "Тайл X", _scatterTileX);
        ScatterField(controls, "Тайл Z", _scatterTileZ);

        Button(
            controls,
            "Выбрать тайл под камерой",
            SelectScatterCameraTile);

        Button(
            controls,
            "Перегенерировать выбранный тайл",
            GenerateScatterTile);

        controls.Children.Add(_scatterInfo);

        root.Children.Add(new Expander
        {
            Header = "Автоматическая генерация SpeedTree",
            IsExpanded = false,
            Content = controls
        });

        _scatterRules.SelectionChanged += (_, _) =>
        {
            if (!_scatterBinding)
            {
                FillScatterRule();
            }
        };
    }

    private static void ScatterField(
        StackPanel parent,
        string label,
        Control control)
    {
        parent.Children.Add(Text(label));
        parent.Children.Add(control);
    }

    private void RefreshScatterControls(Guid? selectedId = null)
    {
        _scatterBinding = true;

        try
        {
            selectedId ??=
                (_scatterRules.SelectedItem as VegetationScatterRule)?.Id;

            var rules = _level?.Vegetation.ScatterRules.ToArray()
                ?? [];

            _scatterRules.ItemsSource = rules;

            _scatterRules.SelectedItem = rules.FirstOrDefault(
                rule => rule.Id == selectedId);

            _scatterModels.ItemsSource = _entries;

            _scatterMaterials.ItemsSource =
                _level?.TerrainDocument?.Layers
                    .Where(layer =>
                        !layer.IsArchived &&
                        layer.Settings.SurfaceType is not (
                            TerrainSurfaceType.Water or
                            TerrainSurfaceType.Rock or
                            TerrainSurfaceType.Asphalt or
                            TerrainSurfaceType.Glass))
                    .ToArray() ?? [];

            int manual = _level?.Vegetation.Instances.Count ?? 0;
            int generated = _level?.Vegetation.Generated.Count ?? 0;

            _scatterInfo.Text =
                $"Ручных экземпляров: {manual}\n" +
                $"Сгенерированных: {generated}\n" +
                $"Правил: {rules.Length}";
        }
        finally
        {
            _scatterBinding = false;
        }

        FillScatterRule();
    }

    private void FillScatterRule()
    {
        if (_scatterRules.SelectedItem is not
            VegetationScatterRule rule)
        {
            return;
        }

        _scatterBinding = true;

        try
        {
            _scatterName.Text = rule.Name;
            _scatterKind.SelectedItem = rule.Kind;

            _scatterModels.SelectedItem = _entries.FirstOrDefault(
                item => string.Equals(
                    item.File,
                    rule.Model,
                    StringComparison.OrdinalIgnoreCase));

            _scatterMaterials.SelectedItem =
                (_scatterMaterials.ItemsSource as
                    IEnumerable<TerrainLayer>)?
                .FirstOrDefault(
                    item => item.Index == rule.MaterialIndex);

            _scatterEnabled.IsChecked = rule.Enabled;
            _scatterNearWater.IsChecked = rule.NearWaterOnly;

            _scatterSeed.Text = rule.Seed.ToString();

            _scatterDensity.Text =
                Format(rule.DensityPer100SquareMeters);

            _scatterScaleMin.Text = Format(rule.MinimumScale);
            _scatterScaleMax.Text = Format(rule.MaximumScale);
            _scatterWeight.Text = Format(rule.MinimumMaterialWeight);
            _scatterSlope.Text = Format(rule.MaximumSlopeDegrees);

            _scatterObjectDistance.Text =
                Format(rule.ObjectClearance);

            _scatterRoadDistance.Text =
                Format(rule.RoadClearance);

            _scatterSpacing.Text =
                Format(rule.MinimumSpacing);

            _scatterShoreDistance.Text =
                Format(rule.MaximumShoreDistance);
        }
        finally
        {
            _scatterBinding = false;
        }
    }

    private void NewScatterRule()
    {
        RequireEditable();

        _scatterBinding = true;

        try
        {
            _scatterRules.SelectedItem = null;
            _scatterName.Text = "Новое правило";
            _scatterKind.SelectedIndex = 0;

            _scatterEnabled.IsChecked = true;
            _scatterNearWater.IsChecked = false;

            _scatterSeed.Text = "12345";
            _scatterDensity.Text = "0.5";
            _scatterScaleMin.Text = "0.8";
            _scatterScaleMax.Text = "1.2";
            _scatterWeight.Text = "0.55";
            _scatterSlope.Text = "35";
            _scatterObjectDistance.Text = "2";
            _scatterRoadDistance.Text = "3";
            _scatterSpacing.Text = "2";
            _scatterShoreDistance.Text = "5";
        }
        finally
        {
            _scatterBinding = false;
        }
    }

    private void SaveScatterRule()
    {
        RequireEditable();

        var scene = _level!.Vegetation;

        var selected = _scatterRules.SelectedItem as
            VegetationScatterRule;

        if (_scatterModels.SelectedItem is not TreeCatalogEntry model)
        {
            throw new InvalidOperationException(
                "Выберите модель SpeedTree.");
        }

        if (_scatterMaterials.SelectedItem is not TerrainLayer material)
        {
            throw new InvalidOperationException(
                "Выберите материал Terrain.");
        }

        if (!int.TryParse(_scatterSeed.Text, out int seed))
        {
            throw new InvalidOperationException(
                "Seed должен быть целым числом.");
        }

        var rule = new VegetationScatterRule
        {
            Id = selected?.Id ?? Guid.NewGuid(),

            Name = (_scatterName.Text ?? "").Trim(),
            Model = model.File,

            Kind = (VegetationScatterKind)(
                _scatterKind.SelectedItem ??
                VegetationScatterKind.Grass),

            MaterialIndex = material.Index,

            Seed = seed,

            DensityPer100SquareMeters = Read(_scatterDensity),
            MinimumScale = Read(_scatterScaleMin),
            MaximumScale = Read(_scatterScaleMax),

            MinimumMaterialWeight = Read(_scatterWeight),
            MaximumSlopeDegrees = Read(_scatterSlope),

            ObjectClearance = Read(_scatterObjectDistance),
            RoadClearance = Read(_scatterRoadDistance),
            MinimumSpacing = Read(_scatterSpacing),

            NearWaterOnly = _scatterNearWater.IsChecked == true,
            MaximumShoreDistance = Read(_scatterShoreDistance),

            Enabled = _scatterEnabled.IsChecked == true
        };

        VegetationScatterGenerator.ValidateRule(rule);

        int index = scene.ScatterRules.FindIndex(
            item => item.Id == rule.Id);

        if (index >= 0)
        {
            scene.ScatterRules[index] = rule;
        }
        else
        {
            scene.ScatterRules.Add(rule);
        }

        scene.IsDirty = true;

        RefreshScatterControls(rule.Id);
        LoadBatchModelSelection();
        UpdateSummary();

        _status.Text =
            "Правило сохранено. Перегенерируйте нужный тайл.";
    }

    private void DeleteScatterRule()
    {
        RequireEditable();

        if (_scatterRules.SelectedItem is not
            VegetationScatterRule rule)
        {
            return;
        }

        var scene = _level!.Vegetation;

        scene.ScatterRules.RemoveAll(
            item => item.Id == rule.Id);

        scene.Generated.RemoveAll(
            item => item.RuleId == rule.Id);

        scene.IsDirty = true;

        RefreshScatterControls();
        LoadBatchModelSelection();
        UpdateSummary();

        _status.Text =
            "Правило и его автоматические экземпляры удалены. " +
            "Ручные деревья сохранены.";
    }

    private void SelectScatterCameraTile()
    {
        RequireEditable();

        if (_level?.TerrainDocument is not { } document ||
            _viewport == null)
        {
            return;
        }

        var pose = _viewport.CameraPose;
        var descriptor = document.Descriptor;

        float tileSize = 256f * descriptor.CellSize;

        int tx = Math.Clamp(
            (int)MathF.Floor(pose.X / tileSize),
            0,
            descriptor.TilesX - 1);

        int tz = Math.Clamp(
            (int)MathF.Floor(pose.Z / tileSize),
            0,
            descriptor.TilesZ - 1);

        _scatterTileX.Text = tx.ToString();
        _scatterTileZ.Text = tz.ToString();

        _status.Text = $"Выбран тайл {tx}, {tz}.";
    }

    private void GenerateScatterTile()
    {
        RequireEditable();

        if (_assets == null || _level == null)
        {
            return;
        }

        if (!int.TryParse(_scatterTileX.Text, out int tx) ||
            !int.TryParse(_scatterTileZ.Text, out int tz))
        {
            throw new InvalidOperationException(
                "Укажите корректные координаты тайла.");
        }

        int count = VegetationScatterGenerator.RegenerateTile(
            _level,
            _assets,
            tx,
            tz);

        RefreshScatterControls();
        UpdateSummary();

        _status.Text =
            $"Тайл {tx}, {tz}: создано {count} экземпляров. " +
            "Сохраните растительность.";
    }
}
