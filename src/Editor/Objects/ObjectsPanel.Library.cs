using System.IO;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Scene;

namespace Editor.Objects;

public sealed partial class ObjectsPanel
{
    private static readonly SemaphoreSlim AssetDecoder = new(1, 1);
    private readonly TextBox _assetSearch = new() { Watermark = "Поиск по пути в packs/objects…" };
    private readonly ListBox _assetsList = new() { MaxHeight = 230 };
    private readonly TextBlock _catalogueSummary = Text("Загрузка каталога…");
    private readonly TextBlock _assetStatus = Text("Выберите модель для предпросмотра.");
    private readonly CheckBox _placement = new() { Content = Text("ЛКМ — расставлять выбранную модель") };
    private Button _previewButton = null!;
    private AssetPaths? _assetPaths;
    private AssetEntry[] _catalogue = [];
    private AssetEntry? _selectedAsset;
    private SceneModel? _readyAsset;
    private SceneData? _readyScene;
    private ObjectPreviewWindow? _previewWindow;
    private CancellationTokenSource? _assetLoad;
    private int _assetVersion;
    private bool _catalogueBinding, _placementBinding, _libraryClosed;

    private bool CanBrowseLibrary => !_libraryClosed && IsVisible && IsEnabled && !IsSaving
        && _viewport?.IsPlaying != true && _viewport?.IsStarting != true;

    private void BuildLibrary(StackPanel root)
    {
        root.Children.Add(Text("КАТАЛОГ МОДЕЛЕЙ · packs/objects"));
        root.Children.Add(_assetSearch);
        root.Children.Add(_catalogueSummary);
        root.Children.Add(_assetsList);
        root.Children.Add(_assetStatus);
        _previewButton = Button(root, "Открыть предпросмотр модели", ShowObjectPreview);
        root.Children.Add(_placement);
        root.Children.Add(Text("Выберите модель, проверьте её в отдельном окне и включите размещение. Каждый щелчок по Terrain или видимому объекту добавляет копию. Esc — закончить; затем доступны оси, параметры и Delete."));
        _assetSearch.TextChanged += (_, _) => RefreshCatalogue();
        _assetsList.SelectionChanged += (_, _) =>
        {
            if (_catalogueBinding) return;
            _selectedAsset = _assetsList.SelectedItem as AssetEntry;
            _ = LoadSelectedAssetAsync();
        };
        _placement.IsCheckedChanged += (_, _) =>
        {
            if (_placementBinding || _viewport == null) return;
            if (_placement.IsChecked == true && CanBrowseLibrary && _readyAsset != null
                && ReferenceEquals(_readyScene, _level?.Objects) && _readyScene?.CanSave == true)
                _viewport.SetObjectPlacement(_readyAsset.File);
            else _viewport.CancelObjectPlacement();
            RefreshLibraryMode();
        };
    }

    private async void AttachLibrary(AssetPaths assets)
    {
        _assetPaths = assets;
        try
        {
            var entries = await Task.Run(() =>
            {
                string directory = Path.Combine(assets.Root, "objects");
                if (!Directory.Exists(directory)) throw new DirectoryNotFoundException($"Не найден каталог {directory}");
                return Directory.EnumerateFiles(directory, "*.model", SearchOption.AllDirectories)
                    .Select(path => new AssetEntry("objects/" + Path.GetRelativePath(directory, path).Replace('\\', '/')))
                    .OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase).ToArray();
            });
            if (_libraryClosed) return;
            _catalogue = entries;
            RefreshCatalogue();
        }
        catch (Exception exception) { if (!_libraryClosed) _catalogueSummary.Text = $"Каталог недоступен: {exception.Message}"; }
        RefreshLibraryMode();
    }

    private void RefreshCatalogue()
    {
        string search = (_assetSearch.Text ?? "").Trim();
        if (_selectedAsset != null && !_selectedAsset.Path.Contains(search, StringComparison.OrdinalIgnoreCase))
        {
            _selectedAsset = null; _readyAsset = null; _readyScene = null;
            CancelLibraryLoad();
            _assetStatus.Text = "Выберите модель из результатов поиска.";
            _previewWindow?.ClearModel("Выберите модель из результатов поиска.");
        }
        var matches = _catalogue.Where(entry => entry.Path.Contains(search, StringComparison.OrdinalIgnoreCase));
        var entries = matches.Take(ListLimit).ToList();
        int total = matches.Count();
        if (_selectedAsset != null && !entries.Contains(_selectedAsset)
            && _selectedAsset.Path.Contains(search, StringComparison.OrdinalIgnoreCase))
        {
            if (entries.Count == ListLimit) entries.RemoveAt(entries.Count - 1);
            entries.Add(_selectedAsset);
        }
        _catalogueBinding = true;
        _assetsList.ItemsSource = entries;
        _assetsList.SelectedItem = entries.FirstOrDefault(entry => entry == _selectedAsset);
        _catalogueBinding = false;
        _catalogueSummary.Text = $"Моделей: {_catalogue.Length:N0} · найдено: {total:N0} · показано: {entries.Count} (до {ListLimit})";
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
        if (CanBrowseLibrary && _selectedAsset != null && _assetLoad == null
            && (_readyAsset == null || (_level?.Objects != null && !ReferenceEquals(_readyScene, _level.Objects))))
            _ = LoadSelectedAssetAsync();
    }

    public void CloseLibrary()
    {
        _libraryClosed = true;
        CancelLibraryLoad();
        _previewWindow?.Close();
        _previewWindow = null;
        if (_level?.Objects is { } scene) scene.Changed -= SceneChanged;
    }

    private async Task LoadSelectedAssetAsync()
    {
        CancelLibraryLoad();
        _readyAsset = null; _readyScene = null;
        _previewWindow?.ClearModel("Загрузка выбранной модели…");
        if (_selectedAsset is not { } entry || _assetPaths is not { } assets) { RefreshLibraryMode(); return; }
        int version = _assetVersion;
        var level = _level;
        var scene = level?.Objects ?? new SceneData(assets, []);
        using var cancellation = new CancellationTokenSource();
        _assetLoad = cancellation;
        var token = cancellation.Token;
        _assetStatus.Text = $"{entry.Path}\nЗагрузка геометрии и авторской коллизии…";
        RefreshLibraryMode();
        bool entered = false;
        try
        {
            await AssetDecoder.WaitAsync(token); entered = true;
            var result = await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                var model = scene.Models.Get(entry.Path);
                token.ThrowIfCancellationRequested();
                var physics = model == null ? null : scene.Physics.Get(entry.Path);
                token.ThrowIfCancellationRequested();
                return (Model: model, Physics: physics);
            }, token);
            if (_libraryClosed || token.IsCancellationRequested || version != _assetVersion
                || entry != _selectedAsset || !ReferenceEquals(level, _level)) return;
            if (result.Model == null || result.Model.Meshes.All(mesh => mesh.Vertices.Length == 0 || mesh.Indices.Length == 0))
                throw new InvalidDataException("Геометрия модели не загружена: отсутствует зависимость или формат не поддерживается.\n"
                    + string.Join("\n", scene.Warnings.TakeLast(5)));
            _readyAsset = result.Model; _readyScene = scene;
            var size = result.Model.Bounds.Maximum - result.Model.Bounds.Minimum;
            long triangles = result.Model.Meshes.Sum(mesh => (long)mesh.Indices.Length / 3);
            _assetStatus.Text = $"{entry.Path}\n{triangles:N0} треугольников · {size.X:G4} × {size.Y:G4} × {size.Z:G4} м\n"
                + (result.Physics == null ? "Авторская коллизия отсутствует или не поддерживается; размещение без коллизии."
                    : $"Авторская коллизия: {result.Physics.Indices.Length / 3:N0} треугольников.");
            var warnings = scene.Warnings.Where(warning => warning.Contains(entry.Path, StringComparison.OrdinalIgnoreCase)).TakeLast(4).ToArray();
            if (warnings.Length > 0) _assetStatus.Text += "\n" + string.Join("\n", warnings);
            if (CanBrowseLibrary) ShowObjectPreview();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception)
        {
            if (version == _assetVersion && !_libraryClosed) _assetStatus.Text = $"{entry.Path}\n{exception.Message}";
        }
        finally
        {
            if (entered) AssetDecoder.Release();
            if (ReferenceEquals(_assetLoad, cancellation)) _assetLoad = null;
            RefreshLibraryMode();
        }
    }

    private void ShowObjectPreview()
    {
        if (_readyAsset == null || _assetPaths == null || !CanBrowseLibrary) return;
        try
        {
            if (_previewWindow == null)
            {
                var window = new ObjectPreviewWindow();
                _previewWindow = window;
                window.Closed += (_, _) =>
                {
                    _viewport?.CancelObjectPlacement();
                    if (ReferenceEquals(_previewWindow, window)) _previewWindow = null;
                };
                window.PreviewFailed += error =>
                {
                    _viewport?.CancelObjectPlacement();
                    _readyAsset = null;
                    _assetStatus.Text = $"Ошибка предпросмотра: {error}";
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
            _viewport?.CancelObjectPlacement();
            _readyAsset = null;
            _previewWindow?.Close(); _previewWindow = null;
            _assetStatus.Text = $"Ошибка предпросмотра: {exception.Message}";
        }
        RefreshLibraryMode();
    }

    private void RefreshLibraryMode()
    {
        bool active = CanBrowseLibrary && !IsDragging;
        _assetSearch.IsEnabled = _assetsList.IsEnabled = active;
        _previewButton.IsEnabled = active && _readyAsset != null;
        bool ready = active && _readyAsset != null && _level?.Objects?.CanSave == true
            && ReferenceEquals(_readyScene, _level.Objects) && _assetLoad == null;
        _placement.IsEnabled = ready;
        if (!ready) _viewport?.CancelObjectPlacement();
        _placementBinding = true;
        _placement.IsChecked = _viewport?.IsObjectPlacing == true;
        _placementBinding = false;
    }

    private sealed record AssetEntry(string Path)
    {
        public override string ToString() => Path["objects/".Length..];
    }
}
