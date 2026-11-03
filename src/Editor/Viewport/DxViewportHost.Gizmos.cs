using System.Numerics;
using ZoneReborn.World.Scene;

namespace Editor.Viewport;

public sealed partial class DxViewportHost
{
    private ObjectGizmoMode _objectTransformMode;
    private ObjectGizmoAxis _hoveredObjectAxis;
    private ObjectDrag? _objectDrag;

    public ObjectGizmoMode ObjectTransformMode
    {
        get => _objectTransformMode;
        set
        {
            if (_objectTransformMode == value) return;
            CancelObjectDrag();
            CancelObjectPlacement();
            _objectTransformMode = value;
            _hoveredObjectAxis = ObjectGizmoAxis.None;
            ObjectGizmoModeChanged?.Invoke();
        }
    }

    public bool IsObjectDragging => _objectDrag != null;
    public event Action? ObjectGizmoModeChanged;
    public event Action? ObjectDragStateChanged;
    public event Action<SceneInstance>? ObjectTransformPreviewed;

    public void CancelObjectDrag() => EndObjectDrag(false);

    private bool TryObjectGizmoFrame(out ObjectGizmoFrame frame)
    {
        frame = default;
        return ObjectEditingEnabled && !IsObjectPlacing && !IsPlaying && !IsStarting && _level?.Objects is { CanSave: true } scene &&
            _selectedObjectId != null && scene.Find(_selectedObjectId) is { } selected &&
            GetClientRect(_windowHandle, out NativeRect rect) &&
            ObjectGizmoGeometry.TryCreateFrame(selected, _camera, rect.Right - rect.Left, rect.Bottom - rect.Top,
                out frame, _renderer?.ObjectGizmoFarPlane ?? 1000f);
    }

    private bool BeginObjectDrag(int x, int y)
    {
        if (_rotating || !TryObjectGizmoFrame(out var frame)) return false;
        var pixel = new Vector2(x, y);
        var axis = ObjectGizmoGeometry.HitTest(frame, ObjectTransformMode, pixel);
        if (axis == ObjectGizmoAxis.None) return false;
        var scene = _level!.Objects!;
        var selected = scene.Find(_selectedObjectId!)!;
        var direction = ObjectGizmoGeometry.AxisDirection(axis);
        var drag = new ObjectDrag(scene, selected, ObjectTransformMode, axis, direction, frame, pixel);
        try
        {
            var ray = frame.RayDirection(pixel);
            if (drag.Mode == ObjectGizmoMode.Move)
            {
                // A view-facing plane containing the selected world axis gives stable
                // dragging without assuming that the object sits on the terrain.
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
            scene.BeginTransformPreview(selected.Id);
            _objectDrag = drag;
            _hoveredObjectAxis = axis;
            _pressedKeys.Clear();
            SetCapture(_windowHandle);
            ObjectDragStateChanged?.Invoke();
        }
        catch (Exception exception)
        {
            CancelObjectDrag();
            RendererError?.Invoke(exception.Message);
        }
        return true;
    }

    private bool MoveObjectDrag(int x, int y)
    {
        if (_objectDrag is not { } drag) return false;
        _mouseX = x; _mouseY = y;
        try
        {
            var pixel = new Vector2(x, y);
            var ray = drag.Frame.RayDirection(pixel);
            var world = drag.Start.World;
            bool snap = (GetKeyState(KeyShift) & 0x8000) != 0;
            if (drag.Mode == ObjectGizmoMode.Move)
            {
                if (!RayPlane(drag.Frame.CameraPosition, ray, drag.Frame.Pivot, drag.PlaneNormal, out var point)) return true;
                float amount = Vector3.Dot(point - drag.Frame.Pivot, drag.Direction) - drag.StartCoordinate;
                if (snap) amount = MathF.Round(amount / 0.5f) * 0.5f;
                world.Translation = drag.Start.World.Translation + drag.Direction * amount;
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
                    // An edge-on rotation ring has no stable ray/plane intersection.
                    // Keep it usable with a screen drag instead of amplifying tiny errors.
                    var delta = pixel - drag.StartMouse;
                    drag.Angle = (delta.X - delta.Y) * 0.01f;
                }
                float angle = snap ? MathF.Round(drag.Angle / (MathF.PI / 12)) * (MathF.PI / 12) : drag.Angle;
                var translation = world.Translation;
                world.Translation = Vector3.Zero;
                world *= Matrix4x4.CreateFromAxisAngle(drag.Direction, angle);
                world.Translation = translation;
            }
            var preview = drag.Scene.PreviewTransform(drag.Start.Id, world);
            ObjectTransformPreviewed?.Invoke(preview);
        }
        catch (Exception exception)
        {
            CancelObjectDrag();
            RendererError?.Invoke(exception.Message);
        }
        return true;
    }

    private void EndObjectDrag(bool commit)
    {
        if (_objectDrag is not { } drag) return;
        // Clear first: releasing capture and scene notifications can reenter our handlers.
        _objectDrag = null;
        try
        {
            if (commit) drag.Scene.CommitTransformPreview();
            else drag.Scene.CancelTransformPreview();
        }
        catch (Exception exception)
        {
            drag.Scene.CancelTransformPreview();
            RendererError?.Invoke(exception.Message);
        }
        finally
        {
            ReleaseCapture();
            _pressedKeys.Clear();
            _hoveredObjectAxis = ObjectGizmoAxis.None;
            ObjectDragStateChanged?.Invoke();
        }
    }

    private void HoverObjectGizmo(int x, int y)
    {
        _hoveredObjectAxis = !_rotating && TryObjectGizmoFrame(out var frame)
            ? ObjectGizmoGeometry.HitTest(frame, ObjectTransformMode, new(x, y)) : ObjectGizmoAxis.None;
    }

    private static bool RayPlane(Vector3 origin, Vector3 ray, Vector3 pivot, Vector3 normal, out Vector3 point)
    {
        point = default;
        float denominator = Vector3.Dot(ray, normal);
        if (!float.IsFinite(denominator) || MathF.Abs(denominator) < 1e-6f) return false;
        float distance = Vector3.Dot(pivot - origin, normal) / denominator;
        if (!float.IsFinite(distance) || distance < 0) return false;
        point = origin + ray * distance;
        return float.IsFinite(point.X) && float.IsFinite(point.Y) && float.IsFinite(point.Z);
    }

    private sealed class ObjectDrag(SceneData scene, SceneInstance start, ObjectGizmoMode mode,
        ObjectGizmoAxis axis, Vector3 direction, ObjectGizmoFrame frame, Vector2 startMouse)
    {
        public SceneData Scene { get; } = scene;
        public SceneInstance Start { get; } = start;
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
    }
}
