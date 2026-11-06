using System.Globalization;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Editor.Viewport;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Terrain;
using ZoneReborn.World.Assets;

namespace Editor.Terrain;

public sealed partial class TerrainEditorPanel : UserControl
{
    private readonly ComboBox _mode = new() { ItemsSource = new[] { "Поднять", "Опустить", "Сгладить", "Вырезать отверстие", "Заполнить отверстие", "Материалы FBX", "Рисовать текстурой" }, SelectedIndex = 0 };
    private readonly StackPanel _brushSettings = new() { Spacing = 8 };
    private readonly TextBox _radius = new() { Text = "5" }, _strength = new() { Text = "0.5" };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    private readonly Button _undo = new() { Content = "Отменить правку Terrain" }, _save = new() { Content = "Сохранить Terrain" };
    private LoadedLevel? _level;
    private DxViewportHost? _viewport;
    private Task _saveTask = Task.CompletedTask;
    public bool IsSaving { get; private set; }
    public bool IsBusy => IsSaving || _materialCancellation != null;
    public bool HasPendingChanges => _level?.TerrainDocument?.IsDirty == true || IsBusy;
    public event Action? BusyChanged;

    public TerrainEditorPanel()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(new TextBlock { Text = "TERRAIN" });
        root.Children.Add(_mode);
        BuildMaterialControls(root);
        _brushSettings.Children.Add(new TextBlock { Text = "Радиус, м (до 100)" }); _brushSettings.Children.Add(_radius);
        _brushSettings.Children.Add(new TextBlock { Text = "Сила кисти: текстуры и сглаживание 0–1; высота — метры" }); _brushSettings.Children.Add(_strength);
        _brushSettings.Children.Add(new TextBlock { Text = "Кисть текстур: ведите с зажатой ЛКМ. Рельеф: ЛКМ — один мазок. ПКМ — осмотр.", TextWrapping = TextWrapping.Wrap });
        root.Children.Add(_brushSettings);
        root.Children.Add(_undo); root.Children.Add(_save); root.Children.Add(_status); Content = root;
        _mode.SelectionChanged += (_, _) => RefreshMode();
        _undo.Click += (_, _) => { if (Active) { _level?.TerrainDocument?.Undo(); RefreshMode(); } };
        _save.Click += async (_, _) => { try { await SavePendingAsync(); } catch (Exception error) { _status.Text = error.Message; } };
    }

    private bool Active => IsVisible && IsEnabled && !IsBusy && _level?.TerrainDocument != null
        && _viewport?.IsPlaying != true && _viewport?.IsStarting != true;

    public void Attach(DxViewportHost viewport, AssetPaths assets)
    {
        _assets = assets; _viewport = viewport; viewport.TerrainBrushRequested += Apply;
        viewport.TerrainStrokeStarted += () => _level?.TerrainDocument?.BeginPaintStroke();
        viewport.TerrainStrokeEnded += () => { _level?.TerrainDocument?.EndPaintStroke(); RefreshMode(); };
    }

    public void Bind(LoadedLevel level) { _viewport?.EndTerrainPaint(); _level = level; RefreshPalette(); RefreshMode(); }

    public void RefreshMode()
    {
        bool active = Active;
        bool paint = _mode.SelectedIndex == 6, material = _mode.SelectedIndex == 5;
        if (_viewport != null)
        {
            if (!active || !paint) _viewport.EndTerrainPaint();
            _viewport.TerrainBrushEnabled = active && !material; _viewport.TerrainBrushFill = _mode.SelectedIndex == 4;
            _viewport.TerrainBrushContinuous = paint;
        }
        _materialControls.IsVisible = paint || material;
        _brushSettings.IsVisible = !material;
        _materialControls.IsEnabled = active;
        _mode.IsEnabled = _radius.IsEnabled = _strength.IsEnabled = active;
        _undo.IsEnabled = active && _level?.TerrainDocument?.CanUndo == true;
        _save.IsEnabled = active && HasPendingChanges;
        if (_level == null) _status.Text = "Откройте уровень или импортируйте папку Escape.";
        else _status.Text = $"Шаг сетки: {_level.Manifest.Terrain.CellSize:G4} м. "
            + (HasPendingChanges ? "Есть несохранённые изменения." : "Terrain сохранён.");
    }

    private void Apply(Vector3 point)
    {
        if (!Active || _level?.TerrainDocument is not { } document) return;
        try
        {
            float Parse(TextBox box) => float.TryParse((box.Text ?? "").Replace(',', '.'), NumberStyles.Float,
                CultureInfo.InvariantCulture, out float value) && float.IsFinite(value) && value > 0
                    ? value : throw new InvalidOperationException("Радиус и сила должны быть положительными числами.");
            float radius = Parse(_radius), strength = Parse(_strength);
            if (_mode.SelectedIndex == 5) return;
            if (_mode.SelectedIndex == 6)
            {
                if (strength > 1) throw new InvalidOperationException("Сила покраски должна быть больше 0 и не больше 1.");
                if (_palette.SelectedItem is not ZoneReborn.World.Terrain.Models.TerrainLayer layer) return;
                if (_viewport != null) _viewport.TerrainBrushSpacing = Math.Max(document.Descriptor.CellSize * 0.25f, radius * 0.2f);
                document.Paint(point.X, point.Z, radius, strength, layer.Index);
            }
            else document.Apply(point.X, point.Z, radius, strength, (TerrainBrushMode)_mode.SelectedIndex);
            RefreshMode();
        }
        catch (Exception error) { _status.Text = error.Message; }
    }

    public async Task SavePendingAsync()
    {
        _viewport?.EndTerrainPaint();
        await _materialTask;
        if (IsSaving) { await _saveTask; return; }
        if (_level?.TerrainDocument?.IsDirty != true) return;
        _saveTask = SaveCoreAsync(); await _saveTask;
    }

    private async Task SaveCoreAsync()
    {
        IsSaving = true; BusyChanged?.Invoke(); RefreshMode();
        try { await _level!.TerrainDocument!.SaveAsync(); }
        finally { IsSaving = false; BusyChanged?.Invoke(); RefreshMode(); }
    }
}
