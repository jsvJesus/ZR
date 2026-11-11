using System.Numerics;
using Editor.Viewport.Terrain;

namespace Editor.Viewport;

public sealed partial class Dx11Renderer
{
    private TerrainBrushOverlayRenderer? _terrainBrushOverlay;
    private Vector3? _terrainBrushPosition;

    private float _terrainBrushRadius = 5f;
    private bool _terrainBrushMaterial;
    private bool _terrainBrushIncludeHoles;

    public void SetTerrainBrushPreview(
        Vector3? position,
        float radius,
        bool material,
        bool includeHoles)
    {
        _terrainBrushPosition = position;
        _terrainBrushRadius = radius;
        _terrainBrushMaterial = material;
        _terrainBrushIncludeHoles = includeHoles;
    }

    private void RenderTerrainBrushPreview(
        Matrix4x4 viewProjection)
    {
        if (_terrain == null ||
            _terrainBrushPosition is not { } position ||
            _terrainBrushRadius <= 0)
        {
            return;
        }

        _terrainBrushOverlay ??= new TerrainBrushOverlayRenderer(
            _device,
            _context,
            _vertexShader,
            _pixelShader,
            _inputLayout
        );

        _terrainBrushOverlay.Render(
            _terrain,
            position,
            _terrainBrushRadius,
            _terrainBrushMaterial,
            _terrainBrushIncludeHoles,
            viewProjection
        );
    }
}
