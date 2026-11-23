using System.Numerics;
using Editor.Viewport.Scene;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Vegetation;

namespace Editor.Viewport;

public sealed partial class Dx11Renderer
{
    private LoadedLevel? _groundCoverLevel;
    private AssetPaths? _groundCoverAssets;
    private SceneData? _groundCoverScene;
    private SceneRenderer? _groundCoverRenderer;
    private (int X, int Z) _groundCoverChunk;
    private (long Cover, long Terrain, long Roads, long Objects) _groundCoverRevisions;
    private bool _groundCoverBuilt;
    private bool _groundCoverWasObjectPreview;
    private float _groundCoverCapacityDistance;

    private void SetGroundCover(LoadedLevel? level, AssetPaths? assets)
    {
        _groundCoverRenderer?.Dispose();
        _groundCoverRenderer = null;
        _groundCoverScene = null;
        _groundCoverLevel = level;
        _groundCoverAssets = assets;
        _groundCoverBuilt = false;
        _groundCoverWasObjectPreview = false;
    }

    private void RenderGroundCover(FreeCamera camera, Matrix4x4 viewProjection)
    {
        if (_groundCoverLevel is not { } level || _groundCoverAssets is not { } assets ||
            !level.GroundCover.CanSave || level.GroundCover.Settings.Models.Length == 0)
        {
            return;
        }

        _groundCoverScene ??= new SceneData(assets, [], sourceName: level.Manifest.Name);
        _groundCoverRenderer ??= new SceneRenderer(_device, _context, assets, _groundCoverScene)
        {
            ForceCutout = true
        };

        float distance = level.GroundCover.Settings.DrawDistance;
        var chunk = ((int)MathF.Floor(camera.Position.X / 8f), (int)MathF.Floor(camera.Position.Z / 8f));
        var revisions = (level.GroundCover.Revision, level.TerrainDocument?.Revision ?? 0,
            level.Roads.Revision, level.Objects?.Revision ?? 0);
        bool objectPreview = level.Objects?.HasTransformPreview == true;
        if (!_groundCoverBuilt || chunk != _groundCoverChunk || revisions != _groundCoverRevisions ||
            objectPreview || objectPreview != _groundCoverWasObjectPreview)
        {
            var center = new Vector3(chunk.Item1 * 8f + 4f, camera.Position.Y, chunk.Item2 * 8f + 4f);
            var instances = GroundCoverGenerator.BuildVisible(level, assets, center, distance + 8f);
            _groundCoverCapacityDistance = distance;
            if (instances.Count >= GroundCoverGenerator.MaximumVisibleInstances)
            {
                float radiusSquared = 0f;
                foreach (var instance in instances)
                {
                    float dx = instance.World.M41 - center.X;
                    float dz = instance.World.M43 - center.Z;
                    radiusSquared = MathF.Max(radiusSquared, dx * dx + dz * dz);
                }
                // Fade inside the supported area when dense settings reach the
                // instance budget; retain padding for every position in this chunk.
                _groundCoverCapacityDistance = MathF.Max(1f, MathF.Sqrt(radiusSquared) - 8f);
            }
            _groundCoverRenderer.SetTransientInstances(instances);
            _groundCoverChunk = chunk;
            _groundCoverRevisions = revisions;
            _groundCoverBuilt = true;
            _groundCoverWasObjectPreview = objectPreview;
        }

        float visibleDistance = MathF.Min(distance, _groundCoverCapacityDistance);
        _groundCoverRenderer.DistanceFadeEnd = visibleDistance;
        _groundCoverRenderer.Render(camera, viewProjection, visibleDistance);
        // Background model/material and renderer errors belong to the cover panel,
        // rather than an otherwise inaccessible transient scene. Do not dirty the level.
        while (_groundCoverScene.Warnings.TryDequeue(out string? warning))
        {
            string message = $"Покрытие: {warning}";
            if (level.GroundCover.Warnings.Count < 256 && !level.GroundCover.Warnings.Contains(message))
                level.GroundCover.Warnings.Add(message);
        }
    }
}
