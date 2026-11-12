using System.Numerics;

namespace Editor.Viewport.Terrain;

internal sealed partial class TerrainRenderer
{
    private TerrainWaterSurfaceRenderer? _waterSurface;

    public void RenderWater(
        Matrix4x4 viewProjection,
        Vector3 cameraPosition,
        Vector3 sunDirection,
        float time)
    {
        var document = _level.TerrainDocument;

        if (document == null || document.WaterBodies.Count == 0)
        {
            return;
        }

        _waterSurface ??= new TerrainWaterSurfaceRenderer(
            _device,
            _context,
            _assets);

        _waterSurface.Render(
            document.WaterBodies,
            viewProjection,
            cameraPosition,
            sunDirection,
            time);
    }
}
