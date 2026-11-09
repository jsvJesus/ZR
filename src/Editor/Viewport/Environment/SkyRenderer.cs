using System.Numerics;
using System.Runtime.InteropServices;
using Editor.Environment;
using Editor.Rendering.Shaders;
using Editor.Viewport.Terrain;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Textures.Dds;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Editor.Viewport.Environment
{
    internal sealed class SkyRenderer : IDisposable
    {
        private readonly DeviceContext _context;

        private VertexShader _vertex = null!;
        private PixelShader _pixel = null!;
        private Buffer _constants = null!;
        private SamplerState _sampler = null!;
        private RasterizerState _rasterizer = null!;
        private DepthStencilState _depthDisabled = null!;

        private TerrainTexture? _cumulus;
        private TerrainTexture? _cirrus;
        private TerrainTexture? _storm;

        public SkyRenderer(
            Device device,
            DeviceContext context,
            AssetPaths assets)
        {
            _context = context;

            try
            {
                var compiler = new ShaderCompiler(assets);

                using var vertexCode = compiler.Compile(
                    "Environment/Sky.hlsl",
                    "VSMain",
                    "vs_4_0");

                using var pixelCode = compiler.Compile(
                    "Environment/Sky.hlsl",
                    "PSMain",
                    "ps_4_0");

                _vertex = new VertexShader(device, vertexCode.Bytecode);
                _pixel = new PixelShader(device, pixelCode.Bytecode);

                _constants = new Buffer(
                    device,
                    Utilities.SizeOf<SkyConstants>(),
                    ResourceUsage.Default,
                    BindFlags.ConstantBuffer,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.None,
                    0);

                _sampler = new SamplerState(
                    device,
                    new SamplerStateDescription
                    {
                        Filter = Filter.MinMagMipLinear,
                        AddressU = TextureAddressMode.Wrap,
                        AddressV = TextureAddressMode.Wrap,
                        AddressW = TextureAddressMode.Wrap,
                        ComparisonFunction = Comparison.Never,
                        MinimumLod = 0,
                        MaximumLod = float.MaxValue
                    });

                _rasterizer = new RasterizerState(
                    device,
                    new RasterizerStateDescription
                    {
                        FillMode = FillMode.Solid,
                        CullMode = CullMode.None,
                        IsDepthClipEnabled = true
                    });

                var depth = DepthStencilStateDescription.Default();
                depth.IsDepthEnabled = false;
                depth.DepthWriteMask = DepthWriteMask.Zero;

                _depthDisabled = new DepthStencilState(device, depth);

                _cumulus = LoadTexture(
                    device,
                    assets,
                    "weather/maps/cumulus_overhead_cloud.dds");

                _cirrus = LoadTexture(
                    device,
                    assets,
                    "weather/maps/cirrus_overhead_cloud.dds");

                _storm = LoadTexture(
                    device,
                    assets,
                    "weather/maps/storm_overhead_cloud.dds");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Render(
            FreeCamera camera,
            EnvironmentSettings settings,
            WeatherFrame weather,
            Vector3 sunDirection,
            float daylight,
            int width,
            int height,
            bool playing,
            float elapsed)
        {
            Vector3 forward = Vector3.Normalize(camera.LookDirection);

            Vector3 axis = MathF.Abs(forward.Y) > 0.99f
                ? Vector3.UnitZ
                : Vector3.UnitY;

            Vector3 right = Vector3.Normalize(
                Vector3.Cross(axis, forward));

            Vector3 up = Vector3.Normalize(
                Vector3.Cross(forward, right));

            float fieldOfView = playing ? 0.78f : 1.05f;
            float tangent = MathF.Tan(fieldOfView * 0.5f);
            float aspect = (float)width / height;

            float clouds = weather.CloudCoverage;
            float storm = weather.StormCover;

            var data = new SkyConstants
            {
                Right = new Vector4(right, 0),
                Up = new Vector4(up, 0),
                Forward = new Vector4(forward, 0),
                Sun = new Vector4(sunDirection, daylight),
                Viewport = new Vector4(
                    width,
                    height,
                    tangent * aspect,
                    tangent),
                Cloud = new Vector4(
                    clouds,
                    settings.CloudSpeed,
                    elapsed,
                    storm),
                Weather = new Vector4(
                    settings.WindDirection * MathF.PI / 180f,
                    settings.WindSpeed,
                    settings.SunIntensity,
                    settings.FogDensity),
                Textures = new Vector4(
                    _cumulus != null ? 1f : 0f,
                    _cirrus != null ? 1f : 0f,
                    _storm != null ? 1f : 0f,
                    weather.Lightning)
            };

            _context.UpdateSubresource(ref data, _constants);

            _context.InputAssembler.InputLayout = null;
            _context.InputAssembler.PrimitiveTopology =
                PrimitiveTopology.TriangleList;

            _context.VertexShader.Set(_vertex);
            _context.PixelShader.Set(_pixel);

            _context.PixelShader.SetConstantBuffer(0, _constants);
            _context.PixelShader.SetSampler(0, _sampler);

            _context.PixelShader.SetShaderResource(
                0,
                _cumulus?.View);

            _context.PixelShader.SetShaderResource(
                1,
                _cirrus?.View);

            _context.PixelShader.SetShaderResource(
                2,
                _storm?.View);

            _context.Rasterizer.State = _rasterizer;
            _context.OutputMerger.SetDepthStencilState(_depthDisabled);
            _context.OutputMerger.SetBlendState(null);

            _context.Draw(3, 0);

            _context.PixelShader.SetShaderResource(0, null);
            _context.PixelShader.SetShaderResource(1, null);
            _context.PixelShader.SetShaderResource(2, null);

            _context.OutputMerger.SetDepthStencilState(null);
            _context.Rasterizer.State = null;
        }

        private static TerrainTexture? LoadTexture(
            Device device,
            AssetPaths assets,
            string relativePath)
        {
            string path = Path.Combine(
                assets.Root,
                relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return new TerrainTexture(device, DdsReader.Read(path));
            }
            catch (InvalidDataException)
            {
                return null;
            }
        }

        public void Dispose()
        {
            _storm?.Dispose();
            _cirrus?.Dispose();
            _cumulus?.Dispose();

            _depthDisabled?.Dispose();
            _rasterizer?.Dispose();
            _sampler?.Dispose();
            _constants?.Dispose();
            _pixel?.Dispose();
            _vertex?.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SkyConstants
        {
            public Vector4 Right;
            public Vector4 Up;
            public Vector4 Forward;
            public Vector4 Sun;
            public Vector4 Viewport;
            public Vector4 Cloud;
            public Vector4 Weather;
            public Vector4 Textures;
        }
    }
}
