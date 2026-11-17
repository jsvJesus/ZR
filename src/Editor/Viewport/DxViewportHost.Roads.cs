using System.Numerics;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Terrain;

namespace Editor.Viewport;

public sealed partial class DxViewportHost
{
    private bool _roadEditingEnabled;
    private RoadDefinition? _selectedRoad;
    private int _selectedRoadNode = -1;
    private RoadPointDrag? _roadDrag;
    public bool IsRoadDragging => _roadDrag != null;
    public event Action<RoadDefinition?, int>? RoadPicked;
    public event Action? RoadsChanged;
    public event Action? RoadDeleteNodeRequested;

    public bool RoadEditingEnabled
    {
        get => _roadEditingEnabled;
        set { if (!value) CancelRoadDrag(); _roadEditingEnabled = value; }
    }

    public void SelectRoad(RoadDefinition? road, int nodeIndex = -1)
    {
        if (!ReferenceEquals(_selectedRoad, road) || _selectedRoadNode != nodeIndex) CancelRoadDrag();
        _selectedRoad = road;
        _selectedRoadNode = road != null && nodeIndex >= 0 && nodeIndex < road.Nodes.Count ? nodeIndex : -1;
        _renderer?.SelectRoad(RoadEditingEnabled && !IsPlaying && !IsStarting ? road : null, _selectedRoadNode);
    }

    private bool CanEditRoads => RoadEditingEnabled && !IsPlaying && !IsStarting &&
        _renderer != null && _level?.Roads.CanSave == true;

    private void PickRoadSegment(int x, int y)
    {
        if (!CanEditRoads || _rotating || !TryObjectRay(x, y, out var direction)) return;
        try
        {
            if ((GetKeyState(KeyControl) & 0x8000) != 0)
            {
                AppendRoadPoint(direction);
                return;
            }
            if (_selectedRoad is { } road && TryPickRoadNode(road, x, y, out int node))
            {
                SelectRoad(road, node);
                RoadPicked?.Invoke(road, node);
                _roadDrag = new(road, node, road.Nodes[node].Copy(), _level!.Roads, new TerrainSurface(_level));
                _pressedKeys.Clear();
                SetCapture(_windowHandle);
                RoadsChanged?.Invoke();
                return;
            }
            if (_renderer!.PickRoad(_camera.Position, direction) is { } selection)
            {
                int index = Math.Clamp(selection.SegmentIndex, 0, selection.Road.Nodes.Count - 1);
                SelectRoad(selection.Road, index);
                RoadPicked?.Invoke(selection.Road, index);
            }
        }
        catch (Exception exception) { CancelRoadDrag(); RendererError?.Invoke(exception.Message); }
    }

    private void AppendRoadPoint(Vector3 direction)
    {
        if (_selectedRoad is not { } road || !_level!.Roads.Roads.Contains(road))
            throw new InvalidOperationException("Создайте или выберите дорогу в панели.");
        if (!_renderer!.PickTerrain(_camera.Position, direction, out var position))
            throw new InvalidOperationException("Наведите курсор на Terrain.");
        var surface = new TerrainSurface(_level);
        if ((GetKeyState(KeyShift) & 0x8000) != 0)
        {
            position.X = MathF.Round(position.X * 2) * 0.5f;
            position.Z = MathF.Round(position.Z * 2) * 0.5f;
            if (!surface.TryGetHeight(position.X, position.Z, out position.Y))
                throw new InvalidOperationException("Точка сетки вне Terrain.");
        }
        var candidate = RoadStorage.Copy(road);
        candidate.Nodes.Add(new() { X = position.X, Y = position.Y, Z = position.Z });
        RoadStorage.Validate(candidate);
        road.Nodes.Add(candidate.Nodes[^1]);
        _level.Roads.Changed(road);
        SelectRoad(road, road.Nodes.Count - 1);
        RoadsChanged?.Invoke();
        RoadPicked?.Invoke(road, road.Nodes.Count - 1);
    }

    private bool TryPickRoadNode(RoadDefinition road, int x, int y, out int index)
    {
        index = -1;
        if (!GetClientRect(_windowHandle, out NativeRect rect) || _level == null) return false;
        var forward = Vector3.Normalize(_camera.LookDirection);
        var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
        var frame = new ObjectGizmoFrame(Vector3.Zero, 1, _camera.Position, forward, right,
            Vector3.Cross(forward, right), rect.Right - rect.Left, rect.Bottom - rect.Top)
        { FarPlane = _renderer?.ObjectGizmoFarPlane ?? 1000 };
        var surface = new TerrainSurface(_level);
        float best = 14 * 14;
        for (int i = 0; i < road.Nodes.Count; i++)
        {
            var node = road.Nodes[i];
            var position = node.Position;
            if (road.FollowTerrain)
            {
                if (!road.TerrainConformed &&
                    surface.TryGetHeight(
                        node.X,
                        node.Z,
                        out float ground))
                {
                    position.Y = ground;
                }

                position.Y += node.HeightOffset;
            }
            position.Y += road.SurfaceOffset;
            if (!frame.TryProject(position, out var pixel)) continue;
            float distance = Vector2.DistanceSquared(new(x, y), pixel);
            if (distance < best) { best = distance; index = i; }
        }
        return index >= 0;
    }

    private bool MoveRoadDrag(int x, int y)
    {
        if (_roadDrag is not { } drag) return false;
        _mouseX = x; _mouseY = y;
        if (!TryObjectRay(x, y, out var direction)) return true;
        try
        {
            Vector3 position;
            if (drag.Road.FollowTerrain)
            {
                if (!_renderer!.PickTerrain(_camera.Position, direction, out position)) return true;
            }
            else if (!RayPlane(_camera.Position, direction, drag.Start.Position, Vector3.UnitY, out position)) return true;
            if ((GetKeyState(KeyShift) & 0x8000) != 0)
            {
                position.X = MathF.Round(position.X * 2) * 0.5f;
                position.Z = MathF.Round(position.Z * 2) * 0.5f;
            }
            var node = drag.Start.Copy();
            node.X = position.X; node.Z = position.Z;
            if (drag.Road.FollowTerrain)
            {
                if (!drag.Surface.TryGetHeight(node.X, node.Z, out float ground)) return true;
                node.Y = ground + node.HeightOffset;
            }
            var candidate = RoadStorage.Copy(drag.Road);
            candidate.Nodes[drag.Index] = node;
            RoadStorage.Validate(candidate);
            drag.Road.Nodes[drag.Index] = node;
            drag.Road.Changed();
            drag.Changed = node.X != drag.Start.X || node.Y != drag.Start.Y || node.Z != drag.Start.Z;
            RoadsChanged?.Invoke();
        }
        catch (InvalidDataException exception) { RendererError?.Invoke(exception.Message); }
        catch (Exception exception) { CancelRoadDrag(); RendererError?.Invoke(exception.Message); }
        return true;
    }

    public void CancelRoadDrag() => EndRoadDrag(false);

    private void EndRoadDrag(bool commit)
    {
        if (_roadDrag is not { } drag) return;
        _roadDrag = null;
        if (!commit) { drag.Road.Nodes[drag.Index] = drag.Start; drag.Road.Changed(); }
        else if (drag.Changed) drag.Network.Changed(drag.Road);
        ReleaseCapture();
        _pressedKeys.Clear();
        RoadsChanged?.Invoke();
    }

    private bool HandleRoadKey(int key, nint lParam)
    {
        if (!CanEditRoads || GetFocus() != _windowHandle) return false;
        if (IsRoadDragging)
        {
            if (key == 0x1B) CancelRoadDrag();
            return true;
        }
        if (key == 0x1B) { SelectRoad(null); RoadPicked?.Invoke(null, -1); return true; }
        if (key != KeyDelete) return false;
        if ((lParam.ToInt64() & (1L << 30)) == 0) RoadDeleteNodeRequested?.Invoke();
        return true;
    }

    public void FocusRoad(RoadDefinition road, int nodeIndex = -1)
    {
        if (IsPlaying || IsStarting || IsRoadDragging || _level == null || road.Nodes.Count == 0) return;
        var samples = RoadSpline.Sample(road, new TerrainSurface(_level));
        Vector3 center;
        float distance;
        if (nodeIndex >= 0 && nodeIndex < road.Nodes.Count)
        {
            var node = road.Nodes[nodeIndex]; center = node.Position;
            if (road.FollowTerrain)
            {
                if (!road.TerrainConformed &&
                    new TerrainSurface(_level).TryGetHeight(
                        node.X,
                        node.Z,
                        out float ground))
                {
                    center.Y = ground;
                }

                center.Y += node.HeightOffset;
            }
            distance = Math.Max(15, road.Width * 3);
        }
        else
        {
            var positions = samples.Count == 0 ? road.Nodes.Select(node => node.Position) : samples.Select(sample => sample.Position);
            var minimum = new Vector3(float.PositiveInfinity); var maximum = new Vector3(float.NegativeInfinity);
            foreach (var point in positions) { minimum = Vector3.Min(minimum, point); maximum = Vector3.Max(maximum, point); }
            center = (minimum + maximum) * 0.5f;
            distance = Math.Clamp(Vector3.Distance(minimum, maximum), 20, 1500);
        }
        SetCamera(new(center.X, center.Y + distance * 0.7f, center.Z - distance, 0, -MathF.Atan(0.7f), Math.Clamp(distance, 8, 250)));
    }

    private sealed class RoadPointDrag(RoadDefinition road, int index, RoadNode start, RoadNetwork network, TerrainSurface surface)
    {
        public RoadDefinition Road { get; } = road;
        public int Index { get; } = index;
        public RoadNode Start { get; } = start;
        public RoadNetwork Network { get; } = network;
        public TerrainSurface Surface { get; } = surface;
        public bool Changed { get; set; }
    }
}
