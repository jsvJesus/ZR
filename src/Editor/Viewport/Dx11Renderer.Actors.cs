using System.Numerics;
using Editor.Viewport.Roads;
using ZoneReborn.World.Actors;

namespace Editor.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private ActorDocument? _actorDocument;

        private Guid? _actorSelection;

        private ObjectGizmoMode _actorGizmoMode;

        private ObjectGizmoAxis _actorGizmoAxis;

        private bool _actorGizmoEnabled;

        public void SetActors(
            ActorDocument? document,
            Guid? selectedActor)
        {
            _actorDocument = document;
            _actorSelection = selectedActor;
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
                _npcRenderers.ContainsKey);
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

            if (!ObjectGizmoGeometry.TryCreateFrame(
                    actor.Position,
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
