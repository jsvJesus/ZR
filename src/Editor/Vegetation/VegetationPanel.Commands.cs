using System.IO;
using ZoneReborn.World.Terrain;
using ZoneReborn.World.Vegetation;

namespace Editor.Vegetation
{
    public sealed partial class VegetationPanel
    {
        private void RequireEditable()
        {
            if (GroundCoverMode || _level == null || !_level.Vegetation.CanSave || IsSaving || _viewport?.IsPlaying == true ||
                _viewport?.IsStarting == true || _viewport?.IsTreeDragging == true)
                throw new InvalidOperationException("Редактирование растительности сейчас недоступно.");
        }
        private void ApplyProperties()
        {
            RequireEditable();
            if (Selected is not { } tree) { RefreshMode(); _status.Text = "Параметры расстановки обновлены."; return; }
            bool follow = _grounded.IsChecked == true;
            var changed = new TreeInstance { Id = tree.Id, Model = tree.Model, X = Read(_x), Y = follow ? TreeHeight(tree) : Read(_y), Z = Read(_z),
                Yaw = Read(_yaw), Pitch = Read(_pitch), Roll = Read(_roll), Scale = Read(_scale),
                FollowTerrain = follow, HeightOffset = follow ? Read(_y) : tree.HeightOffset };
            if (follow && new TerrainSurface(_level!).TryGetHeight(changed.X, changed.Z, out float ground))
                changed.Y = ground + changed.HeightOffset;
            VegetationStorage.Validate(changed);
            tree.X = changed.X; tree.Y = changed.Y; tree.Z = changed.Z; tree.Yaw = changed.Yaw; tree.Scale = changed.Scale;
            tree.Pitch = changed.Pitch; tree.Roll = changed.Roll;
            tree.FollowTerrain = changed.FollowTerrain; tree.HeightOffset = changed.HeightOffset;
            _level!.Vegetation.IsDirty = true; RefreshInstances(tree); _status.Text = "Параметры растения применены.";
        }

        private float TreeHeight(TreeInstance tree) => tree.FollowTerrain && _level != null &&
            new TerrainSurface(_level).TryGetHeight(tree.X, tree.Z, out float ground) ? ground + tree.HeightOffset : tree.Y;

        private void ChangeTerrainAttachment()
        {
            RequireEditable();
            if (Selected is { } tree)
            {
                float y = TreeHeight(tree);
                bool follow = _grounded.IsChecked == true;
                float offset = tree.HeightOffset;
                if (follow)
                {
                    if (!new TerrainSurface(_level!).TryGetHeight(tree.X, tree.Z, out float ground))
                        throw new InvalidOperationException("Растение вне Terrain.");
                    offset = y - ground;
                    if (!float.IsFinite(offset) || MathF.Abs(offset) > 1e6)
                        throw new InvalidOperationException("Слишком большое смещение высоты.");
                }
                tree.Y = y; tree.HeightOffset = offset; tree.FollowTerrain = follow;
                _level!.Vegetation.IsDirty = true;
                UpdateSummary();
            }
            FillHeight(); RefreshMode();
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

        public async Task SavePendingAsync()
        {
            _viewport?.CancelTreeDrag();
            _viewport?.EndGroundCoverPaint();
            if (!_batchTask.IsCompleted)
            {
                await _batchTask;
            }

            if (IsSaving)
            {
                await _saveTask;
                return;
            }

            if (_level == null || (!_level.Vegetation.IsDirty && !_level.GroundCover.IsDirty))
            {
                return;
            }

            _saveTask = SaveCoreAsync();

            await _saveTask;
        }

        private async Task SaveCoreAsync()
        {
            var level = _level!;
            IsSaving = true; BusyChanged?.Invoke(); RefreshMode();
            try
            {
                // A damaged cover document must not prevent saving valid tree edits.
                var errors = new List<Exception>();
                if (level.Vegetation.IsDirty)
                {
                    try
                    {
                        await VegetationScatterStorage.SaveAsync(level.Directory, level.Vegetation);
                        await VegetationStorage.SaveAsync(level.Directory, level.Vegetation);
                    }
                    catch (Exception exception) { errors.Add(exception); }
                }
                if (level.GroundCover.IsDirty)
                {
                    try { await GroundCoverStorage.SaveAsync(level.Directory, level.GroundCover); }
                    catch (Exception exception) { errors.Add(exception); }
                }
                if (errors.Count > 0) throw new AggregateException("Не удалось сохранить часть растительности.", errors);

                if (_level == level)
                {
                    _status.Text = $"Растительность сохранена: {level.Directory}";
                    UpdateSummary();
                    RefreshGroundCoverInfo();
                }
            }
            finally
            {
                IsSaving = false; BusyChanged?.Invoke(); RefreshMode();
            }
        }

        public void CancelLoading()
        {
            _loading?.Cancel();
            _batchCancellation?.Cancel();
            _coverCatalogLoading?.Cancel();
            _viewport?.EndGroundCoverPaint();
        }
    }
}
