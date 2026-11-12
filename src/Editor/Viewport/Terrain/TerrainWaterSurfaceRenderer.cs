using System.Numerics;
using System.Runtime.InteropServices;
using Editor.Rendering.Shaders;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Terrain.Models;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Terrain;

internal sealed class TerrainWaterSurfaceRenderer : IDisposable
{
    private readonly DeviceContext _context;

    private VertexShader _vertexShader = null!;
    private PixelShader _pixelShader = null!;
    private InputLayout _layout = null!;
    private Buffer _vertices = null!;
    private Buffer _constants = null!;
    private BlendState _blend = null!;
    private DepthStencilState _depth = null!;
    private RasterizerState _rasterizer = null!;

    public TerrainWaterSurfaceRenderer(
        Device device,
        DeviceContext context,
        AssetPaths assets)
    {
        _context = context;

        try
        {
            var shaders = new ShaderCompiler(assets);

            using var vertex = shaders.Compile(
                "Terrain/WaterSurface.hlsl",
                "VSMain",
                "vs_4_0");

            using var pixel = shaders.Compile(
                "Terrain/WaterSurface.hlsl",
                "PSMain",
                "ps_4_0");

            _vertexShader = new VertexShader(
                device,
                vertex.Bytecode);

            _pixelShader = new PixelShader(
                device,
                pixel.Bytecode);

            _layout = new InputLayout(
                device,
                vertex.Bytecode,
                [
                    new InputElement(
                        "POSITION",
                        0,
                        Format.R32G32_Float,
                        0,
                        0)
                ]);

            Vector2[] vertices =
            [
                new(-1f, -1f),
                new(-1f, 1f),
                new(1f, -1f),

                new(1f, -1f),
                new(-1f, 1f),
                new(1f, 1f)
            ];

            _vertices = Buffer.Create(
                device,
                BindFlags.VertexBuffer,
                vertices);

            _constants = new Buffer(
                device,
                Utilities.SizeOf<WaterConstants>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            var blend = BlendStateDescription.Default();

            blend.RenderTarget[0].IsBlendEnabled = true;
            blend.RenderTarget[0].SourceBlend =
                BlendOption.SourceAlpha;
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

            _blend = new BlendState(device, blend);

            var depth = DepthStencilStateDescription.Default();

            depth.IsDepthEnabled = true;
            depth.DepthWriteMask = DepthWriteMask.Zero;
            depth.DepthComparison = Comparison.LessEqual;

            _depth = new DepthStencilState(device, depth);

            _rasterizer = new RasterizerState(
                device,
                new RasterizerStateDescription
                {
                    FillMode = FillMode.Solid,
                    CullMode = CullMode.None,
                    IsDepthClipEnabled = true
                });
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Render(
        IReadOnlyList<TerrainWaterBody> waters,
        Matrix4x4 viewProjection,
        Vector3 cameraPosition,
        Vector3 sunDirection,
        float time)
    {
        if (waters.Count == 0)
        {
            return;
        }

        _context.InputAssembler.InputLayout = _layout;
        _context.InputAssembler.PrimitiveTopology =
            PrimitiveTopology.TriangleList;

        _context.InputAssembler.SetVertexBuffers(
            0,
            new VertexBufferBinding(
                _vertices,
                Utilities.SizeOf<Vector2>(),
                0));

        _context.InputAssembler.SetIndexBuffer(
            null,
            Format.Unknown,
            0);

        _context.VertexShader.Set(_vertexShader);
        _context.VertexShader.SetConstantBuffer(0, _constants);

        _context.PixelShader.Set(_pixelShader);
        _context.PixelShader.SetConstantBuffer(0, _constants);

        _context.OutputMerger.SetBlendState(_blend);
        _context.OutputMerger.SetDepthStencilState(_depth);
        _context.Rasterizer.State = _rasterizer;

        try
        {
            foreach (var water in waters)
            {
                var constants = new WaterConstants
                {
                    ViewProjection = viewProjection,

                    Lake = new Vector4(
                        water.CenterX,
                        water.CenterZ,
                        water.SurfaceHeight,
                        0f),

                    Shape = new Vector4(
                        water.RadiusX,
                        water.RadiusZ,
                        0f,
                        0f),

                    CameraTime = new Vector4(
                        cameraPosition,
                        time),

                    Sun = new Vector4(
                        sunDirection,
                        Math.Clamp(
                            sunDirection.Y * 1.5f,
                            0f,
                            1f))
                };

                _context.UpdateSubresource(
                    ref constants,
                    _constants);

                _context.Draw(6, 0);
            }
        }
        finally
        {
            _context.InputAssembler.SetVertexBuffers(
                0,
                new VertexBufferBinding(null, 0, 0));

            _context.InputAssembler.InputLayout = null;

            _context.VertexShader.SetConstantBuffer(0, null);
            _context.VertexShader.Set(null);

            _context.PixelShader.SetConstantBuffer(0, null);
            _context.PixelShader.Set(null);

            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(null);
            _context.Rasterizer.State = null;
        }
    }

    public void Dispose()
    {
        _rasterizer?.Dispose();
        _depth?.Dispose();
        _blend?.Dispose();
        _constants?.Dispose();
        _vertices?.Dispose();
        _layout?.Dispose();
        _pixelShader?.Dispose();
        _vertexShader?.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaterConstants
    {
        public Matrix4x4 ViewProjection;
        public Vector4 Lake;
        public Vector4 Shape;
        public Vector4 CameraTime;
        public Vector4 Sun;
    }
}
