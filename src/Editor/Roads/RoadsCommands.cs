using Avalonia.Interactivity;

namespace Editor;

public sealed partial class MainWindow
{
    private void Roads_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (!SetMode(
                "Дороги",
                "Редактор дорог",
                "Создание и редактирование дорог через контрольные точки."))
        {
            return;
        }

        RoadsPanel.SetActive(true);

        _activeWorkspace = "roads";
        UpdateWorkspaceNavigation();

        StatusText.Text = "Режим редактирования дорог.";
    }
}
