using Avalonia.Interactivity;
using Editor.Viewport;

namespace Editor
{
    public sealed partial class MainWindow
    {
        private void SetMode(
            string category,
            string name,
            string description)
        {
            CurrentModeText.Text = $"{category} / {name}";
            ModeTitle.Text = name;
            ModeDetails.Text = description;
            StatusText.Text = $"Выбран раздел: {name}";
        }

        private void Terrain_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Level editor", "Terrain",
                "Редактирование игрового ландшафта.");
        }

        private void Heights_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Level editor", "Высоты и рельеф",
                "Создание и изменение высот поверхности.");
        }

        private void TerrainMaterials_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Level editor", "Текстуры поверхности",
                "Настройка материалов игрового ландшафта.");
        }

        private void Vegetation_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Level editor", "Растительность",
                "Размещение травы, кустарников и деревьев.");
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
                "Просмотр объектов игрового мира.");
        }

        private void Placement_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Objects editor", "Размещение объектов",
                "Добавление и размещение объектов.");
        }

        private void ObjectProperties_Click(object? sender, RoutedEventArgs e)
        {
            SetMode("Objects editor", "Свойства объектов",
                "Изменение параметров выбранного объекта.");
        }

    }
}
