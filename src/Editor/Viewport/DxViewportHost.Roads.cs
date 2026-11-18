using System.Numerics;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Terrain;

namespace Editor.Viewport;

public enum RoadToolMode
{
    Select,
    Add
}

public sealed partial class DxViewportHost
{
    private RoadNetwork? _roadNetwork;
    private Guid? _selectedRoadId;
    private int _selectedRoadPoint = -1;
    private bool _roadDragging;

    public bool RoadEditingEnabled { get; set; }
    public RoadToolMode RoadTool { get; set; } = RoadToolMode.Select;
    public bool IsRoadDragging => _roadDragging;

    public Guid? SelectedRoadId => _selectedRoadId;
    public int SelectedRoadPoint => _selectedRoadPoint;

    public event Action<Guid?, int>? RoadPointPicked;

    public void SetRoadNetwork(RoadNetwork? network)
    {
        EndRoadDrag();

        _roadNetwork = network;
        _selectedRoadId = null;
        _selectedRoadPoint = -1;
        RoadEditingEnabled = false;
        RoadTool = RoadToolMode.Select;
    }

    public void SelectRoad(Guid? roadId)
    {
        EndRoadDrag();

        _selectedRoadId = roadId.HasValue &&
            _roadNetwork?.Find(roadId.Value) != null
                ? roadId
                : null;

        _selectedRoadPoint = -1;
    }

    public void SelectRoadPoint(int index)
    {
        var road = _selectedRoadId is { } roadId
            ? _roadNetwork?.Find(roadId)
            : null;

        _selectedRoadPoint = road != null &&
            index >= 0 &&
            index < road.Points.Count
                ? index
                : -1;
    }

    private void HandleRoadPointerDown(nint window, int x, int y)
    {
        if (!RoadEditingEnabled ||
            IsPlaying ||
            IsStarting ||
            _rotating ||
            _renderer == null ||
            _roadNetwork == null ||
            _selectedRoadId is not { } roadId ||
            !TryObjectRay(x, y, out Vector3 direction))
        {
            return;
        }

        if (!_renderer.PickTerrain(
                _camera.Position,
                direction,
                out Vector3 hit))
        {
            return;
        }

        var road = _roadNetwork.Find(roadId);

        if (road == null)
        {
            return;
        }

        if (RoadTool == RoadToolMode.Add)
        {
            _selectedRoadPoint = _roadNetwork.AddPoint(
                roadId,
                new RoadPoint(hit.X, hit.Z));

            RoadPointPicked?.Invoke(
                roadId,
                _selectedRoadPoint);

            return;
        }

        var surface = _level == null
            ? null
            : new TerrainSurface(_level);

        int nearest = -1;
        float nearestDistance = float.MaxValue;

        float radius = Math.Clamp(
            Vector3.Distance(_camera.Position, hit) * 0.012f,
            0.75f,
            5f);

        for (int index = 0; index < road.Points.Count; index++)
        {
            var point = road.Points[index];

            if (surface == null ||
                !surface.TryGetHeight(
                    point.X,
                    point.Z,
                    out float height))
            {
                continue;
            }

            float distance = Vector3.Distance(
                hit,
                new Vector3(point.X, height, point.Z));

            if (distance < radius && distance < nearestDistance)
            {
                nearest = index;
                nearestDistance = distance;
            }
        }

        _selectedRoadPoint = nearest;
        RoadPointPicked?.Invoke(roadId, nearest);

        if (nearest < 0)
        {
            return;
        }

        _roadDragging = true;
        SetCapture(window);
    }

    private bool MoveRoadDrag(int x, int y)
    {
        if (!_roadDragging)
        {
            return false;
        }

        if (_renderer == null ||
            _roadNetwork == null ||
            _selectedRoadId is not { } roadId ||
            _selectedRoadPoint < 0 ||
            !TryObjectRay(x, y, out Vector3 direction))
        {
            return true;
        }

        if (_renderer.PickTerrain(
                _camera.Position,
                direction,
                out Vector3 hit))
        {
            _roadNetwork.MovePoint(
                roadId,
                _selectedRoadPoint,
                new RoadPoint(hit.X, hit.Z));
        }

        return true;
    }

    public void EndRoadDrag()
    {
        if (!_roadDragging)
        {
            return;
        }

        _roadDragging = false;
        ReleaseCapture();
    }
}
