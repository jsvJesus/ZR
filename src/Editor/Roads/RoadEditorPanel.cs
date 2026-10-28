using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Editor.Viewport;

namespace Editor.Roads
{
    public sealed class RoadEditorPanel : UserControl
    {
        private readonly ListBox _roadsList = new()
        {
            Height = 155
        };

        private readonly ListBox _pointsList = new()
        {
            Height = 125
        };

        private readonly TextBox _width = new();
        private readonly TextBox _x = new();
        private readonly TextBox _y = new();
        private readonly TextBox _z = new();
        private readonly TextBox _tangentX = new();
        private readonly TextBox _tangentZ = new();

        private readonly TextBlock _levelName = new();
        private readonly TextBlock _material = new();

        private readonly TextBlock _status = new()
        {
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };

        private RoadDocument? _document;
        private DxViewportHost? _viewport;
        private string? _levelDirectory;

        public RoadEditorPanel()
        {
            var root = new StackPanel
            {
                Spacing = 8
            };

            root.Children.Add(CreateTitle("ROAD EDITOR"));
            root.Children.Add(_levelName);

            root.Children.Add(CreateButton(
                "Импортировать оригинальные дороги",
                Import_Click));

            root.Children.Add(CreateButton(
                "Открыть roads.json",
                Open_Click));

            root.Children.Add(CreateButton(
                "Сохранить roads.json",
                Save_Click));

            root.Children.Add(CreateTitle("СПИСОК ДОРОГ"));
            root.Children.Add(_roadsList);

            root.Children.Add(CreateButton(
                "Перейти к выбранной дороге",
                Focus_Click));

            root.Children.Add(CreateTitle("МАТЕРИАЛ"));
            root.Children.Add(_material);

            root.Children.Add(CreateTitle("ШИРИНА"));
            root.Children.Add(_width);

            root.Children.Add(CreateTitle("КОНТРОЛЬНЫЕ ТОЧКИ"));
            root.Children.Add(_pointsList);

            AddField(root, "Position X", _x);
            AddField(root, "Position Y", _y);
            AddField(root, "Position Z", _z);
            AddField(root, "Tangent X", _tangentX);
            AddField(root, "Tangent Z", _tangentZ);

            root.Children.Add(CreateButton(
                "Применить изменения",
                Apply_Click));

            root.Children.Add(_status);

            _roadsList.SelectionChanged += (_, _) =>
            {
                SelectRoad();
            };

            _pointsList.SelectionChanged += (_, _) =>
            {
                SelectPoint();
            };

            _levelName.Text = "Уровень не выбран";

            Content = root;
        }

        public void Attach(DxViewportHost viewport)
        {
            _viewport = viewport;
            RefreshViewport();
        }

        private static TextBlock CreateTitle(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Margin = new Avalonia.Thickness(0, 8, 0, 0)
            };
        }

        private static Button CreateButton(
            string text,
            EventHandler<RoutedEventArgs> handler)
        {
            var button = new Button
            {
                Content = text,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            button.Click += handler;

            return button;
        }

        private static void AddField(
            StackPanel root,
            string label,
            TextBox input)
        {
            root.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11
            });

            root.Children.Add(input);
        }

        private void BindDocument(RoadDocument document)
        {
            _document = document;
            _levelName.Text = $"Уровень: {document.Level}";

            _roadsList.ItemsSource = null;
            _roadsList.ItemsSource = document.Roads;

            _roadsList.SelectedIndex =
                document.Roads.Count > 0 ? 0 : -1;

            RefreshViewport();
        }

        private RoadData? SelectedRoad =>
            _roadsList.SelectedItem as RoadData;

        private RoadControlPoint? SelectedPoint =>
            _pointsList.SelectedItem as RoadControlPoint;

        private void SelectRoad()
        {
            RoadData? road = SelectedRoad;

            _pointsList.ItemsSource = null;
            _pointsList.ItemsSource = road?.Points;

            _pointsList.SelectedIndex =
                road != null && road.Points.Count > 0
                    ? 0
                    : -1;

            _material.Text = road == null
                ? ""
                : $"{road.Material} | Priority {road.DrawPriority}";

            _width.Text = road == null
                ? ""
                : Format(road.Width);

            RefreshViewport();
        }

        private void SelectPoint()
        {
            RoadControlPoint? point = SelectedPoint;

            _x.Text = point == null ? "" : Format(point.X);
            _y.Text = point == null ? "" : Format(point.Y);
            _z.Text = point == null ? "" : Format(point.Z);

            _tangentX.Text = point == null
                ? ""
                : Format(point.TangentX);

            _tangentZ.Text = point == null
                ? ""
                : Format(point.TangentZ);
        }

        private void RefreshViewport()
        {
            _viewport?.SetRoads(
                _document,
                SelectedRoad?.Id);
        }

        private async void Import_Click(
            object? sender,
            RoutedEventArgs e)
        {
            TopLevel? top = TopLevel.GetTopLevel(this);

            if (top == null)
            {
                return;
            }

            var targetFolders = await top.StorageProvider
                .OpenFolderPickerAsync(
                    new FolderPickerOpenOptions
                    {
                        Title = "Выбери готовый уровень из packs/levels",
                        AllowMultiple = false
                    });

            string? targetDirectory =
                targetFolders.FirstOrDefault()?.Path.LocalPath;

            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                return;
            }

            var sourceFolders = await top.StorageProvider
                .OpenFolderPickerAsync(
                    new FolderPickerOpenOptions
                    {
                        Title = "Выбери оригинальный уровень с дорогами",
                        AllowMultiple = false
                    });

            string? sourceDirectory =
                sourceFolders.FirstOrDefault()?.Path.LocalPath;

            if (string.IsNullOrWhiteSpace(sourceDirectory))
            {
                return;
            }

            _status.Text = "Импорт дорог...";

            try
            {
                RoadImportResult result = await Task.Run(() =>
                    LegacyRoadImporter.Import(
                        sourceDirectory,
                        targetDirectory));

                _levelDirectory = targetDirectory;

                BindDocument(result.Document);

                _status.Text =
                    $"Уровень: {result.Document.Level}\n" +
                    $"Импортировано дорог: {result.Document.Roads.Count}\n" +
                    $"Предупреждений: {result.Warnings.Count}";

                if (result.Warnings.Count > 0)
                {
                    _status.Text += "\n" +
                        string.Join(
                            "\n",
                            result.Warnings.Take(8));

                    if (result.Warnings.Count > 8)
                    {
                        _status.Text += "\n...";
                    }
                }
            }
            catch (Exception exception)
            {
                _status.Text =
                    $"Ошибка импорта: {exception.Message}";
            }
        }

        private async void Open_Click(
            object? sender,
            RoutedEventArgs e)
        {
            TopLevel? top = TopLevel.GetTopLevel(this);

            if (top == null)
            {
                return;
            }

            var files = await top.StorageProvider
                .OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = "Открыть дорожную сеть",
                        AllowMultiple = false,
                        FileTypeFilter =
                        [
                            new FilePickerFileType("Road Network")
                            {
                                Patterns = ["*.json"]
                            }
                        ]
                    });

            var file = files.FirstOrDefault();

            if (file == null)
            {
                return;
            }

            try
            {
                await using var stream =
                    await file.OpenReadAsync();

                RoadDocument document =
                    await RoadStorage.LoadAsync(stream);

                _levelDirectory = file.Path.IsFile
                    ? Path.GetDirectoryName(file.Path.LocalPath)
                    : null;

                BindDocument(document);

                _status.Text =
                    $"Загружено дорог: {document.Roads.Count}";
            }
            catch (Exception exception)
            {
                _status.Text =
                    $"Ошибка открытия: {exception.Message}";
            }
        }

        private async void Save_Click(
            object? sender,
            RoutedEventArgs e)
        {
            if (_document == null)
            {
                _status.Text = "Нет данных для сохранения.";
                return;
            }

            if (string.IsNullOrWhiteSpace(_levelDirectory))
            {
                _status.Text =
                    "Сначала выбери уровень или открой roads.json.";
                return;
            }

            string path = Path.Combine(
                _levelDirectory,
                "roads.json");

            try
            {
                await using var stream = new FileStream(
                    path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    useAsync: true);

                await RoadStorage.SaveAsync(
                    stream,
                    _document);

                _status.Text =
                    $"Дорожная сеть сохранена:\n{path}";
            }
            catch (Exception exception)
            {
                _status.Text =
                    $"Ошибка сохранения: {exception.Message}";
            }
        }

        private void Focus_Click(
            object? sender,
            RoutedEventArgs e)
        {
            if (SelectedRoad is RoadData road)
            {
                _viewport?.FocusRoad(road);
            }
        }

        private void Apply_Click(
            object? sender,
            RoutedEventArgs e)
        {
            RoadData? road = SelectedRoad;
            RoadControlPoint? point = SelectedPoint;

            if (road == null || point == null)
            {
                _status.Text = "Выбери дорогу и точку.";
                return;
            }

            try
            {
                float width = Parse(_width.Text);
                float x = Parse(_x.Text);
                float y = Parse(_y.Text);
                float z = Parse(_z.Text);

                float tangentX = Parse(_tangentX.Text);
                float tangentZ = Parse(_tangentZ.Text);

                if (width < 0.2f || width > 200f)
                {
                    throw new InvalidDataException(
                        "Ширина должна быть от 0.2 до 200.");
                }

                road.Width = width;

                point.X = x;
                point.Y = y;
                point.Z = z;

                point.TangentX = tangentX;
                point.TangentZ = tangentZ;

                int selectedIndex =
                    _pointsList.SelectedIndex;

                _pointsList.ItemsSource = null;
                _pointsList.ItemsSource = road.Points;
                _pointsList.SelectedIndex = selectedIndex;

                RefreshViewport();

                _status.Text = "Изменения применены.";
            }
            catch (Exception exception)
            {
                _status.Text =
                    $"Ошибка редактирования: {exception.Message}";
            }
        }

        private static string Format(float value)
        {
            return value.ToString(
                "0.###",
                CultureInfo.InvariantCulture);
        }

        private static float Parse(string? text)
        {
            if (float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out float value) &&
                float.IsFinite(value))
            {
                return value;
            }

            if (float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value) &&
                float.IsFinite(value))
            {
                return value;
            }

            throw new FormatException(
                $"Некорректное число: {text}");
        }
    }
}
