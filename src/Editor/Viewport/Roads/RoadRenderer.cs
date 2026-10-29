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
using ZoneReborn.World.Roads;
using ZoneReborn.World.Textures.Dds;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Roads
{
    internal sealed class RoadRenderer : IDisposable
    {
        private readonly Device _device;
        private readonly DeviceContext _context;
        private readonly AssetPaths _assets;
        private readonly RoadNetwork _network;
        private readonly TerrainRenderer _terrain;
        private readonly List<SegmentResources> _segments = [];
        private readonly List<SegmentResources> _visible = [];
        private readonly Dictionary<string, TerrainTexture> _textures = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string?> _texturePaths = new(StringComparer.OrdinalIgnoreCase);
        private VertexShader _vertex = null!;
        private PixelShader _pixel = null!;
        private PixelShader _depthPixel = null!;
        private InputLayout _layout = null!;
        private Buffer _constants = null!;
        private SamplerState _sampler = null!;
        private RasterizerState _rasterizer = null!;
        private RasterizerState _colorRasterizer = null!;
        private DepthStencilState _depth = null!;
        private DepthStencilState _depthRead = null!;
        private BlendState _blend = null!;
        private BlendState _depthOnly = null!;
        public SourceRoad? SelectedRoad { get; set; }
        public RoadSegment? SelectedSegment { get; set; }

        public RoadRenderer(Device device, DeviceContext context, AssetPaths assets, RoadNetwork network, TerrainRenderer terrain)
        {
            _device = device; _context = context; _assets = assets; _network = network; _terrain = terrain;
            try
            {
                var compiler = new ShaderCompiler(assets);
                using var vs = compiler.Compile("Roads/Road.hlsl", "VSMain", "vs_4_0");
                using var ps = compiler.Compile("Roads/Road.hlsl", "PSMain", "ps_4_0");
                using var depthCode = compiler.Compile("Roads/Road.hlsl", "PSDepth", "ps_4_0");
                _vertex = new(device, vs.Bytecode); _pixel = new(device, ps.Bytecode);
                _depthPixel = new(device, depthCode.Bytecode);
                _layout = new(device, vs.Bytecode,
                    [new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                     new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                     new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0)]);
                _constants = new(device, Utilities.SizeOf<Constants>(), ResourceUsage.Default,
                    BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                _sampler = new(device, new SamplerStateDescription
                {
                    Filter = Filter.Anisotropic, MaximumAnisotropy = 8,
                    AddressU = TextureAddressMode.Wrap, AddressV = TextureAddressMode.Clamp,
                    AddressW = TextureAddressMode.Wrap, MinimumLod = 0, MaximumLod = float.MaxValue,
                    ComparisonFunction = Comparison.Never
                });
                var rasterizer = new RasterizerStateDescription
                {
                    FillMode = FillMode.Solid, CullMode = CullMode.None, IsDepthClipEnabled = true,
                    DepthBias = 0, SlopeScaledDepthBias = 0, DepthBiasClamp = 0
                };
                _rasterizer = new(device, rasterizer);
                // Permit the color pass to cover float rasterization roundoff at coincident junctions.
                rasterizer.DepthBias = -1;
                _colorRasterizer = new(device, rasterizer);
                var depth = DepthStencilStateDescription.Default();
                depth.DepthComparison = Comparison.LessEqual;
                _depth = new(device, depth);
                depth.DepthWriteMask = DepthWriteMask.Zero;
                _depthRead = new(device, depth);
                var blend = BlendStateDescription.Default();
                blend.RenderTarget[0].IsBlendEnabled = true;
                blend.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
                blend.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                blend.RenderTarget[0].BlendOperation = BlendOperation.Add;
                blend.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                blend.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
                blend.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                blend.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                _blend = new(device, blend);
                var depthOnly = BlendStateDescription.Default();
                depthOnly.RenderTarget[0].RenderTargetWriteMask = (ColorWriteMaskFlags)0;
                _depthOnly = new(device, depthOnly);
                foreach (var road in network.Roads.OrderBy(r => r.DrawPriority).ThenBy(r => r.Id, StringComparer.OrdinalIgnoreCase))
                foreach (var segment in road.Segments) _segments.Add(new(road, segment));
            }
            catch { Dispose(); throw; }
        }

        public void Render(Vector3 camera, Matrix4x4 viewProjection, float farPlane, bool playing)
        {
            _visible.Clear();
            foreach (var item in _segments)
            {
                item.Refresh();
                var delta = camera - item.Center;
                float distance = item.Segment.FollowTerrain ? MathF.Sqrt(delta.X * delta.X + delta.Z * delta.Z) : delta.Length();
                if (distance > Math.Min(farPlane, 3500) + item.Radius)
                {
                    item.ReleaseSurface();
                    continue;
                }
                item.UpdateSurface(_terrain);
                if (item.Mesh!.Indices.Length == 0) { item.ReleaseGpu(); continue; }
                item.Upload(_device);
                _visible.Add(item);
            }
            _context.InputAssembler.InputLayout = _layout;
            _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _context.VertexShader.Set(_vertex); _context.VertexShader.SetConstantBuffer(0, _constants);
            _context.PixelShader.SetConstantBuffer(0, _constants);
            _context.PixelShader.SetSampler(0, _sampler);
            // Compose against Terrain depth only. No road may populate depth before all color
            // layers have finished: even a tiny height difference would defeat draw_priority.
            _context.PixelShader.Set(_pixel);
            _context.Rasterizer.State = _colorRasterizer;
            _context.OutputMerger.SetDepthStencilState(_depthRead);
            _context.OutputMerger.SetBlendState(_blend);
            foreach (var item in _visible)
                DrawSegment(item, camera, viewProjection, playing);

            // Publish opaque road depth AFTER compositing, for characters/objects drawn later.
            // This pass cannot change the selected material at road junctions.
            _context.PixelShader.Set(_depthPixel);
            _context.Rasterizer.State = _rasterizer;
            _context.OutputMerger.SetDepthStencilState(_depth);
            _context.OutputMerger.SetBlendState(_depthOnly);
            foreach (var item in _visible)
                DrawSegment(item, camera, viewProjection, playing);
            _context.PixelShader.SetShaderResource(0, null);
            _context.PixelShader.SetShaderResource(1, null);
            _context.PixelShader.SetShaderResource(2, null);
            _context.PixelShader.SetShaderResource(3, null);
            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(null);
            _context.Rasterizer.State = null;
        }

        private void DrawSegment(SegmentResources item, Vector3 camera, Matrix4x4 viewProjection, bool playing)
        {
            var segment = item.Segment;
            var material = RoadMaterials.Find(_network, segment.Material);
            string diffuseName = string.IsNullOrWhiteSpace(segment.DiffuseOverride) ? material?.Diffuse ?? "" : segment.DiffuseOverride;
            string normalName = string.IsNullOrWhiteSpace(segment.NormalOverride) ? material?.Normal ?? "" : segment.NormalOverride;
            string specularName = string.IsNullOrWhiteSpace(segment.SpecularOverride) ? material?.Specular ?? "" : segment.SpecularOverride;
            string powerName = string.IsNullOrWhiteSpace(segment.SpecularPowerOverride) ? material?.SpecularPowerMap ?? "" : segment.SpecularPowerOverride;
            string? diffuse = ResolveTexture(diffuseName);
            string? normal = ResolveTexture(normalName);
            string? specular = ResolveTexture(specularName);
            string? power = ResolveTexture(powerName);
            var color = material ?? new RoadMaterial();
            var diffuseMap = GetTexture(diffuse, $"color:{color.Red}:{color.Green}:{color.Blue}", color.Red, color.Green, color.Blue);
            var normalMap = GetTexture(normal, "flat-normal", 128, 128, 255);
            var specularMap = GetTexture(specular, "white-specular", 255, 255, 255);
            var powerMap = GetTexture(power, "white-specular", 255, 255, 255);
            var constants = new Constants
            {
                ViewProjection = viewProjection,
                Parameters = new Vector4(!playing && item.Road == SelectedRoad && segment == SelectedSegment ? 1 : 0,
                    normal != null ? 1 : 0, power != null ? 1 : 0, 0),
                CameraPosition = new Vector4(camera, 0),
                SpecularParameters = new Vector4(color.SpecularStrength, color.Gloss, 0, 0)
            };
            _context.UpdateSubresource(ref constants, _constants);
            _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(item.VertexBuffer!, 32, 0));
            _context.InputAssembler.SetIndexBuffer(item.IndexBuffer!, Format.R32_UInt, 0);
            _context.PixelShader.SetShaderResource(0, diffuseMap.View);
            _context.PixelShader.SetShaderResource(1, normalMap.View);
            _context.PixelShader.SetShaderResource(2, specularMap.View);
            _context.PixelShader.SetShaderResource(3, powerMap.View);
            _context.DrawIndexed(item.Mesh!.Indices.Length, 0, 0);
        }

        private TerrainTexture GetTexture(string? path, string fallback, byte r, byte g, byte b)
        {
            string key = path ?? fallback;
            if (_textures.TryGetValue(key, out var cached)) return cached;
            TerrainTexture texture;
            try { texture = path == null ? TerrainTexture.Solid(_device, r, g, b) : new(_device, DdsReader.Read(path)); }
            catch (Exception exception) when (exception is IOException or InvalidDataException or NotSupportedException)
            {
                _network.Warnings.Add($"DDS {path}: {exception.Message}");
                texture = TerrainTexture.Solid(_device, r, g, b);
            }
            _textures.Add(key, texture);
            return texture;
        }

        private string? ResolveTexture(string name)
        {
            if (!_texturePaths.TryGetValue(name, out string? path))
            {
                path = RoadMaterials.ResolveTexture(_assets, name);
                _texturePaths.Add(name, path);
            }
            return path;
        }

        public (SourceRoad Road, RoadSegment Segment)? Pick(Vector3 origin, Vector3 direction)
        {
            float nearest = float.MaxValue;
            (SourceRoad Road, RoadSegment Segment)? result = null;
            for (int itemIndex = _visible.Count - 1; itemIndex >= 0; itemIndex--)
            {
                var item = _visible[itemIndex];
                Vector3 relative = item.Center - origin;
                float projection = Vector3.Dot(relative, direction);
                if (projection + item.Radius < 0 || projection - item.Radius > nearest ||
                    relative.LengthSquared() - projection * projection > item.Radius * item.Radius) continue;
                var mesh = item.Mesh!;
                for (int i = 0; i < mesh.Indices.Length; i += 3)
                {
                    var a = mesh.Vertices[mesh.Indices[i]].Position;
                    var edge1 = mesh.Vertices[mesh.Indices[i + 1]].Position - a;
                    var edge2 = mesh.Vertices[mesh.Indices[i + 2]].Position - a;
                    var p = Vector3.Cross(direction, edge2);
                    float determinant = Vector3.Dot(edge1, p);
                    if (MathF.Abs(determinant) < 1e-8f) continue;
                    float inv = 1 / determinant;
                    var t = origin - a;
                    float u = Vector3.Dot(t, p) * inv;
                    if (u < 0 || u > 1) continue;
                    var q = Vector3.Cross(t, edge1);
                    float v = Vector3.Dot(direction, q) * inv;
                    if (v < 0 || u + v > 1) continue;
                    float distance = Vector3.Dot(edge2, q) * inv;
                    float tolerance = Math.Max(0.0001f, 8 * MathF.Abs(MathF.BitIncrement(distance) - distance));
                    if (distance < 0 || distance >= nearest - tolerance) continue;
                    nearest = distance; result = (item.Road, item.Segment);
                }
            }
            return result;
        }

        public void ReloadTextures()
        {
            _context.ClearState();
            foreach (var texture in _textures.Values) texture.Dispose();
            _textures.Clear();
            _texturePaths.Clear();
        }

        public void Dispose()
        {
            foreach (var item in _segments) item.ReleaseGpu();
            foreach (var texture in _textures.Values) texture.Dispose();
            _segments.Clear(); _visible.Clear(); _textures.Clear();
            _depthOnly?.Dispose(); _blend?.Dispose(); _depthRead?.Dispose(); _depth?.Dispose();
            _colorRasterizer?.Dispose(); _rasterizer?.Dispose(); _sampler?.Dispose();
            _constants?.Dispose(); _layout?.Dispose(); _depthPixel?.Dispose(); _pixel?.Dispose(); _vertex?.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Constants
        {
            public Matrix4x4 ViewProjection;
            public Vector4 Parameters;
            public Vector4 CameraPosition;
            public Vector4 SpecularParameters;
        }

        private sealed class SegmentResources(SourceRoad road, RoadSegment segment)
        {
            public SourceRoad Road { get; } = road;
            public RoadSegment Segment { get; } = segment;
            public int Revision { get; private set; } = -1;
            public RoadSegmentMesh? Mesh { get; private set; }
            private RoadSegmentMesh? _sourceMesh;
            private RoadSurfaceProjection? _projection;
            public Vector3 Center { get; private set; }
            public float Radius { get; private set; }
            public Buffer? VertexBuffer { get; private set; }
            public Buffer? IndexBuffer { get; private set; }

            public void Refresh()
            {
                if (Revision == Road.Revision) return;
                ReleaseGpu();
                _sourceMesh = RoadMesh.Build(Road, Segment);
                _projection = null;
                Mesh = _sourceMesh;
                UpdateBounds();
                Revision = Road.Revision;
            }
            public void UpdateSurface(TerrainRenderer terrain)
            {
                if (!Segment.FollowTerrain || (_projection != null && _projection.IsCurrent(terrain))) return;
                ReleaseGpu();
                _projection = RoadSurfaceProjector.Build(Road, Segment, _sourceMesh!, terrain);
                Mesh = _projection.Mesh;
                UpdateBounds();
            }
            private void UpdateBounds()
            {
                if (Mesh!.Vertices.Length == 0) return;
                var min = new Vector3(float.MaxValue); var max = new Vector3(float.MinValue);
                foreach (var v in Mesh.Vertices) { min = Vector3.Min(min, v.Position); max = Vector3.Max(max, v.Position); }
                Center = (min + max) * 0.5f; Radius = Vector3.Distance(min, max) * 0.5f;
            }
            public void Upload(Device device)
            {
                if (VertexBuffer != null) return;
                try
                {
                    VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, Mesh!.Vertices);
                    IndexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, Mesh.Indices);
                }
                catch { ReleaseGpu(); throw; }
            }
            public void ReleaseGpu()
            {
                VertexBuffer?.Dispose(); IndexBuffer?.Dispose(); VertexBuffer = null; IndexBuffer = null;
            }
            public void ReleaseSurface()
            {
                ReleaseGpu();
                if (_projection == null) return;
                _projection = null; Mesh = _sourceMesh; UpdateBounds();
            }
        }
    }
}
