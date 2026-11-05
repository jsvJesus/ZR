using Avalonia.Interactivity;
using Editor.Viewport;

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
            bool showCharacterEditor = false)
        {
            if (Viewport.IsObjectDragging) return;
            ObjectsPanel.CancelLibraryLoad();
            CurrentModeText.Text = $"{category} / {name}";
            ModeTitle.Text = name;
            ModeDetails.Text = description;
            TerrainMaterialsPanel.IsVisible = showTerrainMaterials;
            RoadSegmentsPanel.IsVisible = showRoadSegments;
            RoadSegmentsPanel.RefreshHighlight();
            VegetationPanel.IsVisible = showVegetation;
            VegetationPanel.RefreshMode();
            ObjectsPanel.IsVisible = showObjects;
            ObjectsPanel.RefreshMode();
            if (showObjects) ObjectsPanel.ResumeLibrary();
            CharacterPanel.IsVisible = showCharacterEditor;
            CharacterPanel.SetActive(showCharacterEditor);
            UpdatePlayControls();
            StatusText.Text = $"Выбран раздел: {name}";
        }

        private async void CharacterEditor_Click(object? sender, RoutedEventArgs e)
        {
            if (_isClosing || _levelOpening || _levelSaving || _savingChangesOnClose || _importCancellation != null || Viewport.IsObjectDragging
                || RoadSegmentsPanel.IsSaving || VegetationPanel.IsSaving || ObjectsPanel.IsBusy) return;
            SetMode("Character Editor", "Экипировка персонажа",
                "Одежда, снаряжение и оружие на персонаже текущего уровня.", showCharacterEditor: true);
            await CharacterPanel.ActivateAsync();
        }

        private void Terrain_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Level editor", "Terrain",
                "Редактирование игрового ландшафта.");
        }

        private void RoadSegments_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Level editor", "Road Segments",
                "Сегменты исходных дорог, общие узлы и материалы.", showRoadSegments: true);
        }

        private void Heights_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Level editor", "Высоты и рельеф",
                "Создание и изменение высот поверхности.");
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
            SetMode("Scene editor", "Освещение",
                "Настройка источников света.");
        }

        private void Shadows_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Scene editor", "Тени",
                "Параметры теней игровой сцены.");
        }

        private void Clouds_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Scene editor", "Облака",
                "Параметры облаков и небесной системы.");
        }

        private void Weather_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Scene editor", "Погода",
                "Редактирование системы погоды.");
        }

        private void Objects_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Objects editor", "Объекты сцены",
                "Выбор, изменение и удаление отдельных объектов текущего уровня.", showObjects: true);
        }

        private void Placement_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Objects editor", "Размещение объектов",
                "Добавление и размещение объектов.", showObjects: true);
        }

        private void ObjectProperties_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Objects editor", "Свойства объектов",
                "Параметры выбранного объекта; правки затрагивают только этот экземпляр на карте.", showObjects: true);
        }

    }
}
