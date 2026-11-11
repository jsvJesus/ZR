using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Editor.Objects;

public sealed partial class ObjectsPanel
{
    private readonly CheckBox _collisionCheck = new()
    {
        Content = "Показать коллизию выбранного объекта",
        IsChecked = false
    };

    private void BuildCollisionControls(StackPanel root)
    {
        root.Children.Add(
            Text("ОТОБРАЖЕНИЕ КОЛЛИЗИИ")
        );

        root.Children.Add(_collisionCheck);

        root.Children.Add(
            Text(
                "Выберите объект на уровне. " +
                "Зелёная сетка показывает физическую геометрию, " +
                "включая внутренние поверхности."
            )
        );

        _collisionCheck.IsCheckedChanged += OnCollisionCheckChanged;
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
}
