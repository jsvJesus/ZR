using System.IO;
using ZoneReborn.World.Scene;

namespace Editor.Objects
{
    public sealed partial class ObjectsPanel
    {
        private void RequireEditable()
        {
            if (!CanInteract || IsDragging || _level?.Objects?.CanSave != true)
                throw new InvalidOperationException("Редактирование объектов сейчас недоступно.");
        }

        private void ApplyProperties()
        {
            RequireEditable();
            if (Selected is not { } instance) return;
            if (!_transformSupported) throw new InvalidOperationException("Эту матрицу нельзя изменить числовыми параметрами без потери сдвига осей.");
            var transform = new SceneObjectTransform(Read(_position), Read(_rotation), Read(_scale));
            _level!.Objects!.UpdateTransform(instance.Id, transform.ToWorld());
            _status.Text = "Параметры выбранного объекта применены.";
        }

        private void DeleteSelected()
        {
            RequireEditable();
            if (Selected is not { } instance) return;
            if (_level!.Objects!.Remove(instance.Id)) _status.Text = "Выбранный объект удалён. Ctrl+Z — отменить.";
        }

        private void FocusSelected()
        {
            if (CanInteract && !IsDragging && Selected is { } instance) _viewport?.FocusObject(instance);
        }

        private void Undo()
        {
            RequireEditable();
            if (_level!.Objects!.Undo()) _status.Text = "Изменение объекта отменено.";
        }

        private void Redo()
        {
            RequireEditable();
            if (_level!.Objects!.Redo()) _status.Text = "Изменение объекта повторено.";
        }

        public Task SavePendingAsync()
        {
            _viewport?.CancelObjectPlacement();
            if (IsImporting) return SaveAfterImportAsync();
            if (IsSaving) return _saveTask;
            if (IsDragging) throw new InvalidOperationException("Завершите перемещение или вращение объекта перед сохранением.");
            if (_level?.Objects?.IsDirty != true) return Task.CompletedTask;
            _saveTask = SaveCoreAsync(); return _saveTask;
        }

        private async Task SaveAfterImportAsync()
        {
            await _modelImportTask;
            await SavePendingAsync();
        }

        private async Task SaveCoreAsync()
        {
            var level = _level!;
            if (!level.Objects!.CanSave) throw new InvalidOperationException("Сохранение objects.json отключено: исправьте несовместимый или повреждённый файл.");
            IsSaving = true; BusyChanged?.Invoke(); RefreshMode();
            try
            {
                await SceneEditStorage.SaveAsync(level.Directory, level.Objects);
                if (_level == level)
                {
                    _status.Text = $"Сохранено: {Path.Combine(level.Directory, SceneEditStorage.FileName)}";
                    RefreshInstances();
                }
            }
            finally { IsSaving = false; BusyChanged?.Invoke(); RefreshMode(); }
        }
    }
}
