using System.Numerics;
using Avalonia.Controls;
using Avalonia.Media;
using Editor.Viewport;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Terrain;

namespace Editor.Terrain;

public sealed partial class TerrainEditorPanel : UserControl
{
    private readonly ComboBox _mode = new()
    {
        ItemsSource = new[]
        {
            "Поднять поверхность",
            "Опустить поверхность",
            "Сгладить поверхность",
            "Вырезать отверстие",
            "Заполнить отверстие"
        },
        SelectedIndex = 0
    };

    private readonly ComboBox _materialMode = new()
    {
        ItemsSource = new[]
        {
            "Настройка материалов",
            "Рисовать текстурой"
        },
        SelectedIndex = 1
    };

    private readonly StackPanel _sculptControls = new()
    {
        Spacing = 8
    };

    private readonly StackPanel _brushSettings = new()
    {
        Spacing = 8
    };

    private readonly Slider _radius = new()
    {
        Minimum = 0.5,
        Maximum = 100,
        Value = 5,
        TickFrequency = 0.5
    };

    private readonly Slider _strength = new()
    {
        Minimum = 0.01,
        Maximum = 5,
        Value = 0.5,
        TickFrequency = 0.05
    };

    private readonly TextBlock _radiusValue = new()
    {
        Text = "5.0 м"
    };

    private readonly TextBlock _strengthValue = new()
    {
        Text = "0.50"
    };

    private readonly TextBlock _brushDescription = new()
    {
        TextWrapping = TextWrapping.Wrap,
        FontSize = 12
    };

    private readonly TextBlock _status = new()
    {
        TextWrapping = TextWrapping.Wrap,
        FontSize = 12
    };

    private readonly Button _undo = new()
    {
        Content = "Отменить правку Terrain"
    };

    private readonly Button _save = new()
    {
        Content = "Сохранить Terrain"
    };
    private readonly Button _updateTerrain = new() { Content = "Обновить Terrain из FBX" };

    private LoadedLevel? _level;
    private DxViewportHost? _viewport;
    private Task _saveTask = Task.CompletedTask;
    private bool _materialsSection;

    public bool IsSaving { get; private set; }

    public bool IsBusy =>
        IsSaving || _materialCancellation != null;

    public bool HasPendingChanges =>
        _level?.TerrainDocument?.IsDirty == true || IsBusy;

    public event Action? BusyChanged;
    public event Action? UpdateTerrainRequested;

    public TerrainEditorPanel()
    {
        var root = new StackPanel
        {
            Spacing = 10
        };

        _sculptControls.Children.Add(new TextBlock
        {
            Text = "ИНСТРУМЕНТ РЕЛЬЕФА",
            FontWeight = FontWeight.Bold
        });

        _sculptControls.Children.Add(_mode);
        _sculptControls.Children.Add(_updateTerrain);
        root.Children.Add(_sculptControls);

        root.Children.Add(_materialMode);
        BuildMaterialControls(root);

        var radiusHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        radiusHeader.Children.Add(new TextBlock
        {
            Text = "Радиус кисти",
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });

        Grid.SetColumn(_radiusValue, 1);
        radiusHeader.Children.Add(_radiusValue);

        var strengthHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        strengthHeader.Children.Add(new TextBlock
        {
            Text = "Сила кисти",
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });

        Grid.SetColumn(_strengthValue, 1);
        strengthHeader.Children.Add(_strengthValue);

        _brushSettings.Children.Add(radiusHeader);
        _brushSettings.Children.Add(_radius);
        _brushSettings.Children.Add(strengthHeader);
        _brushSettings.Children.Add(_strength);
        _brushSettings.Children.Add(_brushDescription);

        root.Children.Add(_brushSettings);
        root.Children.Add(_undo);
        root.Children.Add(_save);
        root.Children.Add(_status);

        Content = root;

        _mode.SelectionChanged += (_, _) =>
        {
            _viewport?.EndTerrainPaint();
            RefreshMode();
        };

        _materialMode.SelectionChanged += (_, _) =>
        {
            _viewport?.EndTerrainPaint();
            RefreshMode();
        };

        _radius.ValueChanged += (_, _) =>
        {
            UpdateBrushSettings();
        };

        _strength.ValueChanged += (_, _) =>
        {
            UpdateBrushSettings();
        };

        _undo.Click += (_, _) =>
        {
            if (!Active)
            {
                return;
            }

            _viewport?.EndTerrainPaint();
            _level?.TerrainDocument?.Undo();
            RefreshMode();
        };

        _save.Click += async (_, _) =>
        {
            try
            {
                await SavePendingAsync();
            }
            catch (Exception error)
            {
                _status.Text = error.Message;
            }
        };
        _updateTerrain.Click += (_, _) =>
        {
            if (!Active || _level?.Manifest.Terrain.ImportedFromMesh != true) return;
            _viewport?.EndTerrainPaint(); UpdateTerrainRequested?.Invoke();
        };

        RefreshMode();
    }

    private bool Active =>
        IsVisible &&
        IsEnabled &&
        !IsBusy &&
        _level?.TerrainDocument != null &&
        _viewport?.IsPlaying != true &&
        _viewport?.IsStarting != true;

    private bool IsTexturePainting =>
        _materialsSection && _materialMode.SelectedIndex == 1;

    public void Attach(
        DxViewportHost viewport,
        AssetPaths assets)
    {
        _assets = assets;
        _viewport = viewport;

        viewport.TerrainBrushRequested += Apply;
        viewport.TerrainStrokeStarted += OnStrokeStarted;
        viewport.TerrainStrokeEnded += OnStrokeEnded;
    }

    public void Bind(LoadedLevel level)
    {
        _viewport?.EndTerrainPaint();

        _level = level;

        _selectedVariantIndex = 0;
        _selectedVariantLayer = -1;

        RefreshPalette();
        RefreshWaterControls();
        RefreshMode();
    }

    private void OnStrokeStarted()
    {
        if (!Active || _level?.TerrainDocument is not { } document)
        {
            return;
        }

        if (IsTexturePainting)
        {
            document.BeginPaintStroke();
        }
        else if (!_materialsSection)
        {
            document.BeginSculptStroke();
        }
    }

    private void OnStrokeEnded()
    {
        if (_level?.TerrainDocument is { } document)
        {
            document.EndSculptStroke();
            document.EndPaintStroke();
        }

        RefreshMode();
    }

    public void RefreshMode()
    {
        bool active = Active;
        bool painting = !_materialsSection || IsTexturePainting;

        bool brushEnabled = active && painting;

        RefreshMaterialHierarchy();

        _sculptControls.IsVisible = !_materialsSection;
        _materialMode.IsVisible = _materialsSection;
        _materialControls.IsVisible = _materialsSection;
        _materialControls.IsEnabled = active;

        _brushSettings.IsVisible = painting;
        _mode.IsEnabled = active;
        _materialMode.IsEnabled = active;

        UpdateBrushSettings();

        if (_viewport != null)
        {
            if (!brushEnabled)
            {
                _viewport.EndTerrainPaint();
            }

            _viewport.TerrainBrushEnabled = brushEnabled;
            _viewport.TerrainBrushContinuous = brushEnabled;
            _viewport.TerrainBrushFill =
                !_materialsSection && _mode.SelectedIndex == 4;

            UpdateBrushSpacing();
        }

        bool holes =
            !_materialsSection && _mode.SelectedIndex is 3 or 4;

        _radius.IsEnabled = active;
        _strength.IsEnabled = active && !holes;

        _undo.IsEnabled =
            active && _level?.TerrainDocument?.CanUndo == true;

        _save.IsEnabled =
            active && _level?.TerrainDocument?.IsDirty == true;
        _updateTerrain.IsEnabled = active && _level?.Manifest.Terrain.ImportedFromMesh == true;

        if (_level == null)
        {
            _status.Text = "Откройте или импортируйте уровень.";
            return;
        }

        _status.Text =
            $"Шаг сетки: {_level.Manifest.Terrain.CellSize:G4} м. " +
            (HasPendingChanges
                ? "Есть несохранённые изменения."
                : "Terrain сохранён.");
    }

    private void UpdateBrushSettings()
    {
        bool smooth =
            !_materialsSection &&
            _mode.SelectedIndex == 2;

        bool normalized = IsTexturePainting || smooth;

        double maximum = IsTexturePainting ? 1 : 5;

        _strength.Maximum = maximum;

        if (_strength.Value > maximum)
        {
            _strength.Value = maximum;
        }

        _radiusValue.Text = $"{_radius.Value:0.0} м";

        _strengthValue.Text = normalized
            ? $"{_strength.Value:0.00}"
            : $"{_strength.Value:0.00} м";

        _brushDescription.Text = IsTexturePainting
            ? "ЛКМ + движение — рисование выбранным материалом. " +
              "ПКМ — управление камерой."
            : "ЛКМ + движение — непрерывное редактирование Terrain. " +
              "ПКМ — управление камерой.";

        UpdateBrushSpacing();
    }

    private void UpdateBrushSpacing()
    {
        if (_viewport == null)
        {
            return;
        }

        float cellSize =
            _level?.TerrainDocument?.Descriptor.CellSize ?? 1f;

        float radius = (float)_radius.Value;

        _viewport.TerrainBrushRadius = radius;
        _viewport.TerrainBrushIsMaterial = IsTexturePainting;

        _viewport.TerrainBrushSpacing = Math.Max(
            cellSize * 0.25f,
            radius * 0.2f
        );
    }

    private void Apply(Vector3 point)
    {
        if (!Active || _level?.TerrainDocument is not { } document)
        {
            return;
        }

        try
        {
            float radius = (float)_radius.Value;
            float strength = (float)_strength.Value;

            if (_materialsSection)
            {
                if (!IsTexturePainting ||
                    _palette.SelectedItem is not
                        ZoneReborn.World.Terrain.Models.TerrainLayer layer)
                {
                    return;
                }

                document.Paint(
                    point.X,
                    point.Z,
                    radius,
                    strength,
                    layer.Index
                );
            }
            else
            {
                int index = _mode.SelectedIndex;

                if (index < 0 || index > 4)
                {
                    return;
                }

                document.Apply(
                    point.X,
                    point.Z,
                    radius,
                    strength,
                    (TerrainBrushMode)index
                );
            }

            RefreshMode();
        }
        catch (Exception error)
        {
            _status.Text = error.Message;
        }
    }

    public async Task SavePendingAsync()
    {
        _viewport?.EndTerrainPaint();

        await _materialTask;

        if (IsSaving)
        {
            await _saveTask;
            return;
        }

        if (_level?.TerrainDocument?.IsDirty != true)
        {
            return;
        }

        _saveTask = SaveCoreAsync();
        await _saveTask;
    }

    private async Task SaveCoreAsync()
    {
        IsSaving = true;
        BusyChanged?.Invoke();
        RefreshMode();

        try
        {
            await _level!.TerrainDocument!.SaveAsync();
        }
        finally
        {
            IsSaving = false;
            BusyChanged?.Invoke();
            RefreshMode();
        }
    }
}
