using System.Globalization;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Editor.Viewport;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Terrain;

namespace Editor.Terrain;

public sealed class TerrainEditorPanel : UserControl
{
    private readonly ComboBox _mode = new() { ItemsSource = new[] { "Поднять", "Опустить", "Сгладить", "Вырезать отверстие", "Заполнить отверстие" }, SelectedIndex = 0 };
    private readonly TextBox _radius = new() { Text = "5" }, _strength = new() { Text = "0.5" };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    private readonly Button _undo = new() { Content = "Отменить последний мазок" }, _save = new() { Content = "Сохранить Terrain" };
    private LoadedLevel? _level;
    private DxViewportHost? _viewport;
    private Task _saveTask = Task.CompletedTask;
    public bool IsSaving { get; private set; }
    public bool HasPendingChanges => _level?.TerrainDocument?.IsDirty == true || IsSaving;
    public event Action? BusyChanged;

    public TerrainEditorPanel()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(new TextBlock { Text = "РЕЛЬЕФ И ОТВЕРСТИЯ" });
        root.Children.Add(_mode);
        root.Children.Add(new TextBlock { Text = "Радиус, м (до 100)" }); root.Children.Add(_radius);
        root.Children.Add(new TextBlock { Text = "Изменение высоты за щелчок, м / сила сглаживания" }); root.Children.Add(_strength);
        root.Children.Add(new TextBlock { Text = "ЛКМ по земле — один мазок. ПКМ — осмотр. Заполнение работает и внутри отверстий.", TextWrapping = TextWrapping.Wrap });
        root.Children.Add(_undo); root.Children.Add(_save); root.Children.Add(_status); Content = root;
        _mode.SelectionChanged += (_, _) => RefreshMode();
        _undo.Click += (_, _) => { if (Active) { _level?.TerrainDocument?.Undo(); RefreshMode(); } };
        _save.Click += async (_, _) => { try { await SavePendingAsync(); } catch (Exception error) { _status.Text = error.Message; } };
    }

    private bool Active => IsVisible && IsEnabled && !IsSaving && _level?.TerrainDocument != null
        && _viewport?.IsPlaying != true && _viewport?.IsStarting != true;

    public void Attach(DxViewportHost viewport)
    {
        _viewport = viewport; viewport.TerrainBrushRequested += Apply;
    }

    public void Bind(LoadedLevel level) { _level = level; RefreshMode(); }

    public void RefreshMode()
    {
        bool active = Active;
        if (_viewport != null) { _viewport.TerrainBrushEnabled = active; _viewport.TerrainBrushFill = _mode.SelectedIndex == 4; }
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
            document.Apply(point.X, point.Z, Parse(_radius), Parse(_strength), (TerrainBrushMode)_mode.SelectedIndex);
            RefreshMode();
        }
        catch (Exception error) { _status.Text = error.Message; }
    }

    public Task SavePendingAsync()
    {
        if (IsSaving) return _saveTask;
        if (_level?.TerrainDocument?.IsDirty != true) return Task.CompletedTask;
        _saveTask = SaveCoreAsync(); return _saveTask;
    }

    private async Task SaveCoreAsync()
    {
        IsSaving = true; BusyChanged?.Invoke(); RefreshMode();
        try { await _level!.TerrainDocument!.SaveAsync(); }
        finally { IsSaving = false; BusyChanged?.Invoke(); RefreshMode(); }
    }
}
