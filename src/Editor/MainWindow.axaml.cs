using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Editor
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Viewport.CameraChanged += UpdateCamera;
            Viewport.RendererError += ShowRendererError;

            CameraSpeedSlider.ValueChanged += (_, args) =>
            {
                Viewport.CameraSpeed = (float)args.NewValue;
            };

            GridCheck.IsCheckedChanged += (_, _) =>
            {
                Viewport.ShowGrid = GridCheck.IsChecked == true;
            };

            UpdateCamera(Viewport.CameraPose);
            Closed += (_, _) =>
            {
                _isClosing = true;
                _importCancellation?.Cancel();
            };
        }

        private void Exit_Click(object? sender, RoutedEventArgs e) => Close();
    }
}
