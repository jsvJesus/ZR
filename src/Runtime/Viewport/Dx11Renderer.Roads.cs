using System.Numerics;
using Runtime.Viewport.Roads;
using Runtime.Viewport.Scene;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Terrain;

namespace Runtime.Viewport;

public sealed partial class Dx11Renderer
{
    private RoadOverlayRenderer? _roadOverlay;
    private RoadNetwork? _roadNetwork;
    private TerrainSurface? _roadSurface;

    private Guid? _roadSelection;
    private int _roadPointSelection = -1;

    private LoadedLevel? _roadLevel;
    private RoadCatalog? _roadCatalog;
    private RoadMaterialCatalog? _roadMaterials;
    private SceneData? _roadScene;
    private SceneRenderer? _roadModelRenderer;

    private long _roadBuiltRevision = -1;
    private long _roadBuiltHeightRevision = -1;

    private readonly HashSet<string> _generatedRoadFiles =
        new(StringComparer.OrdinalIgnoreCase);

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
        DisposeRoadModels();

        _roadLevel = level;

        _roadSurface = level == null
            ? null
            : new TerrainSurface(level);

        _roadNetwork = null;
        _roadSelection = null;
        _roadPointSelection = -1;

        _roadBuiltRevision = -1;
        _roadBuiltHeightRevision = -1;

        _generatedRoadFiles.Clear();

        _roadCatalog = null;
        _roadMaterials = null;

        if (level == null)
        {
            return;
        }

        AssetPaths assets = AssetPaths.FromApplication();

        _roadCatalog = RoadCatalog.Scan(assets);
        _roadMaterials = RoadMaterialCatalog.Scan(assets);

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
            _roadMaterials == null ||
            _roadScene == null ||
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
            RoadBuildResult railways = RoadBuilder.Build(
                network,
                _roadCatalog,
                _roadSurface,
                _roadScene.Models);

            RoadBuildResult surfaces = RoadSurfaceBuilder.Build(
                network,
                _roadMaterials,
                _roadSurface);

            RoadBuildResult structures = RoadStructureBuilder.Build(
                network,
                _roadScene.Models,
                _roadSurface);

            var models = railways.Models
                .Concat(surfaces.Models)
                .ToArray();

            var instances = surfaces.Instances
                .Concat(railways.Instances)
                .Concat(structures.Instances)
                .ToArray();

            foreach (string file in _generatedRoadFiles)
            {
                _roadModelRenderer.InvalidateModel(file);
            }

            _generatedRoadFiles.Clear();

            foreach (SceneModel model in models)
            {
                _roadScene.Models.Store(model);
                _generatedRoadFiles.Add(model.File);
            }

            _roadModelRenderer.SetTransientInstances(instances);

            _roadBuiltRevision = revision;
            _roadBuiltHeightRevision = heightRevision;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
            InvalidDataException or
            ArgumentException or
            OverflowException)
        {
            _roadScene.Warnings.Enqueue(
                $"Построение дорог: {exception.Message}");
        }
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
            _roadCatalog,
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
        _generatedRoadFiles.Clear();
    }
}
