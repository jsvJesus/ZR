using System.IO;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Layout;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Scene;

namespace Editor.Objects;

public sealed partial class ObjectsPanel
{
    private static readonly SemaphoreSlim AssetDecoder = new(1, 1);

    private readonly TextBox _assetSearch = new()
    {
        Watermark = "Поиск моделей в текущей папке…"
    };

    private readonly ListBox _assetFolders = new()
    {
        MaxHeight = 170
    };

    private readonly ListBox _assetsList = new()
    {
        MaxHeight = 230
    };

    private readonly TextBlock _assetLocation = Text("Папка: packs/objects/");
    private readonly TextBlock _catalogueSummary = Text("Загрузка каталога…");
    private readonly TextBlock _assetPageSummary = Text("");
    private readonly TextBlock _assetStatus = Text("Выберите модель для предпросмотра.");

    private readonly CheckBox _placement = new()
    {
        Content = Text("ЛКМ — расставлять выбранную модель")
    };

    private Button _previewButton = null!;
    private Button _parentFolderButton = null!;
    private Button _previousPageButton = null!;
    private Button _nextPageButton = null!;

    private AssetPaths? _assetPaths;
    private AssetEntry[] _catalogue = [];
    private AssetEntry? _selectedAsset;

    private SceneModel? _readyAsset;
    private SceneData? _readyScene;
    private ObjectPreviewWindow? _previewWindow;

    private CancellationTokenSource? _assetLoad;

    private string _folderPath = "";
    private int _assetPage;
    private int _assetPageCount = 1;
    private int _assetVersion;
    private int _catalogueVersion;

    private bool _catalogueBinding;
    private bool _folderBinding;
    private bool _placementBinding;
    private bool _libraryClosed;

    private bool CanBrowseLibrary =>
        !_libraryClosed &&
        IsVisible &&
        IsEnabled &&
        !IsBusy &&
        _section == ObjectsSection.PlaceObjects &&
        _viewport?.IsPlaying != true &&
        _viewport?.IsStarting != true;

    private bool CanImportModels =>
        !_libraryClosed &&
        IsVisible &&
        IsEnabled &&
        !IsBusy &&
        _section == ObjectsSection.Import &&
        _viewport?.IsPlaying != true &&
        _viewport?.IsStarting != true;

    private void BuildLibrary(StackPanel root)
    {
        root.Children.Add(Text("КАТАЛОГ МОДЕЛЕЙ · objects / models2"));
        root.Children.Add(_assetLocation);

        _parentFolderButton = Button(
            root,
            "← На уровень выше",
            NavigateParentFolder
        );

        root.Children.Add(Text("ПАПКИ"));
        root.Children.Add(_assetFolders);

        root.Children.Add(Text("МОДЕЛИ"));
        root.Children.Add(_assetSearch);
        root.Children.Add(_catalogueSummary);
        root.Children.Add(_assetsList);
        root.Children.Add(_assetPageSummary);

        var pageControls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        _previousPageButton = new Button
        {
            Content = "← Назад"
        };

        _nextPageButton = new Button
        {
            Content = "Далее →"
        };

        _previousPageButton.Click += (_, _) =>
        {
            if (_assetPage <= 0)
            {
                return;
            }

            _assetPage--;
            RefreshCatalogue();
        };

        _nextPageButton.Click += (_, _) =>
        {
            if (_assetPage >= _assetPageCount - 1)
            {
                return;
            }

            _assetPage++;
            RefreshCatalogue();
        };

        pageControls.Children.Add(_previousPageButton);
        pageControls.Children.Add(_nextPageButton);

        root.Children.Add(pageControls);
        root.Children.Add(_assetStatus);

        _previewButton = Button(
            root,
            "Открыть предпросмотр модели",
            ShowObjectPreview
        );

        root.Children.Add(Text(
            "ЛКМ — выбрать объект на сцене. " +
            "Ctrl+ЛКМ — поставить выбранную модель. " +
            "Delete — удалить. Ctrl+S — сохранить."
        ));

        _assetSearch.TextChanged += (_, _) =>
        {
            _assetPage = 0;
            RefreshCatalogue();
        };

        _assetFolders.SelectionChanged += (_, _) =>
        {
            if (_folderBinding)
            {
                return;
            }

            if (_assetFolders.SelectedItem is FolderEntry folder)
            {
                NavigateFolder(folder.Path);
            }
        };

        _assetsList.SelectionChanged += (_, _) =>
        {
            if (_catalogueBinding)
            {
                return;
            }

            _selectedAsset = _assetsList.SelectedItem as AssetEntry;
            _ = LoadSelectedAssetAsync();
        };
    }

    private async void AttachLibrary(AssetPaths assets)
    {
        _assetPaths = assets;
        try
        {
            await ReloadCatalogueAsync("objects");
        }
        catch (Exception exception)
        {
            if (!_libraryClosed)
            {
                _catalogueSummary.Text =
                    $"Каталог недоступен: {exception.Message}";
            }
        }

        RefreshLibraryMode();
    }

    private async Task ReloadCatalogueAsync(string folder)
    {
        if (_assetPaths is not { } assets) return;
        int version = ++_catalogueVersion;
        var entries = await Task.Run(() =>
        {
            var result = new List<AssetEntry>();
            foreach (var (name, extension) in new[] { ("objects", ".model"), ("models2", ImportedModelStorage.Extension) })
            {
                string directory = Path.Combine(assets.Root, name);
                if (!Directory.Exists(directory)) continue;
                result.AddRange(Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                    .Where(path => Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase))
                    .Select(path => new AssetEntry(name + "/" + Path.GetRelativePath(directory, path).Replace('\\', '/'))));
            }
            return result.OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase).ToArray();
        });
        if (_libraryClosed || version != _catalogueVersion) return;
        _catalogue = entries;
        NavigateFolder(folder);
    }

    private void NavigateParentFolder()
    {
        if (_folderPath.Length == 0)
        {
            return;
        }

        int separator = _folderPath.LastIndexOf('/');

        NavigateFolder(
            separator < 0
                ? ""
                : _folderPath[..separator]
        );
    }

    private void NavigateFolder(string path)
    {
        _folderPath = path.Trim('/');
        _assetPage = 0;

        _selectedAsset = null;
        _readyAsset = null;
        _readyScene = null;

        CancelLibraryLoad();

        _previewWindow?.ClearModel(
            "Выберите модель из текущей папки."
        );

        _assetStatus.Text = "Выберите модель для предпросмотра.";

        if (!string.IsNullOrEmpty(_assetSearch.Text))
        {
            _assetSearch.Text = "";
        }

        RefreshCatalogue();
    }

    private void RefreshCatalogue()
    {
        if (_libraryClosed)
        {
            return;
        }

        string prefix = _folderPath.Length == 0
            ? ""
            : _folderPath + "/";

        string search = (_assetSearch.Text ?? "").Trim();

        _assetLocation.Text = "Папка: packs/" + prefix;

        var folderNames = _catalogue
            .Where(entry =>
                entry.Path.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase
                ))
            .Select(entry => entry.Path[prefix.Length..])
            .Where(path => path.Contains('/'))
            .Select(path => path[..path.IndexOf('/')])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var folders = folderNames
            .Select(name => new FolderEntry(
                name,
                _folderPath.Length == 0
                    ? name
                    : _folderPath + "/" + name
            ))
            .ToArray();

        _folderBinding = true;
        _assetFolders.ItemsSource = folders;
        _assetFolders.SelectedItem = null;
        _folderBinding = false;

        var matches = _catalogue
            .Where(entry =>
                entry.Path.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase
                ))
            .Where(entry =>
            {
                if (search.Length != 0)
                {
                    return entry.Path.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase
                    );
                }

                string relative = entry.Path[prefix.Length..];

                return !relative.Contains('/');
            })
            .ToArray();

        if (_selectedAsset != null &&
            !matches.Contains(_selectedAsset))
        {
            _selectedAsset = null;
            _readyAsset = null;
            _readyScene = null;

            CancelLibraryLoad();

            _previewWindow?.ClearModel(
                "Выберите модель из результатов поиска."
            );

            _assetStatus.Text =
                "Выберите модель для предпросмотра.";
        }

        int total = matches.Length;

        _assetPageCount = Math.Max(
            1,
            (total + ListLimit - 1) / ListLimit
        );

        _assetPage = Math.Clamp(
            _assetPage,
            0,
            _assetPageCount - 1
        );

        var entries = matches
            .Skip(_assetPage * ListLimit)
            .Take(ListLimit)
            .ToArray();

        _catalogueBinding = true;
        _assetsList.ItemsSource = entries;
        _assetsList.SelectedItem = entries.FirstOrDefault(
            entry => entry == _selectedAsset
        );
        _catalogueBinding = false;

        _catalogueSummary.Text =
            $"Всего моделей: {_catalogue.Length:N0}\n" +
            $"Папок: {folders.Length} · Моделей найдено: {total:N0}";

        _assetPageSummary.Text =
            $"Страница {_assetPage + 1} из {_assetPageCount} · " +
            $"Показано: {entries.Length}";

        RefreshLibraryMode();
    }

    public void CancelLibraryLoad()
    {
        ++_assetVersion;
        _assetLoad?.Cancel();
        _assetLoad = null;

        _viewport?.CancelObjectPlacement();

        RefreshLibraryMode();
    }

    public void ResumeLibrary()
    {
        if (CanBrowseLibrary &&
            _selectedAsset != null &&
            _assetLoad == null &&
            (_readyAsset == null ||
             (_level?.Objects != null &&
              !ReferenceEquals(_readyScene, _level.Objects))))
        {
            _ = LoadSelectedAssetAsync();
        }
    }

    public void CloseLibrary()
    {
        _libraryClosed = true;
        CancelModelImport();

        CancelLibraryLoad();

        _previewWindow?.Close();
        _previewWindow = null;

        if (_level?.Objects is { } scene)
        {
            scene.Changed -= SceneChanged;
        }
    }

    private async Task LoadSelectedAssetAsync()
    {
        CancelLibraryLoad();

        _readyAsset = null;
        _readyScene = null;

        _previewWindow?.ClearModel(
            "Загрузка выбранной модели…"
        );

        if (_selectedAsset is not { } entry ||
            _assetPaths is not { } assets)
        {
            RefreshLibraryMode();
            return;
        }

        int version = _assetVersion;

        var level = _level;
        var scene = level?.Objects ?? new SceneData(assets, []);

        using var cancellation = new CancellationTokenSource();

        _assetLoad = cancellation;

        var token = cancellation.Token;

        _assetStatus.Text =
            $"{entry.Path}\nЗагрузка геометрии и коллизии…";

        RefreshLibraryMode();

        bool entered = false;

        try
        {
            await AssetDecoder.WaitAsync(token);
            entered = true;

            var result = await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();

                var model = scene.Models.Get(entry.Path);

                token.ThrowIfCancellationRequested();

                var physics = model == null
                    ? null
                    : scene.Physics.Get(entry.Path);

                token.ThrowIfCancellationRequested();

                return (Model: model, Physics: physics);
            }, token);

            if (_libraryClosed ||
                token.IsCancellationRequested ||
                version != _assetVersion ||
                entry != _selectedAsset ||
                !ReferenceEquals(level, _level))
            {
                return;
            }

            if (result.Model == null ||
                result.Model.Meshes.All(mesh =>
                    mesh.Vertices.Length == 0 ||
                    mesh.Indices.Length == 0))
            {
                throw new InvalidDataException(
                    "Геометрия модели не загружена. " +
                    "Зависимость отсутствует или формат не поддерживается.\n" +
                    string.Join("\n", scene.Warnings.TakeLast(5))
                );
            }

            _readyAsset = result.Model;
            _readyScene = scene;

            if (ReferenceEquals(scene, _level?.Objects) &&
                scene.CanSave)
            {
                _viewport?.SetObjectPlacement(result.Model.File);
            }

            var size =
                result.Model.Bounds.Maximum -
                result.Model.Bounds.Minimum;

            long triangles = result.Model.Meshes.Sum(
                mesh => (long)mesh.Indices.Length / 3
            );

            string collisionStatus = result.Physics switch
            {
                null =>
                    "Коллизия отсутствует или не поддерживается.",

                { IsVisualFallback: true } physics =>
                    $"Резервная коллизия из геометрии: " +
                    $"{physics.Indices.Length / 3:N0} треугольников.",

                { } physics =>
                    $"Авторская коллизия: " +
                    $"{physics.Indices.Length / 3:N0} треугольников."
            };

            _assetStatus.Text =
                $"{entry.Path}\n" +
                $"{triangles:N0} треугольников · " +
                $"{size.X:G4} × {size.Y:G4} × {size.Z:G4} м\n" +
                collisionStatus;

            var warnings = scene.Warnings
                .Where(warning =>
                    warning.Contains(
                        entry.Path,
                        StringComparison.OrdinalIgnoreCase
                    ))
                .TakeLast(4)
                .ToArray();

            if (warnings.Length > 0)
            {
                _assetStatus.Text +=
                    "\n" + string.Join("\n", warnings);
            }
        }
        catch (OperationCanceledException)
            when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (version == _assetVersion && !_libraryClosed)
            {
                _assetStatus.Text =
                    $"{entry.Path}\n{exception.Message}";
            }
        }
        finally
        {
            if (entered)
            {
                AssetDecoder.Release();
            }

            if (ReferenceEquals(_assetLoad, cancellation))
            {
                _assetLoad = null;
            }

            RefreshLibraryMode();
        }
    }

    private void ShowObjectPreview()
    {
        if (_readyAsset == null ||
            _assetPaths == null ||
            !CanBrowseLibrary)
        {
            return;
        }

        try
        {
            if (_previewWindow == null)
            {
                var window = new ObjectPreviewWindow();

                _previewWindow = window;

                window.Closed += (_, _) =>
                {
                    if (ReferenceEquals(_previewWindow, window))
                    {
                        _previewWindow = null;
                    }
                };

                window.PreviewFailed += error =>
                {
                    _readyAsset = null;

                    _assetStatus.Text =
                        $"Ошибка предпросмотра: {error}";

                    RefreshLibraryMode();
                };

                window.SetModel(_assetPaths, _readyAsset);

                if (TopLevel.GetTopLevel(this) is Window owner)
                {
                    window.Show(owner);
                }
                else
                {
                    window.Show();
                }
            }
            else
            {
                _previewWindow.SetModel(_assetPaths, _readyAsset);
                _previewWindow.Activate();
            }
        }
        catch (Exception exception)
        {
            _readyAsset = null;

            _previewWindow?.Close();
            _previewWindow = null;

            _assetStatus.Text =
                $"Ошибка предпросмотра: {exception.Message}";
        }

        RefreshLibraryMode();
    }

    private void RefreshLibraryMode()
    {
        bool active = CanBrowseLibrary && !IsDragging;
        RefreshModelImportMode(CanImportModels && !IsDragging);

        _assetSearch.IsEnabled = active;
        _assetFolders.IsEnabled = active;
        _assetsList.IsEnabled = active;

        _parentFolderButton.IsEnabled =
            active && _folderPath.Length > 0;

        _previousPageButton.IsEnabled =
            active && _assetPage > 0;

        _nextPageButton.IsEnabled =
            active && _assetPage < _assetPageCount - 1;

        _previewButton.IsEnabled =
            active && _readyAsset != null;
    }

    private sealed record FolderEntry(
        string Name,
        string Path)
    {
        public override string ToString() => "📁 " + Name;
    }

    private sealed record AssetEntry(string Path)
    {
        public override string ToString() => Path;
    }
}
