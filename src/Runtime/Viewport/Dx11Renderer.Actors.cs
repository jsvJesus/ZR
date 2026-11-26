using System.Numerics;
using Runtime.Viewport.Roads;
using ZoneReborn.World.Actors;

namespace Runtime.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private ActorDocument? _actorDocument;

        private Guid? _actorSelection;

        private Guid? _lootLocationSelection;

        private ActorItemCatalog? _lootItemCatalog;

        private bool _lootPreviewEnabled;

        private ObjectGizmoMode _actorGizmoMode;

        private ObjectGizmoAxis _actorGizmoAxis;

        private bool _actorGizmoEnabled;

        public void SetActors(
            ActorDocument? document,
            Guid? selectedActor,
            Guid? selectedLocation,
            ActorItemCatalog? itemCatalog,
            bool lootPreviewEnabled)
        {
            _actorDocument = document;
            _actorSelection = selectedActor;
            _lootLocationSelection = selectedLocation;
            _lootItemCatalog = itemCatalog;
            _lootPreviewEnabled = lootPreviewEnabled;
        }

        public void SetActorGizmo(
            ObjectGizmoMode mode,
            ObjectGizmoAxis axis,
            bool enabled)
        {
            _actorGizmoMode = mode;
            _actorGizmoAxis = axis;
            _actorGizmoEnabled = enabled;
        }

        private void RenderActorOverlay(
            FreeCamera camera,
            Matrix4x4 viewProjection)
        {
            if (_actorDocument == null ||
                _actorDocument.Actors.Count == 0)
            {
                return;
            }

            _roadOverlay ??= new RoadOverlayRenderer(
                _device,
                _context,
                _vertexShader,
                _pixelShader,
                _inputLayout);

            _roadOverlay.RenderActors(
                _actorDocument,
                _actorSelection,
                viewProjection,
                _npcRenderers.ContainsKey,
                _lootLocationSelection);

            _roadOverlay.RenderLootLabels(
                _actorDocument,
                _actorSelection,
                viewProjection,
                camera.Position,
                _width,
                _height,
                _lootLocationSelection);
        }

        private void RenderActorGizmo(
            FreeCamera camera,
            Matrix4x4 viewProjection)
        {
            if (!_actorGizmoEnabled ||
                _actorDocument == null ||
                _actorSelection is not Guid id ||
                _actorDocument.Find(id) is not { } actor)
            {
                return;
            }

            Vector3 position = actor.Position;

            if (actor.Kind == ActorKind.LootSpawn &&
                _lootLocationSelection is Guid locationId)
            {
                LootSpawnLocation? location =
                    actor.LootLocations.FirstOrDefault(
                        item => item.Id == locationId);

                if (location != null)
                {
                    position = location.GetWorldPosition(actor);
                }
            }

            if (!ObjectGizmoGeometry.TryCreateFrame(
                    position,
                    camera,
                    _width,
                    _height,
                    out var frame,
                    ObjectGizmoFarPlane))
            {
                return;
            }

            _objectGizmo?.Render(
                frame,
                _actorGizmoMode,
                _actorGizmoAxis,
                viewProjection,
                ObjectGizmoAxis.Y);
        }
    }
}
