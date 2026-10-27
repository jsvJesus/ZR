using System.IO;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Editor.Levels;
using ZoneReborn.World.Levels.Import;
using ZoneReborn.World.Levels.Models;

namespace Editor;

public sealed partial class MainWindow
{
    private readonly LevelWorkspace _workspace = new();
    private LoadedLevel? _level;
    private CancellationTokenSource? _importCancellation;
    private bool _isClosing;
    private bool _levelOpening;

    private void SetLevelCommandsBusy(bool busy)
    {
        bool locked = busy || _levelOpening || Viewport.IsPlaying || Viewport.IsStarting;
        ImportLevelMenu.IsEnabled = !locked;
        ImportAllLevelsMenu.IsEnabled = !locked;
        OpenLevelMenu.IsEnabled = !locked;
        SaveLevelMenu.IsEnabled = !locked;
        PlayButton.IsEnabled = _level != null && !busy && !_levelOpening;
        CancelImportMenu.IsEnabled = busy;
    }
}
