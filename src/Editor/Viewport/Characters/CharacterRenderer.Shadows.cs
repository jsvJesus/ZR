using System.Numerics;
using SharpDX.Direct3D11;

namespace Editor.Viewport.Characters
{
    internal sealed partial class CharacterRenderer
    {
        public void RenderShadow(
            Matrix4x4 relativeViewProjection,
            Vector3 cameraPosition,
            RasterizerState shadowRasterizer)
        {
            RenderMeshes(
                relativeViewProjection,
                cameraPosition,
                1f,
                firstPerson: false,
                shadowRasterizer: shadowRasterizer);
        }
    }
}
