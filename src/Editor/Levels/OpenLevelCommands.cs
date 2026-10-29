using System.IO;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Editor.Levels;
using ZoneReborn.World.Levels.Import;
using ZoneReborn.World.Levels.Models;

namespace Editor;

public sealed partial class MainWindow
{
    private async void OpenLevel_Click(object? sender, RoutedEventArgs e)
    {
        if (_importCancellation != null || _levelOpening || Viewport.IsPlaying || Viewport.IsStarting) return;
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Открыть преобразованный уровень",
                AllowMultiple = false,
                SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(_workspace.Assets.Levels),
                FileTypeFilter = [new FilePickerFileType("Уровень") { Patterns = ["level.json"] }]
            });
            string? path = files.FirstOrDefault()?.TryGetLocalPath();
            if (path != null && !_isClosing) await OpenLevelAsync(path);
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Ошибка открытия: {exception.Message}";
        }
    }

    private async Task OpenLevelAsync(string path)
    {
        _levelOpening = true;
        SetLevelCommandsBusy(_importCancellation != null);
        try
        {
            await RoadSegmentsPanel.SavePendingAsync();
            var level = await _workspace.OpenAsync(path);
            if (_isClosing) return;
            Viewport.SetLevel(level, _workspace.Assets);
            _level = level;
            RoadSegmentsPanel.Bind(level, _workspace.Assets);
            var camera = level.Manifest.Camera;
            Viewport.SetCamera(new(camera.X, camera.Y, camera.Z, camera.Yaw, camera.Pitch, camera.Speed));
            LevelNameText.Text = $"Уровень: {level.Manifest.Name}";
            TerrainLevelInfo.Text = $"{level.Manifest.Terrain.SamplesX} × {level.Manifest.Terrain.SamplesZ} • {level.Layers.Count} слоёв";
            TerrainLayersList.ItemsSource = level.Layers.Select(layer => layer.Name).ToArray();
            StatusText.Text = $"Открыт {level.Manifest.Name} · дорог: {level.Roads.Roads.Count}"
                + (level.Roads.Warnings.Count > 0 ? $" · замечаний по дорогам: {level.Roads.Warnings.Count}" : "");
        }
        finally
        {
            _levelOpening = false;
            if (!_isClosing) UpdatePlayControls();
        }
    }

}
