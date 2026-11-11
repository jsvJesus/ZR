using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Editor.Objects;

public enum ObjectsSection
{
    Import,
    ObjectsInScene,
    PlaceObjects,
    ObjectsCollision
}

public sealed partial class ObjectsPanel
{
    private ObjectsSection _section = ObjectsSection.ObjectsInScene;

    private readonly StackPanel _importContent = new()
    {
        Spacing = 8
    };

    private readonly StackPanel _sceneContent = new()
    {
        Spacing = 8
    };

    private readonly StackPanel _placeContent = new()
    {
        Spacing = 8
    };

    private readonly StackPanel _collisionContent = new()
    {
        Spacing = 8
    };

    private readonly Button _importTab = CreateSectionButton("Import");
    private readonly Button _sceneTab = CreateSectionButton("Objects in Scene");
    private readonly Button _placeTab = CreateSectionButton("Place Objects");
    private readonly Button _collisionTab = CreateSectionButton("Objects Collision");

    public event Action<ObjectsSection>? SectionChanged;

    private void BuildSections(StackPanel root)
    {
        var navigation = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto")
        };

        AddTab(navigation, _importTab, 0, 0, true, true);
        AddTab(navigation, _sceneTab, 0, 1, false, true);
        AddTab(navigation, _placeTab, 1, 0, true, false);
        AddTab(navigation, _collisionTab, 1, 1, false, false);

        _importTab.Click += (_, _) =>
            ShowSection(ObjectsSection.Import);

        _sceneTab.Click += (_, _) =>
            ShowSection(ObjectsSection.ObjectsInScene);

        _placeTab.Click += (_, _) =>
            ShowSection(ObjectsSection.PlaceObjects);

        _collisionTab.Click += (_, _) =>
            ShowSection(ObjectsSection.ObjectsCollision);

        root.Children.Add(navigation);

        _importContent.Children.Add(Text("ИМПОРТ ОБЪЕКТОВ"));
        _importContent.Children.Add(Text(
            "Исходные FBX находятся в bin/packs/import/models/. " +
            "После импорта модели появятся в models2."
        ));
        BuildModelImport(_importContent);

        BuildSceneSection(_sceneContent);

        _placeContent.Children.Add(Text("РАЗМЕЩЕНИЕ ОБЪЕКТОВ"));
        BuildLibrary(_placeContent);

        BuildCollisionControls(_collisionContent);

        root.Children.Add(_importContent);
        root.Children.Add(_sceneContent);
        root.Children.Add(_placeContent);
        root.Children.Add(_collisionContent);

        UpdateSectionVisibility();
    }

    public void ShowSection(ObjectsSection section)
    {
        bool leavingPlacement =
            _section == ObjectsSection.PlaceObjects &&
            section != ObjectsSection.PlaceObjects;

        _section = section;

        if (leavingPlacement)
        {
            CancelLibraryLoad();
        }

        UpdateSectionVisibility();
        RefreshMode();

        if (_section == ObjectsSection.PlaceObjects)
        {
            ResumeLibrary();

            if (_readyAsset != null &&
                ReferenceEquals(_readyScene, _level?.Objects))
            {
                _viewport?.SetObjectPlacement(_readyAsset.File);
            }
        }

        SectionChanged?.Invoke(section);
    }

    private void UpdateSectionVisibility()
    {
        _importContent.IsVisible =
            _section == ObjectsSection.Import;

        _sceneContent.IsVisible =
            _section == ObjectsSection.ObjectsInScene;

        _placeContent.IsVisible =
            _section == ObjectsSection.PlaceObjects;

        _collisionContent.IsVisible =
            _section == ObjectsSection.ObjectsCollision;

        UpdateTabAppearance(
            _importTab,
            _section == ObjectsSection.Import
        );

        UpdateTabAppearance(
            _sceneTab,
            _section == ObjectsSection.ObjectsInScene
        );

        UpdateTabAppearance(
            _placeTab,
            _section == ObjectsSection.PlaceObjects
        );

        UpdateTabAppearance(
            _collisionTab,
            _section == ObjectsSection.ObjectsCollision
        );
    }

    private static Button CreateSectionButton(string title)
    {
        return new Button
        {
            Content = new TextBlock
            {
                Text = title,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11
            },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            MinHeight = 38,
            Padding = new Thickness(5, 7)
        };
    }

    private static void AddTab(
        Grid navigation,
        Button button,
        int row,
        int column,
        bool left,
        bool top)
    {
        Grid.SetRow(button, row);
        Grid.SetColumn(button, column);

        button.Margin = new Thickness(
            0,
            0,
            left ? 5 : 0,
            top ? 5 : 0
        );

        navigation.Children.Add(button);
    }

    private static void UpdateTabAppearance(
        Button button,
        bool selected)
    {
        button.Background = new SolidColorBrush(
            selected
                ? Color.FromRgb(52, 80, 108)
                : Color.FromRgb(43, 49, 59)
        );

        button.BorderBrush = new SolidColorBrush(
            selected
                ? Color.FromRgb(108, 165, 210)
                : Color.FromRgb(61, 70, 81)
        );

        button.BorderThickness = new Thickness(1);

        if (button.Content is TextBlock text)
        {
            text.Foreground = new SolidColorBrush(
                selected
                    ? Color.FromRgb(235, 245, 255)
                    : Color.FromRgb(189, 199, 211)
            );

            text.FontWeight = selected
                ? FontWeight.Bold
                : FontWeight.Normal;
        }
    }
}
