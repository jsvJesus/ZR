using System.Numerics;
using System.Runtime.InteropServices;

namespace Editor.Viewport;

public sealed partial class DxViewportHost
{
    public bool TerrainBrushEnabled { get; set; }
    public bool TerrainBrushFill { get; set; }
    public bool TerrainBrushContinuous { get; set; }
    public bool TerrainBrushIsMaterial { get; set; }

    public float TerrainBrushRadius { get; set; } = 5f;
    public float TerrainBrushSpacing { get; set; } = 1f;

    private bool _terrainPainting;
    private Vector3? _lastTerrainPaint;

    public event Action<Vector3>? TerrainBrushRequested;
    public event Action? TerrainStrokeStarted;
    public event Action? TerrainStrokeEnded;

    private void BeginTerrainPaint(
        nint window,
        int x,
        int y)
    {
        if (_rotating ||
            IsPlaying ||
            IsStarting ||
            !TerrainBrushEnabled)
        {
            return;
        }

        if (TerrainBrushContinuous)
        {
            _terrainPainting = true;
            _lastTerrainPaint = null;

            SetCapture(window);
            TerrainStrokeStarted?.Invoke();
        }

        PaintTerrain(x, y);
    }

    public void EndTerrainPaint()
    {
        if (!_terrainPainting)
        {
            return;
        }

        _terrainPainting = false;
        _lastTerrainPaint = null;

        TerrainStrokeEnded?.Invoke();
        ReleaseCapture();
    }

    private void PaintTerrain(
        int x,
        int y)
    {
        if (!TerrainBrushEnabled ||
            IsPlaying ||
            IsStarting ||
            _rotating ||
            _renderer == null ||
            !TryObjectRay(x, y, out Vector3 direction))
        {
            return;
        }

        if (!_renderer.PickTerrain(
                _camera.Position,
                direction,
                out Vector3 point,
                TerrainBrushFill))
        {
            _lastTerrainPaint = null;
            return;
        }

        float spacing = Math.Max(
            0.05f,
            TerrainBrushSpacing
        );

        if (_terrainPainting &&
            _lastTerrainPaint is { } last)
        {
            float distance = Vector2.Distance(
                new Vector2(last.X, last.Z),
                new Vector2(point.X, point.Z)
            );

            if (distance < spacing)
            {
                return;
            }

            int steps = (int)MathF.Floor(
                distance / spacing
            );

            if (steps > 64)
            {
                _lastTerrainPaint = point;
                TerrainBrushRequested?.Invoke(point);
                return;
            }

            for (int step = 1; step <= steps; step++)
            {
                float t = step * spacing / distance;

                TerrainBrushRequested?.Invoke(
                    Vector3.Lerp(last, point, t)
                );
            }

            _lastTerrainPaint = Vector3.Lerp(
                last,
                point,
                steps * spacing / distance
            );
        }
        else
        {
            TerrainBrushRequested?.Invoke(point);

            if (_terrainPainting)
            {
                _lastTerrainPaint = point;
            }
        }
    }

    private void UpdateTerrainBrushPreview()
    {
        if (_renderer == null)
        {
            return;
        }

        Vector3? position = null;

        if (TerrainBrushEnabled &&
            TerrainBrushRadius > 0 &&
            _windowHandle != nint.Zero &&
            _level != null &&
            !IsPlaying &&
            !IsStarting &&
            !IsCharacterEditing &&
            !IsObjectDragging &&
            !_rotating &&
            GetCursorPos(out TerrainCursorPoint cursor) &&
            ScreenToClient(_windowHandle, ref cursor) &&
            GetClientRect(_windowHandle, out NativeRect rect))
        {
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            bool inside =
                cursor.X >= 0 &&
                cursor.Y >= 0 &&
                cursor.X < width &&
                cursor.Y < height;

            if (inside &&
                TryObjectRay(
                    cursor.X,
                    cursor.Y,
                    out Vector3 direction))
            {
                if (_renderer.PickTerrain(
                        _camera.Position,
                        direction,
                        out Vector3 hit,
                        TerrainBrushFill))
                {
                    position = hit;
                }
            }
        }

        _renderer.SetTerrainBrushPreview(
            position,
            TerrainBrushRadius,
            TerrainBrushIsMaterial,
            TerrainBrushFill
        );
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TerrainCursorPoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(
        out TerrainCursorPoint point
    );

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ScreenToClient(
        nint window,
        ref TerrainCursorPoint point
    );
}
