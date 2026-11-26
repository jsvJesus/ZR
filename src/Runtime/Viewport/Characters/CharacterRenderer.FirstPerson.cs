using System.Numerics;

namespace Runtime.Viewport.Characters;

internal sealed partial class CharacterRenderer
{
    public void RenderFirstPerson(
        Matrix4x4 relativeViewProjection,
        Vector3 cameraPosition,
        float firstPersonBlend)
    {
        // Switch the visible mesh set without opacity fading.
        // Camera movement and skeleton animation remain smooth.
        if (!float.IsFinite(firstPersonBlend) ||
            firstPersonBlend < ViewSwitchThreshold)
        {
            return;
        }

        RenderMeshes(
            relativeViewProjection,
            cameraPosition,
            1f,
            firstPerson: true
        );
    }
}
