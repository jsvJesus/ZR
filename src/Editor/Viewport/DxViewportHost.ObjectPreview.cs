using ZoneReborn.World.Assets;
using ZoneReborn.World.Scene;

namespace Editor.Viewport;

public sealed partial class DxViewportHost
{
    private SceneData? _objectPreview;

    public void SetObjectPreview(AssetPaths assets, SceneData scene)
    {
        CancelObjectDrag(); CancelObjectPlacement();
        _objectPreview = scene; _assets = assets;
        _renderer?.SetObjectPreview(assets, scene);
        _lastFrameTime = System.Diagnostics.Stopwatch.GetTimestamp();
        _timer.Start();
    }

    public void ClearObjectPreview()
    {
        _objectPreview = null;
        _renderer?.ClearObjectPreview();
    }
}
