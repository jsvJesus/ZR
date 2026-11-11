using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Scene.Import;

namespace Editor.Objects;

public sealed partial class ObjectsPanel
{
    private static readonly string[] ProfileIds =
    [
        "auto",
        "vehicle",
        "building",
        "small",
        "complex",
        "general",
        "custom"
    ];

    private static readonly string[] ProfileNames =
    [
        "Автоматически",
        "Транспорт",
        "Здания",
        "Малые объекты",
        "Сложная геометрия",
        "Универсальный",
        "Ручные настройки"
    ];

    private readonly CheckBox _collisionCheck = new()
    {
        Content = "Показать коллизию выбранного объекта",
        IsChecked = false
    };

    private readonly ComboBox _collisionProfile = new()
    {
        ItemsSource = ProfileNames,
        SelectedIndex = 0,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private readonly TextBox _collisionPercent = new()
    {
        Text = "12",
        Watermark = "5–100"
    };

    private readonly TextBox _collisionError = new()
    {
        Text = "15",
        Watermark = "1–100"
    };

    private readonly CheckBox _collisionBorders = new()
    {
        Content = "Сохранять открытые границы",
        IsChecked = true
    };

    private readonly Button _collisionGenerate = new()
    {
        Content = "Сохранить и перегенерировать",
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private readonly TextBlock _collisionInfo = Text(
        "Выберите объект из models2."
    );

    private readonly TextBlock _collisionResult = Text("");

    private string? _collisionSettingsPath;
    private bool _collisionBuilding;
    private bool _collisionBinding;

    private void BuildCollisionControls(StackPanel root)
    {
        root.Children.Add(
            Text("ОТОБРАЖЕНИЕ КОЛЛИЗИИ")
        );

        root.Children.Add(_collisionCheck);

        root.Children.Add(Text(
            "Зелёная сетка показывает физическую геометрию " +
            "выбранного объекта."
        ));

        root.Children.Add(Text("ОПТИМИЗАЦИЯ КОЛЛИЗИИ"));

        root.Children.Add(Text("Профиль генерации"));
        root.Children.Add(_collisionProfile);

        root.Children.Add(
            Text("Целевое количество треугольников, %")
        );
        root.Children.Add(_collisionPercent);

        root.Children.Add(Text("Максимальная ошибка, мм"));
        root.Children.Add(_collisionError);

        root.Children.Add(_collisionBorders);
        root.Children.Add(_collisionGenerate);

        root.Children.Add(Text("СТАТИСТИКА"));
        root.Children.Add(_collisionInfo);
        root.Children.Add(_collisionResult);

        _collisionCheck.IsCheckedChanged +=
            OnCollisionCheckChanged;

        _collisionProfile.SelectionChanged +=
            OnCollisionProfileChanged;

        _collisionGenerate.Click += async (_, _) =>
            await RegenerateCollisionAsync();
    }

    private void OnCollisionCheckChanged(
        object? sender,
        RoutedEventArgs e)
    {
        UpdateCollisionVisibility();
    }

    private void OnCollisionProfileChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_collisionBinding ||
            _collisionBuilding ||
            _assetPaths is not { } assets ||
            Selected is not { } selected)
        {
            return;
        }

        string profile = SelectedProfile();

        if (profile == "custom")
        {
            return;
        }

        try
        {
            string path = AssetPathResolver.Resolve(
                assets.Root,
                selected.Model
            );

            var settings = CollisionProfiles.Resolve(
                profile,
                path
            );

            ApplySettings(settings);

            _collisionResult.Text =
                "Профиль выбран. Нажмите " +
                "«Сохранить и перегенерировать».";
        }
        catch (Exception exception)
        {
            _collisionResult.Text = exception.Message;
        }
    }

    private void ApplySettings(
        CollisionGenerationSettings settings)
    {
        _collisionPercent.Text =
            (settings.TargetRatio * 100f).ToString(
                "0.##",
                CultureInfo.InvariantCulture
            );

        _collisionError.Text =
            (settings.MaximumError * 1000f).ToString(
                "0.##",
                CultureInfo.InvariantCulture
            );

        _collisionBorders.IsChecked =
            settings.PreserveBorders;
    }

    private string SelectedProfile()
    {
        int index = _collisionProfile.SelectedIndex;

        return index >= 0 && index < ProfileIds.Length
            ? ProfileIds[index]
            : "custom";
    }

    private void RefreshCollisionMode()
    {
        _collisionCheck.IsEnabled = CanInteract;
        UpdateCollisionVisibility();

        if (_collisionBuilding)
        {
            _collisionProfile.IsEnabled = false;
            _collisionPercent.IsEnabled = false;
            _collisionError.IsEnabled = false;
            _collisionBorders.IsEnabled = false;
            _collisionGenerate.IsEnabled = false;
            return;
        }

        var selected = Selected;

        bool supported =
            selected != null &&
            selected.Model.StartsWith(
                "models2/",
                StringComparison.OrdinalIgnoreCase
            ) &&
            Path.GetExtension(selected.Model).Equals(
                ImportedModelStorage.Extension,
                StringComparison.OrdinalIgnoreCase
            );

        bool enabled =
            supported &&
            CanInteract &&
            _assetPaths != null;

        _collisionProfile.IsEnabled = enabled;
        _collisionPercent.IsEnabled = enabled;
        _collisionError.IsEnabled = enabled;
        _collisionBorders.IsEnabled = enabled;
        _collisionGenerate.IsEnabled = enabled;

        if (!supported || _assetPaths == null)
        {
            _collisionSettingsPath = null;
            _collisionInfo.Text = "Выберите объект из models2.";
            return;
        }

        try
        {
            string modelPath = AssetPathResolver.Resolve(
                _assetPaths.Root,
                selected!.Model
            );

            if (!string.Equals(
                    _collisionSettingsPath,
                    modelPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                var settings =
                    CollisionGenerationSettingsStorage.Read(
                        modelPath
                    );

                _collisionBinding = true;

                try
                {
                    int index = Array.IndexOf(
                        ProfileIds,
                        settings.Profile
                    );

                    _collisionProfile.SelectedIndex =
                        index >= 0 ? index : ProfileIds.Length - 1;

                    ApplySettings(settings);
                }
                finally
                {
                    _collisionBinding = false;
                }

                _collisionSettingsPath = modelPath;
                _collisionResult.Text = "";
            }

            string profileName = SelectedProfile() == "auto"
                ? "Автоматически → " +
                    CollisionProfiles.DisplayName(
                        CollisionProfiles.Detect(modelPath)
                    )
                : CollisionProfiles.DisplayName(
                    SelectedProfile()
                );

            var scene = _level?.Objects;

            if (scene != null &&
                scene.Physics.TryGet(
                    selected.Model,
                    out ScenePhysicsMesh? physical) &&
                physical != null)
            {
                int triangles = physical.Indices.Length / 3;

                _collisionInfo.Text =
                    $"Модель: {Path.GetFileName(selected.Model)}\n" +
                    $"Профиль: {profileName}\n" +
                    $"Коллизия: {triangles:N0} треугольников\n" +
                    $"Вершины: {physical.Vertices.Length:N0}";

                int recommended =
                    CollisionProfiles.RecommendedTriangles(modelPath);

                if (triangles > recommended)
                {
                    _collisionInfo.Text +=
                        $"\nОриентир для этого типа: " +
                        $"{recommended:N0} треугольников.";
                }
            }
            else
            {
                _collisionInfo.Text =
                    $"Модель: {Path.GetFileName(selected.Model)}\n" +
                    $"Профиль: {profileName}\n" +
                    "Коллизия ещё не загружена.";
            }
        }
        catch (Exception exception)
        {
            _collisionGenerate.IsEnabled = false;
            _collisionResult.Text = exception.Message;
        }
    }

    private void UpdateCollisionVisibility()
    {
        if (_viewport == null)
        {
            return;
        }

        _viewport.ShowSelectedCollision =
            _collisionCheck.IsChecked == true;
    }

    private async Task RegenerateCollisionAsync()
    {
        if (_collisionBuilding ||
            !CanInteract ||
            _assetPaths is not { } assets ||
            _level?.Objects is not { } scene ||
            Selected is not { } selected)
        {
            return;
        }

        string model = selected.Model;

        if (!model.StartsWith(
                "models2/",
                StringComparison.OrdinalIgnoreCase) ||
            !Path.GetExtension(model).Equals(
                ImportedModelStorage.Extension,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        bool entered = false;

        try
        {
            float percent = ParseNumber(
                _collisionPercent.Text,
                "Процент треугольников"
            );

            float errorMillimeters = ParseNumber(
                _collisionError.Text,
                "Максимальная ошибка"
            );

            if (percent < 5f || percent > 100f)
            {
                throw new InvalidDataException(
                    "Укажи процент от 5 до 100."
                );
            }

            if (errorMillimeters < 1f ||
                errorMillimeters > 100f)
            {
                throw new InvalidDataException(
                    "Укажи ошибку от 1 до 100 мм."
                );
            }

            string modelPath = AssetPathResolver.Resolve(
                assets.Root,
                model
            );

            string profile = SelectedProfile();

            var settings = new CollisionGenerationSettings(
                percent / 100f,
                errorMillimeters / 1000f,
                _collisionBorders.IsChecked == true,
                profile
            );

            if (profile != "custom")
            {
                var preset = CollisionProfiles.Resolve(
                    profile,
                    modelPath
                );

                if (MathF.Abs(
                        preset.TargetRatio -
                        settings.TargetRatio) > 0.000001f ||
                    MathF.Abs(
                        preset.MaximumError -
                        settings.MaximumError) > 0.000001f ||
                    preset.PreserveBorders != settings.PreserveBorders)
                {
                    settings = settings with
                    {
                        Profile = "custom"
                    };
                }
            }

            _collisionBuilding = true;

            _collisionResult.Text =
                "Генерация и подготовка BVH…";

            BusyChanged?.Invoke();
            RefreshMode();

            await AssetDecoder.WaitAsync();
            entered = true;

            var generated = await Task.Run(() =>
            {
                var stopwatch = Stopwatch.StartNew();

                CollisionGenerationSettingsStorage.Write(
                    modelPath,
                    settings
                );

                var result = CollisionGenerator.Generate(
                    assets,
                    model
                );

                stopwatch.Stop();

                var loadTimer = Stopwatch.StartNew();

                var physical = CollisionStorage.Read(
                    assets,
                    model
                );

                loadTimer.Stop();

                var tree = SceneCollision.PrepareTree(
                    physical
                );

                long fileBytes = new FileInfo(
                    CollisionStorage.GetPath(modelPath)
                ).Length;

                return (
                    Result: result,
                    Physics: physical,
                    GenerateMs: stopwatch.Elapsed.TotalMilliseconds,
                    LoadMs: loadTimer.Elapsed.TotalMilliseconds,
                    Tree: tree,
                    FileBytes: fileBytes
                );
            });

            if (!ReferenceEquals(_level?.Objects, scene))
            {
                return;
            }

            scene.Physics.Store(
                model,
                generated.Physics
            );

            _collisionBinding = true;

            try
            {
                _collisionProfile.SelectedIndex =
                    Array.IndexOf(
                        ProfileIds,
                        settings.Profile
                    );
            }
            finally
            {
                _collisionBinding = false;
            }

            int original = generated.Result.SourceTriangles;
            int simplified = generated.Result.CollisionTriangles;

            double reduction = original == 0
                ? 0
                : 100.0 * (1.0 -
                    (double)simplified / original);

            long meshBytes =
                generated.Physics.Vertices.LongLength * 12L +
                generated.Physics.Indices.LongLength * 4L;

            long estimatedMemory =
                meshBytes + generated.Tree.EstimatedBytes;

            int recommended =
                CollisionProfiles.RecommendedTriangles(modelPath);

            string warning = simplified > recommended
                ? $"\nВнимание: коллизия тяжелее ориентира " +
                    $"({recommended:N0} треугольников)."
                : "";

            _collisionResult.Text =
                "Коллизия успешно обновлена.\n" +
                $"Исходная: {original:N0} треугольников.\n" +
                $"Новая: {simplified:N0} треугольников.\n" +
                $"Уменьшение: {reduction:F1}%.\n" +
                $"Вершины: {generated.Physics.Vertices.Length:N0}.\n" +
                $"Размер файла: " +
                $"{generated.FileBytes / 1048576.0:F2} МиБ.\n" +
                $"Генерация: {generated.GenerateMs:F0} мс.\n" +
                $"Чтение коллизии: {generated.LoadMs:F0} мс.\n" +
                $"BVH: {generated.Tree.Nodes:N0} узлов.\n" +
                $"Подготовка BVH: " +
                $"{generated.Tree.PreparationMilliseconds:F0} мс.\n" +
                $"Память меша и BVH (оценка): " +
                $"{estimatedMemory / 1048576.0:F2} МиБ." +
                warning +
                "\nНастройки сохранены рядом с моделью.";
        }
        catch (Exception exception)
        {
            _collisionResult.Text =
                "Ошибка генерации: " + exception.Message;
        }
        finally
        {
            if (entered)
            {
                AssetDecoder.Release();
            }

            _collisionBuilding = false;

            BusyChanged?.Invoke();
            RefreshMode();
        }
    }

    private static float ParseNumber(
        string? text,
        string name)
    {
        string normalized = (text ?? "")
            .Trim()
            .Replace(',', '.');

        if (!float.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float value) ||
            !float.IsFinite(value))
        {
            throw new InvalidDataException(
                $"Некорректное значение: {name}."
            );
        }

        return value;
    }
}
