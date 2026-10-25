using Avalonia.Interactivity;
using Editor.Viewport;

namespace Editor
{
    public sealed partial class MainWindow
    {
        private void UpdateCamera(CameraPose pose)
        {
            PositionText.Text =
                $"X: {pose.X:F2}  Y: {pose.Y:F2}  Z: {pose.Z:F2}";

            RotationText.Text =
                $"Yaw: {pose.Yaw * 180 / MathF.PI:F1}  " +
                $"Pitch: {pose.Pitch * 180 / MathF.PI:F1}";

            SpeedText.Text = $"{pose.Speed:F1} м/с";

            if (Math.Abs(CameraSpeedSlider.Value - pose.Speed) > 0.1)
            {
                CameraSpeedSlider.Value = pose.Speed;
            }
        }

        private void ShowRendererError(string message)
        {
            StatusText.Text = $"Ошибка DX11: {message}";
        }

    }
}
