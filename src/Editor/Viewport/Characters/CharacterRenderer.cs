using System.Numerics;
using System.Runtime.InteropServices;
using Editor.Rendering.Shaders;
using Editor.Viewport.Terrain;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters;
using ZoneReborn.World.Characters.Locomotion;
using ZoneReborn.World.Textures.Dds;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Characters;

internal sealed class CharacterRenderer : IDisposable
{
    private readonly DeviceContext _context;
    private readonly CharacterPose _pose;
    private readonly CharacterAsset _asset;
    private readonly List<(Buffer Vertices, Buffer Indices, TerrainTexture Texture, TerrainTexture DiffuseMask,
        TerrainTexture ColourMask, TerrainTexture OpacityMask, Vector3 Centre)> _meshes = [];
    private readonly Dictionary<string, TerrainTexture> _textures = new(StringComparer.OrdinalIgnoreCase);
    private TerrainTexture? _white;
    private readonly CharacterLocomotion _locomotion;
    private VertexShader _vertex = null!;
    private PixelShader _pixel = null!;
    private InputLayout _layout = null!;
    private Buffer _constants = null!;
    private SamplerState _sampler = null!;
    private RasterizerState _rasterizer = null!;
    private RasterizerState _doubleSided = null!;
    private DepthStencilState _depth = null!;
    private DepthStencilState _transparentDepth = null!;
    private BlendState _alpha = null!;
    public Vector3 Position => _locomotion.Position;
    public float Yaw => _locomotion.Yaw;

    public CharacterRenderer(Device device, DeviceContext context, AssetPaths assets, CharacterAsset asset, CharacterLocomotion locomotion)
    {
        _context = context; _asset = asset; _locomotion = locomotion; _pose = locomotion.Animator.Pose;
        try
        {
            var compiler = new ShaderCompiler(assets);
            using var vs = compiler.Compile("Characters/Character.hlsl", "VSMain", "vs_4_0");
            using var ps = compiler.Compile("Characters/Character.hlsl", "PSMain", "ps_4_0");
            _vertex = new(device, vs.Bytecode); _pixel = new(device, ps.Bytecode);
            _layout = new(device, vs.Bytecode, [new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0), new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0),
                new InputElement("TEXCOORD", 1, Format.R32G32_Float, 32, 0)]);
            _constants = new(device, Utilities.SizeOf<Constants>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _sampler = new(device, new SamplerStateDescription { Filter = Filter.Anisotropic,
                AddressU = TextureAddressMode.Wrap, AddressV = TextureAddressMode.Wrap, AddressW = TextureAddressMode.Wrap,
                MinimumLod = 0, MaximumLod = float.MaxValue, MaximumAnisotropy = 16, ComparisonFunction = Comparison.Never });
            _rasterizer = new(device, new RasterizerStateDescription { FillMode = FillMode.Solid, CullMode = CullMode.Back, IsDepthClipEnabled = true });
            _doubleSided = new(device, new RasterizerStateDescription { FillMode = FillMode.Solid, CullMode = CullMode.None, IsDepthClipEnabled = true });
            _depth = new(device, DepthStencilStateDescription.Default());
            var transparentDepth = DepthStencilStateDescription.Default();
            transparentDepth.DepthWriteMask = DepthWriteMask.Zero;
            _transparentDepth = new(device, transparentDepth);
            var blend = BlendStateDescription.Default();
            blend.RenderTarget[0].IsBlendEnabled = true;
            blend.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            blend.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            blend.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blend.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            blend.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
            blend.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            blend.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            _alpha = new(device, blend);
            _white = TerrainTexture.Solid(device, 255, 255, 255);
            TerrainTexture Texture(string? path)
            {
                if (path == null) return _white!;
                if (!_textures.TryGetValue(path, out var texture))
                    _textures.Add(path, texture = new TerrainTexture(device, DdsReader.Read(path)));
                return texture;
            }
            for (int i = 0; i < asset.Meshes.Length; i++)
            {
                Buffer? vertices = null, indices = null;
                try
                {
                    vertices = Buffer.Create(device, BindFlags.VertexBuffer, _pose.Vertices[i]);
                    indices = Buffer.Create(device, BindFlags.IndexBuffer, asset.Meshes[i].Indices);
                    var source = asset.Meshes[i];
                    var centre = _pose.Vertices[i].Length == 0 ? Vector3.Zero
                        : _pose.Vertices[i].Aggregate(Vector3.Zero, (sum, vertex) => sum + vertex.Position) / _pose.Vertices[i].Length;
                    _meshes.Add((vertices, indices, Texture(source.DiffusePath), Texture(source.Surface.DiffuseMaskPath),
                        Texture(source.Surface.ColourMaskPath), Texture(source.Surface.OpacityMaskPath), centre));
                }
                catch { indices?.Dispose(); vertices?.Dispose(); throw; }
            }
        }
        catch { Dispose(); throw; }
    }

    public void Render(Matrix4x4 relativeViewProjection, Vector3 cameraPosition)
    {
        var rotation = Matrix4x4.CreateRotationY(Yaw);
        var offset = new Vector3(0, _pose.FloorOffset, 0);
        // Keep vertices near the camera before projection to avoid large world
        // coordinate cancellation between overlapping clothing surfaces.
        var constants = new Constants { World = rotation
            * Matrix4x4.CreateTranslation(Position - cameraPosition + offset), ViewProjection = relativeViewProjection };
        _context.InputAssembler.InputLayout = _layout;
        _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        _context.VertexShader.Set(_vertex); _context.PixelShader.Set(_pixel);
        _context.VertexShader.SetConstantBuffer(0, _constants);
        _context.PixelShader.SetConstantBuffer(0, _constants);
        _context.PixelShader.SetSampler(0, _sampler);
        // Opaque surfaces write depth; transparent overlays are sorted and only read depth.
        foreach (int i in Enumerable.Range(0, _meshes.Count).OrderBy(i => _asset.Meshes[i].Alpha)
            .ThenByDescending(i => _asset.Meshes[i].Alpha
                ? Vector3.DistanceSquared(Vector3.Transform(_meshes[i].Centre, rotation) + Position + offset, cameraPosition) : 0))
        {
            var mesh = _meshes[i];
            var source = _asset.Meshes[i];
            var surface = source.Surface;
            constants.DiffuseColor = source.DiffuseColor;
            constants.ColourR = surface.ColourR; constants.ColourG = surface.ColourG;
            constants.ColourB = surface.ColourB; constants.ColourA = surface.ColourA;
            constants.PatternScaleShift = surface.PatternScaleShift;
            constants.PatternOptions = new(surface.PatternRotation.X, surface.PatternRotation.Y,
                surface.ColourByUv2 ? 1 : 0, surface.ColourMaskPath != null && surface.DiffuseMaskPath != null ? 1 : 0);
            constants.OpacityScaleShift = surface.OpacityScaleShift;
            constants.SurfaceOptions = new(surface.UsesAlpha ? 1 : 0, surface.AlphaCutoff, 0, 0);
            _context.UpdateSubresource(ref constants, _constants);
            _context.UpdateSubresource(_pose.Vertices[i], mesh.Vertices);
            _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(mesh.Vertices, Utilities.SizeOf<SkinnedVertex>(), 0));
            _context.InputAssembler.SetIndexBuffer(mesh.Indices, Format.R32_UInt, 0);
            _context.PixelShader.SetShaderResource(0, mesh.Texture.View);
            _context.PixelShader.SetShaderResource(1, mesh.DiffuseMask.View);
            _context.PixelShader.SetShaderResource(2, mesh.ColourMask.View);
            _context.PixelShader.SetShaderResource(3, mesh.OpacityMask.View);
            _context.Rasterizer.State = surface.DoubleSided ? _doubleSided : _rasterizer;
            _context.OutputMerger.SetDepthStencilState(source.Alpha ? _transparentDepth : _depth);
            _context.OutputMerger.SetBlendState(source.Alpha ? _alpha : null);
            _context.DrawIndexed(_asset.Meshes[i].Indices.Length, 0, 0);
        }
        _context.PixelShader.SetShaderResource(0, null);
        _context.PixelShader.SetShaderResource(1, null);
        _context.PixelShader.SetShaderResource(2, null);
        _context.PixelShader.SetShaderResource(3, null);
        _context.OutputMerger.SetBlendState(null);
        _context.OutputMerger.SetDepthStencilState(null);
        _context.Rasterizer.State = null;
    }

    public void Dispose()
    {
        foreach (var m in _meshes) { m.Indices.Dispose(); m.Vertices.Dispose(); }
        _meshes.Clear();
        foreach (var texture in _textures.Values) texture.Dispose();
        _textures.Clear();
        _white?.Dispose();
        _alpha?.Dispose(); _depth?.Dispose(); _rasterizer?.Dispose(); _sampler?.Dispose();
        _transparentDepth?.Dispose(); _doubleSided?.Dispose();
        _constants?.Dispose(); _layout?.Dispose(); _pixel?.Dispose(); _vertex?.Dispose();
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Constants
    {
        public Matrix4x4 World;
        public Matrix4x4 ViewProjection;
        public System.Numerics.Vector4 DiffuseColor;
        public Vector4 ColourR, ColourG, ColourB, ColourA;
        public Vector4 PatternScaleShift, PatternOptions, OpacityScaleShift, SurfaceOptions;
    }
}
