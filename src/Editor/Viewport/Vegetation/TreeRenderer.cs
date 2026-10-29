using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Editor.Rendering.Shaders;
using Editor.Viewport.Terrain;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Textures.Dds;
using ZoneReborn.World.Vegetation;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Vegetation
{
    internal sealed class TreeRenderer : IDisposable
    {
        private readonly Device _device;
        private readonly DeviceContext _context;
        private readonly VegetationScene _scene;
        private readonly TerrainRenderer _terrain;
        private readonly Dictionary<string, ModelResources> _models = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TerrainTexture> _textures = new(StringComparer.OrdinalIgnoreCase);
        private VertexShader _vertex = null!;
        private PixelShader _pixel = null!;
        private InputLayout _layout = null!;
        private Buffer _constants = null!;
        private SamplerState _sampler = null!;
        private RasterizerState _rasterizer = null!;
        private DepthStencilState _depth = null!;
        public Guid? Selected { get; set; }

        public TreeRenderer(Device device, DeviceContext context, AssetPaths assets, VegetationScene scene, TerrainRenderer terrain)
        {
            _device = device; _context = context; _scene = scene; _terrain = terrain;
            try
            {
                var compiler = new ShaderCompiler(assets);
                using var vs = compiler.Compile("Vegetation/Tree.hlsl", "VSMain", "vs_4_0");
                using var ps = compiler.Compile("Vegetation/Tree.hlsl", "PSMain", "ps_4_0");
                _vertex = new(device, vs.Bytecode); _pixel = new(device, ps.Bytecode);
                _layout = new(device, vs.Bytecode,
                    [new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                     new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                     new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0),
                     new InputElement("TEXCOORD", 1, Format.R32G32B32A32_Float, 32, 0),
                     new InputElement("TEXCOORD", 2, Format.R32G32_Float, 48, 0)]);
                _constants = new(device, Utilities.SizeOf<Constants>(), ResourceUsage.Default,
                    BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                _sampler = new(device, new SamplerStateDescription { Filter = Filter.Anisotropic, MaximumAnisotropy = 8,
                    AddressU = TextureAddressMode.Wrap, AddressV = TextureAddressMode.Wrap, AddressW = TextureAddressMode.Wrap,
                    MinimumLod = 0, MaximumLod = float.MaxValue, ComparisonFunction = Comparison.Never });
                _rasterizer = new(device, new RasterizerStateDescription { FillMode = FillMode.Solid,
                    CullMode = CullMode.None, IsDepthClipEnabled = true });
                _depth = new(device, DepthStencilStateDescription.Default());
            }
            catch { Dispose(); throw; }
        }

        public void Render(FreeCamera camera, Matrix4x4 viewProjection, bool playing)
        {
            var forward = Vector3.Normalize(camera.LookDirection);
            var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
            var up = Vector3.Cross(forward, right);
            _context.InputAssembler.InputLayout = _layout;
            _context.VertexShader.Set(_vertex); _context.VertexShader.SetConstantBuffer(0, _constants);
            _context.PixelShader.Set(_pixel); _context.PixelShader.SetConstantBuffer(0, _constants);
            _context.PixelShader.SetSampler(0, _sampler);
            _context.Rasterizer.State = _rasterizer;
            _context.OutputMerger.SetDepthStencilState(_depth);
            _context.OutputMerger.SetBlendState(null);
            foreach (var tree in _scene.Instances)
            {
                if (!_scene.Assets.TryGetValue(tree.Model, out var asset)) continue;
                var world = World(tree, _terrain);
                var position = new Vector3(world.M41, world.M42, world.M43);
                float radius = Math.Max(asset.Minimum.Length(), asset.Maximum.Length()) * tree.Scale;
                float distance = Vector3.Distance(position, camera.Position);
                if (distance > 1800 + radius || Vector3.Dot(position - camera.Position, forward) < -radius) continue;
                if (!_models.TryGetValue(tree.Model, out var model))
                {
                    model = new ModelResources(_device, asset);
                    _models.Add(tree.Model, model);
                }
                float height = Math.Max(1, asset.Maximum.Y - asset.Minimum.Y) * tree.Scale;
                var constants = new Constants { World = world, ViewProjection = viewProjection,
                    CameraRight = new Vector4(right, tree.Scale), CameraUp = new Vector4(up, 0) };
                for (int i = 0; i < asset.Parts.Length; i++)
                {
                    var part = asset.Parts[i]; var gpu = model.Parts[i];
                    int lod = Math.Clamp((int)(distance / Math.Max(40, height * 8)), 0, part.Lods.Length - 1);
                    while (part.Lods[lod].Length == 0 && lod > 0) lod--;
                    if (part.Lods[lod].Length == 0) continue;
                    constants.Parameters = new Vector4(!playing && tree.Id == Selected ? 1 : 0,
                        part.Kind == TreePartKind.Branches ? 0 : 0.4f, string.IsNullOrEmpty(part.Normal) ? 0 : 1, 0);
                    _context.UpdateSubresource(ref constants, _constants);
                    _context.InputAssembler.PrimitiveTopology = part.Kind is TreePartKind.Branches or TreePartKind.Fronds
                        ? PrimitiveTopology.TriangleStrip : PrimitiveTopology.TriangleList;
                    _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(gpu.Vertices, 56, 0));
                    _context.InputAssembler.SetIndexBuffer(gpu.Indices[lod], Format.R32_UInt, 0);
                    _context.PixelShader.SetShaderResource(0, Texture(part.Diffuse, false).View);
                    _context.PixelShader.SetShaderResource(1, Texture(part.Normal, true).View);
                    _context.DrawIndexed(part.Lods[lod].Length, 0, 0);
                }
            }
            _context.PixelShader.SetShaderResource(0, null); _context.PixelShader.SetShaderResource(1, null);
            _context.OutputMerger.SetDepthStencilState(null); _context.Rasterizer.State = null;
        }

        internal static Matrix4x4 World(TreeInstance tree, TerrainRenderer? terrain)
        {
            float y = tree.Y;
            if (tree.FollowTerrain && terrain != null && terrain.SurfaceHeight(tree.X, tree.Z, out float ground))
                y = ground + tree.HeightOffset;
            return Matrix4x4.CreateScale(tree.Scale) * Matrix4x4.CreateRotationY(tree.Yaw * MathF.PI / 180)
                * Matrix4x4.CreateTranslation(tree.X, y, tree.Z);
        }

        private TerrainTexture Texture(string path, bool normal)
        {
            string key = path.Length > 0 ? path : normal ? "flat-normal" : "missing-diffuse";
            if (_textures.TryGetValue(key, out var cached)) return cached;
            TerrainTexture texture;
            try { texture = path.Length > 0 ? new(_device, DdsReader.Read(path)) : Fallback(normal); }
            catch (Exception exception) when (exception is IOException or InvalidDataException or NotSupportedException)
            {
                _scene.Warnings.Add($"DDS дерева {path}: {exception.Message}"); texture = Fallback(normal);
            }
            _textures.Add(key, texture); return texture;
        }
        private TerrainTexture Fallback(bool normal) => normal ? TerrainTexture.Solid(_device, 128, 128, 255)
            : TerrainTexture.Solid(_device, 150, 150, 150);

        public void Dispose()
        {
            foreach (var model in _models.Values) model.Dispose();
            foreach (var texture in _textures.Values) texture.Dispose();
            _models.Clear(); _textures.Clear();
            _depth?.Dispose(); _rasterizer?.Dispose(); _sampler?.Dispose(); _constants?.Dispose();
            _layout?.Dispose(); _pixel?.Dispose(); _vertex?.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Constants
        {
            public Matrix4x4 World, ViewProjection;
            public Vector4 CameraRight, CameraUp, Parameters;
        }
        private sealed class ModelResources : IDisposable
        {
            public List<(Buffer Vertices, Buffer?[] Indices)> Parts { get; } = [];
            public ModelResources(Device device, TreeAsset asset)
            {
                try
                {
                    foreach (var part in asset.Parts)
                    {
                        var vertex = Buffer.Create(device, BindFlags.VertexBuffer, part.Vertices);
                        var indices = new Buffer?[part.Lods.Length];
                        Parts.Add((vertex, indices));
                        for (int i = 0; i < indices.Length; i++)
                            if (part.Lods[i].Length > 0) indices[i] = Buffer.Create(device, BindFlags.IndexBuffer, part.Lods[i]);
                    }
                }
                catch { Dispose(); throw; }
            }
            public void Dispose()
            {
                foreach (var part in Parts) { part.Vertices.Dispose(); foreach (var buffer in part.Indices) buffer?.Dispose(); }
                Parts.Clear();
            }
        }
    }
}
