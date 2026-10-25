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

    private void SetLevelCommandsBusy(bool busy)
    {
        ImportLevelMenu.IsEnabled = !busy;
        ImportAllLevelsMenu.IsEnabled = !busy;
        OpenLevelMenu.IsEnabled = !busy;
        SaveLevelMenu.IsEnabled = !busy;
        CancelImportMenu.IsEnabled = busy;
    }
}
