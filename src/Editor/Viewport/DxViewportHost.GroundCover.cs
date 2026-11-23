using System.Numerics;

namespace Editor.Viewport;

public sealed partial class DxViewportHost
{
    private bool _groundCoverBrushEnabled;
    private bool _groundCoverPainting;
    private Vector3? _lastGroundCoverPaint;

    public bool GroundCoverBrushEnabled
    {
        get => _groundCoverBrushEnabled;
        set
        {
            if (!value) EndGroundCoverPaint();
            _groundCoverBrushEnabled = value;
        }
    }

    public bool GroundCoverBrushErase { get; set; }
    public float GroundCoverBrushRadius { get; set; } = 5f;
    public event Action<Vector3, bool>? GroundCoverBrushRequested;
    public event Action? GroundCoverStrokeStarted;
    public event Action? GroundCoverStrokeEnded;

    private void BeginGroundCoverPaint(nint window, int x, int y)
    {
        if (!GroundCoverBrushEnabled || _rotating || IsPlaying || IsStarting ||
            _level?.GroundCover.CanSave != true) return;
        try
        {
            _groundCoverPainting = true;
            _lastGroundCoverPaint = null;
            SetCapture(window);
            GroundCoverStrokeStarted?.Invoke();
            PaintGroundCover(x, y);
        }
        catch (Exception error)
        {
            EndGroundCoverPaint();
            RendererError?.Invoke(error.Message);
        }
    }

    private void PaintGroundCover(int x, int y)
    {
        if (!_groundCoverPainting || !GroundCoverBrushEnabled || IsPlaying || IsStarting ||
            _rotating || _renderer == null || !TryObjectRay(x, y, out var direction)) return;
        try
        {
            if (!_renderer.PickTerrain(_camera.Position, direction, out var point))
            {
                _lastGroundCoverPaint = null;
                return;
            }

            bool erase = GroundCoverBrushErase || (GetKeyState(KeyShift) & 0x8000) != 0;
            float spacing = Math.Clamp(GroundCoverBrushRadius * 0.2f, 0.25f, 2f);
            if (_lastGroundCoverPaint is { } last)
            {
                float distance = Vector2.Distance(new Vector2(last.X, last.Z), new Vector2(point.X, point.Z));
                if (distance < spacing) return;
                int steps = (int)MathF.Floor(distance / spacing);
                if (steps <= 128)
                {
                    for (int step = 1; step <= steps; step++)
                        GroundCoverBrushRequested?.Invoke(Vector3.Lerp(last, point, step * spacing / distance), erase);
                    _lastGroundCoverPaint = Vector3.Lerp(last, point, steps * spacing / distance);
                    return;
                }
            }

            GroundCoverBrushRequested?.Invoke(point, erase);
            _lastGroundCoverPaint = point;
        }
        catch (Exception error)
        {
            EndGroundCoverPaint();
            RendererError?.Invoke(error.Message);
        }
    }

    public void EndGroundCoverPaint()
    {
        if (!_groundCoverPainting) return;
        _groundCoverPainting = false;
        _lastGroundCoverPaint = null;
        try { GroundCoverStrokeEnded?.Invoke(); }
        finally { ReleaseCapture(); }
    }
}
