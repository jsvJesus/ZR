using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;

namespace Editor;

public sealed partial class MainWindow
{
    private readonly Dictionary<string, Button> _workspaceButtons = new();
    private string _activeWorkspace = "terrain";
    private double _leftPanelWidth = 270;
    private double _rightPanelWidth = 310;

    private bool CanSwitchWorkspace =>
        !_isClosing && !_levelOpening && !_levelSaving && !_savingChangesOnClose &&
        _importCancellation == null && !Viewport.IsObjectDragging &&
        !CharacterPanel.IsBusy &&
        !RoadsPanel.IsSaving &&
        !VegetationPanel.IsSaving &&
        !ObjectsPanel.IsBusy && !TerrainEditorPanel.IsBusy;

    private void InitializeWorkspaceLayout()
    {
        AddBrowser(TerrainEditorPanel, TerrainEditorPanel.BrowserContent);
        AddBrowser(ObjectsPanel, ObjectsPanel.BrowserContent);
        AddBrowser(VegetationPanel, VegetationPanel.BrowserContent);
        AddBrowser(CharacterPanel, CharacterPanel.BrowserContent);
        AddBrowser(ActorsPanel, ActorsPanel.BrowserContent);
        EnvironmentBrowser.Bind(IsEnabledProperty, new ReflectionBinding(nameof(IsEnabled)) { Source = EnvironmentPanel });

        AddWorkspace("terrain", "Рельеф", "M1,13 L6,3 L10,9 L13,5 L17,13 Z", Terrain_Click);
        AddWorkspace("materials", "Материалы", "M2,2 L14,2 L14,14 L2,14 Z M2,8 L14,8 M8,2 L8,14", TerrainMaterials_Click);
        AddWorkspace("objects", "Объекты", "M8,1 L15,5 L15,13 L8,17 L1,13 L1,5 Z M1,5 L8,9 L15,5 M8,9 L8,17", ObjectsInScene_Click);
        AddWorkspace("vegetation", "Растения", "M8,1 L2,9 L5,9 L1,13 L15,13 L11,9 L14,9 Z M8,13 L8,17", Vegetation_Click);
        AddWorkspace("roads", "Дороги", "M2,14 L6,2 M10,14 L14,2 M8,4 L8,6 M8,9 L8,11", Roads_Click);
        AddWorkspace("environment", "Окружение", "M5,5 L11,5 L11,11 L5,11 Z M8,0 L8,2 M8,14 L8,16 M0,8 L2,8 M14,8 L16,8 M2,2 L3,3 M13,13 L14,14 M2,14 L3,13 M13,3 L14,2", Lighting_Click);
        AddWorkspace("character", "Персонаж", "M5,1 L11,1 L11,7 L5,7 Z M2,17 L2,13 L5,10 L11,10 L14,13 L14,17", CharacterEditor_Click);
        AddWorkspace("actors", "Actors", "M8,1 L11,5 L8,9 L5,5 Z M8,9 L8,16 M1,12 L15,12 M4,16 L12,16", Actors_Click);

        SetSidebarBounds(0, 220, 340);
        SetSidebarBounds(4, 260, 400);
        ToggleLeftButton.Classes.Add("selected");
        ToggleRightButton.Classes.Add("selected");
        ObjectsPanel.HistoryAvailabilityChanged += UpdateHistoryButtons;
        TerrainEditorPanel.HistoryAvailabilityChanged += UpdateHistoryButtons;
        UpdateHistoryButtons();
    }

    private void AddBrowser(Control owner, Control browser)
    {
        // Separate visual hosts share the original controls and their callbacks.
        // Bind the owner's local mode state, not its effective visibility:
        // hiding the inspector must not deactivate the left browser or editing.
        browser.Bind(IsVisibleProperty, new ReflectionBinding(nameof(IsVisible)) { Source = owner });
        browser.Bind(IsEnabledProperty, new ReflectionBinding(nameof(IsEnabled)) { Source = owner });
        BrowserPanels.Children.Add(browser);
    }

    private void AddWorkspace(string key, string title, string geometry, EventHandler<RoutedEventArgs> handler)
    {
        int index = _workspaceButtons.Count;
        var icon = new Path
        {
            Data = Geometry.Parse(geometry), Width = 16, Height = 16,
            Stretch = Stretch.Uniform, StrokeThickness = 1.4,
            VerticalAlignment = VerticalAlignment.Center
        };
        var content = new Grid { ColumnDefinitions = new ColumnDefinitions("16,*") };
        content.Children.Add(icon);
        var label = new TextBlock
        {
            Text = title, VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(7, 0, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(label, 1);
        content.Children.Add(label);
        var button = new Button
        {
            Content = content, Margin = new Thickness(0, 0, index % 2 == 0 ? 4 : 0, 4),
            Padding = new Thickness(7, 7), HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        button.Classes.Add("nav");
        icon.Bind(Shape.StrokeProperty, new ReflectionBinding(nameof(Foreground)) { Source = button });
        button.Click += handler;
        ToolTip.SetTip(button, title);
        Grid.SetRow(button, index / 2);
        Grid.SetColumn(button, index % 2);
        WorkspaceNavigation.Children.Add(button);
        _workspaceButtons.Add(key, button);
    }

    private void UpdateWorkspaceNavigation()
    {
        foreach (var (key, button) in _workspaceButtons)
            button.Classes.Set("selected", key == _activeWorkspace);

        EnvironmentBrowser.IsVisible = EnvironmentPanel.IsVisible;
        foreach (var button in EnvironmentBrowser.Children.OfType<Button>())
            button.Classes.Set("selected", Equals(button.Content, ModeTitle.Text));

        UpdateHistoryButtons();
    }

    private void UpdateHistoryButtons()
    {
        UndoButton.IsEnabled = CanSwitchWorkspace &&
            (ObjectsPanel.IsVisible && ObjectsPanel.CanUndo || TerrainEditorPanel.IsVisible && TerrainEditorPanel.CanUndo);
        RedoButton.IsEnabled = CanSwitchWorkspace && ObjectsPanel.IsVisible && ObjectsPanel.CanRedo;
    }

    private void Undo_Click(object? sender, RoutedEventArgs e)
    {
        if (!CanSwitchWorkspace) return;
        if (ObjectsPanel.IsVisible) ObjectsPanel.UndoFromToolbar();
        else if (TerrainEditorPanel.IsVisible) TerrainEditorPanel.UndoFromToolbar();
        UpdateHistoryButtons();
    }

    private void Redo_Click(object? sender, RoutedEventArgs e)
    {
        if (CanSwitchWorkspace && ObjectsPanel.IsVisible) ObjectsPanel.RedoFromToolbar();
        UpdateHistoryButtons();
    }

    private void SetSidebarBounds(int column, double minimum, double maximum)
    {
        WorkspaceGrid.ColumnDefinitions[column].MinWidth = minimum;
        WorkspaceGrid.ColumnDefinitions[column].MaxWidth = maximum;
    }

    private void ToggleLeftPanel_Click(object? sender, RoutedEventArgs e) =>
        ToggleSidebar(LeftSidebar, LeftSplitter, 0, 1, 220, ref _leftPanelWidth);

    private void ToggleRightPanel_Click(object? sender, RoutedEventArgs e) =>
        ToggleSidebar(RightSidebar, RightSplitter, 4, 3, 260, ref _rightPanelWidth);

    private void ToggleSidebar(Control sidebar, Control splitter, int column, int separator, double minimum, ref double savedWidth)
    {
        var definition = WorkspaceGrid.ColumnDefinitions[column];
        bool show = !sidebar.IsVisible;
        if (!show) savedWidth = definition.ActualWidth;
        definition.MinWidth = show ? minimum : 0;
        definition.Width = new GridLength(show ? Math.Clamp(savedWidth, minimum, definition.MaxWidth) : 0);
        WorkspaceGrid.ColumnDefinitions[separator].Width = new GridLength(show ? 5 : 0);
        sidebar.IsVisible = show;
        splitter.IsVisible = show;
        ToggleLeftButton.Classes.Set("selected", LeftSidebar.IsVisible);
        ToggleRightButton.Classes.Set("selected", RightSidebar.IsVisible);
    }
}
