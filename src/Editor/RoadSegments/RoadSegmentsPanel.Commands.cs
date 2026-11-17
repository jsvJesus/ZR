using System.IO;
using System.Numerics;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Terrain;

namespace Editor.RoadSegments;

public sealed partial class RoadSegmentsPanel
{
    private void RequireEditable()
    {
        if (_level == null || !_level.Roads.CanSave || IsSaving || _viewport?.IsPlaying == true ||
            _viewport?.IsStarting == true || _viewport?.IsRoadDragging == true)
            throw new InvalidOperationException("Редактирование дорог сейчас недоступно.");
    }

    private RoadDefinition RequireRoad()
    {
        RequireEditable();
        return SelectedRoad ?? throw new InvalidOperationException("Выберите дорогу.");
    }

    private void CreateRoad(RoadType type)
    {
        RequireEditable();
        if (_level!.Roads.Roads.Count >= RoadStorage.MaximumRoads)
            throw new InvalidOperationException("Достигнут лимит дорог уровня.");
        var road = RoadDefinition.Create(type);
        road.Name += $" {_level.Roads.Roads.Count + 1}";
        _level.Roads.Roads.Add(road);
        _level.Roads.Changed(road);
        RefreshRoadList(road, -1);
        _status.Text = "Ctrl + ЛКМ на Terrain — добавить точку. Геометрия появится после второй точки.";
    }

    private void ApplyProfile()
    {
        var road = RequireRoad();
        var candidate = RoadStorage.Copy(road);
        candidate.Name = (_name.Text ?? "").Trim();
        candidate.Type = (RoadType)_type.SelectedIndex;
        candidate.Width = Parse(_width); candidate.ShoulderWidth = Parse(_shoulder);
        candidate.Thickness = Parse(_thickness); candidate.SurfaceOffset = Parse(_surfaceOffset);
        candidate.RailGauge = Parse(_gauge); candidate.SleeperSpacing = Parse(_spacing);
        if (!int.TryParse(_lanes.Text, out int lanes)) throw new InvalidDataException("Число полос должно быть целым.");
        candidate.LaneCount = lanes;
        candidate.FollowTerrain = _follow.IsChecked == true;
        if (candidate.FollowTerrain != road.FollowTerrain)
        {
            var surface = new TerrainSurface(_level!);
            foreach (var node in candidate.Nodes)
            {
                bool found = surface.TryGetHeight(node.X, node.Z, out float ground);
                float y = road.FollowTerrain && found ? ground + node.HeightOffset : node.Y;
                if (candidate.FollowTerrain && !found)
                    throw new InvalidOperationException("Для привязки все точки должны находиться над Terrain.");
                node.Y = y;
                if (candidate.FollowTerrain) node.HeightOffset = y - ground;
            }
        }
        RoadStorage.Validate(candidate);
        road.Name = candidate.Name; road.Type = candidate.Type;
        road.Width = candidate.Width; road.ShoulderWidth = candidate.ShoulderWidth;
        road.Thickness = candidate.Thickness; road.SurfaceOffset = candidate.SurfaceOffset;
        road.LaneCount = candidate.LaneCount; road.RailGauge = candidate.RailGauge;
        road.SleeperSpacing = candidate.SleeperSpacing; road.FollowTerrain = candidate.FollowTerrain;
        road.Nodes = candidate.Nodes;
        Changed(road, NodeIndex, "Профиль применён; геометрия дороги обновлена.");
    }

    private void ApplyNode()
    {
        var road = RequireRoad();
        if (NodeIndex < 0 || NodeIndex >= road.Nodes.Count) throw new InvalidOperationException("Выберите точку.");
        var candidate = RoadStorage.Copy(road);
        var node = candidate.Nodes[NodeIndex];
        node.X = Parse(_x); node.Z = Parse(_z);
        if (road.FollowTerrain)
        {
            node.HeightOffset = Parse(_y);
            if (!new TerrainSurface(_level!).TryGetHeight(node.X, node.Z, out float ground))
                throw new InvalidOperationException("Точка вне Terrain; отключите привязку для свободной высоты.");
            node.Y = ground + node.HeightOffset;
        }
        else node.Y = Parse(_y);
        RoadStorage.Validate(candidate);
        road.Nodes[NodeIndex] = node;
        Changed(road, NodeIndex, "Точка обновлена.");
    }

    private void InsertNode()
    {
        var road = RequireRoad();
        if (NodeIndex < 0 || NodeIndex + 1 >= road.Nodes.Count)
            throw new InvalidOperationException("Выберите точку перед нужным участком. В конец дороги точки добавляются через Ctrl + ЛКМ.");
        int after = NodeIndex;
        var surface = new TerrainSurface(_level!);
        var samples = RoadSpline.Sample(road, surface).Where(sample => sample.SegmentIndex == after).ToArray();
        Vector3 position = samples.Length > 0 ? samples[samples.Length / 2].Position
            : Vector3.Lerp(road.Nodes[after].Position, road.Nodes[after + 1].Position, 0.5f);
        position.Y -= road.SurfaceOffset;
        float offset = (road.Nodes[after].HeightOffset + road.Nodes[after + 1].HeightOffset) * 0.5f;
        if (road.FollowTerrain && surface.TryGetHeight(position.X, position.Z, out float ground)) offset = position.Y - ground;
        var node = new RoadNode { X = position.X, Y = position.Y, Z = position.Z, HeightOffset = offset };
        var candidate = RoadStorage.Copy(road);
        candidate.Nodes.Insert(after + 1, node);
        RoadStorage.Validate(candidate);
        road.Nodes.Insert(after + 1, node);
        Changed(road, after + 1, "Добавлена опорная точка.");
    }

    private void SnapNode()
    {
        var road = RequireRoad();
        if (NodeIndex < 0 || NodeIndex >= road.Nodes.Count) throw new InvalidOperationException("Выберите точку.");
        var candidate = RoadStorage.Copy(road);
        var node = candidate.Nodes[NodeIndex];
        if (!new TerrainSurface(_level!).TryGetHeight(node.X, node.Z, out float ground))
            throw new InvalidOperationException("Точка вне Terrain.");
        node.Y = ground; node.HeightOffset = 0;
        RoadStorage.Validate(candidate);
        road.Nodes[NodeIndex] = node;
        Changed(road, NodeIndex, "Точка установлена на Terrain с учётом смещения поверхности дороги.");
    }

    private void DeleteNode()
    {
        var road = RequireRoad();
        if (NodeIndex < 0 || NodeIndex >= road.Nodes.Count) return;
        int index = NodeIndex;
        var candidate = RoadStorage.Copy(road);
        candidate.Nodes.RemoveAt(index);
        RoadStorage.Validate(candidate);
        road.Nodes.RemoveAt(index);
        Changed(road, Math.Min(index, road.Nodes.Count - 1), "Точка удалена.");
    }

    private void ConformRoadTerrain()
    {
        var road = RequireRoad();

        if (_level?.TerrainDocument is not { } document)
        {
            throw new InvalidOperationException(
                "Terrain уровня не загружен.");
        }

        if (road.Nodes.Count < 2)
        {
            throw new InvalidOperationException(
                "Для выравнивания необходимо минимум " +
                "две опорные точки.");
        }

        int changed = document.ConformRoad(road);

        if (!road.TerrainConformed)
        {
            _level.Roads.Changed(road);
            road.TerrainConformed = true;
        }

        RefreshRoadList(road, NodeIndex);

        if (changed == 0)
        {
            _status.Text =
                "Terrain соответствует профилю дороги. " +
                "Высотный профиль зафиксирован.";
            return;
        }

        _status.Text =
            "Дорожный коридор сформирован. " +
            $"Изменено вершин Terrain: {changed:N0}. " +
            "Высотный профиль дороги зафиксирован. " +
            "Сохраните уровень и дороги.";
    }

    private void DeleteRoad()
    {
        var road = RequireRoad();
        _viewport?.SelectRoad(null);
        _level!.Roads.Roads.Remove(road);
        _level.Roads.Changed();
        RefreshRoadList(_level.Roads.Roads.FirstOrDefault(), -1);
        _status.Text = "Дорога удалена. Сохраните сеть дорог.";
    }

    private void Changed(RoadDefinition road, int node, string status)
    {
        _level!.Roads.Changed(road);
        RefreshRoadList(road, node);
        _status.Text = status;
    }

    public Task SavePendingAsync()
    {
        _viewport?.CancelRoadDrag();
        if (IsSaving) return _saveTask;
        if (_level == null || !_level.Roads.IsDirty) return Task.CompletedTask;
        _saveTask = SaveCoreAsync();
        return _saveTask;
    }

    private async Task SaveCoreAsync()
    {
        var level = _level!;
        IsSaving = true; BusyChanged?.Invoke(); RefreshHighlight();
        try
        {
            await RoadStorage.SaveAsync(level.Directory, level.Roads);
            if (ReferenceEquals(_level, level))
            {
                RefreshRoadList(SelectedRoad, NodeIndex);
                _status.Text = $"Сохранено: {Path.Combine(level.Directory, RoadStorage.FileName)}";
            }
        }
        finally { IsSaving = false; BusyChanged?.Invoke(); RefreshHighlight(); }
    }
}
