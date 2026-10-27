using Avalonia.Interactivity;
using ZoneReborn.World.Characters.Locomotion;

namespace Editor
{
    public sealed partial class MainWindow
    {
        private async void Play_Click(object? sender, RoutedEventArgs e)
        {
            if (Viewport.IsPlaying || Viewport.IsStarting)
            {
                Viewport.StopPlay();
                StatusText.Text = "Редактор: свободная камера.";
                return;
            }
            if (_level == null || _importCancellation != null || _levelOpening) return;
            try
            {
                StatusText.Text = "Загрузка персонажа…";
                await Viewport.StartPlayAsync();
                if (!_isClosing) StatusText.Text = Viewport.IsPlaying
                    ? "Play: WASD — бег, Alt — ходьба, Shift — спринт, Ctrl/C — присед, Space — прыжок, Esc — Stop."
                    : "Play остановлен.";
            }
            catch (Exception exception)
            {
                if (!_isClosing) StatusText.Text = $"Ошибка Play: {exception.Message}";
            }
        }

        private void UpdatePlayControls()
        {
            if (_isClosing) return;
            bool active = Viewport.IsPlaying || Viewport.IsStarting;
            PlayButton.Content = active ? "■ Stop" : "▶ Play";
            PlayButton.IsEnabled = _level != null && _importCancellation == null && !_levelOpening;
            PlayHint.Text = active ? "WASD · Alt — шаг · Shift — спринт · Ctrl — присед · Space — прыжок" : "Персонаж появится на Terrain под камерой";
            CameraSpeedSlider.IsEnabled = !active;
            FreeNavigationHints.IsVisible = !active;
            PlayNavigationHints.IsVisible = active;
            if (!active) AnimStateInfo.Text = "";
            SetLevelCommandsBusy(_importCancellation != null);
        }

        private void UpdateLocomotionInfo(LocomotionSnapshot snapshot)
        {
            AnimStateInfo.Text = $"AnimState: {snapshot.State}\n{snapshot.Action}\nКадр: {snapshot.Frame:F1} · {snapshot.Speed:F2} м/с\n"
                + (snapshot.Grounded ? "На земле" : "В воздухе");
        }
    }
}
