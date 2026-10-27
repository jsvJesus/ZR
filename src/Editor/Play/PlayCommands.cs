using Avalonia.Interactivity;

namespace Editor;

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
                ? "Play: ПКМ + мышь — обзор, колёсико — дистанция, Esc — Stop."
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
        PlayHint.Text = active ? "ПКМ — обзор · Колёсико — дистанция · Esc — Stop" : "Персонаж появится на Terrain под камерой";
        CameraSpeedSlider.IsEnabled = !active;
        FreeNavigationHints.IsVisible = !active;
        PlayNavigationHints.IsVisible = active;
        SetLevelCommandsBusy(_importCancellation != null);
    }
}
