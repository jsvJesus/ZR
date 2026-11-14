namespace Editor.Viewport;

public sealed partial class DxViewportHost
{
    private const int KeyF8 = 0x77;
    private readonly FreeCamera _inspectionCamera = new();

    public bool IsPlayInspecting { get; private set; }
    private FreeCamera ActivePlayCamera => IsPlayInspecting ? _inspectionCamera : _playCamera;

    private void TogglePlayInspection()
    {
        if (!IsPlaying || IsStarting || IsCharacterEditing) return;
        if (!IsPlayInspecting)
        {
            UpdatePlayCamera();
            _inspectionCamera.Restore(_playCamera.Pose with { Roll = 0 });
        }
        SetPlayInspection(!IsPlayInspecting);
    }

    private void SetPlayInspection(bool enabled)
    {
        if (IsPlayInspecting == enabled) return;
        _pressedKeys.Clear();
        ReleasePlayerInput();
        if (_rotating)
        {
            _rotating = false;
            ReleaseCapture();
        }
        IsPlayInspecting = enabled;
        UpdatePlayCamera();
        CameraChanged?.Invoke(ActivePlayCamera.Pose);
        PlayStateChanged?.Invoke();
    }
}
