using System.Numerics;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Terrain;
using ZoneReborn.World.Vegetation;

namespace Runtime.Viewport;

public sealed partial class DxViewportHost
{
    private ObjectGizmoMode _treeTransformMode;
    private ObjectGizmoAxis _hoveredTreeAxis;
    private TreeDrag? _treeDrag;
    public bool IsTreeDragging => _treeDrag != null;
    public event Action? TreeToolChanged;

    public ObjectGizmoMode TreeTransformMode
    {
        get => _treeTransformMode;
        set
        {
            if (_treeTransformMode == value) return;
            CancelTreeDrag();
            _treeTransformMode = value;
            _hoveredTreeAxis = ObjectGizmoAxis.None;
            TreeToolChanged?.Invoke();
        }
    }

    private bool TryTreeGizmoFrame(out ObjectGizmoFrame frame)
    {
        frame = default;
        return TreeSelection() is { } selected &&
            GetClientRect(_windowHandle, out NativeRect rect) &&
            ObjectGizmoGeometry.TryCreateFrame(selected, _camera, rect.Right - rect.Left,
                rect.Bottom - rect.Top, out frame, _renderer?.ObjectGizmoFarPlane ?? 1000f);
    }

    private void HoverTreeGizmo(int x, int y) => _hoveredTreeAxis =
        !_rotating && TryTreeGizmoFrame(out var frame)
            ? ObjectGizmoGeometry.HitTest(frame, TreeTransformMode, new(x, y)) : ObjectGizmoAxis.None;

    private bool BeginTreeDrag(int x, int y)
    {
        if (!TryTreeGizmoFrame(out var frame) || _selectedTree is not { } tree) return false;
        var pixel = new Vector2(x, y);
        var axis = ObjectGizmoGeometry.HitTest(frame, TreeTransformMode, pixel);
        if (axis == ObjectGizmoAxis.None) return false;
        var direction = ObjectGizmoGeometry.AxisDirection(axis);
        var start = new TreeInstance { Id = tree.Id, Model = tree.Model };
        CopyTreeTransform(tree, start);
        var drag = new TreeDrag(tree, start, _level!.Vegetation, _renderer!.TreeWorld(tree),
            TreeTransformMode, axis, direction, frame, pixel, new TerrainSurface(_level));
        var ray = frame.RayDirection(pixel);
        if (drag.Mode == ObjectGizmoMode.Move)
        {
            var normal = frame.CameraForward - direction * Vector3.Dot(frame.CameraForward, direction);
            if (normal.LengthSquared() < 1e-6f)
                normal = frame.CameraUp - direction * Vector3.Dot(frame.CameraUp, direction);
            if (normal.LengthSquared() < 1e-6f) return true;
            drag.PlaneNormal = Vector3.Normalize(normal);
            if (!RayPlane(frame.CameraPosition, ray, frame.Pivot, drag.PlaneNormal, out var point)) return true;
            drag.StartCoordinate = Vector3.Dot(point - frame.Pivot, direction);
        }
        else
        {
            drag.PlaneNormal = direction;
            drag.RotationUsesPlane = RayPlane(frame.CameraPosition, ray, frame.Pivot, direction, out var point)
                && (point - frame.Pivot).LengthSquared() > frame.Size * frame.Size * 0.01f
                && MathF.Abs(Vector3.Dot(ray, direction)) > 0.02f;
            if (drag.RotationUsesPlane) drag.LastRadial = Vector3.Normalize(point - frame.Pivot);
        }
        _treeDrag = drag;
        _hoveredTreeAxis = axis;
        _pressedKeys.Clear();
        SetCapture(_windowHandle);
        TreeToolChanged?.Invoke();
        return true;
    }

    private bool MoveTreeDrag(int x, int y)
    {
        if (_treeDrag is not { } drag) return false;
        _mouseX = x; _mouseY = y;
        try
        {
            var pixel = new Vector2(x, y);
            var ray = drag.Frame.RayDirection(pixel);
            var world = drag.World;
            bool snap = (GetKeyState(KeyShift) & 0x8000) != 0;
            if (drag.Mode == ObjectGizmoMode.Move)
            {
                if (!RayPlane(drag.Frame.CameraPosition, ray, drag.Frame.Pivot, drag.PlaneNormal, out var point)) return true;
                float amount = Vector3.Dot(point - drag.Frame.Pivot, drag.Direction) - drag.StartCoordinate;
                if (snap) amount = MathF.Round(amount / 0.5f) * 0.5f;
                world.Translation += drag.Direction * amount;
            }
            else
            {
                if (drag.RotationUsesPlane)
                {
                    if (!RayPlane(drag.Frame.CameraPosition, ray, drag.Frame.Pivot, drag.Direction, out var point) ||
                        (point - drag.Frame.Pivot).LengthSquared() < drag.Frame.Size * drag.Frame.Size * 0.01f) return true;
                    var radial = Vector3.Normalize(point - drag.Frame.Pivot);
                    drag.Angle += MathF.Atan2(Vector3.Dot(drag.Direction, Vector3.Cross(drag.LastRadial, radial)),
                        Math.Clamp(Vector3.Dot(drag.LastRadial, radial), -1, 1));
                    drag.LastRadial = radial;
                }
                else
                {
                    var delta = pixel - drag.StartMouse;
                    drag.Angle = (delta.X - delta.Y) * 0.01f;
                }
                float angle = snap ? MathF.Round(drag.Angle / (MathF.PI / 12)) * (MathF.PI / 12) : drag.Angle;
                var position = world.Translation;
                world.Translation = Vector3.Zero;
                world *= Matrix4x4.CreateFromAxisAngle(drag.Direction, angle);
                world.Translation = position;
            }
            if (!SceneObjectTransform.TryFromWorld(world, out var transform)) return true;
            var changed = new TreeInstance { Id = drag.Tree.Id, Model = drag.Tree.Model };
            CopyTreeTransform(drag.Start, changed);
            changed.X = transform.Position.X; changed.Y = transform.Position.Y; changed.Z = transform.Position.Z;
            if (drag.Mode == ObjectGizmoMode.Rotate)
            {
                changed.Yaw = transform.Rotation.X; changed.Pitch = transform.Rotation.Y; changed.Roll = transform.Rotation.Z;
            }
            if (changed.FollowTerrain && drag.Surface.TryGetHeight(changed.X, changed.Z, out float ground))
            {
                if (drag.Mode == ObjectGizmoMode.Move && drag.Axis == ObjectGizmoAxis.Y)
                    changed.HeightOffset = changed.Y - ground;
                else changed.Y = ground + changed.HeightOffset;
            }
            VegetationStorage.Validate(changed);
            CopyTreeTransform(changed, drag.Tree);
            drag.Changed = drag.Tree.World(drag.Tree.Y) != drag.World
                || drag.Tree.HeightOffset != drag.Start.HeightOffset;
            TreeTransformed?.Invoke(drag.Tree);
        }
        catch (Exception exception)
        {
            CancelTreeDrag();
            RendererError?.Invoke(exception.Message);
        }
        return true;
    }

    public void CancelTreeDrag() => EndTreeDrag(false);

    private void EndTreeDrag(bool commit)
    {
        if (_treeDrag is not { } drag) return;
        _treeDrag = null;
        if (!commit) CopyTreeTransform(drag.Start, drag.Tree);
        else if (drag.Changed) drag.Scene.IsDirty = true;
        ReleaseCapture();
        _pressedKeys.Clear();
        _hoveredTreeAxis = ObjectGizmoAxis.None;
        TreeTransformed?.Invoke(drag.Tree);
        TreeToolChanged?.Invoke();
    }

    private bool HandleTreeKey(int key, nint lParam)
    {
        if (!TreeEditingEnabled || IsPlaying || IsStarting || GetFocus() != _windowHandle) return false;
        if (IsTreeDragging)
        {
            if (key == 0x1B) CancelTreeDrag();
            return true;
        }
        bool control = (GetKeyState(KeyControl) & 0x8000) != 0;
        if (!control && (key is 0x31 or 0x32))
        {
            TreeTransformMode = key == 0x31 ? ObjectGizmoMode.Move : ObjectGizmoMode.Rotate;
            return true;
        }
        if (key != KeyDelete) return false;
        if ((lParam.ToInt64() & (1L << 30)) == 0) TreeDeleteRequested?.Invoke();
        return true;
    }

    private static void CopyTreeTransform(TreeInstance source, TreeInstance target)
    {
        target.X = source.X; target.Y = source.Y; target.Z = source.Z;
        target.Yaw = source.Yaw; target.Pitch = source.Pitch; target.Roll = source.Roll; target.Scale = source.Scale;
        target.FollowTerrain = source.FollowTerrain; target.HeightOffset = source.HeightOffset;
    }

    private sealed class TreeDrag(TreeInstance tree, TreeInstance start, VegetationScene scene, Matrix4x4 world,
        ObjectGizmoMode mode, ObjectGizmoAxis axis, Vector3 direction, ObjectGizmoFrame frame, Vector2 startMouse, TerrainSurface surface)
    {
        public TreeInstance Tree { get; } = tree;
        public TreeInstance Start { get; } = start;
        public VegetationScene Scene { get; } = scene;
        public TerrainSurface Surface { get; } = surface;
        public Matrix4x4 World { get; } = world;
        public ObjectGizmoMode Mode { get; } = mode;
        public ObjectGizmoAxis Axis { get; } = axis;
        public Vector3 Direction { get; } = direction;
        public ObjectGizmoFrame Frame { get; } = frame;
        public Vector2 StartMouse { get; } = startMouse;
        public Vector3 PlaneNormal { get; set; }
        public float StartCoordinate { get; set; }
        public bool RotationUsesPlane { get; set; }
        public Vector3 LastRadial { get; set; }
        public float Angle { get; set; }
        public bool Changed { get; set; }
    }
}
