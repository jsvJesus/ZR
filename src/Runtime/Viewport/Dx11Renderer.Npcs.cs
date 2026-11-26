using System.Diagnostics;
using System.Numerics;
using Runtime.Viewport.Characters;
using ZoneReborn.World.Actors;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters;

namespace Runtime.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private const int MaximumNpcPreviews = 12;

        private const float NpcPreviewDistance = 180f;

        private readonly Dictionary<Guid, NpcPreviewEntry>
            _npcRenderers = new();

        private readonly Dictionary<NpcAppearancePreset, CharacterAsset>
            _npcAssets = new();

        private readonly HashSet<NpcAppearancePreset>
            _npcFailedPresets = [];

        private ActorDocument? _npcDocument;
        private AssetPaths? _npcAssetPaths;

        private CancellationTokenSource? _npcCancellation;

        private Task<NpcPreviewLoadResult>? _npcLoading;

        private long _npcStartTime;

        private sealed record NpcPreviewEntry(
            NpcAppearancePreset Preset,
            CharacterRenderer Renderer);

        private sealed record NpcPreviewLoadResult(
            NpcAppearancePreset Preset,
            CharacterAsset? Asset,
            string? Error);

        private void SetNpcLevel(
            ActorDocument? document,
            AssetPaths? assets)
        {
            DisposeNpcPreviews();

            _npcDocument = document;
            _npcAssetPaths = assets;

            if (document == null || assets == null)
            {
                return;
            }

            _npcCancellation = new CancellationTokenSource();

            _npcStartTime = Stopwatch.GetTimestamp();
        }

        private void UpdateNpcPreviews(
            Vector3 cameraPosition,
            float farPlane)
        {
            if (_npcDocument == null ||
                _npcAssetPaths == null ||
                _npcCancellation == null)
            {
                return;
            }

            CompleteNpcLoading();

            float distanceLimit = Math.Min(
                NpcPreviewDistance,
                farPlane);

            float distanceSquared =
                distanceLimit * distanceLimit;

            LevelActor[] visibleActors = _npcDocument.Actors
                .Where(actor =>
                    actor.Kind == ActorKind.NpcSpawn &&
                    actor.Enabled &&
                    !string.IsNullOrWhiteSpace(actor.TemplateId) &&
                    Vector3.DistanceSquared(
                        actor.Position,
                        cameraPosition) <= distanceSquared)
                .OrderBy(actor => actor.Id != _actorSelection)
                .ThenBy(actor => Vector3.DistanceSquared(
                    actor.Position,
                    cameraPosition))
                .Take(MaximumNpcPreviews)
                .ToArray();

            var visibleIds = visibleActors.ToDictionary(
                actor => actor.Id);

            foreach (Guid id in _npcRenderers.Keys.ToArray())
            {
                NpcPreviewEntry preview = _npcRenderers[id];

                if (visibleIds.TryGetValue(
                        id,
                        out LevelActor? actor) &&
                    actor.NpcAppearance == preview.Preset)
                {
                    continue;
                }

                preview.Renderer.Dispose();

                _npcRenderers.Remove(id);
            }

            double animationTime = Stopwatch
                .GetElapsedTime(_npcStartTime)
                .TotalSeconds;

            bool createdThisFrame = false;

            foreach (LevelActor actor in visibleActors)
            {
                NpcAppearancePreset preset =
                    actor.NpcAppearance;

                if (!_npcRenderers.TryGetValue(
                        actor.Id,
                        out NpcPreviewEntry? preview))
                {
                    if (_npcFailedPresets.Contains(preset))
                    {
                        continue;
                    }

                    if (!_npcAssets.TryGetValue(
                            preset,
                            out CharacterAsset? asset))
                    {
                        BeginNpcLoading(preset);

                        continue;
                    }

                    if (createdThisFrame)
                    {
                        continue;
                    }

                    createdThisFrame = true;

                    try
                    {
                        var renderer = new CharacterRenderer(
                            _device,
                            _context,
                            _npcAssetPaths,
                            asset);

                        preview = new NpcPreviewEntry(
                            preset,
                            renderer);

                        _npcRenderers.Add(
                            actor.Id,
                            preview);
                    }
                    catch (Exception exception)
                    {
                        _npcFailedPresets.Add(preset);

                        Trace.TraceWarning(
                            $"NPC renderer {actor.TemplateId}: " +
                            exception.Message);

                        continue;
                    }
                }

                double phase =
                    (actor.Id.GetHashCode() & 0xFFFF) /
                    65535.0 * 5.0;

                preview.Renderer.UpdateNpcPose(
                    actor.Position,
                    actor.Yaw,
                    animationTime + phase);
            }
        }

        private void BeginNpcLoading(
            NpcAppearancePreset preset)
        {
            if (_npcLoading != null ||
                _npcAssetPaths == null ||
                _npcCancellation == null ||
                _npcAssets.ContainsKey(preset) ||
                _npcFailedPresets.Contains(preset))
            {
                return;
            }

            AssetPaths assets = _npcAssetPaths;

            CancellationToken token =
                _npcCancellation.Token;

            _npcLoading = Task.Run(() =>
            {
                try
                {
                    CharacterAsset asset = NpcAppearance.Load(
                        assets,
                        preset,
                        token);

                    return new NpcPreviewLoadResult(
                        preset,
                        asset,
                        null);
                }
                catch (OperationCanceledException)
                {
                    return new NpcPreviewLoadResult(
                        preset,
                        null,
                        null);
                }
                catch (Exception exception)
                {
                    return new NpcPreviewLoadResult(
                        preset,
                        null,
                        exception.Message);
                }
            });
        }

        private void CompleteNpcLoading()
        {
            if (_npcLoading == null ||
                !_npcLoading.IsCompleted)
            {
                return;
            }

            NpcPreviewLoadResult result =
                _npcLoading.GetAwaiter().GetResult();

            _npcLoading = null;

            if (result.Asset != null)
            {
                _npcAssets[result.Preset] =
                    result.Asset;

                return;
            }

            if (result.Error != null)
            {
                _npcFailedPresets.Add(result.Preset);

                Trace.TraceWarning(
                    $"NPC asset {result.Preset}: " +
                    result.Error);
            }
        }

        private void RenderNpcPreviews(
            Matrix4x4 relativeViewProjection,
            Vector3 cameraPosition)
        {
            foreach (NpcPreviewEntry preview in
                     _npcRenderers.Values)
            {
                preview.Renderer.Render(
                    relativeViewProjection,
                    cameraPosition,
                    0f);
            }
        }

        private void DisposeNpcPreviews()
        {
            _npcCancellation?.Cancel();
            _npcCancellation?.Dispose();

            _npcCancellation = null;
            _npcLoading = null;

            foreach (NpcPreviewEntry preview in
                     _npcRenderers.Values)
            {
                preview.Renderer.Dispose();
            }

            _npcRenderers.Clear();
            _npcAssets.Clear();
            _npcFailedPresets.Clear();

            _npcDocument = null;
            _npcAssetPaths = null;
        }
    }
}
