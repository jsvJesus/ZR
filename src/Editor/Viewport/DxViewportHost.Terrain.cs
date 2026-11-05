using System.Numerics;

namespace Editor.Viewport;

public sealed partial class DxViewportHost
{
    public bool TerrainBrushEnabled { get; set; }
    public bool TerrainBrushFill { get; set; }
    public event Action<Vector3>? TerrainBrushRequested;

    private void PaintTerrain(int x, int y)
    {
        if (!TerrainBrushEnabled || IsPlaying || IsStarting || _rotating || _renderer == null || !TryObjectRay(x, y, out var ray)) return;
        if (_renderer.PickTerrain(_camera.Position, ray, out var point, TerrainBrushFill)) TerrainBrushRequested?.Invoke(point);
    }
}
