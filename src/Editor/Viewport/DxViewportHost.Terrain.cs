using System.Numerics;

namespace Editor.Viewport;

public sealed partial class DxViewportHost
{
    public bool TerrainBrushEnabled { get; set; }
    public bool TerrainBrushFill { get; set; }
    public bool TerrainBrushContinuous { get; set; }
    public float TerrainBrushSpacing { get; set; } = 1;
    private bool _terrainPainting;
    private Vector3? _lastTerrainPaint;
    public event Action<Vector3>? TerrainBrushRequested;
    public event Action? TerrainStrokeStarted;
    public event Action? TerrainStrokeEnded;

    private void BeginTerrainPaint(nint window, int x, int y)
    {
        if (_rotating || IsPlaying || IsStarting) return;
        if (TerrainBrushContinuous)
        {
            _terrainPainting = true; _lastTerrainPaint = null; SetCapture(window); TerrainStrokeStarted?.Invoke();
        }
        PaintTerrain(x, y);
    }

    public void EndTerrainPaint()
    {
        if (!_terrainPainting) return;
        _terrainPainting = false; _lastTerrainPaint = null;
        TerrainStrokeEnded?.Invoke(); ReleaseCapture();
    }

    private void PaintTerrain(int x, int y)
    {
        if (!TerrainBrushEnabled || IsPlaying || IsStarting || _rotating || _renderer == null || !TryObjectRay(x, y, out var ray)) return;
        if (!_renderer.PickTerrain(_camera.Position, ray, out var point, TerrainBrushFill)) { _lastTerrainPaint = null; return; }
        float spacing = Math.Max(0.05f, TerrainBrushSpacing);
        if (_terrainPainting && _lastTerrainPaint is { } last)
        {
            float distance = Vector2.Distance(new(last.X, last.Z), new(point.X, point.Z));
            if (distance < spacing) return;
            int steps = (int)MathF.Floor(distance / spacing);
            // A pointer jump must not generate an unbounded number of edits in one native message.
            if (steps > 64) { _lastTerrainPaint = point; TerrainBrushRequested?.Invoke(point); return; }
            for (int step = 1; step <= steps; step++) TerrainBrushRequested?.Invoke(Vector3.Lerp(last, point, step * spacing / distance));
            _lastTerrainPaint = Vector3.Lerp(last, point, steps * spacing / distance);
        }
        else { TerrainBrushRequested?.Invoke(point); if (_terrainPainting) _lastTerrainPaint = point; }
    }
}
