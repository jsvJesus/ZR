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

namespace Editor.RoadSegments
{
    public sealed partial class RoadSegmentsPanel : UserControl
    {
        private readonly ListBox _roads = new() { MaxHeight = 150 };
        private readonly ListBox _segments = new() { MaxHeight = 130 };
        private readonly ComboBox _endpoint = new() { ItemsSource = new[] { "Начало сегмента", "Конец сегмента" }, SelectedIndex = 0 };
        private readonly ComboBox _materials = new() { MaxDropDownHeight = 260 };
        private readonly CheckBox _followTerrain = new() { Content = "По поверхности Terrain", IsChecked = true };
        private readonly TextBlock _heightLabel = Text("Смещение высоты, м");
        private readonly TextBox _x = new(), _y = new(), _z = new(), _tx = new(), _tz = new(), _width = new();
        private readonly TextBox _diffuse = new(), _normal = new(), _specular = new(), _specularPower = new(), _uv = new();
        private readonly TextBlock _summary = Text("Откройте уровень."), _status = Text(""), _textureInfo = Text("");
        private readonly TextBlock _diagnostics = Text("");
        private DxViewportHost? _viewport;
        private LoadedLevel? _level;
        private AssetPaths? _assets;
        private bool _binding;
        public bool IsSaving { get; private set; }
        public bool HasPendingChanges => _level?.Roads.IsDirty == true || IsSaving;
        private Task _saveTask = Task.CompletedTask;
        public event Action? BusyChanged;

        public RoadSegmentsPanel()
        {
            var root = new StackPanel { Spacing = 8 };
            root.Children.Add(_summary);
            root.Children.Add(Title("ДОРОГИ")); root.Children.Add(_roads);
            root.Children.Add(Title("СЕГМЕНТЫ")); root.Children.Add(_segments);
            AddButton(root, "Показать сегмент", () =>
            {
                if (SelectedRoad is { } road && SelectedSegment is { } segment) _viewport?.FocusRoadSegment(road, segment);
            });
            root.Children.Add(Title("УЗЕЛ")); root.Children.Add(_followTerrain); root.Children.Add(_endpoint);
            AddField(root, "X", _x); root.Children.Add(_heightLabel); root.Children.Add(_y); AddField(root, "Z", _z);
            AddField(root, "Ручка Bézier X", _tx); AddField(root, "Ручка Bézier Z", _tz); AddField(root, "Ширина, м", _width);
            AddButton(root, "Применить узел и размещение", ApplyNode);
            root.Children.Add(Text("Общий узел изменяет оба соседних сегмента."));
            AddButton(root, "Высоты узлов по Terrain", FitToTerrain);
            root.Children.Add(Title("МАТЕРИАЛ СЕГМЕНТА")); root.Children.Add(_materials);
            AddField(root, "Текстура DDS (пусто — из материала)", _diffuse);
            AddAsyncButton(root, "Выбрать текстуру…", () => BrowseTextureAsync(_diffuse));
            AddField(root, "Normal DDS (пусто — из материала)", _normal);
            AddAsyncButton(root, "Выбрать normal map…", () => BrowseTextureAsync(_normal));
            AddField(root, "Specular DDS (пусто — из материала)", _specular);
            AddAsyncButton(root, "Выбрать specular map…", () => BrowseTextureAsync(_specular));
            AddField(root, "Specular power DDS (пусто — из материала)", _specularPower);
            AddAsyncButton(root, "Выбрать карту резкости блика…", () => BrowseTextureAsync(_specularPower));
            AddField(root, "Повторение вдоль дороги", _uv);
            root.Children.Add(_textureInfo);
            AddButton(root, "Применить материал к сегменту", () => ApplyMaterial(false));
            AddButton(root, "Применить материал ко всей дороге", () => ApplyMaterial(true));
            AddButton(root, "Перечитать DDS", () => { _viewport?.ReloadRoadTextures(); UpdateTextureInfo(); UpdateSummary(); });
            AddAsyncButton(root, "Сохранить изменения дорог", SavePendingAsync);
            root.Children.Add(_status);
            root.Children.Add(new Expander { Header = "Диагностика исходных данных", Content = _diagnostics });
            Content = root;
            _roads.SelectionChanged += (_, _) =>
            {
                if (_binding) return;
                _segments.ItemsSource = SelectedRoad?.Segments;
                _segments.SelectedIndex = SelectedRoad?.Segments.Length > 0 ? 0 : -1;
                RefreshSelection();
            };
            _segments.SelectionChanged += (_, _) => { if (!_binding) RefreshSelection(); };
            _endpoint.SelectionChanged += (_, _) => { if (!_binding) FillNode(); };
            _followTerrain.IsCheckedChanged += (_, _) => { if (!_binding) FillNode(); };
            _materials.SelectionChanged += (_, _) => { if (!_binding) UpdateTextureInfo(); };
        }

        public void Attach(DxViewportHost viewport) => _viewport = viewport;

        public void Bind(LoadedLevel level, AssetPaths assets)
        {
            _binding = true;
            _level = level; _assets = assets;
            _roads.ItemsSource = level.Roads.Roads;
            _roads.SelectedIndex = level.Roads.Roads.Count > 0 ? 0 : -1;
            _segments.ItemsSource = SelectedRoad?.Segments;
            _segments.SelectedIndex = SelectedRoad?.Segments.Length > 0 ? 0 : -1;
            _materials.ItemsSource = level.Roads.Materials;
            _status.Text = "Изменения сохраняются отдельно от исходных DAT и XML.";
            _binding = false;
            UpdateSummary(); RefreshSelection();
        }

        public void RefreshHighlight()
        {
            if (_viewport != null) _viewport.RoadEditingEnabled = IsVisible;
            _viewport?.SelectRoadSegment(IsVisible ? SelectedRoad : null, IsVisible ? SelectedSegment : null);
        }

        public void Select(SourceRoad road, RoadSegment segment)
        {
            if (!IsVisible || !IsEnabled) return;
            _roads.SelectedItem = road;
            _segments.SelectedItem = segment;
            RefreshSelection();
        }

        private SourceRoad? SelectedRoad => _roads.SelectedItem as SourceRoad;
        private RoadSegment? SelectedSegment => _segments.SelectedItem as RoadSegment;
        private RoadNode? SelectedNode => SelectedRoad is { } road && SelectedSegment is { } segment
            ? road.Nodes[segment.Index + (_endpoint.SelectedIndex == 1 ? 1 : 0)] : null;

        private void RefreshSelection()
        {
            _binding = true;
            var segment = SelectedSegment;
            _followTerrain.IsChecked = segment?.FollowTerrain ?? true;
            _materials.SelectedItem = segment == null || _level == null ? null : RoadMaterials.Find(_level.Roads, segment.Material);
            _diffuse.Text = segment?.DiffuseOverride ?? ""; _normal.Text = segment?.NormalOverride ?? "";
            _specular.Text = segment?.SpecularOverride ?? ""; _specularPower.Text = segment?.SpecularPowerOverride ?? "";
            _uv.Text = Format(segment?.UvScale ?? 1);
            _binding = false;
            FillNode(); UpdateTextureInfo(); RefreshHighlight();
        }

        private void FillNode()
        {
            var node = SelectedNode;
            int nodeIndex = (SelectedSegment?.Index ?? 0) + (_endpoint.SelectedIndex == 1 ? 1 : 0);
            float originalY = SelectedRoad?.OriginalNodes[nodeIndex].Y ?? 0;
            bool follow = _followTerrain.IsChecked == true;
            _heightLabel.Text = follow ? "Смещение высоты, м" : "Y (исходная высота DAT)";
            _x.Text = node == null ? "" : Format(node.X); _y.Text = node == null ? "" : Format(follow ? node.Y - originalY : node.Y);
            _z.Text = node == null ? "" : Format(node.Z); _tx.Text = node == null ? "" : Format(node.TangentX);
            _tz.Text = node == null ? "" : Format(node.TangentZ); _width.Text = node == null ? "" : Format(node.Width);
        }

        private void UpdateTextureInfo()
        {
            if (_assets == null) return;
            var material = _materials.SelectedItem as RoadMaterial;
            string diffuse = string.IsNullOrWhiteSpace(_diffuse.Text) ? material?.Diffuse ?? "" : _diffuse.Text!.Trim();
            string normal = string.IsNullOrWhiteSpace(_normal.Text) ? material?.Normal ?? "" : _normal.Text!.Trim();
            string specular = string.IsNullOrWhiteSpace(_specular.Text) ? material?.Specular ?? "" : _specular.Text!.Trim();
            string power = string.IsNullOrWhiteSpace(_specularPower.Text) ? material?.SpecularPowerMap ?? "" : _specularPower.Text!.Trim();
            try
            {
                _textureInfo.Text = $"Цвет: {Describe(diffuse)}\nNormal: {Describe(normal)}\nSpecular: {Describe(specular)}\nРезкость блика: {Describe(power)}";
                string Describe(string value) => string.IsNullOrWhiteSpace(value) ? "не указан"
                    : RoadMaterials.ResolveTexture(_assets, value) == null ? $"нет DDS: {value}" : $"найден: {value}";
            }
            catch (Exception exception) { _textureInfo.Text = exception.Message; }
        }

        private void UpdateSummary()
        {
            if (_level == null || _assets == null) return;
            var network = _level.Roads;
            int missing = network.Roads.SelectMany(r => r.Segments).Select(s =>
                string.IsNullOrWhiteSpace(s.DiffuseOverride) ? RoadMaterials.Find(network, s.Material)?.Diffuse ?? "" : s.DiffuseOverride)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count(p => RoadMaterials.ResolveTexture(_assets, p) == null);
            _summary.Text = $"{_level.Manifest.Name}: {network.Roads.Count} дорог · {network.Roads.Sum(r => r.Segments.Length)} сегментов\n"
                + $"Материалов без DDS: {missing}" + (network.IsDirty ? "\nЕсть несохранённые изменения." : "");
            _diagnostics.Text = network.Warnings.Count == 0 ? "Замечаний нет."
                : string.Join("\n", network.Warnings);
        }

        private static TextBlock Text(string value) => new() { Text = value, TextWrapping = TextWrapping.Wrap, FontSize = 12 };
        private static TextBlock Title(string value) => new() { Text = value, FontWeight = FontWeight.SemiBold,
            FontSize = 12, Margin = new Thickness(0, 8, 0, 0) };
        private static void AddField(StackPanel root, string label, Control field)
        {
            root.Children.Add(Text(label)); root.Children.Add(field);
        }
        private void AddButton(StackPanel root, string label, Action action)
        {
            var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Stretch };
            button.Click += (_, _) =>
            {
                try { action(); } catch (Exception exception) { _status.Text = exception.Message; }
            };
            root.Children.Add(button);
        }
        private void AddAsyncButton(StackPanel root, string label, Func<Task> action)
        {
            var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Stretch };
            button.Click += async (_, _) =>
            {
                try { await action(); } catch (Exception exception) { _status.Text = exception.Message; }
            };
            root.Children.Add(button);
        }
        private static string Format(float value) => value.ToString("G9", CultureInfo.InvariantCulture);
        private static float Parse(TextBox box)
        {
            string text = (box.Text ?? "").Trim().Replace(',', '.');
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && float.IsFinite(value)
                ? value : throw new InvalidDataException("Ожидается конечное число.");
        }
    }
}
