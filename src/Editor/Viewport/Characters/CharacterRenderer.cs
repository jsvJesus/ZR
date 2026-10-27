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

namespace Editor.Viewport.Characters;

internal sealed class CharacterRenderer : IDisposable
{
    private readonly DeviceContext _context;
    private readonly CharacterPose _pose;
    private readonly CharacterAsset _asset;
    private readonly List<(Buffer Vertices, Buffer Indices, TerrainTexture Texture)> _meshes = [];
    private readonly CharacterLocomotion _locomotion;
    private VertexShader _vertex = null!;
    private PixelShader _pixel = null!;
    private InputLayout _layout = null!;
    private Buffer _constants = null!;
    private SamplerState _sampler = null!;
    private RasterizerState _rasterizer = null!;
    private DepthStencilState _depth = null!;
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
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0), new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0)]);
            _constants = new(device, Utilities.SizeOf<Constants>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _sampler = new(device, new SamplerStateDescription { Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap, AddressV = TextureAddressMode.Wrap, AddressW = TextureAddressMode.Wrap,
                MinimumLod = 0, MaximumLod = float.MaxValue, MaximumAnisotropy = 1, ComparisonFunction = Comparison.Never });
            _rasterizer = new(device, new RasterizerStateDescription { FillMode = FillMode.Solid, CullMode = CullMode.None, IsDepthClipEnabled = true });
            _depth = new(device, DepthStencilStateDescription.Default());
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
            for (int i = 0; i < asset.Meshes.Length; i++)
            {
                Buffer? vertices = null, indices = null; TerrainTexture? texture = null;
                try
                {
                    vertices = Buffer.Create(device, BindFlags.VertexBuffer, _pose.Vertices[i]);
                    indices = Buffer.Create(device, BindFlags.IndexBuffer, asset.Meshes[i].Indices);
                    texture = new TerrainTexture(device, DdsReader.Read(asset.Meshes[i].DiffusePath));
                    _meshes.Add((vertices, indices, texture));
                }
                catch { texture?.Dispose(); indices?.Dispose(); vertices?.Dispose(); throw; }
            }
        }
        catch { Dispose(); throw; }
    }

    public void Render(Matrix4x4 viewProjection)
    {
        var constants = new Constants { World = Matrix4x4.CreateRotationY(Yaw)
            * Matrix4x4.CreateTranslation(Position + new Vector3(0, _pose.FloorOffset, 0)), ViewProjection = viewProjection };
        _context.UpdateSubresource(ref constants, _constants);
        _context.InputAssembler.InputLayout = _layout;
        _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        _context.VertexShader.Set(_vertex); _context.PixelShader.Set(_pixel);
        _context.VertexShader.SetConstantBuffer(0, _constants);
        _context.PixelShader.SetSampler(0, _sampler);
        _context.Rasterizer.State = _rasterizer;
        _context.OutputMerger.SetDepthStencilState(_depth);
        // Draw opaque parts first, alpha eye overlays last.
        foreach (int i in Enumerable.Range(0, _meshes.Count).OrderBy(i => _asset.Meshes[i].Alpha))
        {
            var mesh = _meshes[i];
            _context.UpdateSubresource(_pose.Vertices[i], mesh.Vertices);
            _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(mesh.Vertices, 32, 0));
            _context.InputAssembler.SetIndexBuffer(mesh.Indices, Format.R32_UInt, 0);
            _context.PixelShader.SetShaderResource(0, mesh.Texture.View);
            _context.OutputMerger.SetBlendState(_asset.Meshes[i].Alpha ? _alpha : null);
            _context.DrawIndexed(_asset.Meshes[i].Indices.Length, 0, 0);
        }
        _context.PixelShader.SetShaderResource(0, null);
        _context.OutputMerger.SetBlendState(null);
        _context.OutputMerger.SetDepthStencilState(null);
        _context.Rasterizer.State = null;
    }

    public void Dispose()
    {
        foreach (var m in _meshes) { m.Texture.Dispose(); m.Indices.Dispose(); m.Vertices.Dispose(); }
        _meshes.Clear();
        _alpha?.Dispose(); _depth?.Dispose(); _rasterizer?.Dispose(); _sampler?.Dispose();
        _constants?.Dispose(); _layout?.Dispose(); _pixel?.Dispose(); _vertex?.Dispose();
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Constants { public Matrix4x4 World; public Matrix4x4 ViewProjection; }
}
