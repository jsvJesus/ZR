using System.IO;
using ZoneReborn.World.Terrain;
using ZoneReborn.World.Vegetation;

namespace Editor.Vegetation
{
    public sealed partial class VegetationPanel
    {
        private void RequireEditable()
        {
            if (_level == null || !_level.Vegetation.CanSave || IsSaving || _viewport?.IsPlaying == true || _viewport?.IsStarting == true)
                throw new InvalidOperationException("Редактирование растительности сейчас недоступно.");
        }
        private void ApplyProperties()
        {
            RequireEditable();
            if (Selected is not { } tree) { RefreshMode(); _status.Text = "Параметры расстановки обновлены."; return; }
            bool follow = _grounded.IsChecked == true;
            var changed = new TreeInstance { Id = tree.Id, Model = tree.Model, X = Read(_x), Y = follow ? tree.Y : Read(_y), Z = Read(_z),
                Yaw = Read(_yaw), Scale = Read(_scale), FollowTerrain = follow, HeightOffset = follow ? Read(_y) : tree.HeightOffset };
            VegetationStorage.Validate(changed);
            tree.X = changed.X; tree.Y = changed.Y; tree.Z = changed.Z; tree.Yaw = changed.Yaw; tree.Scale = changed.Scale;
            tree.FollowTerrain = changed.FollowTerrain; tree.HeightOffset = changed.HeightOffset;
            _level!.Vegetation.IsDirty = true; RefreshInstances(tree); _status.Text = "Параметры дерева применены.";
        }
        private void SnapToGround()
        {
            RequireEditable();
            if (Selected is not { } tree) throw new InvalidOperationException("Выберите дерево уровня.");
            if (!new TerrainSurface(_level!).TryGetHeight(tree.X, tree.Z, out float height))
                throw new InvalidOperationException("Дерево вне Terrain.");
            tree.Y = height; tree.FollowTerrain = true; tree.HeightOffset = 0;
            _level!.Vegetation.IsDirty = true; RefreshInstances(tree);
        }
        private void DeleteSelected()
        {
            RequireEditable();
            if (Selected is not { } tree) return;
            _level!.Vegetation.Instances.Remove(tree); _level.Vegetation.IsDirty = true; RefreshInstances(null);
            _status.Text = "Дерево удалено.";
        }

        public Task SavePendingAsync()
        {
            if (IsSaving) return _saveTask;
            if (_level == null || !_level.Vegetation.IsDirty) return Task.CompletedTask;
            _saveTask = SaveCoreAsync(); return _saveTask;
        }
        private async Task SaveCoreAsync()
        {
            var level = _level!;
            IsSaving = true; BusyChanged?.Invoke(); RefreshMode();
            try
            {
                await VegetationStorage.SaveAsync(level.Directory, level.Vegetation);
                if (_level == level) { _status.Text = $"Сохранено: {Path.Combine(level.Directory, VegetationStorage.FileName)}"; UpdateSummary(); }
            }
            finally { IsSaving = false; BusyChanged?.Invoke(); RefreshMode(); }
        }

        public void CancelLoading() => _loading?.Cancel();
    }
}
