using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using Editor.Rendering.Shaders;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Terrain.Storage;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Terrain;

internal sealed class TerrainOverviewResources : IDisposable
{
    private readonly Device _device;
    private readonly LoadedLevel _level;
    private readonly int _width;
    private readonly int _height;
    private readonly int _step;
    private readonly ushort[] _heights;
    private readonly HashSet<(int X, int Z)> _covered = [];
    private readonly Dictionary<(int X, int Z), TerrainSurfaceGrid> _surfaceGrids = [];
    private VertexShader _vertexShader = null!;
    private PixelShader _pixelShader = null!;
    private InputLayout _layout = null!;
    private Buffer _vertices = null!;
    private Buffer? _indices;
    private Buffer _constants = null!;
    private int _indexCount;

    public TerrainOverviewResources(Device device, LoadedLevel level, AssetPaths assets)
    {
        _device = device;
        _level = level;
        var terrain = level.Manifest.Terrain;
        var overview = TerrainOverviewReader.Read(AssetPathResolver.Resolve(level.Directory, level.Manifest.OverviewFile));
        _width = overview.Width;
        _height = overview.Height;
        _step = overview.Step;
        _heights = overview.Heights;
        if (_width != (terrain.SamplesX - 1 + _step - 1) / _step + 1 ||
            _height != (terrain.SamplesZ - 1 + _step - 1) / _step + 1)
            throw new InvalidDataException("Terrain overview dimensions do not match the level.");
        try
        {
            var vertices = new Vertex[_width * _height];
            for (int z = 0; z < _height; z++)
            for (int x = 0; x < _width; x++)
            {
                int left = Math.Max(x - 1, 0), right = Math.Min(x + 1, _width - 1);
                int top = Math.Max(z - 1, 0), bottom = Math.Min(z + 1, _height - 1);
                float dx = terrain.DecodeHeight(overview.Heights[z * _width + right]) - terrain.DecodeHeight(overview.Heights[z * _width + left]);
                float dz = terrain.DecodeHeight(overview.Heights[bottom * _width + x]) - terrain.DecodeHeight(overview.Heights[top * _width + x]);
                float spacingX = (Math.Min(right * _step, terrain.SamplesX - 1) - Math.Min(left * _step, terrain.SamplesX - 1)) * terrain.CellSize;
                float spacingZ = (Math.Min(bottom * _step, terrain.SamplesZ - 1) - Math.Min(top * _step, terrain.SamplesZ - 1)) * terrain.CellSize;
                vertices[z * _width + x] = new Vertex(new Vector3(
                    Math.Min(x * _step, terrain.SamplesX - 1) * terrain.CellSize,
                    terrain.DecodeHeight(overview.Heights[z * _width + x]),
                    Math.Min(z * _step, terrain.SamplesZ - 1) * terrain.CellSize),
                    Vector3.Normalize(new Vector3(-dx / spacingX, 1, -dz / spacingZ)));
            }
            _vertices = Buffer.Create(device, BindFlags.VertexBuffer, vertices);
            var shaders = new ShaderCompiler(assets);
            using var vertexCode = shaders.Compile("Terrain/TerrainOverview.hlsl", "VSMain", "vs_4_0");
            using var pixelCode = shaders.Compile("Terrain/TerrainOverview.hlsl", "PSMain", "ps_4_0");
            _vertexShader = new VertexShader(device, vertexCode.Bytecode);
            _pixelShader = new PixelShader(device, pixelCode.Bytecode);
            _layout = new InputLayout(device, vertexCode.Bytecode, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0)
            });
            _constants = new Buffer(device, Utilities.SizeOf<Constants>(), ResourceUsage.Default,
                BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            RebuildIndices();
        }
        catch { Dispose(); throw; }
    }

    public void SetCoveredTiles(IEnumerable<(int X, int Z)> ready)
    {
        var next = ready.ToHashSet();
        if (_covered.SetEquals(next)) return;
        _covered.Clear();
        _covered.UnionWith(next);
        RebuildIndices();
    }

    public float SampleHeight(int globalX, int globalZ)
    {
        var terrain = _level.Manifest.Terrain;
        globalX = Math.Clamp(globalX, 0, terrain.SamplesX - 1);
        globalZ = Math.Clamp(globalZ, 0, terrain.SamplesZ - 1);
        int x = Math.Min(globalX / _step, _width - 2);
        int z = Math.Min(globalZ / _step, _height - 2);
        float u = (globalX - x * _step) / (float)(Math.Min((x + 1) * _step, terrain.SamplesX - 1) - x * _step);
        float v = (globalZ - z * _step) / (float)(Math.Min((z + 1) * _step, terrain.SamplesZ - 1) - z * _step);
        float a = terrain.DecodeHeight(_heights[z * _width + x]);
        float b = terrain.DecodeHeight(_heights[z * _width + x + 1]);
        float c = terrain.DecodeHeight(_heights[(z + 1) * _width + x]);
        float d = terrain.DecodeHeight(_heights[(z + 1) * _width + x + 1]);
        // Match the overview's actual triangle diagonal, rather than a bilinear surface.
        return u + v <= 1 ? a + (b - a) * u + (c - a) * v
            : d + (c - d) * (1 - u) + (b - d) * (1 - v);
    }

    public TerrainSurfaceGrid GetSurfaceGrid(int tileX, int tileZ)
    {
        if (_surfaceGrids.TryGetValue((tileX, tileZ), out var cached)) return cached;
        var terrain = _level.Manifest.Terrain;
        int side = 256 / _step + 1;
        var positions = new Vector3[side * side];
        for (int z = 0; z < side; z++)
        for (int x = 0; x < side; x++)
        {
            int gx = tileX * 256 / _step + x, gz = tileZ * 256 / _step + z;
            positions[z * side + x] = new Vector3(Math.Min(gx * _step, terrain.SamplesX - 1) * terrain.CellSize,
                terrain.DecodeHeight(_heights[gz * _width + gx]), Math.Min(gz * _step, terrain.SamplesZ - 1) * terrain.CellSize);
        }
        var grid = new TerrainSurfaceGrid(positions, side);
        _surfaceGrids.Add((tileX, tileZ), grid);
        return grid;
    }

    private void RebuildIndices()
    {
        var indices = new List<int>((_width - 1) * (_height - 1) * 6);
        for (int z = 0; z < _height - 1; z++)
        for (int x = 0; x < _width - 1; x++)
        {
            if (_covered.Contains((x * _step / 256, z * _step / 256))) continue;
            int a = z * _width + x, b = a + 1, c = a + _width, d = c + 1;
            indices.Add(a); indices.Add(c); indices.Add(b);
            indices.Add(b); indices.Add(c); indices.Add(d);
        }
        Buffer? replacement = indices.Count > 0 ? Buffer.Create(_device, BindFlags.IndexBuffer, indices.ToArray()) : null;
        _indices?.Dispose();
        _indices = replacement;
        _indexCount = indices.Count;
    }

    public void Render(DeviceContext context, Matrix4x4 viewProjection, TerrainMaterialCache materials)
    {
        if (_indexCount == 0) return;
        var terrain = _level.Manifest.Terrain;
        var layer = _level.Layers[0];
        bool farDiffuse = !string.IsNullOrWhiteSpace(terrain.FarDiffuseTexture);
        bool farNormal = !string.IsNullOrWhiteSpace(terrain.FarNormalTexture);
        var constants = new Constants
        {
            ViewProjection = viewProjection,
            WorldExtent = new Vector4(terrain.SamplesX * terrain.CellSize, terrain.SamplesZ * terrain.CellSize, 0, 0),
            Material = new Vector4(
                farDiffuse ? 1 : layer.ScaleU,
                farDiffuse ? 1 : layer.ScaleV,
                farNormal ? 1 : layer.ScaleU * layer.NormalUvMultiplier,
                farNormal ? 1 : layer.ScaleV * layer.NormalUvMultiplier),
            MapMode = new Vector4(farDiffuse ? 1 : 0, farNormal ? 1 : 0, 0, 0)
        };
        context.UpdateSubresource(ref constants, _constants);
        context.InputAssembler.InputLayout = _layout;
        context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertices, Utilities.SizeOf<Vertex>(), 0));
        context.InputAssembler.SetIndexBuffer(_indices, Format.R32_UInt, 0);
        context.VertexShader.Set(_vertexShader);
        context.VertexShader.SetConstantBuffer(0, _constants);
        context.PixelShader.Set(_pixelShader);
        context.PixelShader.SetConstantBuffer(0, _constants);
        context.PixelShader.SetShaderResource(0, materials.Get(
            farDiffuse ? terrain.FarDiffuseTexture : layer.DiffuseTexture, false).View);
        context.PixelShader.SetShaderResource(1, materials.Get(
            farNormal ? terrain.FarNormalTexture : layer.NormalTexture, true).View);
        context.DrawIndexed(_indexCount, 0, 0);
    }

    public void Dispose()
    {
        _constants?.Dispose();
        _indices?.Dispose();
        _vertices?.Dispose();
        _layout?.Dispose();
        _pixelShader?.Dispose();
        _vertexShader?.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Vertex(Vector3 Position, Vector3 Normal);

    [StructLayout(LayoutKind.Sequential)]
    private struct Constants
    {
        public Matrix4x4 ViewProjection;
        public Vector4 WorldExtent;
        public Vector4 Material;
        public Vector4 MapMode;
    }
}
