using System;
using System.Collections.Generic;
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
    internal sealed class HdrBloom : IDisposable
    {
        private const int MaximumLevels = 5;

        private readonly Device _device;
        private readonly VertexShader _vertex;
        private readonly PixelShader _prefilter;
        private readonly PixelShader _downsample;
        private readonly PixelShader _upsample;
        private readonly SamplerState _sampler;
        private readonly Buffer _constants;

        private readonly List<Surface> _down = new();
        private readonly List<Surface> _up = new();

        private int _width;
        private int _height;
        private bool _disposed;

        public HdrBloom(
            Device device,
            ShaderCompiler compiler)
        {
            _device = device;

            VertexShader? vertex = null;
            PixelShader? prefilter = null;
            PixelShader? downsample = null;
            PixelShader? upsample = null;
            SamplerState? sampler = null;
            Buffer? constants = null;

            try
            {
                using var vs = compiler.Compile(
                    "PostProcess/Bloom.hlsl",
                    "VSMain",
                    "vs_4_0");

                using var pre = compiler.Compile(
                    "PostProcess/Bloom.hlsl",
                    "PSPrefilter",
                    "ps_5_0");

                using var down = compiler.Compile(
                    "PostProcess/Bloom.hlsl",
                    "PSDownsample",
                    "ps_5_0");

                using var up = compiler.Compile(
                    "PostProcess/Bloom.hlsl",
                    "PSUpsample",
                    "ps_5_0");

                vertex = new VertexShader(device, vs.Bytecode);
                prefilter = new PixelShader(device, pre.Bytecode);
                downsample = new PixelShader(device, down.Bytecode);
                upsample = new PixelShader(device, up.Bytecode);

                sampler = new SamplerState(
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

                constants = new Buffer(
                    device,
                    Utilities.SizeOf<BloomConstants>(),
                    ResourceUsage.Default,
                    BindFlags.ConstantBuffer,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.None,
                    0);

                _vertex = vertex;
                _prefilter = prefilter;
                _downsample = downsample;
                _upsample = upsample;
                _sampler = sampler;
                _constants = constants;
            }
            catch
            {
                constants?.Dispose();
                sampler?.Dispose();
                upsample?.Dispose();
                downsample?.Dispose();
                prefilter?.Dispose();
                vertex?.Dispose();
                throw;
            }
        }

        public ShaderResourceView Render(
            DeviceContext context,
            int width,
            int height,
            ShaderResourceView source,
            float threshold)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HdrBloom));
            }

            EnsureSize(width, height);

            var data = new BloomConstants
            {
                Parameters = new Vector4(
                    Math.Clamp(threshold, 0.05f, 10f),
                    0.5f,
                    0f,
                    0f)
            };

            context.ClearState();

            try
            {
                context.UpdateSubresource(
                    ref data,
                    _constants);

                context.InputAssembler.InputLayout = null;
                context.InputAssembler.PrimitiveTopology =
                    PrimitiveTopology.TriangleList;

                context.VertexShader.Set(_vertex);

                context.PixelShader.SetConstantBuffer(
                    0,
                    _constants);

                context.PixelShader.SetSampler(
                    0,
                    _sampler);

                Draw(
                    context,
                    _prefilter,
                    source,
                    null,
                    _down[0]);

                for (int index = 1; index < _down.Count; index++)
                {
                    Draw(
                        context,
                        _downsample,
                        _down[index - 1].View,
                        null,
                        _down[index]);
                }

                if (_down.Count == 1)
                {
                    return _down[0].View;
                }

                for (int index = _down.Count - 2; index >= 0; index--)
                {
                    ShaderResourceView lower =
                        index == _down.Count - 2
                            ? _down[index + 1].View
                            : _up[index + 1].View;

                    Draw(
                        context,
                        _upsample,
                        _down[index].View,
                        lower,
                        _up[index]);
                }

                return _up[0].View;
            }
            finally
            {
                context.ClearState();
            }
        }

        private static void Draw(
            DeviceContext context,
            PixelShader shader,
            ShaderResourceView source,
            ShaderResourceView? lower,
            Surface destination)
        {
            context.OutputMerger.SetRenderTargets(
                null,
                destination.Target);

            context.Rasterizer.SetViewport(
                0,
                0,
                destination.Width,
                destination.Height);

            context.PixelShader.SetShaderResource(
                0,
                source);

            context.PixelShader.SetShaderResource(
                1,
                lower);

            context.PixelShader.Set(shader);
            context.Draw(3, 0);

            context.PixelShader.SetShaderResource(0, null);
            context.PixelShader.SetShaderResource(1, null);
        }

        private void EnsureSize(int width, int height)
        {
            if (_width == width &&
                _height == height &&
                _down.Count > 0)
            {
                return;
            }

            ReleaseTargets();

            try
            {
                int currentWidth = Math.Max(1, (width + 1) / 2);
                int currentHeight = Math.Max(1, (height + 1) / 2);

                for (int index = 0; index < MaximumLevels; index++)
                {
                    _down.Add(new Surface(
                        _device,
                        currentWidth,
                        currentHeight));

                    _up.Add(new Surface(
                        _device,
                        currentWidth,
                        currentHeight));

                    if (currentWidth == 1 &&
                        currentHeight == 1)
                    {
                        break;
                    }

                    currentWidth = Math.Max(1, (currentWidth + 1) / 2);
                    currentHeight = Math.Max(1, (currentHeight + 1) / 2);
                }

                _width = width;
                _height = height;
            }
            catch
            {
                ReleaseTargets();
                throw;
            }
        }

        public void ReleaseTargets()
        {
            foreach (var surface in _up)
            {
                surface.Dispose();
            }

            foreach (var surface in _down)
            {
                surface.Dispose();
            }

            _up.Clear();
            _down.Clear();

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
            _sampler.Dispose();
            _upsample.Dispose();
            _downsample.Dispose();
            _prefilter.Dispose();
            _vertex.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BloomConstants
        {
            public Vector4 Parameters;
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
                int height)
            {
                Width = width;
                Height = height;

                var texture = new Texture2D(
                    device,
                    new Texture2DDescription
                    {
                        Width = width,
                        Height = height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = Format.R16G16B16A16_Float,
                        SampleDescription = new SampleDescription(1, 0),
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
