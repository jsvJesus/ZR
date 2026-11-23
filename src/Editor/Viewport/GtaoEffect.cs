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
    internal sealed class GtaoEffect : IDisposable
    {
        private readonly Device _device;

        private VertexShader _vertex = null!;
        private PixelShader _core = null!;
        private PixelShader _bilateral = null!;
        private PixelShader _temporal = null!;
        private SamplerState _pointSampler = null!;
        private Buffer _constants = null!;

        private Surface? _raw;
        private Surface? _blur;
        private Surface? _history0;
        private Surface? _history1;

        private int _width;
        private int _height;
        private int _historyIndex;
        private int _frameIndex;

        private float _lastRadius = -1f;
        private int _lastQuality = -1;

        private bool _hasHistory;
        private bool _disposed;

        public GtaoEffect(
            Device device,
            ShaderCompiler compiler)
        {
            _device = device;

            try
            {
                using var vs = compiler.Compile(
                    "PostProcess/Gtao.hlsl",
                    "VSMain",
                    "vs_4_0");

                using var core = compiler.Compile(
                    "PostProcess/Gtao.hlsl",
                    "PSGtao",
                    "ps_5_0");

                using var bilateral = compiler.Compile(
                    "PostProcess/Gtao.hlsl",
                    "PSBilateral",
                    "ps_5_0");

                using var temporal = compiler.Compile(
                    "PostProcess/Gtao.hlsl",
                    "PSTemporal",
                    "ps_5_0");

                _vertex = new VertexShader(
                    device,
                    vs.Bytecode);

                _core = new PixelShader(
                    device,
                    core.Bytecode);

                _bilateral = new PixelShader(
                    device,
                    bilateral.Bytecode);

                _temporal = new PixelShader(
                    device,
                    temporal.Bytecode);

                _pointSampler = new SamplerState(
                    device,
                    new SamplerStateDescription
                    {
                        Filter = Filter.MinMagMipPoint,
                        AddressU = TextureAddressMode.Clamp,
                        AddressV = TextureAddressMode.Clamp,
                        AddressW = TextureAddressMode.Clamp,
                        ComparisonFunction = Comparison.Never,
                        MinimumLod = 0,
                        MaximumLod = float.MaxValue
                    });

                _constants = new Buffer(
                    device,
                    Utilities.SizeOf<AoConstants>(),
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
            Matrix4x4 inverseViewProjection,
            Matrix4x4 currentViewProjection,
            Matrix4x4 previousViewProjection,
            Vector3 cameraPosition,
            ShaderResourceView depth,
            ShaderResourceView normal,
            float radius,
            int quality)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(GtaoEffect));
            }

            EnsureSize(width, height);

            radius = Math.Clamp(radius, 0.1f, 5f);
            quality = Math.Clamp(quality, 1, 3);

            if (MathF.Abs(radius - _lastRadius) > 0.001f ||
                quality != _lastQuality)
            {
                ResetHistory();

                _lastRadius = radius;
                _lastQuality = quality;
            }

            bool previousProjectionValid = Matrix4x4.Invert(
                previousViewProjection,
                out Matrix4x4 previousInverseViewProjection);

            if (!previousProjectionValid)
            {
                ResetHistory();
                previousInverseViewProjection = Matrix4x4.Identity;
            }

            Surface previous = _historyIndex == 0
                ? _history0!
                : _history1!;

            Surface destination = _historyIndex == 0
                ? _history1!
                : _history0!;

            var data = new AoConstants
            {
                InverseViewProjection = inverseViewProjection,
                CurrentViewProjection = currentViewProjection,
                PreviousViewProjection = previousViewProjection,
                PreviousInverseViewProjection = previousInverseViewProjection,

                Resolution = new Vector4(
                    width,
                    height,
                    _raw!.Width,
                    _raw.Height),

                Options = new Vector4(
                    radius,
                    quality,
                    _hasHistory ? 1f : 0f,
                    _frameIndex),

                Camera = new Vector4(
                    cameraPosition,
                    1f),

                BlurAxis = Vector4.Zero
            };

            context.ClearState();

            try
            {
                context.InputAssembler.InputLayout = null;

                context.InputAssembler.PrimitiveTopology =
                    PrimitiveTopology.TriangleList;

                context.VertexShader.Set(_vertex);

                context.PixelShader.SetConstantBuffer(
                    0,
                    _constants);

                context.PixelShader.SetSampler(
                    0,
                    _pointSampler);

                context.UpdateSubresource(
                    ref data,
                    _constants);

                Draw(
                    context,
                    _core,
                    _raw!,
                    depth,
                    normal,
                    null,
                    null);

                data.BlurAxis = new Vector4(
                    1f,
                    0f,
                    0f,
                    0f);

                context.UpdateSubresource(
                    ref data,
                    _constants);

                Draw(
                    context,
                    _bilateral,
                    _blur!,
                    depth,
                    normal,
                    _raw!.View,
                    null);

                data.BlurAxis = new Vector4(
                    0f,
                    1f,
                    0f,
                    0f);

                context.UpdateSubresource(
                    ref data,
                    _constants);

                Draw(
                    context,
                    _bilateral,
                    _raw!,
                    depth,
                    normal,
                    _blur!.View,
                    null);

                data.BlurAxis = Vector4.Zero;

                context.UpdateSubresource(
                    ref data,
                    _constants);

                Draw(
                    context,
                    _temporal,
                    destination,
                    depth,
                    normal,
                    _raw!.View,
                    _hasHistory ? previous.View : null);

                _historyIndex = 1 - _historyIndex;
                _hasHistory = true;
                _frameIndex = (_frameIndex + 1) % 64;

                return destination.View;
            }
            finally
            {
                context.ClearState();
            }
        }

        private static void Draw(
            DeviceContext context,
            PixelShader shader,
            Surface target,
            ShaderResourceView depth,
            ShaderResourceView normal,
            ShaderResourceView? ao,
            ShaderResourceView? history)
        {
            context.OutputMerger.SetRenderTargets(
                null,
                target.Target);

            context.Rasterizer.SetViewport(
                0,
                0,
                target.Width,
                target.Height);

            context.PixelShader.SetShaderResource(
                0,
                depth);

            context.PixelShader.SetShaderResource(
                1,
                normal);

            context.PixelShader.SetShaderResource(
                2,
                ao);

            context.PixelShader.SetShaderResource(
                3,
                history);

            context.PixelShader.Set(shader);
            context.Draw(3, 0);

            for (int slot = 0; slot < 4; slot++)
            {
                context.PixelShader.SetShaderResource(
                    slot,
                    null);
            }
        }

        private void EnsureSize(int width, int height)
        {
            if (_width == width &&
                _height == height &&
                _history0 != null)
            {
                return;
            }

            ReleaseTargets();

            int halfWidth = Math.Max(
                1,
                (width + 1) / 2);

            int halfHeight = Math.Max(
                1,
                (height + 1) / 2);

            try
            {
                _raw = new Surface(
                    _device,
                    halfWidth,
                    halfHeight,
                    Format.R16_Float);

                _blur = new Surface(
                    _device,
                    halfWidth,
                    halfHeight,
                    Format.R16_Float);

                _history0 = new Surface(
                    _device,
                    width,
                    height,
                    Format.R16G16B16A16_Float);

                _history1 = new Surface(
                    _device,
                    width,
                    height,
                    Format.R16G16B16A16_Float);

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
            _frameIndex = 0;
        }

        public void ReleaseTargets()
        {
            _history1?.Dispose();
            _history1 = null;

            _history0?.Dispose();
            _history0 = null;

            _blur?.Dispose();
            _blur = null;

            _raw?.Dispose();
            _raw = null;

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
            _pointSampler?.Dispose();
            _temporal?.Dispose();
            _bilateral?.Dispose();
            _core?.Dispose();
            _vertex?.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AoConstants
        {
            public Matrix4x4 InverseViewProjection;
            public Matrix4x4 CurrentViewProjection;
            public Matrix4x4 PreviousViewProjection;
            public Matrix4x4 PreviousInverseViewProjection;

            public Vector4 Resolution;
            public Vector4 Options;
            public Vector4 Camera;
            public Vector4 BlurAxis;
        }

        private sealed class Surface : IDisposable
        {
            public Texture2D Texture { get; }
            public RenderTargetView Target { get; }
            public ShaderResourceView View { get; }

            public int Width { get; }
            public int Height { get; }

            public Surface(
                Device device,
                int width,
                int height,
                Format format)
            {
                Width = width;
                Height = height;

                var texture = new Texture2D(
                    device,
                    new Texture2DDescription
                    {
                        Width = width,
                        Height = height,
                        ArraySize = 1,
                        MipLevels = 1,
                        Format = format,

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
