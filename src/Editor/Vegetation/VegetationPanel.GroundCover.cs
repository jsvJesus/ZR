using System.Globalization;
using System.IO;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Layout;
using Runtime.Viewport;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Vegetation;

namespace Editor.Vegetation;

public sealed partial class VegetationPanel
{
    private static readonly string[] VegetationModes = ["Деревья и кусты", "Трава и мелкие камни"];
    private readonly ComboBox _vegetationMode = new() { ItemsSource = VegetationModes, SelectedIndex = 0 };
    private readonly ComboBox _vegetationBrowserMode = new() { ItemsSource = VegetationModes, SelectedIndex = 0 };
    private readonly StackPanel _treeControls = new() { Spacing = 8 };
    private readonly StackPanel _treeBrowser = new() { Spacing = 8 };
    private readonly StackPanel _coverControls = new() { Spacing = 8, IsVisible = false };
    private readonly StackPanel _coverBrowser = new() { Spacing = 8, IsVisible = false };
    private readonly StackPanel _coverModelList = new() { Spacing = 4 };
    private readonly ComboBox _coverCategory = new();
    private readonly ComboBox _coverMaterial = new();
    private readonly TextBox _coverSearch = new() { PlaceholderText = "Поиск травы и камней objects/flora…" };
    private readonly TextBlock _coverCount = Text("Каталог загружается…");
    private readonly TextBlock _coverInfo = Text("");
    private readonly TextBlock _coverBrowserInfo = Text("");
    private readonly TextBox _coverDensity = new() { Text = "4" };
    private readonly TextBox _coverScaleMin = new() { Text = "0.8" };
    private readonly TextBox _coverScaleMax = new() { Text = "1.2" };
    private readonly TextBox _coverSlope = new() { Text = "45" };
    private readonly TextBox _coverRoadMargin = new() { Text = "1" };
    private readonly TextBox _coverObjectMargin = new() { Text = "0.5" };
    private readonly TextBox _coverDrawDistance = new() { Text = "40" };
    private readonly TextBox _coverSeed = new() { Text = "12345" };
    private readonly TextBox _coverRadius = new() { Text = "5" };
    private readonly CheckBox _coverBrushEnabled = new() { Content = "Включить кисть покрытия" };
    private readonly ComboBox _coverBrushAction = new() { ItemsSource = new[] { "Рисовать", "Стирать" }, SelectedIndex = 0 };
    private readonly Button _coverUndo = new() { Content = "Отменить мазок", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly Button _coverRedo = new() { Content = "Повторить мазок", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly HashSet<string> _coverSelectedModels = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<GroundCoverCatalogEntry> _coverEntries = [];
    private CancellationTokenSource? _coverCatalogLoading;
    private bool _coverBinding;
    private bool _coverModeBinding;
    private int _coverWarningCount;
    private bool GroundCoverMode => _vegetationMode.SelectedIndex == 1;

    private StackPanel BuildGroundCoverControls()
    {
        var layout = new StackPanel { Spacing = 8 };
        layout.Children.Add(_vegetationMode);
        // Keep cancellation outside the disabled tree editor during generation.
        _batchCancel.IsVisible = false;
        layout.Children.Add(_batchCancel);
        layout.Children.Add(_treeControls);
        layout.Children.Add(_coverControls);
        BrowserContent.Children.Add(_vegetationBrowserMode);
        BrowserContent.Children.Add(_treeBrowser);
        BrowserContent.Children.Add(_coverBrowser);
        _vegetationMode.SelectionChanged += (_, _) => ChangeVegetationMode(_vegetationMode.SelectedIndex);
        _vegetationBrowserMode.SelectionChanged += (_, _) => ChangeVegetationMode(_vegetationBrowserMode.SelectedIndex);

        _coverBrowser.Children.Add(_coverBrowserInfo);
        _coverBrowser.Children.Add(Title("ТРАВА И МЕЛКИЕ КАМНИ OBJECTS/FLORA"));
        _coverBrowser.Children.Add(Text("Модели SCO/SCB из objects/flora. Пары файлов показаны одной моделью; " +
            "отдельные LOD, деревья и кусты скрыты."));
        _coverBrowser.Children.Add(_coverCategory);
        _coverBrowser.Children.Add(_coverSearch);
        _coverBrowser.Children.Add(_coverCount);
        var selections = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        Button(selections, "Выбрать найденные", () => SelectCoverModels(true));
        Button(selections, "Снять найденные", () => SelectCoverModels(false));
        _coverBrowser.Children.Add(selections);
        _coverBrowser.Children.Add(new ScrollViewer { Content = _coverModelList, MaxHeight = 400 });
        _coverCategory.SelectionChanged += (_, _) => RebuildCoverModels();
        _coverSearch.TextChanged += (_, _) => RebuildCoverModels();

        _coverControls.Children.Add(Title("ПЛОТНОЕ ПОКРЫТИЕ TERRAIN"));
        _coverControls.Children.Add(Text("Выберите модели в каталоге слева. Плотность задаётся на 1 м². " +
            "Покрытие появляется рядом с камерой; дороги, объекты и уклон ограничивают расстановку."));
        Field(_coverControls, "Материал Terrain", _coverMaterial);
        Field(_coverControls, "Плотность, экземпляров на м² (0.1–16)", _coverDensity);
        Field(_coverControls, "Минимальный масштаб (0.05–10)", _coverScaleMin);
        Field(_coverControls, "Максимальный масштаб (до 10)", _coverScaleMax);
        Field(_coverControls, "Максимальный уклон, градусы (0–85)", _coverSlope);
        Field(_coverControls, "Отступ от дорог, м (0–30)", _coverRoadMargin);
        Field(_coverControls, "Отступ от объектов, м (0–30)", _coverObjectMargin);
        Field(_coverControls, "Дальность отображения, м (1–100)", _coverDrawDistance);
        Field(_coverControls, "Seed", _coverSeed);
        Button(_coverControls, "Применить параметры покрытия", () => ApplyGroundCoverSettings());
        Button(_coverControls, "Автопокрытие по материалу Terrain", () => ApplyGroundCoverSettings(true));
        Button(_coverControls, "Выключить автопокрытие", () =>
        {
            RequireGroundCoverEditable();
            StopGroundCoverBrush();
            _level!.GroundCover.SetSettings(_level.GroundCover.Settings with { AutoGenerate = false });
            RefreshGroundCoverInfo();
            _status.Text = "Автопокрытие выключено. Мазки кисти сохранены.";
        });
        Button(_coverControls, "Очистить всё покрытие", () =>
        {
            RequireGroundCoverEditable();
            StopGroundCoverBrush();
            _level!.GroundCover.Clear();
            BindGroundCover();
            _status.Text = "Покрытие очищено. Сохраните растительность.";
        });
        _coverControls.Children.Add(Title("КИСТЬ ПОКРЫТИЯ"));
        Field(_coverControls, "Радиус кисти, м (0.25–100)", _coverRadius);
        Field(_coverControls, "Действие кисти", _coverBrushAction);
        _coverControls.Children.Add(_coverBrushEnabled);
        _coverControls.Children.Add(Text("ЛКМ — мазок по Terrain. Повторный мазок не накапливает экземпляры. " +
            "Shift + ЛКМ — временное стирание. Esc / ПКМ — завершить мазок."));
        _coverControls.Children.Add(_coverUndo);
        _coverControls.Children.Add(_coverRedo);
        _coverControls.Children.Add(_coverInfo);
        _coverUndo.Click += (_, _) => ChangeCoverHistory(false);
        _coverRedo.Click += (_, _) => ChangeCoverHistory(true);
        _coverBrushEnabled.IsCheckedChanged += (_, _) =>
        {
            if (_coverBinding) return;
            try
            {
                if (_coverBrushEnabled.IsChecked == true)
                {
                    RequireGroundCoverEditable();
                    ApplyGroundCoverSettings(requireModels: _coverBrushAction.SelectedIndex != 1);
                    ReadCoverRadius();
                }
                else _viewport?.EndGroundCoverPaint();
            }
            catch (Exception exception) { StopGroundCoverBrush(); _status.Text = exception.Message; }
            RefreshMode();
        };
        _coverBrushAction.SelectionChanged += (_, _) =>
        {
            if (_coverBinding) return;
            _viewport?.EndGroundCoverPaint();
            if (_coverBrushAction.SelectedIndex != 1 && _coverSelectedModels.Count == 0)
            {
                StopGroundCoverBrush();
                _status.Text = "Для рисования выберите модели покрытия.";
            }
            RefreshMode();
        };
        _coverMaterial.SelectionChanged += (_, _) => CoverSettingsEdited();
        foreach (var field in new[] { _coverDensity, _coverScaleMin, _coverScaleMax, _coverSlope,
                     _coverRoadMargin, _coverObjectMargin, _coverDrawDistance, _coverSeed, _coverRadius })
            field.TextChanged += (_, _) => CoverSettingsEdited();
        return layout;
    }

    private void ChangeVegetationMode(int index)
    {
        if (_coverModeBinding) return;
        _viewport?.CancelTreeDrag();
        StopGroundCoverBrush();
        _coverModeBinding = true;
        try { _vegetationMode.SelectedIndex = index; _vegetationBrowserMode.SelectedIndex = index; }
        finally { _coverModeBinding = false; }
        RefreshMode();
    }

    private void AttachGroundCover(DxViewportHost viewport, AssetPaths assets)
    {
        viewport.GroundCoverStrokeStarted += () =>
        {
            if (CanEditGroundCover() && _coverBrushEnabled.IsChecked == true) _level!.GroundCover.BeginStroke();
        };
        viewport.GroundCoverStrokeEnded += () => { _level?.GroundCover.EndStroke(); RefreshGroundCoverInfo(); };
        viewport.GroundCoverBrushRequested += PaintGroundCover;
        viewport.CameraChanged += _ =>
        {
            if (IsVisible && GroundCoverMode && _level is { } level &&
                level.GroundCover.Warnings.Count != _coverWarningCount)
                RefreshGroundCoverInfo();
        };
        _ = LoadGroundCoverCatalogAsync(assets);
        RefreshMode();
    }

    private async Task LoadGroundCoverCatalogAsync(AssetPaths assets)
    {
        _coverCatalogLoading?.Cancel();
        var loading = new CancellationTokenSource();
        _coverCatalogLoading = loading;
        try
        {
            var entries = await Task.Run(() => GroundCoverCatalog.Catalog(assets), loading.Token);
            if (_coverCatalogLoading != loading || loading.IsCancellationRequested) return;
            _coverEntries = entries;
            _coverCategory.ItemsSource = new[] { "Все категории" }.Concat(entries.Select(entry => entry.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(category => category, StringComparer.OrdinalIgnoreCase)).ToArray();
            _coverCategory.SelectedIndex = 0;
            RebuildCoverModels();
            RefreshGroundCoverInfo();
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            if (_coverCatalogLoading == loading) _coverCount.Text = $"Ошибка каталога: {exception.Message}";
        }
        finally { if (_coverCatalogLoading == loading) _coverCatalogLoading = null; loading.Dispose(); }
    }

    private void BindGroundCover()
    {
        _coverBinding = true;
        try
        {
            _coverBrushEnabled.IsChecked = false;
            var settings = _level?.GroundCover.Settings ?? new GroundCoverSettings();
            _coverSelectedModels.Clear();
            foreach (string model in settings.Models) _coverSelectedModels.Add(model);
            _coverDensity.Text = Format(settings.DensityPerSquareMeter);
            _coverScaleMin.Text = Format(settings.MinimumScale);
            _coverScaleMax.Text = Format(settings.MaximumScale);
            _coverSlope.Text = Format(settings.MaximumSlopeDegrees);
            _coverRoadMargin.Text = Format(settings.RoadClearance);
            _coverObjectMargin.Text = Format(settings.ObjectClearance);
            _coverDrawDistance.Text = Format(settings.DrawDistance);
            _coverSeed.Text = settings.Seed.ToString(CultureInfo.InvariantCulture);
            RefreshCoverMaterials(settings.MaterialIndex);
        }
        finally { _coverBinding = false; }
        RebuildCoverModels();
        RefreshGroundCoverInfo();
    }

    private IEnumerable<GroundCoverCatalogEntry> FilteredCoverModels()
    {
        string search = (_coverSearch.Text ?? "").Trim();
        string? category = _coverCategory.SelectedItem as string;
        return _coverEntries.Where(entry => (category == null || category == "Все категории" || entry.Category == category) &&
            (entry.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || entry.File.Contains(search, StringComparison.OrdinalIgnoreCase)));
    }

    private void RefreshCoverMaterials(int? selectedIndex = null)
    {
        int index = selectedIndex ?? (_coverMaterial.SelectedItem as CoverMaterialChoice)?.Index ??
            _level?.GroundCover.Settings.MaterialIndex ?? -1;
        var materials = new List<CoverMaterialChoice> { new(-1, "Все материалы Terrain") };
        if (_level?.TerrainDocument is { } terrain)
            materials.AddRange(terrain.Layers.Where(layer => !layer.IsArchived)
                .Select(layer => new CoverMaterialChoice(layer.Index, layer.ToString())));
        // Keep a missing/archived saved material explicit; never silently replace it with 'all'.
        if (materials.All(material => material.Index != index))
            materials.Add(new CoverMaterialChoice(index, $"Недоступный материал #{index}"));
        if ((_coverMaterial.ItemsSource as IEnumerable<CoverMaterialChoice>)?.SequenceEqual(materials) == true &&
            (_coverMaterial.SelectedItem as CoverMaterialChoice)?.Index == index) return;
        bool binding = _coverBinding;
        _coverBinding = true;
        try
        {
            _coverMaterial.ItemsSource = materials;
            _coverMaterial.SelectedItem = materials.First(material => material.Index == index);
        }
        finally { _coverBinding = binding; }
    }

    private void RebuildCoverModels()
    {
        _coverModelList.Children.Clear();
        foreach (var entry in FilteredCoverModels())
        {
            var checkbox = new CheckBox { Content = entry.ToString(), IsChecked = _coverSelectedModels.Contains(entry.File) };
            checkbox.IsCheckedChanged += (_, _) =>
            {
                if (checkbox.IsChecked == true) _coverSelectedModels.Add(entry.File);
                else _coverSelectedModels.Remove(entry.File);
                CoverSettingsEdited();
                UpdateCoverModelCount();
            };
            _coverModelList.Children.Add(checkbox);
        }
        UpdateCoverModelCount();
        if (_coverEntries.Count == 0)
            _coverCount.Text = "В objects/flora не найдены модели травы и мелких камней (.sco/.scb).";
    }

    private void SelectCoverModels(bool selected)
    {
        foreach (var entry in FilteredCoverModels())
            if (selected) _coverSelectedModels.Add(entry.File); else _coverSelectedModels.Remove(entry.File);
        CoverSettingsEdited();
        RebuildCoverModels();
    }

    private void UpdateCoverModelCount() => _coverCount.Text =
        $"Показано: {FilteredCoverModels().Count()} · Всего: {_coverEntries.Count} · Выбрано: {_coverSelectedModels.Count}";

    private bool CanEditGroundCover() => GroundCoverMode && IsVisible && IsEnabled && !IsSaving &&
        _level?.GroundCover.CanSave == true && _level.TerrainDocument != null &&
        _viewport != null && !_viewport.IsPlaying && !_viewport.IsStarting && !_viewport.IsTreeDragging;

    private void RequireGroundCoverEditable()
    {
        if (!CanEditGroundCover()) throw new InvalidOperationException("Редактирование покрытия сейчас недоступно.");
    }

    private void ApplyGroundCoverSettings(bool? autoGenerate = null, bool requireModels = true)
    {
        RequireGroundCoverEditable();
        _viewport?.EndGroundCoverPaint();
        if (requireModels && _coverSelectedModels.Count == 0)
            throw new InvalidOperationException("Выберите хотя бы одну модель травы или мелких камней.");
        if (!int.TryParse((_coverSeed.Text ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed))
            throw new InvalidDataException("Seed должен быть целым числом.");
        var document = _level!.GroundCover;
        var settings = document.Settings with
        {
            Models = _coverSelectedModels.OrderBy(model => model, StringComparer.OrdinalIgnoreCase).ToArray(),
            MaterialIndex = (_coverMaterial.SelectedItem as CoverMaterialChoice)?.Index ?? -1,
            DensityPerSquareMeter = Read(_coverDensity),
            MinimumScale = Read(_coverScaleMin), MaximumScale = Read(_coverScaleMax),
            MaximumSlopeDegrees = Read(_coverSlope), RoadClearance = Read(_coverRoadMargin),
            ObjectClearance = Read(_coverObjectMargin), DrawDistance = Read(_coverDrawDistance),
            Seed = seed, AutoGenerate = autoGenerate ?? document.Settings.AutoGenerate
        };
        document.SetSettings(settings);
        RefreshGroundCoverInfo();
        _status.Text = settings.AutoGenerate ? "Автопокрытие включено. Сохраните растительность." : "Параметры покрытия применены.";
    }

    private float ReadCoverRadius()
    {
        float radius = Read(_coverRadius);
        if (radius is < 0.25f or > 100) throw new InvalidDataException("Радиус кисти: от 0.25 до 100 м.");
        return radius;
    }

    private void StopGroundCoverBrush()
    {
        _viewport?.EndGroundCoverPaint();
        bool binding = _coverBinding;
        _coverBinding = true;
        try { _coverBrushEnabled.IsChecked = false; }
        finally { _coverBinding = binding; }
        if (_viewport != null) _viewport.GroundCoverBrushEnabled = false;
    }

    private void CoverSettingsEdited()
    {
        if (_coverBinding) return;
        StopGroundCoverBrush();
        _status.Text = "Параметры изменены. Примените их или включите кисть.";
    }

    private void PaintGroundCover(Vector3 center, bool erase)
    {
        if (!CanEditGroundCover() || _coverBrushEnabled.IsChecked != true || _assets == null) return;
        try
        {
            int changed = GroundCoverGenerator.Paint(_level!, _assets, center, ReadCoverRadius(), erase);
            RefreshGroundCoverInfo();
            _status.Text = $"{(erase ? "Стирание" : "Покрытие")}: изменено ячеек {changed}.";
        }
        catch (Exception exception) { StopGroundCoverBrush(); _status.Text = exception.Message; }
    }

    private void ChangeCoverHistory(bool redo)
    {
        try
        {
            RequireGroundCoverEditable();
            _viewport?.EndGroundCoverPaint();
            if (redo) _level!.GroundCover.Redo(); else _level!.GroundCover.Undo();
            BindGroundCover();
            RefreshMode();
        }
        catch (Exception exception) { _status.Text = exception.Message; }
    }

    private void RefreshGroundCoverMode()
    {
        RefreshCoverMaterials();
        bool cover = GroundCoverMode;
        _treeControls.IsVisible = _treeBrowser.IsVisible = !cover;
        _coverControls.IsVisible = _coverBrowser.IsVisible = cover;
        bool editable = CanEditGroundCover();
        _coverControls.IsEnabled = _coverBrowser.IsEnabled = editable;
        _vegetationMode.IsEnabled = _vegetationBrowserMode.IsEnabled = !IsSaving;
        if (_viewport != null)
        {
            bool enabled = editable && _coverBrushEnabled.IsChecked == true;
            if (!enabled) _viewport.EndGroundCoverPaint();
            _viewport.GroundCoverBrushEnabled = enabled;
            _viewport.GroundCoverBrushErase = _coverBrushAction.SelectedIndex == 1;
            if (enabled)
            {
                try { _viewport.GroundCoverBrushRadius = ReadCoverRadius(); }
                catch (Exception exception) { StopGroundCoverBrush(); _status.Text = exception.Message; }
            }
        }
        RefreshGroundCoverInfo();
    }

    private void RefreshGroundCoverInfo()
    {
        var document = _level?.GroundCover;
        _coverWarningCount = document?.Warnings.Count ?? 0;
        string info = document == null ? "Откройте уровень с Terrain." :
            $"{_level!.Manifest.Name}\nАвтопокрытие: {(document.Settings.AutoGenerate ? "включено" : "выключено")}\n" +
            $"Моделей: {document.Settings.Models.Length} · Плотность: {Format(document.Settings.DensityPerSquareMeter)} / м²\n" +
            (document.IsDirty ? "Есть несохранённые изменения." : "Изменения сохранены.");
        if (document?.Warnings.Count > 0) info += "\n" + string.Join("\n", document.Warnings);
        _coverInfo.Text = _coverBrowserInfo.Text = info;
        _coverUndo.IsEnabled = CanEditGroundCover() && document?.CanUndo == true;
        _coverRedo.IsEnabled = CanEditGroundCover() && document?.CanRedo == true;
    }

    private sealed record CoverMaterialChoice(int Index, string Name)
    {
        public override string ToString() => Name;
    }
}
