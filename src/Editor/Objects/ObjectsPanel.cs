using System.Globalization;
using System.IO;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Editor.Viewport;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Scene;

namespace Editor.Objects
{
    public sealed partial class ObjectsPanel : UserControl
    {
        private const int ListLimit = 200;
        private readonly TextBox _search = new() { Watermark = "Модель, prefab или ID…" };
        private readonly ListBox _instances = new() { MaxHeight = 210 };
        private readonly TextBlock _summary = Text("Откройте уровень."), _selection = Text("Выберите объект."), _status = Text("");
        private readonly TextBox[] _position = [new(), new(), new()];
        private readonly TextBox[] _rotation = [new(), new(), new()];
        private readonly TextBox[] _scale = [new(), new(), new()];
        private readonly StackPanel _transformFields = new() { Spacing = 6 };
        private readonly Button _move, _rotate, _apply, _delete, _focus, _undo, _redo, _save;
        private DxViewportHost? _viewport;
        private LoadedLevel? _level;
        private string? _selectedId;
        private bool _binding, _transformSupported;
        private Task _saveTask = Task.CompletedTask;

        public bool IsSaving { get; private set; }
        public bool IsBusy => IsSaving || IsImporting;
        public bool HasPendingChanges => _level?.Objects?.IsDirty == true || IsBusy;
        public event Action? BusyChanged;

        public ObjectsPanel()
        {
            var root = new StackPanel { Spacing = 8 };
            root.Children.Add(_summary);
            BuildLibrary(root);
            BuildCollisionControls(root);
            root.Children.Add(Text("ИНСТРУМЕНТ ОБЪЕКТА"));
            _move = Button(root, "Перемещение [1]", () => SetTransformMode(ObjectGizmoMode.Move));
            _rotate = Button(root, "Вращение [2]", () => SetTransformMode(ObjectGizmoMode.Rotate));
            root.Children.Add(Text("ЛКМ — тянуть цветную ось или кольцо: X — красная, Y — зелёная, Z — синяя. Esc / ПКМ — отменить. Shift — шаг 0,5 м / 15°."));
            root.Children.Add(Text("ОБЪЕКТЫ УРОВНЯ"));
            root.Children.Add(_search); root.Children.Add(_instances); root.Children.Add(_selection);
            root.Children.Add(Text("Правки относятся только к выбранному объекту этого экземпляра prefab. ЛКМ — выбрать в сцене."));
            _focus = Button(root, "Показать выбранное", FocusSelected);
            VectorFields(_transformFields, "Позиция · X / Y / Z, м", _position);
            VectorFields(_transformFields, "Поворот · yaw / pitch / roll, °", _rotation);
            VectorFields(_transformFields, "Масштаб · X / Y / Z", _scale);
            root.Children.Add(_transformFields);
            _apply = Button(root, "Применить параметры", ApplyProperties);
            _delete = Button(root, "Удалить выбранный объект", DeleteSelected);
            _undo = Button(root, "Отменить · Ctrl+Z", Undo);
            _redo = Button(root, "Повторить · Ctrl+Y", Redo);
            _save = new Button { Content = "Сохранить объекты", HorizontalAlignment = HorizontalAlignment.Stretch };
            _save.Click += async (_, _) =>
            {
                try { await SavePendingAsync(); }
                catch (Exception exception) { _status.Text = exception.Message; }
            };
            root.Children.Add(_save); root.Children.Add(_status); Content = root;
            _search.TextChanged += (_, _) => { if (!IsDragging) RefreshInstances(); };
            _instances.SelectionChanged += (_, _) =>
            {
                if (_binding || IsDragging) return;
                _selectedId = (_instances.SelectedItem as ObjectEntry)?.Id;
                FillSelected();
            };
            RefreshMode();
        }

        public void Attach(DxViewportHost viewport, AssetPaths assets)
        {
            _viewport = viewport;
            AttachLibrary(assets);
            viewport.ObjectPicked += instance =>
            {
                if (!CanInteract || IsDragging) return;
                _selectedId = instance?.Id; RefreshInstances();
            };
            viewport.ObjectPlaced += instance =>
            {
                _selectedId = instance.Id; RefreshInstances();
            };
            viewport.ObjectPlacementChanged += RefreshLibraryMode;
            viewport.ObjectDeleteRequested += () => Run(DeleteSelected);
            viewport.ObjectUndoRequested += () => Run(Undo);
            viewport.ObjectRedoRequested += () => Run(Redo);
            viewport.ObjectGizmoModeChanged += RefreshMode;
            viewport.ObjectDragStateChanged += RefreshMode;
            viewport.ObjectTransformPreviewed += instance =>
            {
                if (instance.Id != _selectedId) return;
                if (Dispatcher.UIThread.CheckAccess()) FillSelected();
                else Dispatcher.UIThread.Post(FillSelected);
            };
            RefreshMode();
        }

        public void Bind(LoadedLevel level)
        {
            _viewport?.CancelObjectDrag();
            CancelLibraryLoad();
            if (_level?.Objects is { } previous) previous.Changed -= SceneChanged;
            _level = level; _selectedId = null;
            if (level.Objects is { } scene) scene.Changed += SceneChanged;
            RefreshInstances();
            if (_selectedAsset != null) _ = LoadSelectedAssetAsync();
            _status.Text = level.Objects == null ? "В уровне нет объектов сцены."
                : !level.Objects.CanSave ? "Файл objects.json несовместим или повреждён. Доступен просмотр; перезапись отключена.\n"
                    + string.Join("\n", level.Objects.Warnings.Take(5))
                : "Выберите объект в списке или щелчком в сцене. Delete — удалить.";
        }

        private bool CanInteract => IsVisible && IsEnabled && !IsBusy && _level?.Objects != null
            && _viewport?.IsPlaying != true && _viewport?.IsStarting != true;
        private bool IsDragging => _viewport?.IsObjectDragging == true;
        private SceneInstance? Selected => _selectedId == null ? null : _level?.Objects?.Find(_selectedId);

        public void RefreshMode()
        {
            RefreshLibraryMode();
            RefreshCollisionMode();
            bool active = CanInteract;
            bool controlsEnabled = active && !IsDragging;
            bool editable = controlsEnabled && _level?.Objects?.CanSave == true;
            bool selected = Selected != null;
            _search.IsEnabled = controlsEnabled;
            _instances.IsEnabled = controlsEnabled;
            _move.IsEnabled = _rotate.IsEnabled = controlsEnabled;
            bool moving = _viewport?.ObjectTransformMode != ObjectGizmoMode.Rotate;
            _move.Content = moving ? "● Перемещение [1]" : "Перемещение [1]";
            _rotate.Content = moving ? "Вращение [2]" : "● Вращение [2]";
            _move.FontWeight = moving ? FontWeight.Bold : FontWeight.Normal;
            _rotate.FontWeight = moving ? FontWeight.Normal : FontWeight.Bold;
            _transformFields.IsEnabled = editable && selected && _transformSupported;
            _apply.IsEnabled = _transformFields.IsEnabled;
            _delete.IsEnabled = editable && selected;
            _focus.IsEnabled = controlsEnabled && selected;
            _undo.IsEnabled = editable && _level?.Objects?.CanUndo == true;
            _redo.IsEnabled = editable && _level?.Objects?.CanRedo == true;
            _save.IsEnabled = editable && _level?.Objects?.IsDirty == true;
            if (_viewport == null) return;
            _viewport.ObjectEditingEnabled = active;
            if (!IsDragging) _viewport.SelectObject(active ? Selected?.Id : null);
        }

        private void SetTransformMode(ObjectGizmoMode mode)
        {
            if (CanInteract && !IsDragging && _viewport != null)
            {
                _viewport.CancelObjectPlacement();
                _viewport.ObjectTransformMode = mode;
            }
        }

        private void SceneChanged()
        {
            if (Dispatcher.UIThread.CheckAccess()) RefreshInstances();
            else Dispatcher.UIThread.Post(RefreshInstances);
        }

        private void RefreshInstances()
        {
            string search = (_search.Text ?? "").Trim();
            var entries = new List<ObjectEntry>(ListLimit);
            int count = 0;
            if (_level?.Objects is { } scene)
            {
                foreach (var instance in scene.Instances)
                {
                    if (search.Length != 0 && !instance.Model.Contains(search, StringComparison.OrdinalIgnoreCase)
                        && !instance.Id.Contains(search, StringComparison.OrdinalIgnoreCase)
                        && !(instance.PrefabName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)) continue;
                    count++;
                    if (entries.Count < ListLimit) entries.Add(Entry(instance));
                }
            }
            var selected = Selected;
            if (selected == null) _selectedId = null;
            else if (!entries.Any(entry => entry.Id == selected.Id))
            {
                if (entries.Count == ListLimit) entries.RemoveAt(entries.Count - 1);
                entries.Add(Entry(selected));
            }
            _binding = true;
            _instances.ItemsSource = entries;
            _instances.SelectedItem = entries.FirstOrDefault(entry => entry.Id == _selectedId);
            _binding = false;
            _summary.Text = _level == null ? "Откройте уровень."
                : $"{_level.Manifest.Name}: {_level.Objects?.Instances.Count ?? 0} объектов\n"
                    + $"Найдено: {count} · в списке: {entries.Count} (до {ListLimit}, включая выбранное)"
                    + (_level.Objects?.IsDirty == true ? "\nЕсть несохранённые изменения." : "");
            FillSelected();
        }

        private void FillSelected()
        {
            var instance = Selected;
            _transformSupported = false;
            foreach (var field in _position.Concat(_rotation).Concat(_scale)) field.Text = "";
            if (instance == null) _selection.Text = "Выберите объект.";
            else
            {
                _selection.Text = $"{instance.Model}\nID: {instance.Id}\nPrefab: {instance.PrefabName ?? "—"}";
                _transformSupported = SceneObjectTransform.TryFromWorld(instance.World, out var transform);
                if (_transformSupported)
                {
                    Fill(_position, transform.Position); Fill(_rotation, transform.Rotation); Fill(_scale, transform.Scale);
                }
                else _selection.Text += "\nМатрица содержит сдвиг осей или неподдерживаемое преобразование. Числовые поля недоступны; используйте инструмент в сцене.";
            }
            RefreshMode();
        }

        private static ObjectEntry Entry(SceneInstance instance) => new(instance.Id,
            $"{Path.GetFileNameWithoutExtension(instance.Model.Replace('\\', '/'))} · {instance.Id}");
        private sealed record ObjectEntry(string Id, string Label)
        {
            public override string ToString() => Label;
        }
        private static TextBlock Text(string value) => new() { Text = value, TextWrapping = TextWrapping.Wrap, FontSize = 12 };
        private static void VectorFields(StackPanel root, string label, TextBox[] fields)
        {
            root.Children.Add(Text(label));
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*") };
            for (int index = 0; index < fields.Length; index++)
            {
                Grid.SetColumn(fields[index], index);
                fields[index].Margin = new Thickness(0, 0, index < fields.Length - 1 ? 4 : 0, 0);
                row.Children.Add(fields[index]);
            }
            root.Children.Add(row);
        }
        private Button Button(StackPanel root, string label, Action action)
        {
            var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Stretch };
            button.Click += (_, _) => Run(action); root.Children.Add(button); return button;
        }
        private void Run(Action action)
        {
            try { action(); }
            catch (Exception exception) { _status.Text = exception.Message; }
        }
        private static void Fill(TextBox[] fields, Vector3 vector)
        {
            fields[0].Text = Format(vector.X); fields[1].Text = Format(vector.Y); fields[2].Text = Format(vector.Z);
        }
        private static string Format(float value) => value.ToString("G9", CultureInfo.InvariantCulture);
        private static Vector3 Read(TextBox[] fields) => new(Read(fields[0]), Read(fields[1]), Read(fields[2]));
        private static float Read(TextBox field) => float.TryParse((field.Text ?? "").Trim().Replace(',', '.'),
            NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && float.IsFinite(value)
                ? value : throw new InvalidDataException("Ожидается конечное число.");
    }
}
