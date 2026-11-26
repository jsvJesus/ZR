using System.Numerics;
using ZoneReborn.World.Actors;

namespace Editor.Viewport
{
    public sealed partial class DxViewportHost
    {
        private ObjectGizmoMode _actorTransformMode =
            ObjectGizmoMode.Move;

        private ObjectGizmoAxis _hoveredActorAxis;

        private ActorDrag? _actorDrag;

        public bool IsActorDragging =>
            _actorDrag != null;

        public event Action? ActorDragStateChanged;

        public ObjectGizmoMode ActorTransformMode
        {
            get => _actorTransformMode;

            set
            {
                if (_actorTransformMode == value)
                {
                    return;
                }

                CancelActorDrag();

                _actorTransformMode = value;
                _hoveredActorAxis = ObjectGizmoAxis.None;
            }
        }

        private LevelActor? ActorSelection()
        {
            if (!ActorEditingEnabled ||
                IsPlaying ||
                IsStarting ||
                _selectedActorId is not Guid id)
            {
                return null;
            }

            return _level?.Actors.Find(id);
        }

        private bool TryActorGizmoFrame(
            out ObjectGizmoFrame frame)
        {
            frame = default;

            return ActorSelection() is { } actor &&
                   GetClientRect(
                       _windowHandle,
                       out NativeRect rect) &&
                   ObjectGizmoGeometry.TryCreateFrame(
                       LootLocationSelection()?.GetWorldPosition(actor) ??
                       actor.Position,
                       _camera,
                       rect.Right - rect.Left,
                       rect.Bottom - rect.Top,
                       out frame,
                       _renderer?.ObjectGizmoFarPlane ?? 1000f);
        }

        private void HoverActorGizmo(int x, int y)
        {
            _hoveredActorAxis =
                !_rotating &&
                TryActorGizmoFrame(out var frame)
                    ? ObjectGizmoGeometry.HitTest(
                        frame,
                        ActorTransformMode,
                        new Vector2(x, y),
                        ObjectGizmoAxis.Y)
                    : ObjectGizmoAxis.None;
        }

        private bool BeginActorDrag(int x, int y)
        {
            if (_rotating ||
                IsActorDragging ||
                ActorSelection() is not { } actor ||
                !TryActorGizmoFrame(out var frame) ||
                _level == null)
            {
                return false;
            }

            var pixel = new Vector2(x, y);

            ObjectGizmoAxis axis =
                ObjectGizmoGeometry.HitTest(
                    frame,
                    ActorTransformMode,
                    pixel,
                    ObjectGizmoAxis.Y);

            if (axis == ObjectGizmoAxis.None)
            {
                return false;
            }

            Vector3 direction =
                ObjectGizmoGeometry.AxisDirection(axis);

            var drag = new ActorDrag(
                actor,
                _level.Actors,
                ActorTransformMode,
                axis,
                direction,
                frame,
                pixel,
                LootLocationSelection());

            Vector3 ray = frame.RayDirection(pixel);

            if (drag.Mode == ObjectGizmoMode.Move)
            {
                Vector3 normal =
                    frame.CameraForward -
                    direction * Vector3.Dot(
                        frame.CameraForward,
                        direction);

                if (normal.LengthSquared() < 1e-6f)
                {
                    normal = frame.CameraUp -
                        direction * Vector3.Dot(
                            frame.CameraUp,
                            direction);
                }

                if (normal.LengthSquared() < 1e-6f)
                {
                    return true;
                }

                drag.PlaneNormal =
                    Vector3.Normalize(normal);

                if (!RayPlane(
                        frame.CameraPosition,
                        ray,
                        frame.Pivot,
                        drag.PlaneNormal,
                        out Vector3 point))
                {
                    return true;
                }

                drag.StartCoordinate = Vector3.Dot(
                    point - frame.Pivot,
                    direction);
            }
            else
            {
                drag.PlaneNormal = direction;

                drag.RotationUsesPlane =
                    RayPlane(
                        frame.CameraPosition,
                        ray,
                        frame.Pivot,
                        direction,
                        out Vector3 point) &&
                    (point - frame.Pivot).LengthSquared() >
                    frame.Size * frame.Size * 0.01f &&
                    MathF.Abs(Vector3.Dot(ray, direction)) > 0.02f;

                if (drag.RotationUsesPlane)
                {
                    drag.LastRadial = Vector3.Normalize(
                        point - frame.Pivot);
                }
            }

            _actorDrag = drag;
            _hoveredActorAxis = axis;

            _pressedKeys.Clear();

            SetCapture(_windowHandle);

            ActorDragStateChanged?.Invoke();

            return true;
        }

        private bool MoveActorDrag(int x, int y)
        {
            if (_actorDrag is not { } drag)
            {
                return false;
            }

            _mouseX = x;
            _mouseY = y;

            try
            {
                var pixel = new Vector2(x, y);

                Vector3 ray =
                    drag.Frame.RayDirection(pixel);

                bool snap =
                    (GetKeyState(KeyShift) & 0x8000) != 0;

                if (drag.Mode == ObjectGizmoMode.Move)
                {
                    if (!RayPlane(
                            drag.Frame.CameraPosition,
                            ray,
                            drag.Frame.Pivot,
                            drag.PlaneNormal,
                            out Vector3 point))
                    {
                        return true;
                    }

                    float amount = Vector3.Dot(
                        point - drag.Frame.Pivot,
                        drag.Direction) - drag.StartCoordinate;

                    if (snap)
                    {
                        amount =
                            MathF.Round(amount / 0.5f) * 0.5f;
                    }

                    Vector3 position =
                        drag.StartPosition +
                        drag.Direction * amount;

                    if (!float.IsFinite(position.X) ||
                        !float.IsFinite(position.Y) ||
                        !float.IsFinite(position.Z) ||
                        MathF.Abs(position.X) > 10000000f ||
                        MathF.Abs(position.Y) > 10000000f ||
                        MathF.Abs(position.Z) > 10000000f)
                    {
                        return true;
                    }

                    drag.Position = position;
                }
                else
                {
                    if (drag.RotationUsesPlane)
                    {
                        if (!RayPlane(
                                drag.Frame.CameraPosition,
                                ray,
                                drag.Frame.Pivot,
                                drag.Direction,
                                out Vector3 point) ||
                            (point - drag.Frame.Pivot)
                                .LengthSquared() <
                            drag.Frame.Size *
                            drag.Frame.Size * 0.01f)
                        {
                            return true;
                        }

                        Vector3 radial = Vector3.Normalize(
                            point - drag.Frame.Pivot);

                        drag.Angle += MathF.Atan2(
                            Vector3.Dot(
                                drag.Direction,
                                Vector3.Cross(
                                    drag.LastRadial,
                                    radial)),
                            Math.Clamp(
                                Vector3.Dot(
                                    drag.LastRadial,
                                    radial),
                                -1f,
                                1f));

                        drag.LastRadial = radial;
                    }
                    else
                    {
                        Vector2 delta =
                            pixel - drag.StartMouse;

                        drag.Angle =
                            (delta.X - delta.Y) * 0.01f;
                    }

                    float angle = drag.Angle;

                    if (snap)
                    {
                        float step = MathF.PI / 12f;

                        angle =
                            MathF.Round(angle / step) * step;
                    }

                    if (!float.IsFinite(angle))
                    {
                        return true;
                    }

                    drag.Actor.Yaw = MathF.IEEERemainder(
                        drag.StartYaw + angle,
                        MathF.Tau);
                }
            }
            catch (Exception exception)
            {
                CancelActorDrag();

                RendererError?.Invoke(exception.Message);
            }

            return true;
        }

        public void CancelActorDrag()
        {
            EndActorDrag(false);
        }

        private void EndActorDrag(bool commit)
        {
            if (_actorDrag is not { } drag)
            {
                return;
            }

            _actorDrag = null;

            bool changed =
                drag.Position != drag.StartPosition ||
                drag.Actor.Yaw != drag.StartYaw;

            if (!commit)
            {
                drag.Position = drag.StartPosition;
                drag.Actor.Yaw = drag.StartYaw;
            }
            else if (changed)
            {
                drag.Document.Touch();
            }

            ReleaseCapture();

            _pressedKeys.Clear();

            _hoveredActorAxis = ObjectGizmoAxis.None;

            if (commit && changed)
            {
                ActorChanged?.Invoke(drag.Actor.Id);
            }

            ActorDragStateChanged?.Invoke();
        }

        private sealed class ActorDrag(
            LevelActor actor,
            ActorDocument document,
            ObjectGizmoMode mode,
            ObjectGizmoAxis axis,
            Vector3 direction,
            ObjectGizmoFrame frame,
            Vector2 startMouse,
            LootSpawnLocation? location)
        {
            public LevelActor Actor { get; } = actor;

            public ActorDocument Document { get; } = document;

            public ObjectGizmoMode Mode { get; } = mode;

            public ObjectGizmoAxis Axis { get; } = axis;

            public Vector3 Direction { get; } = direction;

            public ObjectGizmoFrame Frame { get; } = frame;

            public Vector2 StartMouse { get; } = startMouse;

            public Vector3 StartPosition { get; } =
                location?.GetWorldPosition(actor) ?? actor.Position;

            public float StartYaw { get; } = actor.Yaw;

            public Vector3 Position
            {
                get => location?.GetWorldPosition(Actor) ??
                       Actor.Position;

                set
                {
                    if (location == null)
                    {
                        Actor.Position = value;
                    }
                    else
                    {
                        location.SetWorldPosition(Actor, value);
                    }
                }
            }

            public Vector3 PlaneNormal { get; set; }

            public float StartCoordinate { get; set; }

            public bool RotationUsesPlane { get; set; }

            public Vector3 LastRadial { get; set; }

            public float Angle { get; set; }
        }
    }
}
