using System.Numerics;
using System.Runtime.CompilerServices;

namespace ZoneReborn.World.Scene;

/// <summary>Shared model-space visual geometry, independent of collision proxies.</summary>
public static class SceneVisualPicker
{
    private static readonly ConditionalWeakTable<SceneModel, SceneTriangleTree> Trees = new();

    public static bool Pick(SceneModel model, Matrix4x4 inverseWorld, Vector3 origin, Vector3 direction,
        float maximumDistance, out float distance)
    {
        distance = maximumDistance;
        if (!SceneTriangle.IsFinite(origin) || !SceneTriangle.IsFinite(direction) ||
            !float.IsFinite(maximumDistance) || maximumDistance <= 0) return false;
        var localOrigin = Vector3.Transform(origin, inverseWorld);
        // Keep the original parameter: normalising here loses world distance under scale.
        var localDirection = Vector3.TransformNormal(direction, inverseWorld);
        if (!SceneTriangle.IsFinite(localOrigin) || !SceneTriangle.IsFinite(localDirection) ||
            localDirection.LengthSquared() == 0 ||
            !Intersects(model.Bounds, localOrigin, localDirection, maximumDistance, out _)) return false;
        return Trees.GetValue(model, source => new SceneTriangleTree(source))
            .Raycast(localOrigin, localDirection, maximumDistance, out distance);
    }

    public static bool Intersects(SceneBounds bounds, Vector3 origin, Vector3 direction,
        float maximumDistance, out float distance)
    {
        float near = 0, far = maximumDistance;
        for (int axis = 0; axis < 3; axis++)
        {
            float start = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
            float step = axis == 0 ? direction.X : axis == 1 ? direction.Y : direction.Z;
            float minimum = axis == 0 ? bounds.Minimum.X : axis == 1 ? bounds.Minimum.Y : bounds.Minimum.Z;
            float maximum = axis == 0 ? bounds.Maximum.X : axis == 1 ? bounds.Maximum.Y : bounds.Maximum.Z;
            if (step == 0)
            {
                if (start < minimum || start > maximum) { distance = 0; return false; }
                continue;
            }
            float a = (minimum - start) / step, b = (maximum - start) / step;
            if (a > b) (a, b) = (b, a);
            near = Math.Max(near, a); far = Math.Min(far, b);
            if (near > far) { distance = 0; return false; }
        }
        distance = near;
        return true;
    }
}
