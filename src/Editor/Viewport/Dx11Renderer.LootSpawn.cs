using System.Diagnostics;
using System.Numerics;
using Editor.Viewport.Scene;
using ZoneReborn.World.Actors;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Scene;

namespace Editor.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private const int MaximumLootPreviews = 256;

        private const float LootPreviewDistance = 120f;

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
                _actorDocument == null)
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

            float distance = Math.Min(
                LootPreviewDistance,
                farPlane);

            float distanceSquared =
                distance * distance;

            var candidates = _actorDocument.Actors
                .Where(actor =>
                    actor.Kind == ActorKind.LootSpawn &&
                    actor.Enabled)
                .SelectMany(actor => actor.LootLocations
                    .Where(location =>
                        location.Loot?.Count > 0)
                    .Select(location => new
                    {
                        Actor = actor,
                        Location = location,
                        Position = location.GetWorldPosition(actor)
                    }))
                .Where(candidate =>
                    Vector3.DistanceSquared(
                        candidate.Position,
                        camera.Position) <= distanceSquared)
                .OrderBy(candidate =>
                    candidate.Actor.Id != _actorSelection)
                .ThenBy(candidate =>
                    Vector3.DistanceSquared(
                        candidate.Position,
                        camera.Position))
                .Take(MaximumLootPreviews);

            var instances = new List<SceneInstance>();

            foreach (var candidate in candidates)
            {
                LevelActor actor =
                    candidate.Actor;

                LootSpawnLocation location =
                    candidate.Location;

                LootSpawnEntry? entry =
                    location.Loot?.FirstOrDefault();

                if (entry == null)
                {
                    continue;
                }

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

                Vector3 position =
                    candidate.Position;

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
                    Id = $"loot-preview/{actor.Id:N}/{location.Id:N}",
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
