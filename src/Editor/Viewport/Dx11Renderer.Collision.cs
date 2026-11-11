using System.Numerics;
using Editor.Viewport.Scene;
using ZoneReborn.World.Scene;

namespace Editor.Viewport;

public sealed partial class Dx11Renderer
{
    private SceneCollisionOverlayRenderer? _collisionOverlay;
    private SceneData? _collisionRequestedScene;
    private string? _collisionRequestedModel;

    public bool ShowSelectedCollision { get; set; }

    private void RenderSelectedCollision(
        SceneInstance selected,
        Matrix4x4 viewProjection)
    {
        if (!ShowSelectedCollision ||
            _scene == null ||
            selected.IsEnvironmentCollision)
        {
            return;
        }

        var scene = _scene;

        if (!scene.Physics.TryGet(
                selected.Model,
                out ScenePhysicsMesh? physics))
        {
            if (!ReferenceEquals(
                    _collisionRequestedScene,
                    scene) ||
                !string.Equals(
                    _collisionRequestedModel,
                    selected.Model,
                    StringComparison.OrdinalIgnoreCase))
            {
                _collisionRequestedScene = scene;
                _collisionRequestedModel = selected.Model;

                string model = selected.Model;

                _ = Task.Run(() =>
                {
                    try
                    {
                        scene.Physics.Get(model);
                    }
                    catch (Exception exception)
                    {
                        scene.Warnings.Enqueue(
                            $"Collision preview {model}: " +
                            exception.Message
                        );
                    }
                });
            }

            return;
        }

        if (physics == null)
        {
            return;
        }

        _collisionOverlay ??= new SceneCollisionOverlayRenderer(
            _device,
            _context,
            _vertexShader,
            _pixelShader,
            _inputLayout,
            message => scene.Warnings.Enqueue(message)
        );

        _collisionOverlay.Render(
            physics,
            selected.World,
            viewProjection
        );
    }
}
