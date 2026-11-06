using Avalonia.Interactivity;
using ZoneReborn.World.Characters.Locomotion;

namespace Editor
{
    public sealed partial class MainWindow
    {
        private async void Play_Click(object? sender, RoutedEventArgs e)
        {
            if (CharacterPanel.IsActive || Viewport.IsObjectDragging) return;
            if (Viewport.IsPlaying || Viewport.IsStarting)
            {
                Viewport.StopPlay();
                StatusText.Text = "Редактор: свободная камера.";
                return;
            }
            if (_level == null || _isClosing || _importCancellation != null || _levelOpening || _levelSaving || _savingChangesOnClose
                || RoadSegmentsPanel.IsSaving || VegetationPanel.IsSaving || ObjectsPanel.IsBusy || TerrainEditorPanel.IsBusy) return;
            try
            {
                StatusText.Text = "Загрузка персонажа…";
                await Viewport.StartPlayAsync();
                if (!_isClosing) StatusText.Text = Viewport.IsPlaying
                    ? "Play: мышь — обзор, WASD — бег, Alt — ходьба, Shift — спринт, Ctrl/C — присед, Space — прыжок, F8 — осмотр, Esc — Stop."
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
            PlayHint.Text = CharacterPanel.IsActive ? "Character Editor · ПКМ — осмотр · колесо — расстояние"
                : Viewport.IsPlayInspecting ? "Осмотр · WASD — полёт · Space/Shift — вверх/вниз · F8 — к персонажу"
                : active ? "WASD · Alt — шаг · Shift — спринт · Ctrl — присед · Space — прыжок · F8 — осмотр" : "Персонаж появится на Terrain под камерой";
            CameraSpeedSlider.IsEnabled = !active && !Viewport.IsObjectDragging;
            PlayNavigationHints.IsVisible = active && !CharacterPanel.IsActive && !Viewport.IsPlayInspecting;
            InspectionNavigationHints.IsVisible = Viewport.IsPlayInspecting && !CharacterPanel.IsActive;
            FreeNavigationHints.IsVisible = !active && !CharacterPanel.IsActive;
            AnimStateInfo.IsVisible = active && !CharacterPanel.IsActive;
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
