using Avalonia.Interactivity;
using Editor.Viewport;
using Editor.Environment;
using Editor.Objects;

namespace Editor
{
    public sealed partial class MainWindow
    {
        private void SetMode(
            string category,
            string name,
            string description,
            bool showTerrainMaterials = false,
            bool showRoadSegments = false,
            bool showVegetation = false,
            bool showObjects = false,
            bool showCharacterEditor = false,
            bool showTerrainEditing = false,
            bool showEnvironment = false)
        {
            if (Viewport.IsObjectDragging)
            {
                return;
            }

            if (!showObjects || !ObjectsPanel.IsVisible)
            {
                ObjectsPanel.CancelLibraryLoad();
            }

            CurrentModeText.Text = $"{category} / {name}";

            ModeTitle.Text = name;
            ModeDetails.Text = description;
            TerrainMaterialsPanel.IsVisible = false;
            TerrainEditorPanel.IsVisible = showTerrainEditing || showTerrainMaterials;
            TerrainEditorPanel.SetPainting(showTerrainMaterials);
            EnvironmentPanel.IsVisible = showEnvironment;
            RoadSegmentsPanel.IsVisible = showRoadSegments;
            RoadSegmentsPanel.RefreshHighlight();
            VegetationPanel.IsVisible = showVegetation;
            VegetationPanel.RefreshMode();
            ObjectsPanel.IsVisible = showObjects;
            ObjectsPanel.RefreshMode();
            TerrainEditorPanel.RefreshMode();
            if (showObjects) ObjectsPanel.ResumeLibrary();
            CharacterPanel.IsVisible = showCharacterEditor;
            CharacterPanel.SetActive(showCharacterEditor);
            UpdatePlayControls();
            StatusText.Text = $"Выбран раздел: {name}";
        }

        private async void CharacterEditor_Click(object? sender, RoutedEventArgs e)
        {
            if (_isClosing || _levelOpening || _levelSaving || _savingChangesOnClose || _importCancellation != null || Viewport.IsObjectDragging
                || RoadSegmentsPanel.IsSaving || VegetationPanel.IsSaving || ObjectsPanel.IsBusy || TerrainEditorPanel.IsBusy) return;
            SetMode("Character Editor", "Экипировка персонажа",
                "Одежда, снаряжение и оружие на персонаже текущего уровня.", showCharacterEditor: true);
            await CharacterPanel.ActivateAsync();
        }

        private void Terrain_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Level editor", "Terrain",
                "Редактирование игрового ландшафта.", showTerrainEditing: true);
        }

        private void RoadSegments_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Level editor", "Road Segments",
                "Сегменты исходных дорог, общие узлы и материалы.", showRoadSegments: true);
        }

        private void Heights_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Level editor", "Высоты и рельеф",
                "Создание и изменение высот поверхности.", showTerrainEditing: true);
        }

        private void TerrainMaterials_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Level editor", "Текстуры поверхности",
                "Настройка материалов игрового ландшафта.",
                showTerrainMaterials: true);
        }

        private void Vegetation_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Level editor", "Растительность",
                "Каталог SpeedTree и размещение деревьев на уровне.", showVegetation: true);
        }

        private void Lighting_Click(object? sender, RoutedEventArgs e)
        {
            SetMode(
                "Scene editor",
                "Освещение",
                "Солнце, время суток и окружающее освещение.",
                showEnvironment: true);

            EnvironmentPanel.ShowSection(EnvironmentSection.Lighting);
        }

        private void Shadows_Click(object? sender, RoutedEventArgs e)
        {
            SetMode(
                "Scene editor",
                "Тени",
                "Параметры динамических теней.",
                showEnvironment: true);

            EnvironmentPanel.ShowSection(EnvironmentSection.Shadows);
        }

        private void Clouds_Click(object? sender, RoutedEventArgs e)
        {
            SetMode(
                "Scene editor",
                "Облака",
                "Облачность и движение облачных слоёв.",
                showEnvironment: true);

            EnvironmentPanel.ShowSection(EnvironmentSection.Clouds);
        }

        private void Weather_Click(object? sender, RoutedEventArgs e)
        {
            SetMode(
                "Scene editor",
                "Погода",
                "Осадки, ветер и туман.",
                showEnvironment: true);

            EnvironmentPanel.ShowSection(EnvironmentSection.Weather);
        }

        private void ObjectsImport_Click(
            object? sender,
            RoutedEventArgs e)
        {
            ActivateObjectsSection(ObjectsSection.Import);
        }

        private void ObjectsInScene_Click(
            object? sender,
            RoutedEventArgs e)
        {
            ActivateObjectsSection(ObjectsSection.ObjectsInScene);
        }

        private void ObjectsPlace_Click(
            object? sender,
            RoutedEventArgs e)
        {
            ActivateObjectsSection(ObjectsSection.PlaceObjects);
        }

        private void ObjectsCollision_Click(
            object? sender,
            RoutedEventArgs e)
        {
            ActivateObjectsSection(ObjectsSection.ObjectsCollision);
        }

        private void ActivateObjectsSection(ObjectsSection section)
        {
            if (Viewport.IsObjectDragging)
            {
                return;
            }

            SetMode(
                "Objects editor",
                "Объекты",
                "Импорт, размещение, редактирование и коллизия.",
                showObjects: true
            );

            ObjectsPanel.ShowSection(section);
        }

        private void UpdateObjectsSectionTitle(
            ObjectsSection section)
        {
            if (!ObjectsPanel.IsVisible)
            {
                return;
            }

            var (name, description) = section switch
            {
                ObjectsSection.Import => (
                    "Import",
                    "Импорт FBX-моделей и настройка исходных текстур."
                ),

                ObjectsSection.ObjectsInScene => (
                    "Objects in Scene",
                    "Редактирование объектов текущего уровня."
                ),

                ObjectsSection.PlaceObjects => (
                    "Place Objects",
                    "Выбор моделей из каталога и размещение на сцене."
                ),

                ObjectsSection.ObjectsCollision => (
                    "Objects Collision",
                    "Просмотр, генерация и оптимизация коллизий."
                ),

                _ => (
                    "Objects editor",
                    "Инструменты работы с объектами."
                )
            };

            CurrentModeText.Text = $"Objects editor / {name}";
            ModeTitle.Text = name;
            ModeDetails.Text = description;
            StatusText.Text = $"Выбран раздел: {name}";
        }
    }
}
