using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Terrain.Storage;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Terrain;

internal sealed class TerrainRenderer : IDisposable
{
    private const int Capacity = 64;
    private readonly Device _device;
    private readonly DeviceContext _context;
    private readonly LoadedLevel _level;
    private readonly Dictionary<(int X, int Z), TerrainTileReference> _references;
    private readonly Dictionary<(int X, int Z), TerrainTileResources> _tiles = [];
    private readonly HashSet<(int X, int Z)> _failed = [];
    private TerrainMaterialCache _materials = null!;
    private TerrainOverviewResources _overview = null!;
    private VertexShader _vertexShader = null!;
    private PixelShader _pixelShader = null!;
    private InputLayout _layout = null!;
    private Buffer _constants = null!;
    private SamplerState _wrap = null!;
    private SamplerState _clamp = null!;
    private RasterizerState _rasterizer = null!;
    private DepthStencilState _depthWrite = null!;
    private DepthStencilState _depthRead = null!;
    private BlendState _add = null!;

    public float FarPlane => Math.Max(1000, Math.Max(_level.Manifest.Terrain.WorldSizeX, _level.Manifest.Terrain.WorldSizeZ) * 1.5f);

    public TerrainRenderer(Device device, DeviceContext context, LoadedLevel level, AssetPaths assets)
    {
        _device = device;
        _context = context;
        _level = level;
        _references = level.Manifest.Tiles.ToDictionary(tile => (tile.X, tile.Z));
        try
        {
            _materials = new TerrainMaterialCache(device, assets);
            using var sourceStream = typeof(TerrainRenderer).Assembly.GetManifestResourceStream(
                "Editor.Viewport.Terrain.Shaders.Terrain.hlsl")
                ?? throw new InvalidOperationException("Embedded Terrain shader was not found.");
            using var reader = new StreamReader(sourceStream);
            string source = reader.ReadToEnd();
            using var vertex = ShaderBytecode.Compile(source, "VSMain", "vs_4_0");
            using var pixel = ShaderBytecode.Compile(source, "PSMain", "ps_4_0");
            _vertexShader = new VertexShader(device, vertex.Bytecode);
            _pixelShader = new PixelShader(device, pixel.Bytecode);
            _layout = new InputLayout(device, vertex.Bytecode, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0)
            });
            _constants = new Buffer(device, Utilities.SizeOf<Constants>(), ResourceUsage.Default,
                BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            var sampler = new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear, AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap, AddressW = TextureAddressMode.Wrap,
                ComparisonFunction = Comparison.Never, MinimumLod = 0, MaximumLod = float.MaxValue,
                MaximumAnisotropy = 1
            };
            _wrap = new SamplerState(device, sampler);
            sampler.AddressU = sampler.AddressV = sampler.AddressW = TextureAddressMode.Clamp;
            _clamp = new SamplerState(device, sampler);
            _rasterizer = new RasterizerState(device, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid, CullMode = CullMode.None, IsDepthClipEnabled = true
            });
            var depth = DepthStencilStateDescription.Default();
            depth.IsDepthEnabled = true;
            depth.DepthWriteMask = DepthWriteMask.All;
            depth.DepthComparison = Comparison.LessEqual;
            _depthWrite = new DepthStencilState(device, depth);
            depth.DepthWriteMask = DepthWriteMask.Zero;
            depth.DepthComparison = Comparison.Equal;
            _depthRead = new DepthStencilState(device, depth);
            var blend = BlendStateDescription.Default();
            blend.RenderTarget[0].IsBlendEnabled = true;
            blend.RenderTarget[0].SourceBlend = BlendOption.One;
            blend.RenderTarget[0].DestinationBlend = BlendOption.One;
            blend.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blend.RenderTarget[0].SourceAlphaBlend = BlendOption.Zero;
            blend.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
            blend.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            blend.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            _add = new BlendState(device, blend);
            _overview = new TerrainOverviewResources(device, level);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Render(FreeCamera camera, Matrix4x4 viewProjection)
    {
        var descriptor = _level.Manifest.Terrain;
        float tileWorldSize = descriptor.TileSize * descriptor.CellSize;
        // A bounded neighborhood keeps large levels usable without expanding the whole map.
        var nearest = _references.Values.OrderBy(tile =>
        {
            float dx = (tile.X + 0.5f) * tileWorldSize - camera.Position.X;
            float dz = (tile.Z + 0.5f) * tileWorldSize - camera.Position.Z;
            return dx * dx + dz * dz;
        }).Take(Capacity).ToArray();
        var wanted = nearest.Select(tile => (tile.X, tile.Z)).ToHashSet();
        foreach (var key in _tiles.Keys.Where(key => !wanted.Contains(key)).ToArray())
        {
            _tiles[key].Dispose();
            _tiles.Remove(key);
        }
        // At most four native tile reads and one mesh upload per frame, including shared-edge samples.
        foreach (var reference in nearest)
        {
            var key = (reference.X, reference.Z);
            if (_tiles.ContainsKey(key) || _failed.Contains(key)) continue;
            try
            {
                var tile = Read(reference.X, reference.Z)!;
                _tiles.Add(key, new TerrainTileResources(_device, descriptor, tile,
                    Read(tile.X + 1, tile.Z), Read(tile.X, tile.Z + 1), Read(tile.X + 1, tile.Z + 1),
                    _level.Layers.Count, _overview.SampleHeight));
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
            {
                _failed.Add(key);
                Trace.TraceError("Terrain tile {0}, {1}: {2}", key.Item1, key.Item2, exception.Message);
            }
            break;
        }

        _materials.BeginFrame();
        _overview.SetCoveredTiles(_tiles.Keys);
        _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        _context.Rasterizer.State = _rasterizer;
        _context.OutputMerger.SetBlendState(null);
        _context.OutputMerger.SetDepthStencilState(_depthWrite);
        _context.PixelShader.SetSampler(0, _wrap);
        _context.PixelShader.SetSampler(1, _clamp);
        _overview.Render(_context, viewProjection, _materials);
        _context.InputAssembler.InputLayout = _layout;
        _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        _context.VertexShader.Set(_vertexShader);
        _context.VertexShader.SetConstantBuffer(0, _constants);
        _context.PixelShader.Set(_pixelShader);
        _context.PixelShader.SetConstantBuffer(0, _constants);
        _context.PixelShader.SetSampler(0, _wrap);
        _context.PixelShader.SetSampler(1, _clamp);
        _context.Rasterizer.State = _rasterizer;
        var constants = new Constants
        {
            ViewProjection = viewProjection,
            Camera = new Vector4(camera.Position, 1),
            WorldExtent = new Vector4(descriptor.SamplesX * descriptor.CellSize, descriptor.SamplesZ * descriptor.CellSize, 0, 0)
        };
        foreach (var reference in nearest)
        {
            if (!_tiles.TryGetValue((reference.X, reference.Z), out var tile)) continue;
            tile.SkirtEdges = 0;
            if (reference.Z > 0 && !_tiles.ContainsKey((reference.X, reference.Z - 1))) tile.SkirtEdges |= 1;
            if (reference.Z + 1 < descriptor.TilesZ && !_tiles.ContainsKey((reference.X, reference.Z + 1))) tile.SkirtEdges |= 2;
            if (reference.X > 0 && !_tiles.ContainsKey((reference.X - 1, reference.Z))) tile.SkirtEdges |= 4;
            if (reference.X + 1 < descriptor.TilesX && !_tiles.ContainsKey((reference.X + 1, reference.Z))) tile.SkirtEdges |= 8;
            _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(tile.Vertices, Utilities.SizeOf<TerrainVertex>(), 0));
            _context.InputAssembler.SetIndexBuffer(tile.Indices, Format.R32_UInt, 0);
            _context.PixelShader.SetShaderResource(2, tile.Normals.View);
            _context.PixelShader.SetShaderResource(4, tile.WeightSum.View);
            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(_depthWrite);
            DrawLayer(tile, _level.Layers[0], null, true, ref constants);
            _context.OutputMerger.SetBlendState(_add);
            _context.OutputMerger.SetDepthStencilState(_depthRead);
            foreach (byte index in tile.ActiveLayers)
            {
                if (index == 0 || index >= _level.Layers.Count || !tile.Masks.TryGetValue((index - 1) / 3, out var mask)) continue;
                DrawLayer(tile, _level.Layers[index], mask, false, ref constants);
            }
        }
        for (int index = 0; index < 5; index++) _context.PixelShader.SetShaderResource(index, null);
        _context.OutputMerger.SetBlendState(null);
        _context.OutputMerger.SetDepthStencilState(null);
        _context.Rasterizer.State = null;
    }

    private void DrawLayer(TerrainTileResources tile, TerrainLayer layer, TerrainTexture? mask,
        bool isBase, ref Constants constants)
    {
        constants.Layer = new Vector4(layer.ScaleU, layer.ScaleV, layer.NormalUvMultiplier, layer.Specular);
        constants.Pass = new Vector4(isBase ? 1 : 0, isBase ? 0 : (layer.Index - 1) % 3, 0, 0);
        _context.UpdateSubresource(ref constants, _constants);
        _context.PixelShader.SetShaderResource(0, _materials.Get(layer.DiffuseTexture, false).View);
        _context.PixelShader.SetShaderResource(1, _materials.Get(layer.NormalTexture, true).View);
        _context.PixelShader.SetShaderResource(3, (mask ?? _materials.DiffuseFallback).View);
        _context.DrawIndexed(tile.IndexCount, 0, 0);
        for (int edge = 0; edge < 4; edge++)
            if ((tile.SkirtEdges & (1 << edge)) != 0)
                _context.DrawIndexed(tile.EdgeIndexCount, tile.IndexCount + edge * tile.EdgeIndexCount, 0);
    }

    private TerrainTileData? Read(int x, int z)
    {
        if (!_references.TryGetValue((x, z), out var reference)) return null;
        var tile = TerrainTileReader.Read(AssetPathResolver.Resolve(_level.Directory, reference.File));
        if (tile.X != x || tile.Z != z) throw new InvalidDataException("Terrain tile coordinates do not match the manifest.");
        return tile;
    }

    public void Dispose()
    {
        foreach (var tile in _tiles.Values) tile.Dispose();
        _tiles.Clear();
        _overview?.Dispose();
        _materials?.Dispose();
        _add?.Dispose();
        _depthRead?.Dispose();
        _depthWrite?.Dispose();
        _rasterizer?.Dispose();
        _clamp?.Dispose();
        _wrap?.Dispose();
        _constants?.Dispose();
        _layout?.Dispose();
        _pixelShader?.Dispose();
        _vertexShader?.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Constants
    {
        public Matrix4x4 ViewProjection;
        public Vector4 Camera;
        public Vector4 WorldExtent;
        public Vector4 Layer;
        public Vector4 Pass;
    }
}
