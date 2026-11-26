using System.Runtime.InteropServices;
using Runtime.Rendering.Shaders;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters.Stats;
using Device = SharpDX.Direct3D11.Device;

namespace Runtime.UI.Hud
{
    internal sealed class HtmlHudRenderer : IDisposable
    {
        private HudBrowser _browser = null!;

        private Texture2D _texture = null!;
        private ShaderResourceView _textureView = null!;

        private VertexShader _vertexShader = null!;
        private PixelShader _pixelShader = null!;

        private BlendState _blend = null!;
        private DepthStencilState _depth = null!;
        private RasterizerState _rasterizer = null!;
        private SamplerState _sampler = null!;

        private bool _hasFrame;
        private bool _disposed;

        public HtmlHudRenderer(
            Device device,
            AssetPaths assets)
        {
            try
            {
                Initialize(device, assets);

                _browser = new HudBrowser(
                    Path.Combine(
                        assets.Root,
                        "ui",
                        "hud",
                        "index.html"));
            }
            catch
            {
                Dispose();

                throw;
            }
        }

        private void Initialize(
            Device device,
            AssetPaths assets)
        {
            var compiler = new ShaderCompiler(assets);

            using var vertex = compiler.Compile(
                "UI/Hud.hlsl",
                "VSMain",
                "vs_4_0");

            using var pixel = compiler.Compile(
                "UI/Hud.hlsl",
                "PSMain",
                "ps_4_0");

            _vertexShader = new VertexShader(
                device,
                vertex.Bytecode);

            _pixelShader = new PixelShader(
                device,
                pixel.Bytecode);

            _texture = new Texture2D(
                device,
                new Texture2DDescription
                {
                    Width = HudBrowser.Width,
                    Height = HudBrowser.Height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None
                });

            _textureView = new ShaderResourceView(
                device,
                _texture);

            var blend = BlendStateDescription.Default();

            blend.RenderTarget[0].IsBlendEnabled = true;

            blend.RenderTarget[0].SourceBlend =
                BlendOption.One;

            blend.RenderTarget[0].DestinationBlend =
                BlendOption.InverseSourceAlpha;

            blend.RenderTarget[0].BlendOperation =
                BlendOperation.Add;

            blend.RenderTarget[0].SourceAlphaBlend =
                BlendOption.One;

            blend.RenderTarget[0].DestinationAlphaBlend =
                BlendOption.InverseSourceAlpha;

            blend.RenderTarget[0].AlphaBlendOperation =
                BlendOperation.Add;

            blend.RenderTarget[0].RenderTargetWriteMask =
                ColorWriteMaskFlags.All;

            _blend = new BlendState(
                device,
                blend);

            var depth = DepthStencilStateDescription.Default();

            depth.IsDepthEnabled = false;
            depth.DepthWriteMask = DepthWriteMask.Zero;
            depth.IsStencilEnabled = false;

            _depth = new DepthStencilState(
                device,
                depth);

            _rasterizer = new RasterizerState(
                device,
                new RasterizerStateDescription
                {
                    FillMode = FillMode.Solid,
                    CullMode = CullMode.None,
                    IsDepthClipEnabled = true
                });

            _sampler = new SamplerState(
                device,
                new SamplerStateDescription
                {
                    Filter = Filter.MinMagMipLinear,
                    AddressU = TextureAddressMode.Clamp,
                    AddressV = TextureAddressMode.Clamp,
                    AddressW = TextureAddressMode.Clamp,
                    ComparisonFunction = Comparison.Never,
                    MaximumAnisotropy = 1,
                    MinimumLod = 0f,
                    MaximumLod = float.MaxValue
                });
        }

        private void UploadFrame(
            DeviceContext context,
            byte[] pixels)
        {
            var mapped = context.MapSubresource(
                _texture,
                0,
                MapMode.WriteDiscard,
                SharpDX.Direct3D11.MapFlags.None);

            try
            {
                int stride = HudBrowser.Width * 4;

                for (int y = 0; y < HudBrowser.Height; y++)
                {
                    Marshal.Copy(
                        pixels,
                        y * stride,
                        IntPtr.Add(
                            mapped.DataPointer,
                            y * mapped.RowPitch),
                        stride);
                }
            }
            finally
            {
                context.UnmapSubresource(
                    _texture,
                    0);
            }

            _hasFrame = true;
        }

        public void Render(
            DeviceContext context,
            RenderTargetView target,
            int screenWidth,
            int screenHeight,
            PlayerStatsSnapshot snapshot)
        {
            if (_disposed ||
                screenWidth <= 24 ||
                screenHeight <= 24)
            {
                return;
            }

            _browser.Publish(snapshot);

            byte[]? pixels = _browser.TakeFrame();

            if (pixels != null)
            {
                UploadFrame(
                    context,
                    pixels);
            }

            if (!_hasFrame)
            {
                return;
            }

            float scale = Math.Min(
                1f,
                Math.Min(
                    (screenWidth - 24f) / HudBrowser.Width,
                    (screenHeight - 24f) / HudBrowser.Height));

            int width = Math.Max(
                1,
                (int)(HudBrowser.Width * scale));

            int height = Math.Max(
                1,
                (int)(HudBrowser.Height * scale));

            context.ClearState();

            try
            {
                context.OutputMerger.SetRenderTargets(
                    null,
                    target);

                context.OutputMerger.SetBlendState(_blend);
                context.OutputMerger.SetDepthStencilState(_depth);

                context.Rasterizer.State = _rasterizer;

                context.Rasterizer.SetViewport(
                    12,
                    12,
                    width,
                    height);

                context.InputAssembler.InputLayout = null;

                context.InputAssembler.PrimitiveTopology =
                    PrimitiveTopology.TriangleList;

                context.VertexShader.Set(_vertexShader);
                context.PixelShader.Set(_pixelShader);

                context.PixelShader.SetShaderResource(
                    0,
                    _textureView);

                context.PixelShader.SetSampler(
                    0,
                    _sampler);

                context.Draw(3, 0);
            }
            finally
            {
                context.ClearState();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _browser?.Dispose();

            _sampler?.Dispose();
            _rasterizer?.Dispose();
            _depth?.Dispose();
            _blend?.Dispose();

            _pixelShader?.Dispose();
            _vertexShader?.Dispose();

            _textureView?.Dispose();
            _texture?.Dispose();
        }
    }
}
