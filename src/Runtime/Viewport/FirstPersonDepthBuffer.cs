using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Runtime.Rendering.Shaders;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector4 = System.Numerics.Vector4;

namespace Runtime.Viewport
{
    internal sealed class FirstPersonDepthBuffer : IDisposable
    {
        private readonly Device _device;
        private readonly VertexShader _vertex;
        private readonly PixelShader _pixel;
        private readonly Buffer _constants;

        private Texture2D? _texture;
        private DepthStencilView? _target;
        private ShaderResourceView? _resource;

        public ShaderResourceView? Resource => _resource;
        internal DepthStencilView? DepthTarget => _target;

        private int _width;
        private int _height;
        private bool _disposed;

        public FirstPersonDepthBuffer(
            Device device,
            ShaderCompiler compiler)
        {
            _device = device;

            VertexShader? vertex = null;
            PixelShader? pixel = null;
            Buffer? constants = null;

            try
            {
                using var vs = compiler.Compile(
                    "PostProcess/Frame.hlsl",
                    "VSMain",
                    "vs_4_0");

                using var ps = compiler.Compile(
                    "PostProcess/FirstPersonDepth.hlsl",
                    "PSMain",
                    "ps_5_0");

                vertex = new VertexShader(
                    device,
                    vs.Bytecode);

                pixel = new PixelShader(
                    device,
                    ps.Bytecode);

                constants = new Buffer(
                    device,
                    Utilities.SizeOf<DepthConstants>(),
                    ResourceUsage.Default,
                    BindFlags.ConstantBuffer,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.None,
                    0);

                _vertex = vertex;
                _pixel = pixel;
                _constants = constants;
            }
            catch
            {
                constants?.Dispose();
                pixel?.Dispose();
                vertex?.Dispose();
                throw;
            }
        }

        public void Prepare(
            DeviceContext context,
            ShaderResourceView sceneDepth,
            RenderTargetView sceneColor,
            RenderTargetView sceneAmbient,
            int width,
            int height,
            float worldNear,
            float firstPersonNear,
            float farPlane)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(FirstPersonDepthBuffer));
            }

            EnsureSize(width, height);

            var constants = new DepthConstants
            {
                DepthPlanes = new Vector4(
                    worldNear,
                    firstPersonNear,
                    farPlane,
                    0f)
            };

            context.ClearState();

            try
            {
                context.ClearDepthStencilView(
                    _target!,
                    DepthStencilClearFlags.Depth,
                    1f,
                    0);

                // This pass writes depth only; preserve both HDR color targets.
                context.OutputMerger.SetRenderTargets(_target);

                context.Rasterizer.SetViewport(
                    0,
                    0,
                    width,
                    height);

                context.InputAssembler.InputLayout = null;

                context.InputAssembler.PrimitiveTopology =
                    PrimitiveTopology.TriangleList;

                context.UpdateSubresource(
                    ref constants,
                    _constants);

                context.VertexShader.Set(_vertex);

                context.PixelShader.Set(_pixel);

                context.PixelShader.SetConstantBuffer(
                    0,
                    _constants);

                context.PixelShader.SetShaderResource(
                    0,
                    sceneDepth);

                context.Draw(3, 0);
            }
            finally
            {
                context.ClearState();
            }

            context.OutputMerger.SetRenderTargets(
                _target,
                sceneColor,
                sceneAmbient);

            context.Rasterizer.SetViewport(
                0,
                0,
                width,
                height);
        }

        private void EnsureSize(int width, int height)
        {
            if (_width == width &&
                _height == height &&
                _target != null)
            {
                return;
            }

            ReleaseTargets();

            var description = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R32_Typeless,

                SampleDescription =
                    new SampleDescription(1, 0),

                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            Texture2D? texture = null;

            try
            {
                texture = new Texture2D(
                    _device,
                    description);

                var target = new DepthStencilView(
                    _device,
                    texture,
                    new DepthStencilViewDescription
                    {
                        Format = Format.D32_Float,
                        Dimension = DepthStencilViewDimension.Texture2D
                    });

                _texture = texture;
                _target = target;
                _resource = new ShaderResourceView(_device, texture, new ShaderResourceViewDescription
                {
                    Format = Format.R32_Float,
                    Dimension = ShaderResourceViewDimension.Texture2D,
                    Texture2D = new ShaderResourceViewDescription.Texture2DResource
                    {
                        MostDetailedMip = 0,
                        MipLevels = 1
                    }
                });
                _width = width;
                _height = height;
            }
            catch
            {
                if (_texture == null) texture?.Dispose();
                ReleaseTargets();
                throw;
            }
        }

        public void ReleaseTargets()
        {
            _resource?.Dispose();
            _resource = null;
            _target?.Dispose();
            _target = null;

            _texture?.Dispose();
            _texture = null;

            _width = 0;
            _height = 0;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            ReleaseTargets();

            _constants.Dispose();
            _pixel.Dispose();
            _vertex.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DepthConstants
        {
            public Vector4 DepthPlanes;
        }
    }
}
