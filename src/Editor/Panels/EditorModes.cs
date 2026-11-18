using Avalonia.Interactivity;
using Editor.Viewport;
using Editor.Environment;
using Editor.Objects;

namespace Editor
{
    public sealed partial class MainWindow
    {
        private bool SetMode(
            string category,
            string name,
            string description,
            bool showTerrainMaterials = false,
            bool showVegetation = false,
            bool showObjects = false,
            bool showCharacterEditor = false,
            bool showTerrainEditing = false,
            bool showEnvironment = false)
        {
            if (!CanSwitchWorkspace)
            {
                return false;
            }

            RoadsPanel.SetActive(false);

            if (!showObjects || !ObjectsPanel.IsVisible)
            {
                ObjectsPanel.CancelLibraryLoad();
            }

            CurrentModeText.Text = $"{category} / {name}";

            ModeTitle.Text = name;
            ModeDetails.Text = description;

            TerrainEditorPanel.IsVisible =
                showTerrainEditing || showTerrainMaterials;

            TerrainEditorPanel.SetPainting(showTerrainMaterials);

            EnvironmentPanel.IsVisible = showEnvironment;


            VegetationPanel.IsVisible = showVegetation;
            VegetationPanel.RefreshMode();

            ObjectsPanel.IsVisible = showObjects;
            ObjectsPanel.RefreshMode();

            TerrainEditorPanel.RefreshMode();

            if (showObjects)
            {
                ObjectsPanel.ResumeLibrary();
            }

            CharacterPanel.IsVisible = showCharacterEditor;
            CharacterPanel.SetActive(showCharacterEditor);

            _activeWorkspace = showObjects ? "objects"
                : showCharacterEditor ? "character"
                : showVegetation ? "vegetation"
                : showEnvironment ? "environment"
                : showTerrainMaterials ? "materials" : "terrain";
            UpdateWorkspaceNavigation();
            UpdatePlayControls();

            StatusText.Text = $"Выбран раздел: {name}";
            return true;
        }

        private async void CharacterEditor_Click(
            object? sender,
            RoutedEventArgs e)
        {
            if (_isClosing ||
                _levelOpening ||
                _levelSaving ||
                _savingChangesOnClose ||
                _importCancellation != null ||
                Viewport.IsObjectDragging ||
                VegetationPanel.IsSaving ||
                ObjectsPanel.IsBusy ||
                TerrainEditorPanel.IsBusy)
            {
                return;
            }

            if (!SetMode(
                "Персонаж",
                "Экипировка персонажа",
                "Одежда, снаряжение и оружие на персонаже текущего уровня.",
                showCharacterEditor: true
            )) return;

            await CharacterPanel.ActivateAsync();
        }

        private void Terrain_Click(
            object? sender,
            RoutedEventArgs e)
        {
            SetMode(
                "Ландшафт",
                "Рельеф",
                "Поднятие, опускание, сглаживание поверхности и редактирование отверстий.",
                showTerrainEditing: true
            );
        }

        private void TerrainMaterials_Click(
            object? sender,
            RoutedEventArgs e)
        {
            SetMode(
                "Ландшафт",
                "Материалы",
                "Материалы поверхности, слои, текстуры и кисть покраски.",
                showTerrainMaterials: true
            );
        }

        private void Vegetation_Click(
            object? sender,
            RoutedEventArgs e)
        {
            SetMode(
                "Растительность",
                "Растительность",
                "Каталог SpeedTree и размещение растительности.",
                showVegetation: true
            );
        }

        private void Lighting_Click(
            object? sender,
            RoutedEventArgs e)
        {
            if (!SetMode(
                "Окружение",
                "Освещение",
                "Солнце, время суток и окружающее освещение.",
                showEnvironment: true
            )) return;

            EnvironmentPanel.ShowSection(
                EnvironmentSection.Lighting
            );
        }

        private void Shadows_Click(
            object? sender,
            RoutedEventArgs e)
        {
            if (!SetMode(
                "Окружение",
                "Тени",
                "Параметры динамических теней.",
                showEnvironment: true
            )) return;

            EnvironmentPanel.ShowSection(
                EnvironmentSection.Shadows
            );
        }

        private void Clouds_Click(
            object? sender,
            RoutedEventArgs e)
        {
            if (!SetMode(
                "Окружение",
                "Облака",
                "Облачность и движение облачных слоёв.",
                showEnvironment: true
            )) return;

            EnvironmentPanel.ShowSection(
                EnvironmentSection.Clouds
            );
        }

        private void Weather_Click(
            object? sender,
            RoutedEventArgs e)
        {
            if (!SetMode(
                "Окружение",
                "Погода",
                "Осадки, ветер и туман.",
                showEnvironment: true
            )) return;

            EnvironmentPanel.ShowSection(
                EnvironmentSection.Weather
            );
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

            if (!SetMode(
                "Объекты",
                "Объекты",
                "Импорт, размещение, редактирование и коллизия.",
                showObjects: true
            )) return;

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
                    "Импорт объектов",
                    "Импорт FBX-моделей и настройка исходных текстур."
                ),

                ObjectsSection.ObjectsInScene => (
                    "Объекты сцены",
                    "Редактирование объектов текущего уровня."
                ),

                ObjectsSection.PlaceObjects => (
                    "Размещение объектов",
                    "Выбор моделей из каталога и размещение на сцене."
                ),

                ObjectsSection.ObjectsCollision => (
                    "Коллизия объектов",
                    "Просмотр, генерация и оптимизация коллизий."
                ),

                _ => (
                    "Объекты",
                    "Инструменты работы с объектами."
                )
            };

            CurrentModeText.Text = $"Объекты / {name}";
            ModeTitle.Text = name;
            ModeDetails.Text = description;
            UpdateWorkspaceNavigation();
            StatusText.Text = $"Выбран раздел: {name}";
        }
    }
}
