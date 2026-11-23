using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Editor.Rendering.Shaders;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport
{
    internal sealed class TaaEffect : IDisposable
    {
        private readonly Device _device;

        private VertexShader _vertex = null!;
        private PixelShader _resolve = null!;
        private SamplerState _sampler = null!;
        private Buffer _constants = null!;

        private Surface? _history0;
        private Surface? _history1;

        private int _width;
        private int _height;
        private int _historyIndex;

        private bool _hasHistory;
        private bool _disposed;

        public TaaEffect(
            Device device,
            ShaderCompiler compiler)
        {
            _device = device;

            try
            {
                using var vertexCode = compiler.Compile(
                    "PostProcess/Taa.hlsl",
                    "VSMain",
                    "vs_4_0");

                using var pixelCode = compiler.Compile(
                    "PostProcess/Taa.hlsl",
                    "PSResolve",
                    "ps_5_0");

                _vertex = new VertexShader(
                    device,
                    vertexCode.Bytecode);

                _resolve = new PixelShader(
                    device,
                    pixelCode.Bytecode);

                _sampler = new SamplerState(
                    device,
                    new SamplerStateDescription
                    {
                        Filter = Filter.MinMagMipLinear,
                        AddressU = TextureAddressMode.Clamp,
                        AddressV = TextureAddressMode.Clamp,
                        AddressW = TextureAddressMode.Clamp,
                        ComparisonFunction = Comparison.Never,
                        MinimumLod = 0,
                        MaximumLod = float.MaxValue
                    });

                _constants = new Buffer(
                    device,
                    Utilities.SizeOf<TaaConstants>(),
                    ResourceUsage.Default,
                    BindFlags.ConstantBuffer,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.None,
                    0);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public ShaderResourceView Render(
            DeviceContext context,
            int width,
            int height,
            ShaderResourceView currentColor,
            ShaderResourceView depth,
            ShaderResourceView motion,
            Matrix4x4 inverseViewProjection,
            Matrix4x4 currentViewProjection,
            Matrix4x4 previousViewProjection,
            float feedback)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(TaaEffect));
            }

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width));
            }

            EnsureSize(width, height);

            Surface previous = _historyIndex == 0
                ? _history0!
                : _history1!;

            Surface destination = _historyIndex == 0
                ? _history1!
                : _history0!;

            var constants = new TaaConstants
            {
                InverseViewProjection =
                    inverseViewProjection,

                CurrentViewProjection =
                    currentViewProjection,

                PreviousViewProjection =
                    previousViewProjection,

                Parameters = new Vector4(
                    width,
                    height,
                    Math.Clamp(feedback, 0f, 0.96f),
                    _hasHistory ? 1f : 0f)
            };

            context.ClearState();

            try
            {
                context.UpdateSubresource(
                    ref constants,
                    _constants);

                context.OutputMerger.SetRenderTargets(
                    null,
                    destination.Target);

                context.Rasterizer.SetViewport(
                    0,
                    0,
                    width,
                    height);

                context.InputAssembler.InputLayout = null;

                context.InputAssembler.PrimitiveTopology =
                    PrimitiveTopology.TriangleList;

                context.VertexShader.Set(
                    _vertex);

                context.PixelShader.Set(
                    _resolve);

                context.PixelShader.SetConstantBuffer(
                    0,
                    _constants);

                context.PixelShader.SetSampler(
                    0,
                    _sampler);

                context.PixelShader.SetShaderResource(
                    0,
                    currentColor);

                context.PixelShader.SetShaderResource(
                    1,
                    _hasHistory ? previous.View : null);

                context.PixelShader.SetShaderResource(
                    2,
                    depth);

                context.PixelShader.SetShaderResource(
                    3,
                    motion);

                context.Draw(3, 0);

                _historyIndex = 1 - _historyIndex;
                _hasHistory = true;

                return destination.View;
            }
            finally
            {
                context.ClearState();
            }
        }

        private void EnsureSize(int width, int height)
        {
            if (_width == width &&
                _height == height &&
                _history0 != null &&
                _history1 != null)
            {
                return;
            }

            ReleaseTargets();

            try
            {
                _history0 = new Surface(
                    _device,
                    width,
                    height);

                _history1 = new Surface(
                    _device,
                    width,
                    height);

                _width = width;
                _height = height;
            }
            catch
            {
                ReleaseTargets();
                throw;
            }
        }

        public void ResetHistory()
        {
            _hasHistory = false;
            _historyIndex = 0;
        }

        public void ReleaseTargets()
        {
            _history0?.Dispose();
            _history0 = null;

            _history1?.Dispose();
            _history1 = null;

            _width = 0;
            _height = 0;

            ResetHistory();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            ReleaseTargets();

            _constants?.Dispose();
            _sampler?.Dispose();
            _resolve?.Dispose();
            _vertex?.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TaaConstants
        {
            public Matrix4x4 InverseViewProjection;
            public Matrix4x4 CurrentViewProjection;
            public Matrix4x4 PreviousViewProjection;
            public Vector4 Parameters;
        }

        private sealed class Surface : IDisposable
        {
            public Texture2D Texture { get; }
            public RenderTargetView Target { get; }
            public ShaderResourceView View { get; }

            public Surface(
                Device device,
                int width,
                int height)
            {
                var texture = new Texture2D(
                    device,
                    new Texture2DDescription
                    {
                        Width = width,
                        Height = height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = Format.R16G16B16A16_Float,
                        SampleDescription =
                            new SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        BindFlags =
                            BindFlags.RenderTarget |
                            BindFlags.ShaderResource,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = ResourceOptionFlags.None
                    });

                try
                {
                    var target = new RenderTargetView(
                        device,
                        texture);

                    try
                    {
                        var view = new ShaderResourceView(
                            device,
                            texture);

                        Texture = texture;
                        Target = target;
                        View = view;
                    }
                    catch
                    {
                        target.Dispose();
                        throw;
                    }
                }
                catch
                {
                    texture.Dispose();
                    throw;
                }
            }

            public void Dispose()
            {
                View.Dispose();
                Target.Dispose();
                Texture.Dispose();
            }
        }
    }
}
