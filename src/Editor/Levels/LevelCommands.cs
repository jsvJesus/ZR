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
    private bool _levelSaving;

    private void SetLevelCommandsBusy(bool busy)
    {
        bool saving = _levelSaving || _savingChangesOnClose || VegetationPanel.IsSaving || ObjectsPanel.IsBusy || TerrainEditorPanel.IsBusy || RoadsPanel.IsSaving;;
        bool baseLocked = busy || _levelOpening || saving || Viewport.IsPlaying || Viewport.IsStarting || CharacterPanel.IsBusy;
        bool dragging = Viewport.IsObjectDragging;
        bool locked = baseLocked || dragging;
        ImportLevelMenu.IsEnabled = !locked;
        ImportAllLevelsMenu.IsEnabled = !locked;
        OpenLevelMenu.IsEnabled = !locked;
        SaveLevelMenu.IsEnabled = !locked;
        OpenToolbarButton.IsEnabled = !locked;
        SaveToolbarButton.IsEnabled = !locked;
        PlayButton.IsEnabled = _level != null && !busy && !_levelOpening && !saving && !dragging && !CharacterPanel.IsActive;
        CharacterEditorMenu.IsEnabled = !busy && !_levelOpening && !saving && !dragging;
        LevelEditorMenu.IsEnabled = CanSwitchWorkspace;
        SceneEditorMenu.IsEnabled = CanSwitchWorkspace;
        ObjectsEditorMenu.IsEnabled = CanSwitchWorkspace;
        WorkspaceNavigation.IsEnabled = CanSwitchWorkspace;
        TerrainEditorPanel.IsEnabled = !locked;
        EnvironmentPanel.IsEnabled = !busy && !_levelOpening && !saving;
        TerrainEditorPanel.RefreshMode();
        CharacterPanel.IsEnabled = !dragging;
        VegetationPanel.IsEnabled = !locked;
        VegetationPanel.RefreshMode();
        RoadsPanel.IsEnabled = !locked;
        ActorsPanel.IsEnabled = !locked;
        ActorsPanel.RefreshMode();
        ObjectsPanel.IsEnabled = !baseLocked;
        ObjectsPanel.RefreshMode();
        CancelImportMenu.IsEnabled = busy;
        UpdateHistoryButtons();
    }
}
