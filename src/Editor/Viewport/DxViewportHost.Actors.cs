using System.Numerics;
using ZoneReborn.World.Actors;

namespace Editor.Viewport
{
    public sealed partial class DxViewportHost
    {
        private bool _actorEditingEnabled;
        private bool _actorMoveRequested;

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
                    _actorMoveRequested = false;
                }
            }
        }

        public event Action<Guid?>? ActorPicked;
        public event Action<Guid>? ActorChanged;
        public event Action? ActorDeleteRequested;

        public void SelectActor(Guid? id)
        {
            _selectedActorId =
                id.HasValue &&
                _level?.Actors.Find(id.Value) != null
                    ? id
                    : null;

            _actorMoveRequested = false;
        }

        public void BeginActorMove()
        {
            if (ActorEditingEnabled && _selectedActorId.HasValue)
            {
                _actorMoveRequested = true;
            }
        }

        private void HandleActorPointerDown(int x, int y)
        {
            if (!ActorEditingEnabled ||
                IsPlaying ||
                IsStarting ||
                _rotating ||
                _renderer == null ||
                _level == null ||
                !TryObjectRay(x, y, out Vector3 direction))
            {
                return;
            }

            bool control =
                (GetKeyState(KeyControl) & 0x8000) != 0;

            ActorDocument document = _level.Actors;

            if (_actorMoveRequested || control)
            {
                if (!_renderer.PickObjectSurface(
                        _camera.Position,
                        direction,
                        out Vector3 position))
                {
                    RendererError?.Invoke(
                        "Поверхность для актора не найдена.");

                    return;
                }

                if (_actorMoveRequested)
                {
                    _actorMoveRequested = false;

                    if (_selectedActorId is Guid selectedId &&
                        document.Find(selectedId) is { } selected)
                    {
                        selected.Position = position;

                        document.Touch();

                        ActorChanged?.Invoke(selected.Id);
                    }

                    return;
                }

                LevelActor actor = LevelActor.Create(
                    ActorPlacementKind,
                    position,
                    _camera.Yaw);

                document.Add(actor);

                _selectedActorId = actor.Id;

                ActorChanged?.Invoke(actor.Id);

                return;
            }

            Guid? picked = PickActor(
                document,
                _camera.Position,
                direction);

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
                Vector3 center = actor.Position +
                    Vector3.UnitY * 0.8f;

                Vector3 offset = center - origin;

                float distance = Vector3.Dot(
                    offset,
                    direction);

                if (distance <= 0 || distance >= nearest)
                {
                    continue;
                }

                Vector3 closest = origin +
                    direction * distance;

                float radius = Math.Clamp(
                    distance * 0.008f,
                    0.65f,
                    3f);

                if (Vector3.DistanceSquared(center, closest) >
                    radius * radius)
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
                IsStarting)
            {
                return false;
            }

            if (key == 0x1B)
            {
                _actorMoveRequested = false;
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
