using System.Numerics;
using ZoneReborn.World.Scene;

namespace Runtime.Viewport;

public sealed partial class Dx11Renderer
{
    private SceneInstance? _treeSelection;
    private ObjectGizmoMode _treeGizmoMode;
    private ObjectGizmoAxis _treeGizmoAxis;

    public void SetTreeGizmo(SceneInstance? selected, ObjectGizmoMode mode, ObjectGizmoAxis axis)
    {
        _treeSelection = selected;
        _treeGizmoMode = mode;
        _treeGizmoAxis = axis;
    }

    private void RenderTreeEditing(FreeCamera camera, Matrix4x4 viewProjection)
    {
        if (_treeSelection is not { } selected) return;
        _objectSelection?.Render(selected, viewProjection);
        if (ObjectGizmoGeometry.TryCreateFrame(selected, camera, _width, _height, out var frame, ObjectGizmoFarPlane))
            _objectGizmo?.Render(frame, _treeGizmoMode, _treeGizmoAxis, viewProjection);
    }
}
