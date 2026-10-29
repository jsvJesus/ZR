using Avalonia.Controls;
using Avalonia.Interactivity;
using Editor.Viewport;

namespace Editor
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            RoadSegmentsPanel.Attach(Viewport);
            Viewport.RoadSegmentPicked += RoadSegmentsPanel.Select;
            RoadSegmentsPanel.BusyChanged += UpdatePlayControls;

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

            UpdateCamera(Viewport.CameraPose);
            Closing += SaveRoadsBeforeClosing;
            Closed += (_, _) =>
            {
                _isClosing = true;
                _importCancellation?.Cancel();
                Viewport.StopPlay();
            };
        }

        private void Exit_Click(object? sender, RoutedEventArgs e) => Close();

        private bool _savingRoadsOnClose;
        private bool _roadsSavedForClose;

        private async void SaveRoadsBeforeClosing(object? sender, WindowClosingEventArgs e)
        {
            if (_roadsSavedForClose || !RoadSegmentsPanel.HasPendingChanges) return;
            e.Cancel = true;
            if (_savingRoadsOnClose) return;
            _savingRoadsOnClose = true;
            try
            {
                await RoadSegmentsPanel.SavePendingAsync();
                _roadsSavedForClose = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(Close);
            }
            catch (Exception exception) { StatusText.Text = $"Не удалось сохранить дороги перед закрытием: {exception.Message}"; }
            finally { _savingRoadsOnClose = false; }
        }
    }
}
