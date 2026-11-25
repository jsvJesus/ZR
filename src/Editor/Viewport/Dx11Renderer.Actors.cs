using System.Numerics;
using Editor.Viewport.Roads;
using ZoneReborn.World.Actors;

namespace Editor.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private ActorDocument? _actorDocument;
        private Guid? _actorSelection;

        public void SetActors(
            ActorDocument? document,
            Guid? selectedActor)
        {
            _actorDocument = document;
            _actorSelection = selectedActor;
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
                viewProjection);
        }
    }
}
