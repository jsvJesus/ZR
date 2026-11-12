using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using Editor.Rendering.Shaders;
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

internal sealed partial class TerrainRenderer : IDisposable
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
    private readonly AssetPaths _assets;
    private TerrainTexture? _holes;
    private TerrainTexture? _paintOverview;
    private long _paintRevision = -1;
    private long _heightRevision, _holeRevision;
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
    public TerrainDescriptor Descriptor => _level.Manifest.Terrain;

    public TerrainSurfaceGrid GetSurfaceGrid(int tileX, int tileZ) =>
        _tiles.TryGetValue((tileX, tileZ), out var tile) ? tile.Surface : _overview.GetSurfaceGrid(tileX, tileZ);

    public TerrainRenderer(Device device, DeviceContext context, LoadedLevel level, AssetPaths assets)
    {
        _device = device;
        _context = context;
        _level = level;
        _assets = assets;
        _references = level.Manifest.Tiles.ToDictionary(tile => (tile.X, tile.Z));
        try
        {
            _materials = new TerrainMaterialCache(device, assets);
            var shaders = new ShaderCompiler(assets);
            using var vertex = shaders.Compile("Terrain/Terrain.hlsl", "VSMain", "vs_4_0");
            using var pixel = shaders.Compile("Terrain/Terrain.hlsl", "PSMain", "ps_4_0");
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
            _overview = new TerrainOverviewResources(device, level, assets);
            if (level.TerrainDocument?.Holes.Any == true) _holes = TerrainTexture.Holes(device, level.TerrainDocument.Holes);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Render(FreeCamera camera, Matrix4x4 viewProjection)
    {
        if (_level.TerrainDocument is { } paintDocument && paintDocument.MaterialRevision != _paintRevision)
        {
            byte[]? pixels = paintDocument.BuildPaintOverview();
            TerrainTexture? replacement = pixels == null ? null : TerrainTexture.Rgba(_device,
                paintDocument.PaintOverviewWidth, paintDocument.PaintOverviewHeight, pixels);
            _paintOverview?.Dispose(); _paintOverview = replacement; _paintRevision = paintDocument.MaterialRevision;
        }
        if (_level.TerrainDocument is { } document && document.HeightRevision != _heightRevision)
        {
            foreach (var tile in _tiles.Values) tile.Dispose();
            _tiles.Clear(); _failed.Clear(); _overview.Dispose();
            _overview = new TerrainOverviewResources(_device, _level, _assets);
            _heightRevision = document.HeightRevision;
        }
        if (_level.TerrainDocument is { } holeDocument && holeDocument.HoleRevision != _holeRevision)
        {
            _holes?.Dispose(); _holes = null;
            if (holeDocument.Holes.Any) _holes = TerrainTexture.Holes(_device, holeDocument.Holes);
            _holeRevision = holeDocument.HoleRevision;
        }
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
        _context.PixelShader.SetShaderResource(5, _holes?.View);
        _context.PixelShader.SetShaderResource(6, _paintOverview?.View);
        _overview.Render(_context, viewProjection, _materials, _paintOverview != null);
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
            WorldExtent = new Vector4(descriptor.SamplesX * descriptor.CellSize, descriptor.SamplesZ * descriptor.CellSize,
                descriptor.CellSize, _holes == null ? 0 : 1)
        };
        foreach (var reference in nearest)
        {
            if (!_tiles.TryGetValue((reference.X, reference.Z), out var tile)) continue;
            if (_level.TerrainDocument is { } materialsDocument)
            {
                long version = materialsDocument.TileMaterialVersion(reference.X, reference.Z);
                if (tile.MaterialVersion != version)
                {
                    tile.RefreshMaterials(_device, materialsDocument.Tile(reference.X, reference.Z), _level.Layers.Count);
                    tile.MaterialVersion = version;
                }
            }
            int skirtEdges = 0;
            if (reference.Z > 0 && !_tiles.ContainsKey((reference.X, reference.Z - 1))) skirtEdges |= 1;
            if (reference.Z + 1 < descriptor.TilesZ && !_tiles.ContainsKey((reference.X, reference.Z + 1))) skirtEdges |= 2;
            if (reference.X > 0 && !_tiles.ContainsKey((reference.X - 1, reference.Z))) skirtEdges |= 4;
            if (reference.X + 1 < descriptor.TilesX && !_tiles.ContainsKey((reference.X + 1, reference.Z))) skirtEdges |= 8;
            tile.UpdateLodEdges(_context, skirtEdges);
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
        for (int index = 0; index <= 20; index++)
        {
            _context.PixelShader.SetShaderResource(index, null);
        }
        _context.OutputMerger.SetBlendState(null);
        _context.OutputMerger.SetDepthStencilState(null);
        _context.Rasterizer.State = null;
    }

    private void DrawLayer(
        TerrainTileResources tile,
        TerrainLayer layer,
        TerrainTexture? mask,
        bool isBase,
        ref Constants constants)
    {
        var variants = layer.GetTextureVariants();

        var active = new List<TerrainTextureVariant>
        {
            variants[0]
        };

        for (int i = 1; i < variants.Count && active.Count < 4; i++)
        {
            if (!string.IsNullOrWhiteSpace(variants[i].AlbedoTexture))
            {
                active.Add(variants[i]);
            }
        }

        var visual = layer.Settings.Visual;
        var blend = layer.Settings.Blend;

        constants.Layer = new Vector4(
            layer.ScaleU,
            layer.ScaleV,
            layer.NormalUvMultiplier,
            layer.Specular);

        constants.LayerPassParameters = new Vector4(
            isBase ? 1f : 0f,
            isBase ? 0f : (layer.Index - 1) % 3,
            0f,
            0f);

        constants.PbrProperties = new Vector4(
            visual.Roughness,
            visual.Metallic,
            visual.Specular,
            visual.Opacity);

        constants.BlendProperties = new Vector4(
            blend.NoiseIntensity,
            blend.HeightIntensity,
            blend.SlopeIntensity,
            blend.MacroIntensity);

        constants.CoveringSettings = new Vector4(
            active.Count,
            blend.MacroScaleMeters,
            0f,
            0f);

        Span<float> meters = stackalloc float[4];
        Span<float> slope = stackalloc float[4];
        Span<float> roughness = stackalloc float[4];
        Span<float> height = stackalloc float[4];

        meters.Clear();
        slope.Clear();
        roughness.Clear();
        height.Clear();

        for (int index = 0; index < active.Count; index++)
        {
            var variant = active[index];

            float tiling = variant.TilingMeters;

            if (!float.IsFinite(tiling) || tiling <= 0f)
            {
                tiling = 4f;
            }

            meters[index] = 1f / tiling;

            slope[index] = Math.Clamp(
                variant.SlopeBias,
                -1f,
                1f);

            var flags = BindPbrVariant(
                index,
                layer,
                variant);

            roughness[index] = flags.Roughness;
            height[index] = flags.Height;
        }

        constants.CoveringMeters = new Vector4(
            meters[0],
            meters[1],
            meters[2],
            meters[3]);

        constants.CoveringSlope = new Vector4(
            slope[0],
            slope[1],
            slope[2],
            slope[3]);

        constants.RoughnessFlags = new Vector4(
            roughness[0],
            roughness[1],
            roughness[2],
            roughness[3]);

        constants.HeightFlags = new Vector4(
            height[0],
            height[1],
            height[2],
            height[3]);

        _context.PixelShader.SetShaderResource(
            3,
            (mask ?? _materials.DiffuseFallback).View);

        _context.OutputMerger.SetBlendState(
            isBase ? null : _add);

        _context.OutputMerger.SetDepthStencilState(
            isBase ? _depthWrite : _depthRead);

        _context.UpdateSubresource(
            ref constants,
            _constants);

        _context.DrawIndexed(
            tile.IndexCount,
            0,
            0);

        for (int edge = 0; edge < 4; edge++)
        {
            if ((tile.SkirtEdges & (1 << edge)) == 0)
            {
                continue;
            }

            _context.DrawIndexed(
                tile.EdgeIndexCount,
                tile.IndexCount + edge * tile.EdgeIndexCount,
                0);
        }
    }

    private (float Roughness, float Height) BindPbrVariant(
        int index,
        TerrainLayer layer,
        TerrainTextureVariant variant)
    {
        string albedoPath = index == 0
            ? layer.DiffuseTexture
            : variant.AlbedoTexture;

        string normalPath = index == 0
            ? layer.NormalTexture
            : variant.NormalTexture;

        TerrainTexture albedo = _materials.Get(
            albedoPath,
            false);

        TerrainTexture normal = _materials.Get(
            normalPath,
            true);

        TerrainTexture roughness = _materials.Get(
            variant.RoughnessTexture,
            false);

        TerrainTexture height = _materials.Get(
            variant.HeightTexture,
            false);

        int albedoSlot;
        int normalSlot;
        int roughnessSlot;
        int heightSlot;

        if (index == 0)
        {
            albedoSlot = 0;
            normalSlot = 1;
            roughnessSlot = 7;
            heightSlot = 8;
        }
        else
        {
            int first = 9 + (index - 1) * 4;

            albedoSlot = first;
            normalSlot = first + 1;
            roughnessSlot = first + 2;
            heightSlot = first + 3;
        }

        _context.PixelShader.SetShaderResource(
            albedoSlot,
            albedo.View);

        _context.PixelShader.SetShaderResource(
            normalSlot,
            normal.View);

        _context.PixelShader.SetShaderResource(
            roughnessSlot,
            roughness.View);

        _context.PixelShader.SetShaderResource(
            heightSlot,
            height.View);

        bool hasRoughness =
            !string.IsNullOrWhiteSpace(variant.RoughnessTexture) &&
            !ReferenceEquals(
                roughness,
                _materials.DiffuseFallback);

        bool hasHeight =
            !string.IsNullOrWhiteSpace(variant.HeightTexture) &&
            !ReferenceEquals(
                height,
                _materials.DiffuseFallback);

        return (
            hasRoughness ? 1f : 0f,
            hasHeight ? 1f : 0f
        );
    }

    private TerrainTileData? Read(int x, int z)
    {
        if (!_references.TryGetValue((x, z), out var reference)) return null;
        var tile = _level.TerrainDocument?.Tile(x, z)
            ?? TerrainTileReader.Read(AssetPathResolver.Resolve(_level.Directory, reference.File));
        if (tile.X != x || tile.Z != z) throw new InvalidDataException("Terrain tile coordinates do not match the manifest.");
        return tile;
    }

    public void Dispose()
    {
        foreach (var tile in _tiles.Values) tile.Dispose();
        _tiles.Clear();
        _overview?.Dispose();
        _holes?.Dispose();
        _paintOverview?.Dispose();
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
        public Vector4 LayerPassParameters;

        public Vector4 PbrProperties;
        public Vector4 BlendProperties;
        public Vector4 CoveringSettings;
        public Vector4 CoveringMeters;
        public Vector4 CoveringSlope;
        public Vector4 RoughnessFlags;
        public Vector4 HeightFlags;
    }
}
