using System.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ZoneReborn.World.Scene.Import;

namespace Editor.Objects;

public sealed partial class ObjectsPanel
{
    private Button _importModelsButton = null!;
    private Button _textureFolderButton = null!;
    private readonly TextBlock _textureFolderText = Text("Текстуры: пути из FBX и папка исходных моделей.");
    private string? _textureFolder;
    private CancellationTokenSource? _modelImportCancellation;
    private Task _modelImportTask = Task.CompletedTask;

    public bool IsImporting { get; private set; }

    private void BuildModelImport(StackPanel root)
    {
        _importModelsButton = Button(root, "Импорт FBX → models2", () =>
        {
            if (CanImportModels && !IsDragging)
            {
                _modelImportTask = ImportModelsAsync();
            }
        });
        _textureFolderButton = Button(root, "Папка поиска текстур…", () => _ = ChooseTextureFolderAsync());
        root.Children.Add(_textureFolderText);
    }

    private void RefreshModelImportMode(bool active)
    {
        _importModelsButton.IsEnabled = active && _assetPaths != null;
        _textureFolderButton.IsEnabled = active;
    }

    public void CancelModelImport() => _modelImportCancellation?.Cancel();

    private async Task ChooseTextureFolderAsync()
    {
        if (!CanImportModels ||
            IsDragging ||
            TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }
        try
        {
            var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Дополнительная папка поиска текстур FBX", AllowMultiple = false
            });
            if (_libraryClosed || folders.Count == 0) return;
            string? path = folders[0].TryGetLocalPath();
            if (path == null) throw new IOException("Для поиска текстур нужна локальная папка.");
            _textureFolder = path;
            _textureFolderText.Text = "Дополнительные текстуры: " + path;
        }
        catch (Exception exception) { if (!_libraryClosed) _assetStatus.Text = exception.Message; }
    }

    private async Task ImportModelsAsync()
    {
        if (_assetPaths is not { } assets) return;
        using var cancellation = new CancellationTokenSource();
        _modelImportCancellation = cancellation;
        IsImporting = true;
        CancelLibraryLoad();
        _readyAsset = null;
        _readyScene = null;
        _previewWindow?.ClearModel("Импорт моделей…");
        _assetStatus.Text = "Импорт FBX из " + assets.ImportModels;
        BusyChanged?.Invoke();
        RefreshMode();
        var progress = new Progress<ModelImportProgress>(value =>
        {
            if (!_libraryClosed && ReferenceEquals(_modelImportCancellation, cancellation) && !cancellation.IsCancellationRequested)
                _assetStatus.Text = $"Импорт {value.Completed}/{value.Total}: {value.File}";
        });
        bool entered = false;
        try
        {
            await AssetDecoder.WaitAsync(cancellation.Token);
            entered = true;
            string[] folders = _textureFolder == null ? [] : [_textureFolder];
            var report = await new ModelImporter(assets).ImportAsync(folders, progress, cancellation.Token);
            await ReloadCatalogueAsync("models2");
            if (_libraryClosed) return;
            _assetStatus.Text = $"Импортировано: {report.Imported}; уже существуют: {report.Skipped}; текстур: {report.Textures}.";
            if (report.Imported == 0 && report.Skipped == 0 && report.Errors.Length == 0)
                _assetStatus.Text = $"В {assets.ImportModels} не найдены файлы FBX.";
            if (report.Errors.Length > 0)
                _assetStatus.Text += $"\nОшибок: {report.Errors.Length}\n" + string.Join("\n", report.Errors.Take(10));
        }
        catch (OperationCanceledException)
        {
            if (!_libraryClosed)
            {
                try { await ReloadCatalogueAsync("models2"); }
                catch (Exception exception) { _catalogueSummary.Text = exception.Message; }
                _assetStatus.Text = "Импорт отменён. Завершённые модели и текстуры сохранены.";
            }
        }
        catch (Exception exception) { if (!_libraryClosed) _assetStatus.Text = $"Ошибка импорта: {exception.Message}"; }
        finally
        {
            if (entered) AssetDecoder.Release();
            _modelImportCancellation = null;
            IsImporting = false;
            BusyChanged?.Invoke();
            RefreshMode();
        }
    }
}
