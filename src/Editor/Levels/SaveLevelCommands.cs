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
        if (_level == null || _importCancellation != null || _levelOpening || Viewport.IsPlaying || Viewport.IsStarting)
        {
            StatusText.Text = "Сначала откройте преобразованный уровень.";
            return;
        }
        try
        {
            await RoadSegmentsPanel.SavePendingAsync();
            var camera = Viewport.CameraPose;
            await LevelWorkspace.SaveCameraAsync(_level, new(camera.X, camera.Y, camera.Z, camera.Yaw, camera.Pitch, camera.Speed));
            if (!_isClosing) StatusText.Text = "Позиция камеры и изменения дорог сохранены.";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Ошибка сохранения: {exception.Message}";
        }
    }

}
