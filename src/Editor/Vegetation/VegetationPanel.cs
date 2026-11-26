using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Runtime.Viewport;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Vegetation;

namespace Editor.Vegetation
{
    public sealed partial class VegetationPanel : UserControl
    {
        public StackPanel BrowserContent { get; } = new() { Spacing = 8 };
        private readonly TextBox _search = new() { PlaceholderText = "Поиск модели в категории…" };
        private readonly ComboBox _categories = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
        private readonly TextBlock _catalogCount = Text("");
        private readonly ListBox _catalog = new() { MaxHeight = 300 };
        private readonly ListBox _instances = new() { MaxHeight = 150 };
        private readonly Button _move = new() { Content = "Перемещение [1]", HorizontalAlignment = HorizontalAlignment.Stretch };
        private readonly Button _rotate = new() { Content = "Вращение [2]", HorizontalAlignment = HorizontalAlignment.Stretch };
        private readonly StackPanel _transformProperties = new() { Spacing = 8 };
        private readonly TextBox _placementHeight = new() { Text = "0" };
        private readonly CheckBox _grounded = new() { Content = "Привязать к Terrain", IsChecked = true };
        private readonly TextBlock _heightLabel = Text("Смещение высоты, м");
        private readonly TextBox _x = new(), _y = new(), _z = new();
        private readonly TextBox _yaw = new() { Text = "0" }, _scale = new() { Text = "1" };
        private readonly TextBox _pitch = new() { Text = "0" }, _roll = new() { Text = "0" };
        private readonly TextBlock _summary = Text("Откройте уровень."), _modelInfo = Text("Выберите модель."), _status = Text("");
        private IReadOnlyList<TreeCatalogEntry> _entries = [];
        private DxViewportHost? _viewport;
        private AssetPaths? _assets;
        private LoadedLevel? _level;
        private TreeAsset? _selectedAsset;
        private CancellationTokenSource? _loading;
        private bool _binding;
        private Task _saveTask = Task.CompletedTask;
        public bool IsSaving { get; private set; }
        public bool HasPendingChanges => _level?.Vegetation.IsDirty == true || _level?.GroundCover.IsDirty == true || IsSaving;
        public event Action? BusyChanged;

        public VegetationPanel()
        {
            var root = _treeControls;
            _treeBrowser.Children.Add(_summary); _treeBrowser.Children.Add(Title("ДЕРЕВЬЯ И КУСТЫ SPEEDTREE"));
            _treeBrowser.Children.Add(_categories);
            _treeBrowser.Children.Add(_search); _treeBrowser.Children.Add(_catalogCount); _treeBrowser.Children.Add(_catalog);
            _treeBrowser.Children.Add(Title("РАСТИТЕЛЬНОСТЬ УРОВНЯ")); _treeBrowser.Children.Add(_instances);
            root.Children.Add(_modelInfo);
            root.Children.Add(Text("Ctrl + ЛКМ — поставить выбранную модель на Terrain. ЛКМ — выбрать растение."));
            root.Children.Add(_move); root.Children.Add(_rotate);
            root.Children.Add(Text("ЛКМ — тянуть цветную ось или кольцо. X — красная, Y — зелёная, Z — синяя. " +
                "Esc / ПКМ — отменить. Shift — шаг 0,5 м / 15°. Delete — удалить."));
            Field(root, "Смещение новых растений над Terrain, м", _placementHeight);
            Button(root, "Показать выбранное", () => { if (Selected is { } tree) _viewport?.FocusTree(tree); });
            var properties = _transformProperties;
            properties.Children.Add(_grounded);

            Field(properties, "X", _x); properties.Children.Add(_heightLabel); properties.Children.Add(_y); Field(properties, "Z", _z);
            Field(properties, "Поворот Y (yaw), градусы", _yaw);
            Field(properties, "Поворот X (pitch), градусы", _pitch);
            Field(properties, "Поворот Z (roll), градусы", _roll);
            Field(properties, "Масштаб", _scale);
            Button(properties, "Применить параметры", ApplyProperties);
            Button(properties, "Поставить на Terrain", SnapToGround);
            Button(properties, "Удалить выбранное растение", DeleteSelected);
            root.Children.Add(new Expander { Header = "Размещение и трансформация", IsExpanded = true, Content = properties });
            var scatter = new StackPanel { Spacing = 8 };
            BuildScatterControls(scatter);
            root.Children.Add(new Expander { Header = "Массовая расстановка", Content = scatter });

            var save = new Button { Content = "Сохранить растительность", HorizontalAlignment = HorizontalAlignment.Stretch };
            save.Click += async (_, _) =>
            {
                try { await SavePendingAsync(); } catch (Exception exception) { _status.Text = exception.Message; }
            };
            var layout = BuildGroundCoverControls();
            layout.Children.Add(save); layout.Children.Add(_status); Content = layout;
            _search.TextChanged += (_, _) => FilterCatalog();
            _categories.SelectionChanged += (_, _) => FilterCatalog();
            _catalog.SelectionChanged += async (_, _) => { if (!_binding) await LoadSelectedAsync(); };
            _instances.SelectionChanged += (_, _) => { if (!_binding) { FillInstance(); SelectInstanceModel(); } };
            _move.Click += (_, _) => { if (_viewport != null) _viewport.TreeTransformMode = ObjectGizmoMode.Move; };
            _rotate.Click += (_, _) => { if (_viewport != null) _viewport.TreeTransformMode = ObjectGizmoMode.Rotate; };
            _grounded.IsCheckedChanged += (_, _) =>
            {
                if (_binding) return;
                try { ChangeTerrainAttachment(); }
                catch (Exception exception) { _status.Text = exception.Message; FillInstance(); }
            };
            foreach (var field in new[] { _yaw, _pitch, _roll, _scale, _placementHeight })
                field.TextChanged += (_, _) => { if (!_binding) RefreshMode(); };
        }

        public void Attach(DxViewportHost viewport, AssetPaths assets)
        {
            _viewport = viewport; _assets = assets;
            _entries = SpeedTreeReader.Catalog(assets)
                .Where(entry => entry.Category is "Tree" or "Bush").ToArray();
            var categories = _entries.Select(entry => entry.Category).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(category => category == "Tree" ? 0 : 1)
                .ThenBy(category => category, StringComparer.OrdinalIgnoreCase).ToArray();
            _categories.ItemsSource = categories;
            _categories.SelectedItem = categories.FirstOrDefault();
            _batchCategory.ItemsSource = new[] { "Все категории" }.Concat(categories).ToArray();
            _batchCategory.SelectedIndex = 0;
            FilterCatalog();
            RefreshScatterControls();
            RefreshBatchMaterials();
            viewport.TreePlaced += tree =>
            {
                RefreshInstances(tree); _status.Text = "Растение размещено. Ctrl + ЛКМ — следующий экземпляр; ЛКМ — выбрать или переместить.";
            };
            viewport.TreePicked += tree => { _instances.SelectedItem = tree; FillInstance(); SelectInstanceModel(); };
            viewport.TreeTransformed += tree => { if (ReferenceEquals(tree, Selected)) { FillInstance(); UpdateSummary(); } };
            viewport.TreeToolChanged += RefreshMode;
            viewport.TreeDeleteRequested += () =>
            {
                try { DeleteSelected(); } catch (Exception exception) { _status.Text = exception.Message; }
            };
            AttachGroundCover(viewport, assets);
        }

        public void Bind(LoadedLevel level)
        {
            _viewport?.EndGroundCoverPaint();
            _level = level;
            BindGroundCover();
            RefreshScatterControls();
            RefreshBatchMaterials();
            RefreshInstances(null);
            RefreshMode();
            var warnings = level.Vegetation.Warnings.Concat(level.GroundCover.Warnings).ToArray();
            _status.Text = warnings.Length > 0 ? string.Join("\n", warnings)
                : "Расстановка доступна в режиме редактирования.";
        }

        public void RefreshMode()
        {
            RefreshGroundCoverMode();
            if (_viewport == null) return;
            _batchCancel.IsVisible = IsGeneratingScatter && !GroundCoverMode;
            _batchCancel.IsEnabled = IsGeneratingScatter && IsVisible && IsEnabled && !GroundCoverMode &&
                _level?.Vegetation.CanSave == true && !_viewport.IsPlaying && !_viewport.IsStarting;
            bool editable = !GroundCoverMode && IsVisible && IsEnabled && _level?.Vegetation.CanSave == true && !IsSaving &&
                !_viewport.IsPlaying && !_viewport.IsStarting;
            _treeControls.IsEnabled = editable;
            _viewport.TreeEditingEnabled = editable;
            bool controls = editable && !_viewport.IsTreeDragging;
            _move.IsEnabled = controls; _rotate.IsEnabled = controls;
            _transformProperties.IsEnabled = controls;
            _placementHeight.IsEnabled = controls;
            _categories.IsEnabled = controls; _catalog.IsEnabled = controls;
            _search.IsEnabled = controls; _instances.IsEnabled = controls;
            bool moving = _viewport.TreeTransformMode == ObjectGizmoMode.Move;
            _move.Content = moving ? "● Перемещение [1]" : "Перемещение [1]";
            _rotate.Content = moving ? "Вращение [2]" : "● Вращение [2]";
            if (_viewport.IsTreeDragging) return;
            _viewport.SelectTree(editable ? Selected : null);
            if (!editable)
            {
                _viewport.SetTreePlacement(null);
                return;
            }
            try
            {
                float scale = Read(_scale), yaw = Read(_yaw);
                if (scale is < 0.05f or > 100) throw new InvalidDataException("Масштаб: от 0.05 до 100.");
                float offset = Read(_placementHeight);
                if (MathF.Abs(offset) > 1e6) throw new InvalidDataException("Слишком большое смещение высоты.");
                _viewport.SetTreePlacement(editable ? _selectedAsset : null, scale, yaw,
                    Read(_pitch), Read(_roll), _grounded.IsChecked == true, offset);
            }
            catch (Exception exception)
            {
                _viewport.SetTreePlacement(null); _status.Text = exception.Message;
            }
        }

        private TreeInstance? Selected => _instances.SelectedItem as TreeInstance;

        private void FilterCatalog()
        {
            string search = (_search.Text ?? "").Trim();
            string? category = _categories.SelectedItem as string;
            var entries = _entries.Where(e => e.Category == category &&
                e.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToArray();
            var selected = _catalog.SelectedItem as TreeCatalogEntry;
            bool binding = _binding;
            _binding = true;
            try
            {
                _catalog.ItemsSource = entries;
                _catalog.SelectedItem = selected != null && entries.Contains(selected) ? selected : null;
            }
            finally { _binding = binding; }
            _catalogCount.Text = $"{category ?? "Категория не выбрана"}: {entries.Length} моделей";
            if (_catalog.SelectedItem == null) _ = LoadSelectedAsync();
        }

        private async Task LoadSelectedAsync()
        {
            _loading?.Cancel();
            _loading = null;
            var entry = _catalog.SelectedItem as TreeCatalogEntry;
            _selectedAsset = null; RefreshMode();
            if (entry == null || _assets == null) { _modelInfo.Text = "Выберите модель."; return; }
            var cancellation = new CancellationTokenSource(); _loading = cancellation;
            var token = cancellation.Token; var assets = _assets;
            _modelInfo.Text = "Чтение модели…";
            try
            {
                var asset = _level?.Vegetation.Assets.GetValueOrDefault(entry.File)
                    ?? await Task.Run(() => SpeedTreeReader.Read(assets, entry.File, token), token);
                if (_loading != cancellation || token.IsCancellationRequested) return;
                _selectedAsset = asset;
                string collision = !VegetationCategory.HasBarkCollision(asset.File) ? "Без коллизии"
                    : asset.Parts.Any(part => part.Kind == TreePartKind.Branches)
                        ? "Коллизия: bark (ствол и ветки)" : "Без коллизии: в модели нет геометрии bark";
                _modelInfo.Text = $"{entry.Category} / {entry.DisplayName}\nВысота: {asset.Maximum.Y - asset.Minimum.Y:F2} м · частей: {asset.Parts.Length}\n{collision}";
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
            _binding = true;
            try
            {
                if (tree != null) _grounded.IsChecked = tree.FollowTerrain;
                _x.Text = tree == null ? "" : Format(tree.X);
                _z.Text = tree == null ? "" : Format(tree.Z);
                if (tree != null)
                {
                    _yaw.Text = Format(tree.Yaw); _pitch.Text = Format(tree.Pitch); _roll.Text = Format(tree.Roll);
                    _scale.Text = Format(tree.Scale);
                }
            }
            finally { _binding = false; }
            FillHeight(); RefreshMode();
        }
        private void FillHeight()
        {
            bool follow = _grounded.IsChecked == true;
            _heightLabel.Text = follow ? "Смещение высоты, м" : "Y, м";
            _y.Text = Selected is { } tree ? Format(follow ? tree.HeightOffset : TreeHeight(tree)) : "";
        }

        private void SelectInstanceModel()
        {
            if (Selected is not { } tree || _catalog.SelectedItem is TreeCatalogEntry current && current.File == tree.Model) return;
            var entry = _entries.FirstOrDefault(item => item.File.Equals(tree.Model, StringComparison.OrdinalIgnoreCase));
            if (entry == null) return;
            _search.Text = "";
            _categories.SelectedItem = entry.Category;
            _catalog.SelectedItem = entry;
        }
        private void UpdateSummary()
        {
            if (_level == null) return;
            _summary.Text =
                $"{_level.Manifest.Name}\n" +
                $"Размещено вручную: {_level.Vegetation.Instances.Count}\n" +
                $"Сгенерировано: {_level.Vegetation.Generated.Count}\n" +
                $"Каталог SpeedTree: {_entries.Count} моделей" +
                (_level.Vegetation.IsDirty
                    ? "\nЕсть несохранённые изменения."
                    : "");
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
