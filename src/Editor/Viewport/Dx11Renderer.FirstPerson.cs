using System.Numerics;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector3 = System.Numerics.Vector3;

namespace Editor.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private float _playFieldOfView = 0.78f;
        private float _playFirstPersonBlend;
        private float _playAimBlend;
        private bool _showPlayCrosshair;

        private Buffer? _crosshairBuffer;
        private DepthStencilState? _crosshairDepth;

        public void SetPlayCameraState(
            float firstPersonBlend,
            float aimBlend,
            float fieldOfView,
            bool showCrosshair)
        {
            if (!float.IsFinite(fieldOfView) || fieldOfView <= 0 || fieldOfView >= MathF.PI)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldOfView),
                    "Camera FOV must be finite and between 0 and PI radians.");
            }

            _playFirstPersonBlend = Math.Clamp(
                firstPersonBlend,
                0f,
                1f
            );

            _playAimBlend = Math.Clamp(
                aimBlend,
                0f,
                1f
            );

            // Preserve the requested projection. A hidden clamp made different
            // camera settings render identically and invalidated FOV tuning.
            _playFieldOfView = fieldOfView;

            _showPlayCrosshair = showCrosshair;
        }

        private void RenderFirstPersonScene(
            FreeCamera camera,
            Matrix4x4 relativeViewProjection)
        {
            if (_character != null &&
                _playFirstPersonBlend > 0.01f &&
                _depthTarget != null)
            {
                _character.RenderFirstPerson(
                    relativeViewProjection,
                    camera.Position,
                    _playFirstPersonBlend
                );
            }
        }

        private void RenderPlayCrosshair()
        {
            float pixelX = 2f / _width;
            float pixelY = 2f / _height;

            float inner = 3f - 2f * _playAimBlend;
            float outer = 9f - 4f * _playAimBlend;

            var color = new RawColor4(
                0.92f,
                0.95f,
                0.88f,
                1f
            );

            var vertices = new GridVertex[]
            {
                new(new Vector3(-outer * pixelX, 0f, 0.01f), color),
                new(new Vector3(-inner * pixelX, 0f, 0.01f), color),

                new(new Vector3(inner * pixelX, 0f, 0.01f), color),
                new(new Vector3(outer * pixelX, 0f, 0.01f), color),

                new(new Vector3(0f, -outer * pixelY, 0.01f), color),
                new(new Vector3(0f, -inner * pixelY, 0.01f), color),

                new(new Vector3(0f, inner * pixelY, 0.01f), color),
                new(new Vector3(0f, outer * pixelY, 0.01f), color)
            };

            if (_crosshairBuffer == null)
            {
                _crosshairBuffer = Buffer.Create(
                    _device,
                    BindFlags.VertexBuffer,
                    vertices
                );
            }
            else
            {
                _context.UpdateSubresource(
                    vertices,
                    _crosshairBuffer
                );
            }

            if (_crosshairDepth == null)
            {
                var depth = DepthStencilStateDescription.Default();
                depth.IsDepthEnabled = false;
                depth.DepthWriteMask = DepthWriteMask.Zero;

                _crosshairDepth = new DepthStencilState(
                    _device,
                    depth
                );
            }

            Matrix4x4 identity = Matrix4x4.Identity;

            _context.UpdateSubresource(
                ref identity,
                _cameraBuffer
            );

            _context.InputAssembler.InputLayout = _inputLayout;
            _context.InputAssembler.PrimitiveTopology =
                PrimitiveTopology.LineList;

            _context.InputAssembler.SetVertexBuffers(
                0,
                new VertexBufferBinding(
                    _crosshairBuffer,
                    Utilities.SizeOf<GridVertex>(),
                    0
                )
            );

            _context.VertexShader.Set(_vertexShader);
            _context.VertexShader.SetConstantBuffer(
                0,
                _cameraBuffer
            );

            _context.PixelShader.Set(_pixelShader);

            _context.OutputMerger.SetDepthStencilState(
                _crosshairDepth
            );

            _context.OutputMerger.SetBlendState(null);

            _context.Draw(vertices.Length, 0);

            _context.OutputMerger.SetDepthStencilState(null);
        }

        private void DisposeFirstPersonScene()
        {
            _crosshairBuffer?.Dispose();
            _crosshairDepth?.Dispose();

            _crosshairBuffer = null;
            _crosshairDepth = null;
        }
    }
}
