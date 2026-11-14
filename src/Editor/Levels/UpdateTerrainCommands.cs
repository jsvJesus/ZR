using System.IO;
using Avalonia.Platform.Storage;
using Editor.Levels;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Import;

namespace Editor;

public sealed partial class MainWindow
{
    private async void UpdateTerrainFromFbx()
    {
        if (_level?.Manifest.Terrain.ImportedFromMesh != true || _isClosing || _importCancellation != null
            || _levelOpening || _levelSaving || _savingChangesOnClose || RoadSegmentsPanel.IsSaving
            || VegetationPanel.IsSaving || ObjectsPanel.IsBusy || TerrainEditorPanel.IsBusy
            || Viewport.IsObjectDragging || Viewport.IsPlaying || Viewport.IsStarting || CharacterPanel.IsBusy) return;
        using var cancellation = new CancellationTokenSource();
        _importCancellation = cancellation; _levelSaving = true; UpdatePlayControls();
        bool applied = false;
        bool reloadRequired = false;
        try
        {
            var level = _level;
            string source = AssetPathResolver.Resolve(_workspace.Assets.ImportLevels, level.Manifest.Name);
            string[] files = Directory.Exists(source) ? Directory.EnumerateFiles(source, "*", SearchOption.TopDirectoryOnly)
                .Where(file => Path.GetExtension(file).Equals(".fbx", StringComparison.OrdinalIgnoreCase)).ToArray() : [];
            if (files.Length == 0)
            {
                var selected = await StorageProvider.OpenFilePickerAsync(new()
                {
                    Title = "Обновлённый FBX земли (старая часть в прежних координатах)", AllowMultiple = true,
                    FileTypeFilter = [new FilePickerFileType("FBX") { Patterns = ["*.fbx"] }]
                });
                files = selected.Select(file => file.TryGetLocalPath()).OfType<string>().ToArray();
            }
            if (_isClosing || files.Length == 0) return;
            cancellation.Token.ThrowIfCancellationRequested();
            StatusText.Text = "Сохранение текущих правок перед расширением Terrain…";
            await TerrainEditorPanel.SavePendingAsync();
            await RoadSegmentsPanel.SavePendingAsync();
            await VegetationPanel.SavePendingAsync();
            await ObjectsPanel.SavePendingAsync();
            await EnvironmentPanel.SavePendingAsync();
            var pose = Viewport.CameraPose;
            await LevelWorkspace.SaveCameraAsync(level, new(pose.X, pose.Y, pose.Z, pose.Yaw, pose.Pitch, pose.Speed));
            var progress = new Progress<LevelImportProgress>(value =>
            {
                if (!_isClosing && ReferenceEquals(_importCancellation, cancellation) && !cancellation.IsCancellationRequested)
                    StatusText.Text = $"Обновление Terrain: {value.Level} ({value.Percentage}%)";
            });
            var result = await new FbxTerrainUpdater(_workspace.Assets).UpdateAsync(level, files, progress, cancellation.Token);
            applied = true;
            if (_isClosing) return;
            // OpenLevelAsync owns the rebind lifecycle; release only the save guard before entering it.
            _levelSaving = false;
            await OpenLevelAsync(result.ManifestPath);
            if (_isClosing) return;
            StatusText.Text = $"Terrain расширен до {result.SamplesX} × {result.SamplesZ}. Добавлено {result.AddedCells:N0} ячеек. "
                + $"Резервная копия: {result.BackupDirectory}"
                + (result.HeightRangeChanged ? ". Диапазон высот расширен; возможна погрешность хранения до половины шага квантования." : "")
                + (result.Warnings.Count > 0 ? $". Замечаний по текстурам: {result.Warnings.Count}" : "");
        }
        catch (OperationCanceledException)
        {
            reloadRequired = applied;
            if (!_isClosing) StatusText.Text = applied
                ? "Terrain уже записан. Для продолжения перезапустите редактор и откройте уровень; редактирование заблокировано."
                : "Обновление Terrain отменено. Предыдущий Terrain сохранён.";
        }
        catch (Exception error)
        {
            reloadRequired = applied;
            if (!_isClosing) StatusText.Text = applied
                ? $"Terrain записан, но не удалось открыть обновлённый уровень: {error.Message}. Редактирование заблокировано. Перезапустите редактор и откройте уровень."
                : $"Не удалось обновить Terrain: {error.Message}";
        }
        finally
        {
            // Never allow the old in-memory document to overwrite a committed generation.
            _levelSaving = reloadRequired;
            if (ReferenceEquals(_importCancellation, cancellation)) _importCancellation = null;
            if (!_isClosing) UpdatePlayControls();
        }
    }
}
