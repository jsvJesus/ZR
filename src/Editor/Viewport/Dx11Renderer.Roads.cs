using System.Numerics;
using Editor.Viewport.Roads;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Terrain;

namespace Editor.Viewport;

public sealed partial class Dx11Renderer
{
    private RoadOverlayRenderer? _roadOverlay;
    private RoadNetwork? _roadNetwork;
    private TerrainSurface? _roadSurface;

    private Guid? _roadSelection;
    private int _roadPointSelection = -1;

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
        _roadSurface = level == null
            ? null
            : new TerrainSurface(level);

        _roadNetwork = null;
        _roadSelection = null;
        _roadPointSelection = -1;
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
}
