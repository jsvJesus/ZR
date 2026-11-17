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
