using System.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Terrain;

namespace Editor.RoadSegments
{
    public sealed partial class RoadSegmentsPanel
    {
        private void RequireEditable()
        {
            if (_level == null || SelectedRoad == null || SelectedSegment == null)
                throw new InvalidOperationException("Сначала выберите дорогу и сегмент.");
            if (!_level.Roads.CanSave) throw new InvalidOperationException("Исправьте файл предыдущих правок перед редактированием.");
            if (IsSaving || _viewport?.IsPlaying == true || _viewport?.IsStarting == true)
                throw new InvalidOperationException("Редактирование сейчас недоступно.");
        }

        private void ApplyNode()
        {
            RequireEditable();
            var road = SelectedRoad!;
            int nodeIndex = SelectedSegment!.Index + (_endpoint.SelectedIndex == 1 ? 1 : 0);
            bool follow = _followTerrain.IsChecked == true;
            var node = new RoadNode { X = Parse(_x), Y = Parse(_y), Z = Parse(_z),
                TangentX = Parse(_tx), TangentZ = Parse(_tz), Width = Parse(_width) };
            if (follow) node.Y += road.OriginalNodes[nodeIndex].Y;
            RoadEditStorage.ValidateNode(node);
            road.Nodes[nodeIndex] = node;
            SelectedSegment.FollowTerrain = follow;
            MarkChanged(road, true);
            _status.Text = "Узел применён. Соседние сегменты обновлены.";
        }

        private void ApplyMaterial(bool wholeRoad)
        {
            RequireEditable();
            var material = _materials.SelectedItem as RoadMaterial;
            string name = material?.Name ?? SelectedSegment!.Material;
            float scale = Parse(_uv);
            if (scale is < 0.01f or > 1000) throw new InvalidDataException("Повторение UV: от 0.01 до 1000.");
            string diffuse = (_diffuse.Text ?? "").Trim(), normal = (_normal.Text ?? "").Trim();
            string specular = (_specular.Text ?? "").Trim(), power = (_specularPower.Text ?? "").Trim();
            ValidateTexture(diffuse); ValidateTexture(normal);
            ValidateTexture(specular); ValidateTexture(power);
            var road = SelectedRoad!;
            foreach (var segment in wholeRoad ? road.Segments : new[] { SelectedSegment! })
            {
                segment.Material = name; segment.DiffuseOverride = diffuse;
                segment.NormalOverride = normal; segment.UvScale = scale;
                segment.SpecularOverride = specular; segment.SpecularPowerOverride = power;
            }
            MarkChanged(road, false);
            _status.Text = wholeRoad ? "Материал применён ко всей дороге." : "Материал применён к сегменту.";
        }

        private void ValidateTexture(string path)
        {
            if (path.Length == 0) return;
            if (!Path.GetExtension(path).Equals(".dds", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Для текстуры требуется файл DDS.");
            if (ZoneReborn.World.Roads.RoadMaterials.ResolveTexture(_assets!, path) == null)
                throw new FileNotFoundException($"Не найдена текстура: {path}");
        }

        private void FitToTerrain()
        {
            RequireEditable();
            var surface = new TerrainSurface(_level!);
            var road = SelectedRoad!; var segment = SelectedSegment!;
            var a = road.Nodes[segment.Index].Copy(); var b = road.Nodes[segment.Index + 1].Copy();
            if (!surface.TryGetHeight(a.X, a.Z, out float ay) || !surface.TryGetHeight(b.X, b.Z, out float by))
                throw new InvalidOperationException("Узлы вне Terrain. Сначала исправьте X/Z узлов.");
            if (segment.FollowTerrain)
            {
                float SourceOffset(int node) => road.TerrainOffsets.Length == road.Vertices.Length
                    ? Enumerable.Range(0, 4).Average(c => road.TerrainOffsets[node * SourceRoad.SamplesPerSegment * 4 + c]) : 0;
                a.Y = road.OriginalNodes[segment.Index].Y - SourceOffset(segment.Index);
                b.Y = road.OriginalNodes[segment.Index + 1].Y - SourceOffset(segment.Index + 1);
            }
            else { a.Y = ay + 0.02f; b.Y = by + 0.02f; }
            road.Nodes[segment.Index] = a; road.Nodes[segment.Index + 1] = b;
            MarkChanged(road, true); FillNode();
            _status.Text = "Высоты узлов сегмента установлены по Terrain.";
        }

        private void MarkChanged(SourceRoad road, bool geometry)
        {
            road.Changed(geometry); _level!.Roads.IsDirty = true;
            UpdateSummary(); UpdateTextureInfo(); RefreshHighlight();
        }

        private async Task BrowseTextureAsync(TextBox target)
        {
            RequireEditable();
            var top = TopLevel.GetTopLevel(this);
            if (top == null) return;
            var boundLevel = _level;
            var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите DDS дороги", AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("DDS") { Patterns = ["*.dds"] }]
            });
            string? path = files.FirstOrDefault()?.TryGetLocalPath();
            if (path != null && _level == boundLevel)
            {
                string relative = Path.GetRelativePath(_assets!.Root, path);
                target.Text = relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)
                    ? path : relative.Replace('\\', '/');
                UpdateTextureInfo();
            }
        }

        public Task SavePendingAsync()
        {
            if (IsSaving) return _saveTask;
            if (_level == null || !_level.Roads.IsDirty) return Task.CompletedTask;
            _saveTask = SaveCoreAsync();
            return _saveTask;
        }

        private async Task SaveCoreAsync()
        {
            var level = _level!;
            IsSaving = true; IsEnabled = false; BusyChanged?.Invoke();
            try
            {
                await RoadEditStorage.SaveAsync(level.Directory, level.Roads);
                if (_level == level)
                {
                    _status.Text = $"Сохранено: {Path.Combine(level.Directory, RoadEditStorage.FileName)}";
                    UpdateSummary();
                }
            }
            finally { IsSaving = false; IsEnabled = true; BusyChanged?.Invoke(); }
        }
    }
}
