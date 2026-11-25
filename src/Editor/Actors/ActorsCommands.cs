using Avalonia.Interactivity;
using Editor.Actors;

namespace Editor
{
    public sealed partial class MainWindow
    {
        private async void Actors_Click(
            object? sender,
            RoutedEventArgs e)
        {
            if (!SetMode(
                    "Actors",
                    "Акторы",
                    "Размещение точек появления игроков, NPC, мобов и лута."))
            {
                return;
            }

            ActorsPanel.SetActive(true);

            _activeWorkspace = "actors";

            UpdateWorkspaceNavigation();
            UpdatePlayControls();

            StatusText.Text =
                "Actors: Ctrl + ЛКМ — добавить, ЛКМ — выбрать.";

            await ActorsPanel.LoadCatalogAsync();
        }
    }
}
