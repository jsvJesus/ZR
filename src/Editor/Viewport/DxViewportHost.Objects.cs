using System.Numerics;
using System.Runtime.InteropServices;
using ZoneReborn.World.Scene;

namespace Editor.Viewport;

public sealed partial class DxViewportHost
{
    private const int KeyDelete = 0x2E, KeyZ = 0x5A, KeyY = 0x59;
    private bool _objectEditingEnabled;
    private string? _selectedObjectId;

    public bool ObjectEditingEnabled
    {
        get => _objectEditingEnabled;
        set
        {
            _objectEditingEnabled = value;
            if (!value) { CancelObjectDrag(); CancelObjectPlacement(); }
            _renderer?.SelectObject(value && !IsPlaying && !IsStarting ? _selectedObjectId : null);
        }
    }

    public event Action<SceneInstance?>? ObjectPicked;
    public event Action? ObjectDeleteRequested;
    public event Action? ObjectUndoRequested;
    public event Action? ObjectRedoRequested;

    public void SelectObject(string? id)
    {
        if (id != _selectedObjectId) CancelObjectDrag();
        _selectedObjectId = id != null && _level?.Objects?.Find(id) != null ? id : null;
        _renderer?.SelectObject(ObjectEditingEnabled && !IsPlaying && !IsStarting ? _selectedObjectId : null);
    }

    public void FocusObject(SceneInstance instance)
    {
        if (!ObjectEditingEnabled || IsPlaying || IsStarting || IsObjectDragging || _level?.Objects?.Find(instance.Id) is not { } current)
            return;
        var bounds = current.VisualLocalBounds.Transform(current.World);
        float halfAngle = 1.05f * 0.5f;
        if (GetClientRect(_windowHandle, out NativeRect rect) && rect.Right > rect.Left && rect.Bottom > rect.Top)
        {
            float aspect = (float)(rect.Right - rect.Left) / (rect.Bottom - rect.Top);
            halfAngle = Math.Min(halfAngle, MathF.Atan(MathF.Tan(halfAngle) * aspect));
        }
        float distance = Math.Max(3, bounds.Radius / MathF.Sin(halfAngle) * 1.15f);
        var position = bounds.Center - _camera.LookDirection * distance;
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z)) return;
        SetCamera(new(position.X, position.Y, position.Z, _camera.Yaw, _camera.Pitch,
            Math.Clamp(distance, 8, 250)));
    }

    private void PickObject(int x, int y)
    {
        if (!ObjectEditingEnabled || IsPlaying || IsStarting || _renderer == null
            || !TryObjectRay(x, y, out var direction)) return;
        try
        {
            var picked = _renderer.PickObject(_camera.Position, direction);
            SelectObject(picked?.Id);
            ObjectPicked?.Invoke(picked);
        }
        catch (Exception exception) { RendererError?.Invoke(exception.Message); }
    }

    private bool HandleObjectKey(int key, nint lParam)
    {
        if (!ObjectEditingEnabled || IsPlaying || IsStarting || GetFocus() != _windowHandle) return false;
        if (IsObjectPlacing && key == 0x1B) { CancelObjectPlacement(); return true; }
        bool control = (GetKeyState(KeyControl) & 0x8000) != 0;
        bool shift = (GetKeyState(KeyShift) & 0x8000) != 0;
        if (IsObjectDragging)
        {
            if (key == 0x1B) { CancelObjectDrag(); return true; }
            return key is 0x31 or 0x32 or KeyDelete || (control && (key is KeyZ or KeyY));
        }
        if (!control && (key is 0x31 or 0x32))
        {
            CancelObjectPlacement();
            ObjectTransformMode = key == 0x31 ? ObjectGizmoMode.Move : ObjectGizmoMode.Rotate;
            return true;
        }
        Action? command;
        if (key == KeyDelete) command = ObjectDeleteRequested;
        else if (control && key == KeyZ) command = shift ? ObjectRedoRequested : ObjectUndoRequested;
        else if (control && key == KeyY) command = ObjectRedoRequested;
        else return false;
        // WM_KEYDOWN bit 30 marks autorepeat; one physical press performs one edit.
        if ((lParam.ToInt64() & (1L << 30)) == 0) command?.Invoke();
        return true;
    }

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int key);
}
