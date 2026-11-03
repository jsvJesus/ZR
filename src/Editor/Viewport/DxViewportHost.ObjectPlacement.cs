using System.Numerics;
using ZoneReborn.World.Scene;

namespace Editor.Viewport;

public sealed partial class DxViewportHost
{
    private string? _placementModel;
    public bool IsObjectPlacing => _placementModel != null;
    public event Action? ObjectPlacementChanged;
    public event Action<SceneInstance>? ObjectPlaced;

    public void SetObjectPlacement(string model)
    {
        if (!ObjectEditingEnabled || IsPlaying || IsStarting || _level?.Objects is not { CanSave: true } scene
            || !scene.Models.TryGet(model, out _)) return;
        CancelObjectDrag();
        _placementModel = model;
        _hoveredObjectAxis = ObjectGizmoAxis.None;
        ObjectPlacementChanged?.Invoke();
    }

    public void CancelObjectPlacement()
    {
        if (_placementModel == null) return;
        _placementModel = null;
        ObjectPlacementChanged?.Invoke();
    }

    private void PlaceObject(int x, int y)
    {
        if (_placementModel is not { } model || !ObjectEditingEnabled || IsPlaying || IsStarting
            || _level?.Objects is not { CanSave: true } scene || _renderer == null
            || !scene.Models.TryGet(model, out var geometry)) { CancelObjectPlacement(); return; }
        if (_rotating || !TryObjectRay(x, y, out var direction)) return;
        try
        {
            if (!_renderer.PickObjectSurface(_camera.Position, direction, out var point)) return;
            point.Y -= geometry.Bounds.Minimum.Y;
            var instance = scene.AddObject(model, Matrix4x4.CreateTranslation(point));
            SelectObject(instance.Id);
            ObjectPlaced?.Invoke(instance);
        }
        catch (Exception exception) { CancelObjectPlacement(); RendererError?.Invoke(exception.Message); }
    }

    private bool TryObjectRay(int x, int y, out Vector3 direction)
    {
        direction = default;
        if (!GetClientRect(_windowHandle, out NativeRect rect)) return false;
        int width = rect.Right - rect.Left, height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) return false;
        var forward = Vector3.Normalize(_camera.LookDirection);
        var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
        var up = Vector3.Cross(forward, right);
        float scale = MathF.Tan(1.05f * 0.5f);
        direction = Vector3.Normalize(forward + right * ((2f * x / width - 1) * width / height * scale)
            + up * ((1 - 2f * y / height) * scale));
        return true;
    }
}
