using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Editor.Viewport;

namespace Editor
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Viewport.CameraChanged += UpdateCamera;
            Viewport.RendererError += ShowRendererError;

            CameraSpeedSlider.ValueChanged += (_, args) =>
            {
                Viewport.CameraSpeed = (float)args.NewValue;
            };

            GridCheck.IsCheckedChanged += (_, _) =>
            {
                Viewport.ShowGrid = GridCheck.IsChecked == true;
            };

            UpdateCamera(Viewport.CameraPose);
        }

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

        private async void OpenLevel_Click(
            object? sender,
            RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Открыть уровень",
                    AllowMultiple = false,
                    FileTypeFilter =
                    [
                        new FilePickerFileType("Уровень")
                        {
                            Patterns = ["*.json"]
                        }
                    ]
                });

            var file = files.FirstOrDefault();

            if (file == null)
            {
                return;
            }

            try
            {
                await using var stream = await file.OpenReadAsync();

                var document =
                    await JsonSerializer.DeserializeAsync<LevelDocument>(
                        stream);

                if (document == null || document.Version != 1)
                {
                    throw new InvalidDataException(
                        "Неподдерживаемый формат уровня.");
                }

                Viewport.SetCamera(document.Camera);

                LevelNameText.Text = $"Уровень: {file.Name}";
                StatusText.Text = "Уровень открыт";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка открытия: {ex.Message}";
            }
        }

        private async void SaveLevel_Click(
            object? sender,
            RoutedEventArgs e)
        {
            var file = await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Сохранить уровень",
                    SuggestedFileName = "Untitled.json",
                    DefaultExtension = "json",
                    FileTypeChoices =
                    [
                        new FilePickerFileType("Уровень")
                        {
                            Patterns = ["*.json"]
                        }
                    ]
                });

            if (file == null)
            {
                return;
            }

            try
            {
                var document = new LevelDocument
                {
                    Version = 1,
                    Camera = Viewport.CameraPose
                };

                await using var stream = await file.OpenWriteAsync();

                await JsonSerializer.SerializeAsync(
                    stream,
                    document,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                LevelNameText.Text = $"Уровень: {file.Name}";
                StatusText.Text = "Уровень сохранён";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка сохранения: {ex.Message}";
            }
        }

        private void Exit_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public sealed class LevelDocument
    {
        public int Version { get; set; } = 1;

        public CameraPose Camera { get; set; }
    }
}
