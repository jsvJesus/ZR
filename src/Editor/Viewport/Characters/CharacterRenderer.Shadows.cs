using System.Numerics;

namespace Editor.Viewport.Characters
{
    internal sealed partial class CharacterRenderer
    {
        public void RenderShadow(
            Matrix4x4 relativeViewProjection,
            Vector3 cameraPosition)
        {
            RenderMeshes(
                relativeViewProjection,
                cameraPosition,
                1f,
                firstPerson: false);
        }
    }
}
