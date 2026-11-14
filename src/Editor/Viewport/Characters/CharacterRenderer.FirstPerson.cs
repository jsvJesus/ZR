using System.Numerics;

namespace Editor.Viewport.Characters;

internal sealed partial class CharacterRenderer
{
    public void RenderFirstPerson(
        Matrix4x4 relativeViewProjection,
        Vector3 cameraPosition,
        float firstPersonBlend)
    {
        // Render the same evaluated pose and complete authored meshes as TPS.
        // Weapon tracking and hand IK have already run in CharacterPose.
        RenderMeshes(
            relativeViewProjection,
            cameraPosition,
            Math.Clamp(firstPersonBlend, 0, 1),
            firstPerson: true
        );
    }
}
