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
        private FirstPersonDepthBuffer? _firstPersonDepth;

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
            Matrix4x4 relativeView,
            float worldNearPlane,
            float farPlane)
        {
            if (_character == null ||
                _playFirstPersonBlend <= 0.01f ||
                _depthTarget == null ||
                _depthShaderResource == null ||
                _target == null ||
                _firstPersonDepth == null)
            {
                return;
            }

            const float firstPersonNearPlane = 0.01f;

            Matrix4x4 firstPersonProjection =
                CreateProjectionMatrix(
                    _playFieldOfView,
                    (float)_width / _height,
                    firstPersonNearPlane,
                    farPlane);

            _firstPersonDepth.Prepare(
                _context,
                _depthShaderResource,
                _target,
                _width,
                _height,
                worldNearPlane,
                firstPersonNearPlane,
                farPlane);

            try
            {
                _character.RenderFirstPerson(
                    relativeView * firstPersonProjection,
                    camera.Position,
                    _playFirstPersonBlend);
            }
            finally
            {
                _context.OutputMerger.SetRenderTargets(
                    _depthTarget,
                    _target);

                _context.Rasterizer.SetViewport(
                    0,
                    0,
                    _width,
                    _height);
            }
        }

        private void RenderPlayCrosshair()
        {
            // The camera's forward ray projects to (0, 0). Match SO's 3-pixel
            // centre dot; viewport dimensions keep it square at every aspect.
            float pixelX = 3f / _width;
            float pixelY = 3f / _height;

            var color = new RawColor4(
                1f,
                1f,
                1f,
                1f
            );

            var vertices = new GridVertex[]
            {
                new(new Vector3(-pixelX, pixelY, 0.01f), color),
                new(new Vector3(pixelX, pixelY, 0.01f), color),
                new(new Vector3(-pixelX, -pixelY, 0.01f), color),
                new(new Vector3(-pixelX, -pixelY, 0.01f), color),
                new(new Vector3(pixelX, pixelY, 0.01f), color),
                new(new Vector3(pixelX, -pixelY, 0.01f), color)
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
                PrimitiveTopology.TriangleList;

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
            _firstPersonDepth?.Dispose();

            _crosshairBuffer = null;
            _crosshairDepth = null;
            _firstPersonDepth = null;
        }
    }
}
