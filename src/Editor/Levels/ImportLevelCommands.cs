using System.IO;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Editor.Levels;
using ZoneReborn.World.Levels.Import;
using ZoneReborn.World.Levels.Models;

namespace Editor;

public sealed partial class MainWindow
{
    private async void ImportLevel_Click(object? sender, RoutedEventArgs e)
    {
        if (_importCancellation != null) return;
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Выбрать исходный уровень",
                AllowMultiple = false,
                SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(_workspace.Assets.ImportLevels)
            });
            string? selected = folders.FirstOrDefault()?.TryGetLocalPath();
            if (selected == null || _isClosing) return;
            string source = Path.GetFullPath(selected).TrimEnd(Path.DirectorySeparatorChar);
            string parent = Path.GetDirectoryName(source)!;
            if (!string.Equals(parent, Path.GetFullPath(_workspace.Assets.ImportLevels), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Выберите уровень внутри packs/import/levels.");
            await ImportLevelsAsync([Path.GetFileName(source)]);
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Ошибка импорта: {exception.Message}";
        }
    }

    private async void ImportAllLevels_Click(object? sender, RoutedEventArgs e)
    {
        if (_importCancellation != null) return;
        try { await ImportLevelsAsync(_workspace.FindSources()); }
        catch (Exception exception) { StatusText.Text = $"Ошибка импорта: {exception.Message}"; }
    }

    private async Task ImportLevelsAsync(IReadOnlyList<string> names)
    {
        if (names.Count == 0)
        {
            StatusText.Text = "В packs/import/levels нет исходных уровней.";
            return;
        }
        using var cancellation = new CancellationTokenSource();
        _importCancellation = cancellation;
        SetLevelCommandsBusy(true);
        int imported = 0, skipped = 0;
        var failures = new List<string>();
        string? firstLevel = null;
        var progress = new Progress<LevelImportProgress>(value =>
        {
            if (!_isClosing && ReferenceEquals(_importCancellation, cancellation) && !cancellation.IsCancellationRequested)
                StatusText.Text = $"Импорт {value.Level}: {value.Percentage}% ({value.CompletedTiles}/{value.TotalTiles})";
        });
        try
        {
            foreach (string name in names)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                if (_workspace.IsImported(name))
                {
                    skipped++;
                    firstLevel ??= Path.Combine(_workspace.Assets.Levels, name, "level.json");
                    continue;
                }
                try
                {
                    string path = await _workspace.ImportAsync(name, progress, cancellation.Token);
                    firstLevel ??= path;
                    imported++;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception exception)
                {
                    failures.Add($"{name}: {exception.Message}");
                }
            }
            if (firstLevel != null && !_isClosing) await OpenLevelAsync(firstLevel);
            if (!_isClosing)
                StatusText.Text = failures.Count == 0
                    ? $"Terrain: импортировано {imported}, уже существуют {skipped}."
                    : $"Импортировано {imported}; ошибки: {string.Join("; ", failures)}";
        }
        catch (OperationCanceledException)
        {
            if (!_isClosing) StatusText.Text = "Импорт отменён. Завершённые уровни сохранены.";
        }
        catch (Exception exception)
        {
            if (!_isClosing) StatusText.Text = $"Ошибка открытия: {exception.Message}";
        }
        finally
        {
            _importCancellation = null;
            if (!_isClosing) SetLevelCommandsBusy(false);
        }
    }

    private void CancelImport_Click(object? sender, RoutedEventArgs e) => _importCancellation?.Cancel();

}
