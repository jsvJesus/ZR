using System.IO;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Editor.Levels;
using ZoneReborn.World.Levels.Import;
using ZoneReborn.World.Levels.Models;

namespace Editor;

public sealed partial class MainWindow
{
    private async void SaveLevel_Click(object? sender, RoutedEventArgs e)
    {
        if (_level == null || _isClosing || _importCancellation != null || _levelOpening || _levelSaving || _savingChangesOnClose
            || RoadSegmentsPanel.IsSaving || VegetationPanel.IsSaving || ObjectsPanel.IsBusy || TerrainEditorPanel.IsBusy || Viewport.IsObjectDragging || Viewport.IsPlaying || Viewport.IsStarting)
        {
            StatusText.Text = "Сначала откройте преобразованный уровень.";
            return;
        }
        _levelSaving = true;
        UpdatePlayControls();
        try
        {
            await TerrainEditorPanel.SavePendingAsync();
            await RoadSegmentsPanel.SavePendingAsync();
            await VegetationPanel.SavePendingAsync();
            await ObjectsPanel.SavePendingAsync();
            await EnvironmentPanel.SavePendingAsync();

            var camera = Viewport.CameraPose;
            await LevelWorkspace.SaveCameraAsync(_level, new(camera.X, camera.Y, camera.Z, camera.Yaw, camera.Pitch, camera.Speed));
            if (!_isClosing) StatusText.Text = "Terrain, камера, дороги, растительность и объекты сохранены.";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Ошибка сохранения: {exception.Message}";
        }
        finally
        {
            _levelSaving = false;
            if (!_isClosing) UpdatePlayControls();
        }
    }

}
