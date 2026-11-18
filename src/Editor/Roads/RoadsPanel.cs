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

    private static readonly CategoryOption[] Categories =
    [
        new(RoadCategory.Automobile, "Автомобильная"),
        new(RoadCategory.Railway, "Железнодорожная"),
        new(RoadCategory.Dirt, "Грунтовая")
    ];

    private readonly ListBox _routes = new()
    {
        Height = 150
    };

    private readonly ListBox _points = new()
    {
        Height = 180
    };

    private readonly ComboBox _category = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private readonly ComboBox _model = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private readonly Button _create = new()
    {
        Content = "Создать дорогу"
    };

    private readonly Button _addPoint = new()
    {
        Content = "Добавить точки"
    };

    private readonly Button _movePoint = new()
    {
        Content = "Выбрать / переместить"
    };

    private readonly Button _removePoint = new()
    {
        Content = "Удалить точку"
    };

    private readonly Button _removeRoad = new()
    {
        Content = "Удалить дорогу"
    };

    private readonly TextBlock _modelInfo = new()
    {
        TextWrapping = TextWrapping.Wrap
    };

    private readonly TextBlock _status = new()
    {
        Text = "Откройте уровень.",
        TextWrapping = TextWrapping.Wrap
    };

    private readonly TextBlock _mode = new()
    {
        Text = "Режим: выбор и перемещение"
    };

    private DxViewportHost? _viewport;
    private LoadedLevel? _level;
    private RoadCatalog? _catalog;

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

        var root = new StackPanel
        {
            Spacing = 9
        };

        root.Children.Add(new TextBlock
        {
            Text = "ДОРОЖНАЯ СЕТЬ",
            FontWeight = FontWeight.SemiBold
        });

        root.Children.Add(_create);
        root.Children.Add(new TextBlock { Text = "Дороги" });
        root.Children.Add(_routes);

        root.Children.Add(new TextBlock
        {
            Text = "Тип дороги",
            FontWeight = FontWeight.SemiBold
        });

        root.Children.Add(_category);

        root.Children.Add(new TextBlock
        {
            Text = "Модель дорожного полотна",
            FontWeight = FontWeight.SemiBold
        });

        root.Children.Add(_model);
        root.Children.Add(_modelInfo);

        var tools = new StackPanel
        {
            Spacing = 6
        };

        tools.Children.Add(_addPoint);
        tools.Children.Add(_movePoint);

        root.Children.Add(tools);
        root.Children.Add(_mode);

        root.Children.Add(new TextBlock
        {
            Text = "Контрольные точки"
        });

        root.Children.Add(_points);
        root.Children.Add(_removePoint);
        root.Children.Add(_removeRoad);
        root.Children.Add(_status);

        Content = root;

        _create.Click += (_, _) => CreateRoad();

        _addPoint.Click += (_, _) =>
        {
            if (_viewport == null)
            {
                return;
            }

            _viewport.RoadTool = RoadToolMode.Add;
            _mode.Text = "Режим: добавление точек";
        };

        _movePoint.Click += (_, _) =>
        {
            if (_viewport == null)
            {
                return;
            }

            _viewport.RoadTool = RoadToolMode.Select;
            _mode.Text = "Режим: выбор и перемещение";
        };

        _removePoint.Click += (_, _) => RemovePoint();
        _removeRoad.Click += (_, _) => RemoveRoad();

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

        _category.SelectionChanged += (_, _) =>
        {
            if (_updating)
            {
                return;
            }

            RoadCategory category = SelectedCategory();
            RefreshModelList(category, null, true);

            if (_selectedRoad is { } roadId && _level != null)
            {
                string model = (_model.SelectedItem as RoadModelInfo)?.File
                    ?? "";

                _level.Roads.ConfigureRoad(
                    roadId,
                    category,
                    model);
            }
        };

        _model.SelectionChanged += (_, _) =>
        {
            if (_updating)
            {
                return;
            }

            UpdateModelInfo();

            if (_selectedRoad is not { } roadId ||
                _level == null ||
                _model.SelectedItem is not RoadModelInfo model)
            {
                return;
            }

            _level.Roads.ConfigureRoad(
                roadId,
                SelectedCategory(),
                model.File);
        };
    }

    public void Attach(DxViewportHost viewport)
    {
        _viewport = viewport;
        _catalog = RoadCatalog.Scan(AssetPaths.FromApplication());

        RefreshModelList(SelectedCategory(), null, true);

        viewport.RoadPointPicked += (roadId, index) =>
        {
            _selectedRoad = roadId;
            _selectedPoint = index;
            Refresh();
        };
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

    private RoadPath? SelectedRoad()
    {
        return _selectedRoad is { } id
            ? _level?.Roads.Find(id)
            : null;
    }

    private void RefreshModelList(
        RoadCategory category,
        string? selectedFile,
        bool selectFirst)
    {
        bool wasUpdating = _updating;
        _updating = true;

        try
        {
            var models = _catalog?.GetModels(category)
                ?? [];

            _model.ItemsSource = models;

            _model.SelectedItem = models.FirstOrDefault(
                model => string.Equals(
                    model.File,
                    selectedFile,
                    StringComparison.OrdinalIgnoreCase));

            if (_model.SelectedItem == null && selectFirst)
            {
                _model.SelectedIndex = models.Length > 0 ? 0 : -1;
            }
        }
        finally
        {
            _updating = wasUpdating;
        }

        UpdateModelInfo();
    }

    private void UpdateModelInfo()
    {
        if (_model.SelectedItem is not RoadModelInfo model)
        {
            _modelInfo.Text = "Модель не выбрана.";
            return;
        }

        float width = model.Axis == RoadModelAxis.X
            ? model.Bounds.Maximum.Z - model.Bounds.Minimum.Z
            : model.Bounds.Maximum.X - model.Bounds.Minimum.X;

        _modelInfo.Text =
            $"Длина сегмента: {model.Length:F2} м\n" +
            $"Ширина: {width:F2} м\n" +
            model.File;
    }

    private void CreateRoad()
    {
        if (_level == null)
        {
            return;
        }

        RoadCategory category = SelectedCategory();
        string model = (_model.SelectedItem as RoadModelInfo)?.File
            ?? "";

        var road = _level.Roads.CreateRoad();

        _selectedRoad = road.Id;
        _selectedPoint = -1;

        _level.Roads.ConfigureRoad(
            road.Id,
            category,
            model);

        _viewport?.SelectRoad(road.Id);

        if (_viewport != null)
        {
            _viewport.RoadTool = RoadToolMode.Add;
        }

        _mode.Text = "Режим: добавление точек";
        Refresh();
    }

    private void RemovePoint()
    {
        if (_level == null ||
            _selectedRoad == null ||
            _selectedPoint < 0)
        {
            return;
        }

        _level.Roads.RemovePoint(
            _selectedRoad.Value,
            _selectedPoint);

        _selectedPoint = -1;
        _viewport?.SelectRoadPoint(-1);
        Refresh();
    }

    private void RemoveRoad()
    {
        if (_level == null || _selectedRoad == null)
        {
            return;
        }

        _level.Roads.RemoveRoad(_selectedRoad.Value);

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
            var roads = _level?.Roads.Roads.ToArray()
                ?? [];

            _routes.ItemsSource = roads;

            _routes.SelectedItem = roads.FirstOrDefault(
                road => road.Id == _selectedRoad);

            var road = SelectedRoad();

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

            if (road != null)
            {
                _category.SelectedItem = Categories.First(
                    option => option.Category == road.Category);

                RefreshModelList(
                    road.Category,
                    road.Model,
                    false);
            }
        }
        finally
        {
            _updating = false;
        }

        RefreshStatus();
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

        string warning = "";

        var selectedRoad = SelectedRoad();

        if (selectedRoad != null &&
            string.IsNullOrEmpty(selectedRoad.Model))
        {
            warning = "\nДля выбранной дороги назначьте модель.";
        }
        else if (selectedRoad != null &&
                 _catalog?.TryGet(selectedRoad.Model, out _) == false)
        {
            warning = "\nМодель выбранной дороги не найдена.";
        }

        if (_catalog is { Models.Count: 0 })
        {
            warning += "\nНе найдены дорожные модели в packs/objects.";
        }

        _status.Text =
            $"Дорог: {roads}\n" +
            $"Точек: {points}\n" +
            $"Выбрана точка: " +
            (_selectedPoint < 0
                ? "нет"
                : (_selectedPoint + 1).ToString()) +
            warning;
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
