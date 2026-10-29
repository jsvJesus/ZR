using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Editor.Viewport;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Vegetation;

namespace Editor.Vegetation
{
    public sealed partial class VegetationPanel : UserControl
    {
        private readonly TextBox _search = new() { Watermark = "Поиск дерева…" };
        private readonly ListBox _catalog = new() { MaxHeight = 170 };
        private readonly ListBox _instances = new() { MaxHeight = 150 };
        private readonly CheckBox _placing = new() { Content = "ЛКМ — расставлять выбранное дерево" };
        private readonly CheckBox _grounded = new() { Content = "Привязать к Terrain", IsChecked = true };
        private readonly TextBlock _heightLabel = Text("Смещение высоты, м");
        private readonly TextBox _x = new(), _y = new(), _z = new();
        private readonly TextBox _yaw = new() { Text = "0" }, _scale = new() { Text = "1" };
        private readonly TextBlock _summary = Text("Откройте уровень."), _modelInfo = Text("Выберите дерево."), _status = Text("");
        private IReadOnlyList<TreeCatalogEntry> _entries = [];
        private DxViewportHost? _viewport;
        private AssetPaths? _assets;
        private LoadedLevel? _level;
        private TreeAsset? _selectedAsset;
        private CancellationTokenSource? _loading;
        private bool _binding;
        private Task _saveTask = Task.CompletedTask;
        public bool IsSaving { get; private set; }
        public bool HasPendingChanges => _level?.Vegetation.IsDirty == true || IsSaving;
        public event Action? BusyChanged;

        public VegetationPanel()
        {
            var root = new StackPanel { Spacing = 8 };
            root.Children.Add(_summary); root.Children.Add(Title("КАТАЛОГ ДЕРЕВЬЕВ"));
            root.Children.Add(_search); root.Children.Add(_catalog); root.Children.Add(_modelInfo);
            root.Children.Add(_placing);
            root.Children.Add(Text("Снимите флажок, чтобы выбирать уже поставленные деревья щелчком."));
            root.Children.Add(Title("ДЕРЕВЬЯ УРОВНЯ")); root.Children.Add(_instances);
            Button(root, "Показать выбранное", () => { if (Selected is { } tree) _viewport?.FocusTree(tree); });
            root.Children.Add(_grounded);
            Field(root, "X", _x); root.Children.Add(_heightLabel); root.Children.Add(_y); Field(root, "Z", _z);
            Field(root, "Поворот, градусы", _yaw); Field(root, "Масштаб", _scale);
            Button(root, "Применить параметры", ApplyProperties);
            Button(root, "Поставить на Terrain", SnapToGround);
            Button(root, "Удалить выбранное дерево", DeleteSelected);
            var save = new Button { Content = "Сохранить растительность", HorizontalAlignment = HorizontalAlignment.Stretch };
            save.Click += async (_, _) =>
            {
                try { await SavePendingAsync(); } catch (Exception exception) { _status.Text = exception.Message; }
            };
            root.Children.Add(save); root.Children.Add(_status); Content = root;
            _search.TextChanged += (_, _) => FilterCatalog();
            _catalog.SelectionChanged += async (_, _) => { if (!_binding) await LoadSelectedAsync(); };
            _instances.SelectionChanged += (_, _) => { if (!_binding) FillInstance(); };
            _placing.IsCheckedChanged += (_, _) => { if (!_binding) RefreshMode(); };
            _grounded.IsCheckedChanged += (_, _) => { if (!_binding) FillHeight(); };
        }

        public void Attach(DxViewportHost viewport, AssetPaths assets)
        {
            _viewport = viewport; _assets = assets;
            _entries = SpeedTreeReader.Catalog(assets); FilterCatalog();
            viewport.TreePlaced += tree =>
            {
                RefreshInstances(tree); _status.Text = "Дерево поставлено. ЛКМ — следующее дерево.";
            };
            viewport.TreePicked += tree => { _instances.SelectedItem = tree; FillInstance(); };
        }

        public void Bind(LoadedLevel level)
        {
            _level = level; RefreshInstances(null); RefreshMode();
            _status.Text = level.Vegetation.Warnings.Count > 0 ? string.Join("\n", level.Vegetation.Warnings)
                : "Расстановка доступна в режиме редактирования.";
        }

        public void RefreshMode()
        {
            if (_viewport == null) return;
            bool editable = IsVisible && IsEnabled && _level?.Vegetation.CanSave == true && !IsSaving;
            try
            {
                bool placing = _placing.IsChecked == true;
                float scale = Read(_scale), yaw = Read(_yaw);
                if (scale is < 0.05f or > 100) throw new InvalidDataException("Масштаб: от 0.05 до 100.");
                _viewport.SetTreePlacement(editable && placing ? _selectedAsset : null, scale, yaw);
                _viewport.TreeEditingEnabled = editable && (!placing || _selectedAsset != null);
                _viewport.SelectTree(editable ? Selected : null);
            }
            catch (Exception exception)
            {
                _viewport.TreeEditingEnabled = false; _viewport.SetTreePlacement(null); _status.Text = exception.Message;
            }
        }

        private TreeInstance? Selected => _instances.SelectedItem as TreeInstance;

        private void FilterCatalog()
        {
            string search = (_search.Text ?? "").Trim();
            _catalog.ItemsSource = _entries.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        private async Task LoadSelectedAsync()
        {
            _loading?.Cancel();
            var entry = _catalog.SelectedItem as TreeCatalogEntry;
            _selectedAsset = null; RefreshMode();
            if (entry == null || _assets == null) { _modelInfo.Text = "Выберите дерево."; return; }
            var cancellation = new CancellationTokenSource(); _loading = cancellation;
            var token = cancellation.Token; var assets = _assets;
            _modelInfo.Text = "Чтение дерева…";
            try
            {
                var asset = _level?.Vegetation.Assets.GetValueOrDefault(entry.File)
                    ?? await Task.Run(() => SpeedTreeReader.Read(assets, entry.File, token), token);
                if (_loading != cancellation || token.IsCancellationRequested) return;
                _selectedAsset = asset;
                _modelInfo.Text = $"{entry.Name}\nВысота: {asset.Maximum.Y - asset.Minimum.Y:F2} м · частей: {asset.Parts.Length}";
                RefreshMode();
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { if (_loading == cancellation) _modelInfo.Text = $"Ошибка чтения: {exception.Message}"; }
            finally { if (_loading == cancellation) _loading = null; cancellation.Dispose(); }
        }

        private void RefreshInstances(TreeInstance? selected)
        {
            _binding = true; _instances.ItemsSource = null; _instances.ItemsSource = _level?.Vegetation.Instances;
            _instances.SelectedItem = selected; _binding = false; FillInstance(); UpdateSummary();
        }
        private void FillInstance()
        {
            var tree = Selected;
            _binding = true; _grounded.IsChecked = tree?.FollowTerrain ?? true; _binding = false;
            _x.Text = tree == null ? "" : Format(tree.X);
            _z.Text = tree == null ? "" : Format(tree.Z);
            if (tree != null) { _yaw.Text = Format(tree.Yaw); _scale.Text = Format(tree.Scale); }
            FillHeight(); RefreshMode();
        }
        private void FillHeight()
        {
            bool follow = _grounded.IsChecked == true;
            _heightLabel.Text = follow ? "Смещение высоты, м" : "Y, м";
            _y.Text = Selected is { } tree ? Format(follow ? tree.HeightOffset : tree.Y) : "";
        }
        private void UpdateSummary()
        {
            if (_level == null) return;
            _summary.Text = $"{_level.Manifest.Name}: {_level.Vegetation.Instances.Count} деревьев\nКаталог: {_entries.Count} моделей"
                + (_level.Vegetation.IsDirty ? "\nЕсть несохранённые изменения." : "");
        }

        private static TextBlock Text(string value) => new() { Text = value, TextWrapping = TextWrapping.Wrap, FontSize = 12 };
        private static TextBlock Title(string value) => new() { Text = value, FontWeight = FontWeight.SemiBold,
            FontSize = 12, Margin = new Thickness(0, 8, 0, 0) };
        private static void Field(StackPanel root, string label, Control field) { root.Children.Add(Text(label)); root.Children.Add(field); }
        private void Button(StackPanel root, string label, Action action)
        {
            var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Stretch };
            button.Click += (_, _) => { try { action(); } catch (Exception exception) { _status.Text = exception.Message; } };
            root.Children.Add(button);
        }
        private static string Format(float value) => value.ToString("G9", CultureInfo.InvariantCulture);
        private static float Read(TextBox box) => float.TryParse((box.Text ?? "").Trim().Replace(',', '.'),
            NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && float.IsFinite(value)
            ? value : throw new InvalidDataException("Ожидается конечное число.");
    }
}
