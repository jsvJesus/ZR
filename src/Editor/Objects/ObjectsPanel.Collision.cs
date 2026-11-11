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
    private readonly CheckBox _collisionCheck = new()
    {
        Content = "Показать коллизию выбранного объекта",
        IsChecked = false
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

        root.Children.Add(
            Text("НАСТРОЙКИ ГЕНЕРАТОРА")
        );

        root.Children.Add(
            Text("Целевое количество треугольников, %")
        );

        root.Children.Add(_collisionPercent);

        root.Children.Add(
            Text("Максимальная ошибка, мм")
        );

        root.Children.Add(_collisionError);

        root.Children.Add(_collisionBorders);
        root.Children.Add(_collisionGenerate);
        root.Children.Add(_collisionInfo);
        root.Children.Add(_collisionResult);

        _collisionCheck.IsCheckedChanged +=
            OnCollisionCheckChanged;

        _collisionGenerate.Click += async (_, _) =>
            await RegenerateCollisionAsync();
    }

    private void OnCollisionCheckChanged(
        object? sender,
        RoutedEventArgs e)
    {
        UpdateCollisionVisibility();
    }

    private void RefreshCollisionMode()
    {
        _collisionCheck.IsEnabled = CanInteract;

        UpdateCollisionVisibility();

        if (_collisionBuilding)
        {
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

        _collisionPercent.IsEnabled = enabled;
        _collisionError.IsEnabled = enabled;
        _collisionBorders.IsEnabled = enabled;
        _collisionGenerate.IsEnabled = enabled;

        if (!supported || _assetPaths == null)
        {
            _collisionSettingsPath = null;
            _collisionInfo.Text =
                "Выберите объект из models2.";
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

                _collisionSettingsPath = modelPath;
                _collisionResult.Text = "";
            }

            var scene = _level?.Objects;

            if (scene != null &&
                scene.Physics.TryGet(
                    selected.Model,
                    out ScenePhysicsMesh? physical) &&
                physical != null)
            {
                _collisionInfo.Text =
                    $"Модель: {Path.GetFileName(selected.Model)}\n" +
                    $"Коллизия: " +
                    $"{physical.Indices.Length / 3:N0} " +
                    "треугольников.";
            }
            else
            {
                _collisionInfo.Text =
                    $"Модель: {Path.GetFileName(selected.Model)}\n" +
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

            var settings = new CollisionGenerationSettings(
                percent / 100f,
                errorMillimeters / 1000f,
                _collisionBorders.IsChecked == true
            );

            string modelPath = AssetPathResolver.Resolve(
                assets.Root,
                model
            );

            _collisionBuilding = true;
            _collisionResult.Text =
                "Генерация коллизии выполняется…";

            BusyChanged?.Invoke();
            RefreshMode();

            await AssetDecoder.WaitAsync();
            entered = true;

            var generated = await Task.Run(() =>
            {
                CollisionGenerationSettingsStorage.Write(
                    modelPath,
                    settings
                );

                var result = CollisionGenerator.Generate(
                    assets,
                    model
                );

                var physical = CollisionStorage.Read(
                    assets,
                    model
                );

                return (
                    Result: result,
                    Physics: physical
                );
            });

            if (ReferenceEquals(_level?.Objects, scene))
            {
                scene.Physics.Store(
                    model,
                    generated.Physics
                );
            }

            int original =
                generated.Result.SourceTriangles;

            int simplified =
                generated.Result.CollisionTriangles;

            float reduction = original == 0
                ? 0f
                : 100f * (1f - (float)simplified / original);

            _collisionResult.Text =
                "Коллизия успешно обновлена.\n" +
                $"Исходная: {original:N0} треугольников.\n" +
                $"Новая: {simplified:N0} треугольников.\n" +
                $"Уменьшение: {reduction:F1}%.\n" +
                "Настройки сохранены рядом с моделью.";
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
