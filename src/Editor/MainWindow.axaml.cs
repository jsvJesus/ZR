using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Editor.Viewport;

namespace Editor
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Viewport.ShowGrid = GridCheck.IsChecked == true;

            CameraSettingsPanel.IsVisible =
                CameraSettingsToggle.IsChecked == true;

            NavigationHelpPanel.IsVisible =
                NavigationHelpToggle.IsChecked == true;

            CameraSettingsToggle.IsCheckedChanged += (_, _) =>
            {
                CameraSettingsPanel.IsVisible =
                    CameraSettingsToggle.IsChecked == true;
            };

            NavigationHelpToggle.IsCheckedChanged += (_, _) =>
            {
                NavigationHelpPanel.IsVisible =
                    NavigationHelpToggle.IsChecked == true;
            };

            InitializeWorkspaceLayout();
            EnvironmentPanel.Attach(Viewport, _workspace.Assets);
            TerrainEditorPanel.Attach(Viewport, _workspace.Assets);
            TerrainEditorPanel.BusyChanged += UpdatePlayControls;
            TerrainEditorPanel.UpdateTerrainRequested += UpdateTerrainFromFbx;
            VegetationPanel.Attach(Viewport, _workspace.Assets);
            VegetationPanel.BusyChanged += UpdatePlayControls;
            ObjectsPanel.Attach(Viewport, _workspace.Assets);
            RoadsPanel.Attach(Viewport);
            ActorsPanel.Attach(Viewport, _workspace.Assets);
            ObjectsPanel.SectionChanged += UpdateObjectsSectionTitle;
            Viewport.ObjectSaveRequested += () => SaveLevel_Click(this, new RoutedEventArgs());
            ObjectsPanel.BusyChanged += UpdatePlayControls;
            Viewport.ObjectDragStateChanged += UpdatePlayControls;
            Viewport.ActorDragStateChanged += UpdatePlayControls;
            CharacterPanel.Attach(Viewport, _workspace.Assets);
            CharacterPanel.BusyChanged += UpdatePlayControls;

            CameraSpeedSlider.Minimum = (double)FreeCamera.MinimumSpeed;
            CameraSpeedSlider.Maximum = (double)FreeCamera.MaximumSpeed;

            Viewport.CameraChanged += UpdateCamera;
            Viewport.RendererError += ShowRendererError;
            Viewport.PlayStateChanged += UpdatePlayControls;
            Viewport.LocomotionChanged += UpdateLocomotionInfo;

            CameraSpeedSlider.ValueChanged += (_, args) =>
            {
                Viewport.CameraSpeed = (float)args.NewValue;
            };

            GridCheck.IsCheckedChanged += (_, _) =>
            {
                Viewport.ShowGrid = GridCheck.IsChecked == true;
            };

            AddHandler(
                InputElement.KeyDownEvent,
                HandleEditorShortcut,
                RoutingStrategies.Tunnel
            );

            UpdateCamera(Viewport.CameraPose);
            Terrain_Click(this, new RoutedEventArgs());
            Closing += SaveChangesBeforeClosing;
            Closed += (_, _) =>
            {
                _isClosing = true;
                _importCancellation?.Cancel();
                VegetationPanel.CancelLoading();
                ObjectsPanel.CloseLibrary();
                CharacterPanel.SetActive(false);
                Viewport.StopPlay();
            };
        }

        private void HandleEditorShortcut(
            object? sender,
            KeyEventArgs e)
        {
            bool control =
                (e.KeyModifiers & KeyModifiers.Control) != 0;

            if (control && e.Key == Key.S)
            {
                e.Handled = true;
                SaveLevel_Click(this, new RoutedEventArgs());
                return;
            }

            if (e.Key == Key.Delete &&
                e.KeyModifiers == KeyModifiers.None &&
                e.Source is not TextBox)
            {
                if (ActorsPanel.IsVisible)
                {
                    ActorsPanel.DeleteSelected();
                    e.Handled = true;
                    return;
                }

                e.Handled =
                    ObjectsPanel.TryDeleteSelectedFromShortcut();
            }
        }

        private void Exit_Click(object? sender, RoutedEventArgs e) => Close();

        private bool _savingChangesOnClose;
        private bool _changesSavedForClose;

        private async void SaveChangesBeforeClosing(object? sender, WindowClosingEventArgs e)
        {
            Viewport.CancelObjectDrag();
            Viewport.CancelActorDrag();
            Viewport.CancelObjectPlacement();
            ObjectsPanel.CancelModelImport();
            TerrainEditorPanel.CancelMaterialImport();

            if (_changesSavedForClose ||
                (!VegetationPanel.HasPendingChanges &&
                 !ObjectsPanel.HasPendingChanges &&
                 !TerrainEditorPanel.HasPendingChanges &&
                 !EnvironmentPanel.HasPendingChanges &&
                 !RoadsPanel.HasPendingChanges &&
                 !ActorsPanel.HasPendingChanges))
            {
                return;
            }

            e.Cancel = true;
            if (_savingChangesOnClose) return;
            _savingChangesOnClose = true;
            UpdatePlayControls();
            try
            {
                await TerrainEditorPanel.SavePendingAsync();
                await VegetationPanel.SavePendingAsync();
                await ObjectsPanel.SavePendingAsync();
                await EnvironmentPanel.SavePendingAsync();
                await RoadsPanel.SavePendingAsync();
                await ActorsPanel.SavePendingAsync();
                _changesSavedForClose = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(Close);
            }
            catch (Exception exception) { StatusText.Text = $"Не удалось сохранить изменения уровня перед закрытием: {exception.Message}"; }
            finally
            {
                if (!_changesSavedForClose)
                {
                    _savingChangesOnClose = false;
                    UpdatePlayControls();
                }
            }
        }
    }
}
