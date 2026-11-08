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
            TerrainEditorPanel.Attach(Viewport, _workspace.Assets);
            TerrainEditorPanel.BusyChanged += UpdatePlayControls;
            RoadSegmentsPanel.Attach(Viewport);
            Viewport.RoadSegmentPicked += RoadSegmentsPanel.Select;
            RoadSegmentsPanel.BusyChanged += UpdatePlayControls;
            VegetationPanel.Attach(Viewport, _workspace.Assets);
            VegetationPanel.BusyChanged += UpdatePlayControls;
            ObjectsPanel.Attach(Viewport, _workspace.Assets);
            Viewport.ObjectSaveRequested += () => SaveLevel_Click(this, new RoutedEventArgs());
            ObjectsPanel.BusyChanged += UpdatePlayControls;
            Viewport.ObjectDragStateChanged += UpdatePlayControls;
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
            Viewport.CancelObjectPlacement();
            ObjectsPanel.CancelModelImport();
            TerrainEditorPanel.CancelMaterialImport();
            if (_changesSavedForClose || (!RoadSegmentsPanel.HasPendingChanges && !VegetationPanel.HasPendingChanges && !ObjectsPanel.HasPendingChanges && !TerrainEditorPanel.HasPendingChanges)) return;
            e.Cancel = true;
            if (_savingChangesOnClose) return;
            _savingChangesOnClose = true;
            UpdatePlayControls();
            try
            {
                await TerrainEditorPanel.SavePendingAsync();
                await RoadSegmentsPanel.SavePendingAsync();
                await VegetationPanel.SavePendingAsync();
                await ObjectsPanel.SavePendingAsync();
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
