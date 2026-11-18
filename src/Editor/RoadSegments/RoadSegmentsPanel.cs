using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Editor.Viewport;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Roads;

namespace Editor.RoadSegments;

public sealed partial class RoadSegmentsPanel : UserControl
{
    public StackPanel BrowserContent { get; } = new() { Spacing = 8 };
    private readonly ListBox _roads = new() { MaxHeight = 260 };
    private readonly ListBox _nodes = new() { MaxHeight = 220 };
    private readonly StackPanel _profile = new() { Spacing = 8 };
    private readonly StackPanel _point = new() { Spacing = 8 };
    private readonly StackPanel _create = new() { Spacing = 6 };
    private readonly StackPanel _railFields = new() { Spacing = 6 };
    private readonly TextBox _name = new();
    private readonly ComboBox _type = new()
    {
        ItemsSource = new[] { "Автомобильная дорога", "Железная дорога", "Тропинка" },
        SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch
    };
    private readonly CheckBox _follow = new() { Content = "Следовать Terrain", IsChecked = true };
    private readonly TextBox _width = new(), _shoulder = new(), _thickness = new(), _surfaceOffset = new();
    private readonly TextBox _lanes = new(), _gauge = new(), _spacing = new();
    private readonly TextBox _x = new(), _y = new(), _z = new();
    private readonly CheckBox _terrainModifier = new() { Content = "Дорожный коридор Terrain" };
    private readonly CheckBox _preserveElevation = new() { Content = "Сохранить проектную высоту (миграция V1)" };
    private readonly TextBox _cutLimit = new(), _fillLimit = new(), _transition = new(), _baseDepth = new();
    private readonly TextBox _materialSearch = new() { Watermark = "Поиск покрытия по модели или текстуре" };
    private readonly ComboBox _roadMaterial = new() { HorizontalAlignment = HorizontalAlignment.Stretch, MaxDropDownHeight = 280 };
    private readonly TextBox _textureRepeat = new() { Text = "4" }, _materialWetness = new() { Text = "1" };
    private readonly TextBlock _materialInfo = Text("Каталог покрытий: objects/roads и objects/roads_segments.");
    private RoadAssetCatalog? _assetCatalog;
    private AssetPaths? _assets;
    private string _retainedMaterialId = "";
    private readonly TextBlock _heightLabel = Text("Y, м");
    private readonly TextBlock _summary = Text("Откройте уровень."), _status = Text("");
    private DxViewportHost? _viewport;
    private LoadedLevel? _level;
    private bool _binding;
    private Task _saveTask = Task.CompletedTask;
    public bool IsSaving { get; private set; }
    public bool HasPendingChanges => _level?.Roads.IsDirty == true || IsSaving;
    public event Action? BusyChanged;
    private RoadDefinition? SelectedRoad => _roads.SelectedItem as RoadDefinition;
    private int NodeIndex => _nodes.SelectedIndex;

    public RoadSegmentsPanel()
    {
        var root = new StackPanel { Spacing = 8 };
        BrowserContent.Children.Add(_summary);
        BrowserContent.Children.Add(Title("СЕТЬ ДОРОГ"));
        BrowserContent.Children.Add(_roads);
        AddButton(_create, "Новая автомобильная дорога", () => CreateRoad(RoadType.Automobile));
        AddButton(_create, "Новая железная дорога", () => CreateRoad(RoadType.Railway));
        AddButton(_create, "Новая тропинка", () => CreateRoad(RoadType.Path));
        BrowserContent.Children.Add(_create);
        BrowserContent.Children.Add(Title("ОПОРНЫЕ ТОЧКИ"));
        BrowserContent.Children.Add(_nodes);
        root.Children.Add(Text("Ctrl + ЛКМ — добавить точку выбранной дороги на Terrain. " +
            "ЛКМ — выбрать дорогу или точку; тяните точку мышью. Shift — сетка 0,5 м. " +
            "Esc / ПКМ — отменить перетаскивание. Высота задаётся в свойствах точки."));
        AddButton(root, "Показать выбранное", () => { if (SelectedRoad is { } road) _viewport?.FocusRoad(road, NodeIndex); });
        Field(_profile, "Название", _name); Field(_profile, "Тип дороги", _type);
        _profile.Children.Add(_follow);
        Field(_profile, "Ширина полотна / основания, м", _width);
        Field(_profile, "Ширина обочины с каждой стороны, м", _shoulder);
        Field(_profile, "Толщина основания, м", _thickness);
        Field(_profile, "Смещение поверхности, м", _surfaceOffset);
        Field(_profile, "Число автомобильных полос", _lanes);
        Field(_railFields, "Ширина колеи, м", _gauge);
        Field(_railFields, "Шаг шпал, м", _spacing);
        _profile.Children.Add(_railFields);
        _profile.Children.Add(_terrainModifier);
        _profile.Children.Add(_preserveElevation);
        Field(_profile, "Допустимая выемка, м", _cutLimit);
        Field(_profile, "Допустимая насыпь, м", _fillLimit);
        Field(_profile, "Переход к рельефу, м", _transition);
        Field(_profile, "Зазор под покрытием, м", _baseDepth);
        var materials = new StackPanel { Spacing = 6 };
        var loadMaterials = new Button { Content = "Прочитать покрытия из моделей дорог", HorizontalAlignment = HorizontalAlignment.Stretch };
        loadMaterials.Click += async (_, _) =>
        {
            if (_assets == null) return;
            loadMaterials.IsEnabled = false;
            try
            {
                _assetCatalog = await RoadAssetCatalog.LoadCachedAsync(_assets);
                FilterMaterials();
                _materialInfo.Text = $"Моделей: {_assetCatalog.Entries.Count}; наборов текстур: {_assetCatalog.Materials.Count}.";
            }
            catch (Exception exception) { _materialInfo.Text = exception.Message; }
            finally { loadMaterials.IsEnabled = true; }
        };
        materials.Children.Add(loadMaterials); materials.Children.Add(_materialSearch);
        materials.Children.Add(_roadMaterial); materials.Children.Add(_materialInfo);
        Field(materials, "Повтор текстуры, м", _textureRepeat);
        Field(materials, "Намокание покрытия (0–1)", _materialWetness);
        AddButton(materials, "Процедурное покрытие", () => { _retainedMaterialId = ""; _roadMaterial.SelectedItem = null; });
        _profile.Children.Add(new Expander { Header = "Покрытие из дорожных моделей", Content = materials });
        _materialSearch.TextChanged += (_, _) => FilterMaterials();
        _roadMaterial.SelectionChanged += (_, _) =>
        {
            if (!_binding && _roadMaterial.SelectedItem is RoadMaterialOption material)
            { _retainedMaterialId = material.Id; _materialInfo.Text = material.Diffuse; }
        };
        AddButton(root, "Отменить изменение дороги", () => RestoreRoadHistory(false));
        AddButton(root, "Повторить изменение дороги", () => RestoreRoadHistory(true));

        AddButton(
            _profile,
            "Применить профиль дороги",
            ApplyProfile);

        AddButton(
            _profile,
            "Подогнать Terrain к дороге",
            ConformRoadTerrain);

        AddButton(
            _profile,
            "Удалить дорогу",
            DeleteRoad);

        root.Children.Add(new Expander { Header = "Профиль дороги", IsExpanded = true, Content = _profile });
        Field(_point, "X, м", _x); _point.Children.Add(_heightLabel); _point.Children.Add(_y); Field(_point, "Z, м", _z);
        AddButton(_point, "Применить точку", ApplyNode);
        AddButton(_point, "Вставить точку после выбранной", InsertNode);
        AddButton(_point, "Поставить точку на Terrain", SnapNode);
        AddButton(_point, "Удалить точку [Delete]", DeleteNode);
        root.Children.Add(new Expander { Header = "Опорная точка", IsExpanded = true, Content = _point });
        var save = new Button { Content = "Сохранить дороги", HorizontalAlignment = HorizontalAlignment.Stretch };
        save.Click += async (_, _) => { try { await SavePendingAsync(); } catch (Exception exception) { _status.Text = exception.Message; } };
        root.Children.Add(save); root.Children.Add(_status); Content = root;
        _roads.SelectionChanged += (_, _) => { if (!_binding) RefreshSelection(-1); };
        _nodes.SelectionChanged += (_, _) => { if (!_binding) { FillNode(); RefreshHighlight(); } };
        _type.SelectionChanged += (_, _) => _railFields.IsVisible = _type.SelectedIndex == (int)RoadType.Railway;
    }

    public void Attach(DxViewportHost viewport)
    {
        _viewport = viewport;
        viewport.RoadsChanged += () => RefreshRoadList(SelectedRoad, NodeIndex);
        viewport.RoadDeleteNodeRequested += () => { try { DeleteNode(); } catch (Exception exception) { _status.Text = exception.Message; } };
    }

    public void Bind(LoadedLevel level, AssetPaths assets)
    {
        _assets = assets;
        _level = level;
        RefreshRoadList(level.Roads.Roads.FirstOrDefault(), -1);
        _status.Text = level.Roads.Warnings.Count == 0
            ? "Создайте дорогу и добавьте опорные точки через Ctrl + ЛКМ."
            : string.Join("\n", level.Roads.Warnings);
    }

    public void Select(RoadDefinition? road, int nodeIndex)
    {
        if (!IsVisible || !IsEnabled) return;
        RefreshRoadList(road, nodeIndex);
    }

    public void RefreshHighlight()
    {
        bool editable = IsVisible && IsEnabled && !IsSaving && _level?.Roads.CanSave == true &&
            _viewport?.IsPlaying != true && _viewport?.IsStarting != true;
        if (_viewport != null) _viewport.RoadEditingEnabled = editable;
        bool controls = editable && _viewport?.IsRoadDragging != true;
        _create.IsEnabled = controls; _roads.IsEnabled = controls; _nodes.IsEnabled = controls;
        _profile.IsEnabled = controls && SelectedRoad != null;
        _point.IsEnabled = controls && SelectedRoad != null && NodeIndex >= 0;
        if (_viewport?.IsRoadDragging != true) _viewport?.SelectRoad(editable ? SelectedRoad : null, NodeIndex);
    }

    private void RefreshRoadList(RoadDefinition? road, int node)
    {
        _binding = true;
        try
        {
            _roads.ItemsSource = _level?.Roads.Roads.ToArray();
            _roads.SelectedItem = road != null && _level?.Roads.Roads.Contains(road) == true ? road : null;
        }
        finally { _binding = false; }
        RefreshSelection(node);
        if (_level != null) _summary.Text = $"{_level.Manifest.Name}: {_level.Roads.Roads.Count} дорог · " +
            $"{_level.Roads.Roads.Sum(item => item.Nodes.Count)} точек" + (_level.Roads.IsDirty ? "\nЕсть несохранённые изменения." : "");
    }

    private void RefreshSelection(int node)
    {
        _binding = true;
        try
        {
            var road = SelectedRoad;
            _nodes.ItemsSource = road?.Nodes.ToArray();
            _nodes.SelectedIndex = road != null && node >= 0 && node < road.Nodes.Count ? node : -1;
            _name.Text = road?.Name ?? ""; _type.SelectedIndex = (int)(road?.Type ?? RoadType.Automobile);
            _follow.IsChecked = road?.FollowTerrain ?? true;
            _width.Text = Format(road?.Width ?? 7); _shoulder.Text = Format(road?.ShoulderWidth ?? 0.75f);
            _thickness.Text = Format(road?.Thickness ?? 0.2f); _surfaceOffset.Text = Format(road?.SurfaceOffset ?? 0.06f);
            _lanes.Text = (road?.LaneCount ?? 2).ToString(CultureInfo.InvariantCulture);
            _gauge.Text = Format(road?.RailGauge ?? 1.52f); _spacing.Text = Format(road?.SleeperSpacing ?? 0.65f);
            _terrainModifier.IsChecked = road?.TerrainModifierEnabled ?? true;
            _preserveElevation.IsChecked = road?.PreserveSourceElevation ?? false;
            _cutLimit.Text = Format(road?.TerrainCutLimit ?? 6); _fillLimit.Text = Format(road?.TerrainFillLimit ?? 6);
            _transition.Text = Format(road?.TerrainTransitionWidth ?? 4); _baseDepth.Text = Format(road?.TerrainBaseDepth ?? 0.02f);
            _retainedMaterialId = road?.MaterialId ?? "";
            _textureRepeat.Text = Format(road?.TextureRepeatLength ?? 4);
            _materialWetness.Text = Format(road?.MaterialWetness ?? 1);
            FilterMaterials();
        }
        finally { _binding = false; }
        FillNode(); RefreshHighlight();
    }

    private void FillNode()
    {
        var road = SelectedRoad;
        var node = road != null && NodeIndex >= 0 && NodeIndex < road.Nodes.Count ? road.Nodes[NodeIndex] : null;
        _heightLabel.Text = road?.FollowTerrain == true ? "Смещение точки над Terrain, м" : "Y, м";
        _x.Text = node == null ? "" : Format(node.X); _z.Text = node == null ? "" : Format(node.Z);
        _y.Text = node == null ? "" : Format(road!.FollowTerrain ? node.HeightOffset : node.Y);
    }

    private void FilterMaterials()
    {
        bool binding = _binding;
        _binding = true;
        try
        {
            var selected = _assetCatalog?.Materials.FirstOrDefault(item => item.Id == _retainedMaterialId);
            var materials = _assetCatalog?.SearchMaterials(_materialSearch.Text)
                .Where(item => !item.AlphaBlend && !item.RequiresSpecializedShader && !string.IsNullOrEmpty(item.Diffuse))
                .Take(200).ToList() ?? [];
            if (selected != null && !materials.Contains(selected)) materials.Insert(0, selected);
            _roadMaterial.ItemsSource = materials;
            _roadMaterial.SelectedItem = selected;
        }
        finally { _binding = binding; }
    }

    private static TextBlock Text(string text) => new() { Text = text, FontSize = 12, TextWrapping = TextWrapping.Wrap };
    private static TextBlock Title(string text) => new() { Text = text, FontSize = 12, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 8, 0, 0) };
    private static void Field(StackPanel root, string label, Control control) { root.Children.Add(Text(label)); root.Children.Add(control); }
    private void AddButton(StackPanel root, string label, Action action)
    {
        var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Stretch };
        button.Click += (_, _) => { try { action(); } catch (Exception exception) { _status.Text = exception.Message; } };
        root.Children.Add(button);
    }
    private static string Format(float value) => value.ToString("G9", CultureInfo.InvariantCulture);
    private static float Parse(TextBox box) => float.TryParse((box.Text ?? "").Trim().Replace(',', '.'),
        NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && float.IsFinite(value)
        ? value : throw new InvalidDataException("Ожидается конечное число.");
}
