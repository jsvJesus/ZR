using System.Numerics;
using System.Diagnostics;
using Editor.Viewport.Scene;
using ZoneReborn.World.Actors;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Scene;

namespace Editor.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private const int MaximumLootPreviews = 128;

        private AssetPaths? _lootPreviewAssets;

        private SceneData? _lootPreviewScene;

        private SceneRenderer? _lootPreviewRenderer;

        private bool _lootPreviewVisible;

        private void SetLootPreviewLevel(AssetPaths? assets)
        {
            DisposeLootPreview();

            _lootPreviewAssets = assets;
        }

        private void RenderLootPreview(
            FreeCamera camera,
            Matrix4x4 viewProjection,
            float farPlane)
        {
            _lootPreviewVisible = false;

            if (!_lootPreviewEnabled ||
                _lootPreviewAssets == null ||
                _lootItemCatalog == null ||
                _actorDocument == null ||
                _actorSelection is not Guid id ||
                _actorDocument.Find(id) is not { } actor ||
                actor.Kind != ActorKind.LootSpawn ||
                actor.Loot.Count == 0 ||
                actor.LootLocations.Count == 0)
            {
                return;
            }

            _lootPreviewScene ??= new SceneData(
                _lootPreviewAssets,
                []);

            while (_lootPreviewScene.Warnings.TryDequeue(
                       out string? warning))
            {
                Trace.TraceWarning(
                    $"LootSpawn preview: {warning}");
            }

            var instances = new List<SceneInstance>();

            int count = Math.Min(
                actor.LootLocations.Count,
                MaximumLootPreviews);

            for (int index = 0; index < count; index++)
            {
                LootSpawnLocation location =
                    actor.LootLocations[index];

                Vector3 position =
                    location.GetWorldPosition(actor);

                if (Vector3.DistanceSquared(
                        position,
                        camera.Position) > 14400f)
                {
                    continue;
                }

                LootSpawnEntry entry =
                    actor.Loot[index % actor.Loot.Count];

                ActorItemDefinition? item =
                    _lootItemCatalog.Items.FirstOrDefault(
                        definition =>
                            definition.TypeId == entry.TypeId &&
                            definition.Category == entry.Category);

                if (item == null ||
                    string.IsNullOrWhiteSpace(item.ModelPath))
                {
                    continue;
                }

                string modelPath = item.ModelPath;

                if (!_lootPreviewScene.Models.TryGet(
                        modelPath,
                        out SceneModel geometry))
                {
                    _lootPreviewScene.Models.Request(modelPath);

                    continue;
                }

                Matrix4x4 rotation =
                    Matrix4x4.CreateRotationY(actor.Yaw);

                SceneBounds rotatedBounds =
                    geometry.Bounds.Transform(rotation);

                position.Y -= rotatedBounds.Minimum.Y;

                Matrix4x4 world =
                    rotation *
                    Matrix4x4.CreateTranslation(position);

                if (!Matrix4x4.Invert(
                        world,
                        out Matrix4x4 inverse))
                {
                    continue;
                }

                var instance = new SceneInstance(
                    modelPath,
                    world,
                    inverse,
                    geometry.Bounds.Transform(world),
                    false)
                {
                    Id = $"loot-preview/{location.Id:N}",
                    LocalBounds = geometry.Bounds,
                    VisualLocalBounds = geometry.Bounds,
                    FollowTerrain = false
                };

                instances.Add(instance);
            }

            if (instances.Count == 0)
            {
                return;
            }

            _lootPreviewRenderer ??= new SceneRenderer(
                _device,
                _context,
                _lootPreviewAssets,
                _lootPreviewScene);

            _lootPreviewRenderer.SetTransientInstances(
                instances);

            _lootPreviewRenderer.Render(
                camera,
                viewProjection,
                farPlane);

            _lootPreviewVisible = true;
        }

        private void RenderLootPreviewTransparency(
            FreeCamera camera,
            Matrix4x4 viewProjection)
        {
            if (!_lootPreviewVisible)
            {
                return;
            }

            _lootPreviewRenderer?.RenderTransparent(
                camera,
                viewProjection);
        }

        private void DisposeLootPreview()
        {
            _lootPreviewRenderer?.Dispose();

            _lootPreviewRenderer = null;
            _lootPreviewScene = null;
            _lootPreviewAssets = null;
            _lootPreviewVisible = false;
        }
    }
}
