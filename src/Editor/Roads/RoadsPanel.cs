using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Editor.Viewport;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Roads;

namespace Editor.Roads;

public sealed class RoadsPanel : UserControl
{
    private readonly ListBox _routes = new()
    {
        Height = 150
    };

    private readonly ListBox _points = new()
    {
        Height = 180
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
    private Guid? _selectedRoad;
    private int _selectedPoint = -1;

    private bool _updating;
    private Task _saveTask = Task.CompletedTask;

    public bool IsSaving { get; private set; }

    public bool HasPendingChanges =>
        IsSaving || _level?.Roads.IsDirty == true;

    public RoadsPanel()
    {
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
    }

    public void Attach(DxViewportHost viewport)
    {
        _viewport = viewport;

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

        _viewport.RoadEditingEnabled = active && _level != null;

        if (active)
        {
            _viewport.SelectRoad(_selectedRoad);
            _viewport.SelectRoadPoint(_selectedPoint);
        }
    }

    private void CreateRoad()
    {
        if (_level == null)
        {
            return;
        }

        var road = _level.Roads.CreateRoad();

        _selectedRoad = road.Id;
        _selectedPoint = -1;

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

            var road = _selectedRoad is { } id
                ? _level?.Roads.Find(id)
                : null;

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

        _status.Text =
            $"Дорог: {roads}\n" +
            $"Точек: {points}\n" +
            $"Выбрана точка: {(_selectedPoint < 0 ? "нет" : (_selectedPoint + 1).ToString())}";
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
