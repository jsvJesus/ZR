using System.Numerics;
using ZoneReborn.World.Actors;

namespace Editor.Viewport
{
    public sealed partial class DxViewportHost
    {
        private bool _actorEditingEnabled;

        private Guid? _selectedActorId;

        public ActorKind ActorPlacementKind { get; set; } =
            ActorKind.PlayerSpawn;

        public bool ActorEditingEnabled
        {
            get => _actorEditingEnabled;

            set
            {
                _actorEditingEnabled = value;

                if (!value)
                {
                    CancelActorDrag();

                    LootLocationPlacementEnabled = false;
                    _selectedLootLocationId = null;

                    _hoveredActorAxis =
                        ObjectGizmoAxis.None;
                }
            }
        }

        public event Action<Guid?>? ActorPicked;
        public event Action<Guid>? ActorChanged;
        public event Action? ActorDeleteRequested;

        public void SelectActor(Guid? id)
        {
            if (id != _selectedActorId)
            {
                CancelActorDrag();

                _selectedLootLocationId = null;

                LootLocationPlacementEnabled = false;

                _hoveredActorAxis = ObjectGizmoAxis.None;
            }

            _selectedActorId =
                id.HasValue &&
                _level?.Actors.Find(id.Value) != null
                    ? id
                    : null;
        }

        private void HandleActorPointerDown(int x, int y)
        {
            if (!ActorEditingEnabled ||
                IsPlaying ||
                IsStarting ||
                _rotating ||
                IsActorDragging ||
                _renderer == null ||
                _level == null ||
                !TryObjectRay(x, y, out Vector3 direction))
            {
                return;
            }

            bool control =
                (GetKeyState(KeyControl) & 0x8000) != 0;

            ActorDocument document = _level.Actors;

            if (control)
            {
                if (!_renderer.PickObjectSurface(
                        _camera.Position,
                        direction,
                        out Vector3 position))
                {
                    RendererError?.Invoke(
                        "Поверхность не найдена.");

                    return;
                }

                if (PlaceLootLocation(position))
                {
                    return;
                }

                LevelActor actor = LevelActor.Create(
                    ActorPlacementKind,
                    position,
                    _camera.Yaw);

                document.Add(actor);

                SelectActor(actor.Id);

                ActorChanged?.Invoke(actor.Id);

                return;
            }

            if (BeginActorDrag(x, y))
            {
                return;
            }

            if (TryPickLootLocation(
                    _camera.Position,
                    direction))
            {
                return;
            }

            Guid? picked = PickActor(
                document,
                _camera.Position,
                direction);

            SelectLootLocation(null);

            SelectActor(picked);

            ActorPicked?.Invoke(picked);
        }

        private static Guid? PickActor(
            ActorDocument document,
            Vector3 origin,
            Vector3 direction)
        {
            Guid? result = null;

            float nearest = 1500f;

            foreach (LevelActor actor in document.Actors)
            {
                float height =
                    actor.Kind == ActorKind.LootSpawn
                        ? 0.4f
                        : 0.8f;

                Vector3 center = actor.Position +
                    Vector3.UnitY * height;

                Vector3 offset = center - origin;

                float distance = Vector3.Dot(
                    offset,
                    direction);

                if (distance <= 0 ||
                    distance >= nearest)
                {
                    continue;
                }

                Vector3 closest =
                    origin + direction * distance;

                float radius = Math.Clamp(
                    distance * 0.008f,
                    0.65f,
                    3f);

                if (Vector3.DistanceSquared(
                        center,
                        closest) > radius * radius)
                {
                    continue;
                }

                nearest = distance;
                result = actor.Id;
            }

            return result;
        }

        private bool HandleActorKey(
            int key,
            nint lParam)
        {
            if (!ActorEditingEnabled ||
                IsPlaying ||
                IsStarting ||
                GetFocus() != _windowHandle)
            {
                return false;
            }

            if (IsActorDragging)
            {
                if (key == 0x1B)
                {
                    CancelActorDrag();
                }

                return true;
            }

            bool control =
                (GetKeyState(KeyControl) & 0x8000) != 0;

            if (!control && key is 0x31 or 0x32)
            {
                ActorTransformMode = key == 0x31
                    ? ObjectGizmoMode.Move
                    : ObjectGizmoMode.Rotate;

                return true;
            }

            if (key == 0x2E)
            {
                bool firstPress =
                    (lParam.ToInt64() & (1L << 30)) == 0;

                if (firstPress)
                {
                    ActorDeleteRequested?.Invoke();
                }

                return true;
            }

            return false;
        }
    }
}
