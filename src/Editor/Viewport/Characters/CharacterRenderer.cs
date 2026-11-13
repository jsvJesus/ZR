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

internal sealed partial class CharacterRenderer : IDisposable
{
    private const float HideDistance = 0.85f;
    private const float ShowDistance = 1.65f;

    private readonly DeviceContext _context;
    private readonly CharacterPose _pose;
    private readonly CharacterAsset _asset;
    private readonly CharacterLocomotion _locomotion;

    private readonly List<(
        Buffer Vertices,
        Buffer Indices,
        TerrainTexture Texture,
        TerrainTexture DiffuseMask,
        TerrainTexture ColourMask,
        TerrainTexture OpacityMask,
        Vector3 Centre)> _meshes = [];

    private readonly Dictionary<string, TerrainTexture> _textures =
        new(StringComparer.OrdinalIgnoreCase);

    private TerrainTexture? _white;
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

    public CharacterRenderer(
        Device device,
        DeviceContext context,
        AssetPaths assets,
        CharacterAsset asset,
        CharacterLocomotion locomotion)
    {
        _context = context;
        _asset = asset;
        _locomotion = locomotion;
        _pose = locomotion.Animator.Pose;

        try
        {
            var compiler = new ShaderCompiler(assets);

            using var vs = compiler.Compile(
                "Characters/Character.hlsl",
                "VSMain",
                "vs_4_0"
            );

            using var ps = compiler.Compile(
                "Characters/Character.hlsl",
                "PSMain",
                "ps_4_0"
            );

            _vertex = new(device, vs.Bytecode);
            _pixel = new(device, ps.Bytecode);

            _layout = new(device, vs.Bytecode,
            [
                new InputElement(
                    "POSITION", 0, Format.R32G32B32_Float, 0, 0
                ),
                new InputElement(
                    "NORMAL", 0, Format.R32G32B32_Float, 12, 0
                ),
                new InputElement(
                    "TEXCOORD", 0, Format.R32G32_Float, 24, 0
                ),
                new InputElement(
                    "TEXCOORD", 1, Format.R32G32_Float, 32, 0
                )
            ]);

            _constants = new(
                device,
                Utilities.SizeOf<Constants>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0
            );

            _sampler = new(device, new SamplerStateDescription
            {
                Filter = Filter.Anisotropic,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MinimumLod = 0,
                MaximumLod = float.MaxValue,
                MaximumAnisotropy = 16,
                ComparisonFunction = Comparison.Never
            });

            _rasterizer = new(device, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                IsDepthClipEnabled = true
            });

            _doubleSided = new(device, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                IsDepthClipEnabled = true
            });

            _depth = new(device, DepthStencilStateDescription.Default());

            var transparentDepth = DepthStencilStateDescription.Default();
            transparentDepth.DepthWriteMask = DepthWriteMask.Zero;
            _transparentDepth = new(device, transparentDepth);

            var blend = BlendStateDescription.Default();

            blend.RenderTarget[0].IsBlendEnabled = true;
            blend.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            blend.RenderTarget[0].DestinationBlend =
                BlendOption.InverseSourceAlpha;
            blend.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blend.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            blend.RenderTarget[0].DestinationAlphaBlend =
                BlendOption.InverseSourceAlpha;
            blend.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            blend.RenderTarget[0].RenderTargetWriteMask =
                ColorWriteMaskFlags.All;

            _alpha = new(device, blend);
            _white = TerrainTexture.Solid(device, 255, 255, 255);

            TerrainTexture Texture(string? path)
            {
                if (path == null)
                {
                    return _white!;
                }

                if (!_textures.TryGetValue(path, out var texture))
                {
                    texture = new TerrainTexture(
                        device,
                        DdsReader.Read(path)
                    );

                    _textures.Add(path, texture);
                }

                return texture;
            }

            for (int i = 0; i < asset.Meshes.Length; i++)
            {
                Buffer? vertices = null;
                Buffer? indices = null;

                try
                {
                    vertices = Buffer.Create(
                        device,
                        BindFlags.VertexBuffer,
                        _pose.Vertices[i]
                    );

                    indices = Buffer.Create(
                        device,
                        BindFlags.IndexBuffer,
                        asset.Meshes[i].Indices
                    );

                    var source = asset.Meshes[i];

                    var centre = _pose.Vertices[i].Length == 0
                        ? Vector3.Zero
                        : _pose.Vertices[i]
                            .Aggregate(
                                Vector3.Zero,
                                (sum, vertex) => sum + vertex.Position
                            ) / _pose.Vertices[i].Length;

                    _meshes.Add((
                        vertices,
                        indices,
                        Texture(source.DiffusePath),
                        Texture(source.Surface.DiffuseMaskPath),
                        Texture(source.Surface.ColourMaskPath),
                        Texture(source.Surface.OpacityMaskPath),
                        centre
                    ));
                    BuildFirstPersonParts(device);
                }
                catch
                {
                    indices?.Dispose();
                    vertices?.Dispose();
                    throw;
                }
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Render(
        Matrix4x4 relativeViewProjection,
        Vector3 cameraPosition)
    {
        float visibility = CalculateVisibility(cameraPosition);

        if (visibility <= 0.001f)
        {
            return;
        }

        bool fading = visibility < 0.999f;

        var rotation = Matrix4x4.CreateRotationY(Yaw);
        var offset = new Vector3(0f, _pose.FloorOffset, 0f);

        var constants = new Constants
        {
            World = rotation * Matrix4x4.CreateTranslation(
                Position - cameraPosition + offset
            ),
            ViewProjection = relativeViewProjection
        };

        _context.InputAssembler.InputLayout = _layout;
        _context.InputAssembler.PrimitiveTopology =
            PrimitiveTopology.TriangleList;

        _context.VertexShader.Set(_vertex);
        _context.PixelShader.Set(_pixel);

        _context.VertexShader.SetConstantBuffer(0, _constants);
        _context.PixelShader.SetConstantBuffer(0, _constants);
        _context.PixelShader.SetSampler(0, _sampler);

        float MeshDistance(int index)
        {
            Vector3 centre = Vector3.Transform(
                _meshes[index].Centre,
                rotation
            ) + Position + offset;

            return Vector3.DistanceSquared(
                centre,
                cameraPosition
            );
        }

        IEnumerable<int> orderedMeshes = Enumerable.Range(
            0,
            _meshes.Count
        );

        orderedMeshes = fading
            ? orderedMeshes.OrderByDescending(MeshDistance)
            : orderedMeshes
                .OrderBy(i => _asset.Meshes[i].Alpha)
                .ThenByDescending(i =>
                    _asset.Meshes[i].Alpha ? MeshDistance(i) : 0f
                );

        foreach (int i in orderedMeshes)
        {
            var mesh = _meshes[i];
            var source = _asset.Meshes[i];
            var surface = source.Surface;

            constants.DiffuseColor = source.DiffuseColor;

            constants.ColourR = surface.ColourR;
            constants.ColourG = surface.ColourG;
            constants.ColourB = surface.ColourB;
            constants.ColourA = surface.ColourA;

            constants.PatternScaleShift = surface.PatternScaleShift;

            constants.PatternOptions = new Vector4(
                surface.PatternRotation.X,
                surface.PatternRotation.Y,
                surface.ColourByUv2 ? 1f : 0f,
                surface.ColourMaskPath != null &&
                surface.DiffuseMaskPath != null ? 1f : 0f
            );

            constants.OpacityScaleShift = surface.OpacityScaleShift;

            constants.SurfaceOptions = new Vector4(
                surface.UsesAlpha ? 1f : 0f,
                surface.AlphaCutoff,
                visibility,
                0f
            );

            _context.UpdateSubresource(ref constants, _constants);
            _context.UpdateSubresource(_pose.Vertices[i], mesh.Vertices);

            _context.InputAssembler.SetVertexBuffers(
                0,
                new VertexBufferBinding(
                    mesh.Vertices,
                    Utilities.SizeOf<SkinnedVertex>(),
                    0
                )
            );

            _context.InputAssembler.SetIndexBuffer(
                mesh.Indices,
                Format.R32_UInt,
                0
            );

            _context.PixelShader.SetShaderResource(
                0,
                mesh.Texture.View
            );

            _context.PixelShader.SetShaderResource(
                1,
                mesh.DiffuseMask.View
            );

            _context.PixelShader.SetShaderResource(
                2,
                mesh.ColourMask.View
            );

            _context.PixelShader.SetShaderResource(
                3,
                mesh.OpacityMask.View
            );

            _context.Rasterizer.State = surface.DoubleSided
                ? _doubleSided
                : _rasterizer;

            bool transparent = source.Alpha || fading;

            _context.OutputMerger.SetDepthStencilState(
                transparent ? _transparentDepth : _depth
            );

            _context.OutputMerger.SetBlendState(
                transparent ? _alpha : null
            );

            _context.DrawIndexed(
                source.Indices.Length,
                0,
                0
            );
        }

        for (int slot = 0; slot < 4; slot++)
        {
            _context.PixelShader.SetShaderResource(slot, null);
        }

        _context.OutputMerger.SetBlendState(null);
        _context.OutputMerger.SetDepthStencilState(null);
        _context.Rasterizer.State = null;
    }

    private float CalculateVisibility(Vector3 cameraPosition)
    {
        Vector3 pivot = _locomotion.CameraAnchor + new Vector3(
            0f,
            _locomotion.CameraHeight,
            0f
        );

        float distance = Vector3.Distance(
            cameraPosition,
            pivot
        );

        float visibility = Math.Clamp(
            (distance - HideDistance) /
            (ShowDistance - HideDistance),
            0f,
            1f
        );

        // Smoothstep prevents an abrupt change in transparency.
        return visibility * visibility * (3f - 2f * visibility);
    }

    public void Dispose()
    {
        DisposeFirstPersonParts();

        foreach (var mesh in _meshes)
        {
            mesh.Indices.Dispose();
            mesh.Vertices.Dispose();
        }

        _meshes.Clear();

        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();

        _white?.Dispose();
        _alpha?.Dispose();
        _depth?.Dispose();
        _rasterizer?.Dispose();
        _sampler?.Dispose();
        _transparentDepth?.Dispose();
        _doubleSided?.Dispose();
        _constants?.Dispose();
        _layout?.Dispose();
        _pixel?.Dispose();
        _vertex?.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Constants
    {
        public Matrix4x4 World;
        public Matrix4x4 ViewProjection;
        public Vector4 DiffuseColor;
        public Vector4 ColourR;
        public Vector4 ColourG;
        public Vector4 ColourB;
        public Vector4 ColourA;
        public Vector4 PatternScaleShift;
        public Vector4 PatternOptions;
        public Vector4 OpacityScaleShift;
        public Vector4 SurfaceOptions;
    }
}
