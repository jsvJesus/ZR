using System.Numerics;
using Editor.Viewport.Roads;
using Editor.Viewport.Scene;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Terrain;

namespace Editor.Viewport;

public sealed partial class Dx11Renderer
{
    private RoadOverlayRenderer? _roadOverlay;
    private RoadNetwork? _roadNetwork;
    private TerrainSurface? _roadSurface;

    private Guid? _roadSelection;
    private int _roadPointSelection = -1;

    private LoadedLevel? _roadLevel;
    private RoadCatalog? _roadCatalog;
    private SceneData? _roadScene;
    private SceneRenderer? _roadModelRenderer;

    private long _roadBuiltRevision = -1;
    private long _roadBuiltHeightRevision = -1;

    public void SetRoads(
        RoadNetwork? network,
        Guid? selectedRoad,
        int selectedPoint)
    {
        _roadNetwork = network;
        _roadSelection = selectedRoad;
        _roadPointSelection = selectedPoint;
    }

    private void SetRoadSurface(LoadedLevel? level)
    {
        _roadModelRenderer?.Dispose();
        _roadModelRenderer = null;
        _roadScene = null;

        _roadLevel = level;

        _roadSurface = level == null
            ? null
            : new TerrainSurface(level);

        _roadNetwork = null;
        _roadSelection = null;
        _roadPointSelection = -1;

        _roadBuiltRevision = -1;
        _roadBuiltHeightRevision = -1;

        _roadCatalog = null;

        if (level == null)
        {
            return;
        }

        var assets = AssetPaths.FromApplication();

        _roadCatalog = RoadCatalog.Scan(assets);

        _roadScene = new SceneData(
            assets,
            [],
            sourceName: level.Manifest.Name);

        _roadModelRenderer = new SceneRenderer(
            _device,
            _context,
            assets,
            _roadScene);

        _roadModelRenderer.SetTransientInstances([]);
    }

    private void UpdateRoadModels()
    {
        if (_roadLevel == null ||
            _roadSurface == null ||
            _roadCatalog == null ||
            _roadModelRenderer == null)
        {
            return;
        }

        RoadNetwork network = _roadLevel.Roads;

        long revision = network.Revision;
        long heightRevision =
            _roadLevel.TerrainDocument?.HeightRevision ?? 0;

        if (_roadBuiltRevision == revision &&
            _roadBuiltHeightRevision == heightRevision)
        {
            return;
        }

        try
        {
            var instances = RoadBuilder.Build(
                network,
                _roadCatalog,
                _roadSurface);

            _roadModelRenderer.SetTransientInstances(instances);
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
            InvalidDataException or
            ArgumentException or
            OverflowException)
        {
            _roadModelRenderer.SetTransientInstances([]);

            _roadScene?.Warnings.Enqueue(
                $"Построение дороги: {exception.Message}");
        }

        _roadBuiltRevision = revision;
        _roadBuiltHeightRevision = heightRevision;
    }

    private void RenderRoadModels(
        FreeCamera camera,
        Matrix4x4 viewProjection,
        float farPlane)
    {
        UpdateRoadModels();

        _roadModelRenderer?.Render(
            camera,
            viewProjection,
            farPlane);
    }

    private void RenderRoadTransparency(
        FreeCamera camera,
        Matrix4x4 viewProjection)
    {
        _roadModelRenderer?.RenderTransparent(
            camera,
            viewProjection);
    }

    private void RenderRoadOverlay(Matrix4x4 viewProjection)
    {
        if (_roadNetwork == null || _roadSurface == null)
        {
            return;
        }

        _roadOverlay ??= new RoadOverlayRenderer(
            _device,
            _context,
            _vertexShader,
            _pixelShader,
            _inputLayout);

        _roadOverlay.Render(
            _roadNetwork,
            _roadSurface,
            _roadSelection,
            _roadPointSelection,
            viewProjection);
    }

    private void DisposeRoadModels()
    {
        _roadModelRenderer?.Dispose();
        _roadModelRenderer = null;
        _roadScene = null;
    }
}
