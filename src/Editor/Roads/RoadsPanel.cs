using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Editor.Viewport;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Roads;

namespace Editor.Roads;

public sealed class RoadsPanel : UserControl
{
    private sealed record CategoryOption(
        RoadCategory Category,
        string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record StructureKindOption(
        RoadStructureKind Kind,
        string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record PointOption(int Index)
    {
        public override string ToString() => $"Точка {Index + 1}";
    }

    private sealed record StructureModelOption(string File)
    {
        public override string ToString()
        {
            return Path.GetFileNameWithoutExtension(File);
        }
    }

    private static readonly CategoryOption[] Categories =
    [
        new(RoadCategory.Automobile, "Автомобильная"),
        new(RoadCategory.Railway, "Железнодорожная"),
        new(RoadCategory.Dirt, "Тропинка")
    ];

    private static readonly StructureKindOption[] StructureKinds =
    [
        new(RoadStructureKind.Bridge, "Мост"),
        new(RoadStructureKind.Tunnel, "Туннель")
    ];

    private readonly ListBox _routes = new()
    {
        Height = 120
    };

    private readonly ListBox _points = new()
    {
        Height = 120
    };

    private readonly ListBox _structures = new()
    {
        Height = 90
    };

    private readonly ComboBox _category = new();
    private readonly ComboBox _railModel = new();
    private readonly ComboBox _surfaceMaterial = new();
    private readonly ComboBox _shoulderMaterial = new();

    private readonly ComboBox _structureKind = new();
    private readonly ComboBox _structureModel = new();
    private readonly ComboBox _structureStart = new();
    private readonly ComboBox _structureEnd = new();

    private readonly Slider _width = new()
    {
        Minimum = 0.5,
        Maximum = 40,
        Value = 6
    };

    private readonly Slider _shoulderWidth = new()
    {
        Minimum = 0,
        Maximum = 8,
        Value = 0.75
    };

    private readonly TextBlock _widthInfo = new();
    private readonly TextBlock _shoulderInfo = new();

    private readonly Button _create = new()
    {
        Content = "Создать дорогу"
    };

    private readonly Button _removePoint = new()
    {
        Content = "Удалить точку"
    };

    private readonly Button _removeRoad = new()
    {
        Content = "Удалить дорогу"
    };

    private readonly Button _addStructure = new()
    {
        Content = "Назначить на участок"
    };

    private readonly Button _removeStructure = new()
    {
        Content = "Удалить мост / туннель"
    };

    private readonly TextBlock _status = new()
    {
        TextWrapping = TextWrapping.Wrap
    };

    private readonly StackPanel _railSection = new()
    {
        Spacing = 5
    };

    private readonly StackPanel _surfaceSection = new()
    {
        Spacing = 5
    };

    private readonly StackPanel _shoulderSection = new()
    {
        Spacing = 5
    };

    private DxViewportHost? _viewport;
    private LoadedLevel? _level;
    private RoadCatalog? _railCatalog;
    private RoadMaterialCatalog? _materials;

    private Guid? _selectedRoad;
    private int _selectedPoint = -1;
    private bool _updating;

    private Task _saveTask = Task.CompletedTask;

    public bool IsSaving { get; private set; }

    public bool HasPendingChanges =>
        IsSaving || _level?.Roads.IsDirty == true;

    public RoadsPanel()
    {
        _category.ItemsSource = Categories;
        _category.SelectedIndex = 0;

        _structureKind.ItemsSource = StructureKinds;
        _structureKind.SelectedIndex = 0;

        _railSection.Children.Add(Label("Модель железнодорожного полотна"));
        _railSection.Children.Add(_railModel);

        _surfaceSection.Children.Add(Label("Материал дороги"));
        _surfaceSection.Children.Add(_surfaceMaterial);
        _surfaceSection.Children.Add(Label("Ширина полотна"));
        _surfaceSection.Children.Add(_width);
        _surfaceSection.Children.Add(_widthInfo);

        _shoulderSection.Children.Add(Label("Материал обочины (гравий)"));
        _shoulderSection.Children.Add(_shoulderMaterial);
        _shoulderSection.Children.Add(Label("Ширина обочины с каждой стороны"));
        _shoulderSection.Children.Add(_shoulderWidth);
        _shoulderSection.Children.Add(_shoulderInfo);

        var root = new StackPanel
        {
            Spacing = 9
        };

        root.Children.Add(Label("ДОРОЖНАЯ СЕТЬ"));
        root.Children.Add(_create);
        root.Children.Add(Label("Дороги"));
        root.Children.Add(_routes);

        root.Children.Add(Label("Тип дороги"));
        root.Children.Add(_category);

        root.Children.Add(_railSection);
        root.Children.Add(_surfaceSection);
        root.Children.Add(_shoulderSection);

        root.Children.Add(new TextBlock
        {
            Text = "Shift + ЛКМ — добавить точку\n" +
                   "ЛКМ — выбрать или переместить точку",
            TextWrapping = TextWrapping.Wrap
        });

        root.Children.Add(Label("Контрольные точки"));
        root.Children.Add(_points);
        root.Children.Add(_removePoint);

        root.Children.Add(Label("Мосты и туннели"));
        root.Children.Add(_structureKind);
        root.Children.Add(_structureModel);
        root.Children.Add(Label("Начальная точка"));
        root.Children.Add(_structureStart);
        root.Children.Add(Label("Конечная точка"));
        root.Children.Add(_structureEnd);
        root.Children.Add(_addStructure);
        root.Children.Add(_structures);
        root.Children.Add(_removeStructure);

        root.Children.Add(_removeRoad);
        root.Children.Add(_status);

        Content = new ScrollViewer
        {
            Content = root
        };

        _create.Click += (_, _) => CreateRoad();
        _removeRoad.Click += (_, _) => RemoveRoad();
        _removePoint.Click += (_, _) => RemovePoint();

        _addStructure.Click += (_, _) => AddStructure();
        _removeStructure.Click += (_, _) => RemoveStructure();

        _routes.SelectionChanged += (_, _) =>
        {
            if (_updating)
            {
                return;
            }

            _selectedRoad = (_routes.SelectedItem as RoadPath)?.Id;
            _selectedPoint = -1;
            _viewport?.SelectRoad(_selectedRoad);
            Refresh();
        };

        _points.SelectionChanged += (_, _) =>
        {
            if (_updating)
            {
                return;
            }

            _selectedPoint = _points.SelectedIndex;
            _viewport?.SelectRoadPoint(_selectedPoint);
            RefreshStatus();
        };

        _category.SelectionChanged += (_, _) => ChangeCategory();
        _railModel.SelectionChanged += (_, _) => ChangeRailModel();

        _surfaceMaterial.SelectionChanged += (_, _) =>
            ChangeMaterials();

        _shoulderMaterial.SelectionChanged += (_, _) =>
            ChangeMaterials();

        _width.PropertyChanged += (_, args) =>
        {
            if (args.Property == Slider.ValueProperty)
            {
                ChangeWidth();
            }
        };

        _shoulderWidth.PropertyChanged += (_, args) =>
        {
            if (args.Property == Slider.ValueProperty)
            {
                ChangeWidth();
            }
        };

        _structureKind.SelectionChanged += (_, _) =>
        {
            if (!_updating)
            {
                RefreshStructureModels();
            }
        };

        RefreshSectionVisibility();
    }

    private static TextBlock Label(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontWeight = FontWeight.SemiBold
        };
    }

    public void Attach(DxViewportHost viewport)
    {
        _viewport = viewport;

        AssetPaths assets = AssetPaths.FromApplication();

        _railCatalog = RoadCatalog.Scan(assets);
        _materials = RoadMaterialCatalog.Scan(assets);

        viewport.RoadPointPicked += (id, index) =>
        {
            _selectedRoad = id;
            _selectedPoint = index;
            Refresh();
        };

        RefreshStructureModels();
        Refresh();
    }

    public void Bind(LoadedLevel level)
    {
        if (_level != null)
        {
            _level.Roads.Changed -= Refresh;
        }

        _level = level;
        _selectedRoad = null;
        _selectedPoint = -1;

        foreach (RoadPath road in level.Roads.Roads)
        {
            AssignDefaultMaterial(road);
        }

        level.Roads.Changed += Refresh;

        _viewport?.SelectRoad(null);
        Refresh();
    }

    public void SetActive(bool active)
    {
        IsVisible = active;

        if (_viewport == null)
        {
            return;
        }

        if (!active)
        {
            _viewport.EndRoadDrag();
        }

        _viewport.RoadEditingEnabled =
            active && _level != null;

        if (active)
        {
            _viewport.SelectRoad(_selectedRoad);
            _viewport.SelectRoadPoint(_selectedPoint);
        }
    }

    private RoadCategory SelectedCategory()
    {
        return (_category.SelectedItem as CategoryOption)?.Category
            ?? RoadCategory.Automobile;
    }

    private RoadStructureKind SelectedStructureKind()
    {
        return (_structureKind.SelectedItem as StructureKindOption)?.Kind
            ?? RoadStructureKind.Bridge;
    }

    private RoadPath? SelectedRoad()
    {
        return _selectedRoad is { } id
            ? _level?.Roads.Find(id)
            : null;
    }

    private void AssignDefaultMaterial(RoadPath road)
    {
        if (_level == null ||
            _materials == null ||
            road.Category == RoadCategory.Railway)
        {
            return;
        }

        RoadMaterialInfo[] options =
            _materials.GetMaterials(road.Category);

        if (options.Length == 0)
        {
            return;
        }

        string surface = options.Any(item =>
                item.File.Equals(
                    road.Material,
                    StringComparison.OrdinalIgnoreCase))
            ? road.Material
            : options[0].File;

        RoadMaterialInfo[] shoulders =
            _materials.GetShoulderMaterials();

        string shoulder = road.Category == RoadCategory.Automobile &&
                          shoulders.Length > 0
            ? shoulders.FirstOrDefault(item =>
                item.File.Equals(
                    road.ShoulderMaterial,
                    StringComparison.OrdinalIgnoreCase))?.File
              ?? shoulders.FirstOrDefault(item =>
                  item.File.EndsWith(
                      "/Dirt_Road_04_D.dds",
                      StringComparison.OrdinalIgnoreCase))?.File
              ?? shoulders[0].File
            : "";

        _level.Roads.ConfigureMaterials(
            road.Id,
            surface,
            shoulder);
    }

    private void CreateRoad()
    {
        if (_level == null)
        {
            return;
        }

        RoadCategory category = SelectedCategory();

        RoadPath road = _level.Roads.CreateRoad();

        _selectedRoad = road.Id;
        _selectedPoint = -1;

        string railModel = category == RoadCategory.Railway
            ? (_railModel.SelectedItem as RoadModelInfo)?.File ?? ""
            : "";

        _level.Roads.ConfigureRoad(
            road.Id,
            category,
            railModel);

        _level.Roads.ConfigureWidth(
            road.Id,
            category == RoadCategory.Dirt ? 1.8f : 6f,
            category == RoadCategory.Automobile ? 0.75f : 0f);

        AssignDefaultMaterial(road);

        _viewport?.SelectRoad(road.Id);
        Refresh();
    }

    private void ChangeCategory()
    {
        if (_updating ||
            _level == null ||
            _selectedRoad is not { } id)
        {
            RefreshSectionVisibility();
            return;
        }

        RoadCategory category = SelectedCategory();

        string model = category == RoadCategory.Railway
            ? _railCatalog?.GetModels(RoadCategory.Railway)
                .FirstOrDefault()?.File ?? ""
            : "";

        _level.Roads.ConfigureRoad(id, category, model);

        RoadPath? road = _level.Roads.Find(id);

        if (road != null)
        {
            AssignDefaultMaterial(road);
        }

        Refresh();
    }

    private void ChangeRailModel()
    {
        if (_updating ||
            _level == null ||
            _selectedRoad is not { } id ||
            SelectedCategory() != RoadCategory.Railway)
        {
            return;
        }

        string model = (_railModel.SelectedItem as RoadModelInfo)?.File ?? "";

        _level.Roads.ConfigureRoad(
            id,
            RoadCategory.Railway,
            model);
    }

    private void ChangeMaterials()
    {
        if (_updating ||
            _level == null ||
            _selectedRoad is not { } id ||
            SelectedCategory() == RoadCategory.Railway)
        {
            return;
        }

        string material =
            (_surfaceMaterial.SelectedItem as RoadMaterialInfo)?.File ?? "";

        string shoulder = SelectedCategory() == RoadCategory.Automobile
            ? (_shoulderMaterial.SelectedItem as RoadMaterialInfo)?.File ?? ""
            : "";

        _level.Roads.ConfigureMaterials(
            id,
            material,
            shoulder);
    }

    private void ChangeWidth()
    {
        _widthInfo.Text = $"{_width.Value:F1} м";
        _shoulderInfo.Text = $"{_shoulderWidth.Value:F1} м";

        if (_updating ||
            _level == null ||
            _selectedRoad is not { } id ||
            SelectedCategory() == RoadCategory.Railway)
        {
            return;
        }

        _level.Roads.ConfigureWidth(
            id,
            MathF.Round((float)_width.Value, 2),
            MathF.Round((float)_shoulderWidth.Value, 2));
    }

    private void AddStructure()
    {
        if (_level == null || _selectedRoad is not { } id)
        {
            return;
        }

        if (_structureStart.SelectedItem is not PointOption start ||
            _structureEnd.SelectedItem is not PointOption end ||
            _structureModel.SelectedItem is not StructureModelOption model)
        {
            _status.Text = "Выберите две точки и модель.";
            return;
        }

        if (end.Index != start.Index + 1)
        {
            _status.Text =
                "Мост или туннель размещается между соседними точками.";
            return;
        }

        _level.Roads.AddStructure(
            id,
            SelectedStructureKind(),
            start.Index,
            end.Index,
            model.File);

        Refresh();
    }

    private void RemoveStructure()
    {
        if (_level == null ||
            _selectedRoad is not { } roadId ||
            _structures.SelectedItem is not RoadStructure structure)
        {
            return;
        }

        _level.Roads.RemoveStructure(
            roadId,
            structure.Id);

        Refresh();
    }

    private void RefreshStructureModels()
    {
        bool previous = _updating;
        _updating = true;

        try
        {
            string folder = SelectedStructureKind() == RoadStructureKind.Bridge
                ? "objects/props/bridges"
                : "objects/props/tunnels";

            AssetPaths assets = AssetPaths.FromApplication();

            string directory = Path.Combine(
                assets.Root,
                folder.Replace('/', Path.DirectorySeparatorChar));

            StructureModelOption[] models = Directory.Exists(directory)
                ? Directory.EnumerateFiles(
                        directory,
                        "*",
                        SearchOption.AllDirectories)
                    .Where(file => Path.GetExtension(file).Equals(
                        ".model",
                        StringComparison.OrdinalIgnoreCase))
                    .Select(file => new StructureModelOption(
                        Path.GetRelativePath(assets.Root, file)
                            .Replace('\\', '/')))
                    .OrderBy(item => item.File)
                    .ToArray()
                : [];

            _structureModel.ItemsSource = models;
            _structureModel.SelectedIndex = models.Length > 0 ? 0 : -1;
        }
        finally
        {
            _updating = previous;
        }
    }

    private void RemovePoint()
    {
        if (_level == null ||
            _selectedRoad is not { } id ||
            _selectedPoint < 0)
        {
            return;
        }

        _level.Roads.RemovePoint(id, _selectedPoint);
        _selectedPoint = -1;

        _viewport?.SelectRoadPoint(-1);
        Refresh();
    }

    private void RemoveRoad()
    {
        if (_level == null || _selectedRoad is not { } id)
        {
            return;
        }

        _level.Roads.RemoveRoad(id);

        _selectedRoad = null;
        _selectedPoint = -1;

        _viewport?.SelectRoad(null);
        Refresh();
    }

    private void Refresh()
    {
        _updating = true;

        try
        {
            RoadPath[] roads = _level?.Roads.Roads.ToArray() ?? [];

            _routes.ItemsSource = roads;

            _routes.SelectedItem = roads.FirstOrDefault(
                road => road.Id == _selectedRoad);

            RoadPath? road = SelectedRoad();

            _points.ItemsSource = road?.Points
                .Select((point, index) =>
                    $"Точка {index + 1}: X {point.X:F2}, Z {point.Z:F2}")
                .ToArray() ?? [];

            _points.SelectedIndex =
                road != null &&
                _selectedPoint >= 0 &&
                _selectedPoint < road.Points.Count
                    ? _selectedPoint
                    : -1;

            _structures.ItemsSource = road?.Structures.ToArray() ?? [];

            if (road != null)
            {
                _category.SelectedItem = Categories.First(
                    item => item.Category == road.Category);
            }

            RoadCategory category = road?.Category ?? SelectedCategory();

            RoadModelInfo[] railModels =
                _railCatalog?.GetModels(RoadCategory.Railway) ?? [];

            _railModel.ItemsSource = railModels;

            _railModel.SelectedItem = railModels.FirstOrDefault(
                item => item.File.Equals(
                    road?.Model,
                    StringComparison.OrdinalIgnoreCase));

            RoadMaterialInfo[] materials =
                _materials?.GetMaterials(category) ?? [];

            _surfaceMaterial.ItemsSource = materials;

            _surfaceMaterial.SelectedItem = materials.FirstOrDefault(
                item => item.File.Equals(
                    road?.Material,
                    StringComparison.OrdinalIgnoreCase));

            RoadMaterialInfo[] shoulders =
                _materials?.GetShoulderMaterials() ?? [];

            _shoulderMaterial.ItemsSource = shoulders;

            _shoulderMaterial.SelectedItem = shoulders.FirstOrDefault(
                item => item.File.Equals(
                    road?.ShoulderMaterial,
                    StringComparison.OrdinalIgnoreCase));

            _width.Value = road?.Width ?? 6f;
            _shoulderWidth.Value = road?.ShoulderWidth ?? 0.75f;

            PointOption[] options = road?.Points
                .Select((_, index) => new PointOption(index))
                .ToArray() ?? [];

            int selectedStart =
                (_structureStart.SelectedItem as PointOption)?.Index ?? 0;

            int selectedEnd =
                (_structureEnd.SelectedItem as PointOption)?.Index ?? 1;

            _structureStart.ItemsSource = options;
            _structureEnd.ItemsSource = options;

            _structureStart.SelectedItem = options.FirstOrDefault(
                item => item.Index == selectedStart);

            _structureEnd.SelectedItem = options.FirstOrDefault(
                item => item.Index == selectedEnd);

            RefreshSectionVisibility();
        }
        finally
        {
            _updating = false;
        }

        ChangeWidth();
        RefreshStatus();
    }

    private void RefreshSectionVisibility()
    {
        RoadCategory category = SelectedCategory();

        _railSection.IsVisible =
            category == RoadCategory.Railway;

        _surfaceSection.IsVisible =
            category != RoadCategory.Railway;

        _shoulderSection.IsVisible =
            category == RoadCategory.Automobile;
    }

    private void RefreshStatus()
    {
        if (_level == null)
        {
            _status.Text = "Откройте уровень.";
            return;
        }

        int roads = _level.Roads.Roads.Count;
        int points = _level.Roads.Roads.Sum(
            road => road.Points.Count);

        _status.Text =
            $"Дорог: {roads}\n" +
            $"Точек: {points}\n" +
            $"Выбрана точка: " +
            (_selectedPoint < 0
                ? "нет"
                : (_selectedPoint + 1).ToString());
    }

    public Task SavePendingAsync()
    {
        if (!_saveTask.IsCompleted)
        {
            return _saveTask;
        }

        if (_level == null || !_level.Roads.IsDirty)
        {
            return Task.CompletedTask;
        }

        _saveTask = SaveAsync(_level);
        return _saveTask;
    }

    private async Task SaveAsync(LoadedLevel level)
    {
        IsSaving = true;

        try
        {
            await RoadStorage.SaveAsync(
                level.Directory,
                level.Manifest.Name,
                level.Roads);
        }
        finally
        {
            IsSaving = false;
        }
    }
}
