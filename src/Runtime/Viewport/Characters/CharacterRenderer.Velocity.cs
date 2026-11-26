using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Runtime.Rendering.Shaders;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Runtime.Viewport.Characters;

internal sealed partial class CharacterRenderer
{
    private readonly List<VelocityMesh?> _velocityMeshes = [];
    private readonly Dictionary<int, Matrix4x4> _velocityColorMeshes = [];

    private VertexShader? _velocityVertex;
    private PixelShader? _velocityPixel;
    private PixelShader? _velocityReactivePixel;
    private InputLayout? _velocityLayout;
    private Buffer? _velocityConstants;
    private DepthStencilState? _velocityDepth;
    private BlendState? _velocityOpaqueBlend;
    private BlendState? _velocityReactiveBlend;

    private bool _velocityHasHistory;
    private bool _velocityPreviousFirstPerson;
    private long _velocityColorFrame;
    private long _velocityCapturedFrame;
    private Vector3 _velocityPreviousPosition;
    private float _velocityPreviousYaw;

    // Called by the main color pass, including frames with TAA disabled.
    // Shadow passes must never advance this stamp or record visible meshes.
    private void BeginVelocityColorFrame()
    {
        _velocityColorFrame++;
        _velocityColorMeshes.Clear();
    }

    public void RenderVelocity(
        Device device,
        RenderTargetView motionTarget,
        RenderTargetView reactiveTarget,
        DepthStencilView depthTarget,
        Matrix4x4 currentViewProjection,
        Matrix4x4 previousViewProjection,
        Vector3 currentCameraPosition,
        Vector3 previousCameraPosition,
        bool firstPerson,
        bool historyValid,
        int width,
        int height,
        float firstPersonBlend)
    {
        EnsureVelocityResources(device);

        Vector3 currentPosition = Position + new Vector3(0f, _pose.FloorOffset, 0f);
        bool valid = historyValid && _velocityHasHistory &&
            _velocityCapturedFrame + 1 == _velocityColorFrame &&
            _velocityPreviousFirstPerson == firstPerson &&
            Vector3.DistanceSquared(currentPosition, _velocityPreviousPosition) < 16f;

        Matrix4x4 currentRelativeProjection =
            Matrix4x4.CreateTranslation(currentCameraPosition) * currentViewProjection;
        Matrix4x4 previousRelativeProjection =
            Matrix4x4.CreateTranslation(previousCameraPosition) * previousViewProjection;
        Matrix4x4 currentWorld = Matrix4x4.CreateRotationY(Yaw) *
            Matrix4x4.CreateTranslation(currentPosition - currentCameraPosition);
        Matrix4x4 previousWorld = valid
            ? Matrix4x4.CreateRotationY(_velocityPreviousYaw) *
                Matrix4x4.CreateTranslation(_velocityPreviousPosition - previousCameraPosition)
            : currentWorld;

        float blend = Math.Clamp(firstPersonBlend, 0f, 1f);
        float visibility = firstPerson || blend > ViewTransitionEpsilon
            ? 1f
            : CalculateVisibility(currentCameraPosition);
        bool fading = visibility < 0.999f;
        bool matchingMeshDomain = firstPerson == (blend >= ViewSwitchThreshold);

        try
        {
            _context.ClearState();
            if (!matchingMeshDomain || visibility <= 0.001f)
            {
                return;
            }

            _context.OutputMerger.SetDepthStencilState(_velocityDepth);
            _context.Rasterizer.SetViewport(0, 0, width, height);
            _context.InputAssembler.InputLayout = _velocityLayout;
            _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _context.VertexShader.Set(_velocityVertex);
            _context.VertexShader.SetConstantBuffer(0, _velocityConstants);
            _context.PixelShader.SetConstantBuffer(0, _velocityConstants);
            _context.PixelShader.SetSampler(0, _sampler);

            // Draw opaque/cutout motion first. Blended meshes only increase
            // reactivity; they never replace the opaque surface's motion/depth.
            for (int pass = 0; pass < 2; pass++)
            {
                bool reactiveOnly = pass == 1;
                if (reactiveOnly)
                {
                    _context.OutputMerger.SetRenderTargets(depthTarget, reactiveTarget);
                    _context.OutputMerger.SetBlendState(_velocityReactiveBlend);
                    _context.PixelShader.Set(_velocityReactivePixel);
                }
                else
                {
                    _context.OutputMerger.SetRenderTargets(depthTarget, motionTarget, reactiveTarget);
                    _context.OutputMerger.SetBlendState(_velocityOpaqueBlend);
                    _context.PixelShader.Set(_velocityPixel);
                }

                for (int i = 0; i < _meshes.Count; i++)
                {
                    var source = _asset.Meshes[i];
                    var mesh = _meshes[i];
                    if (!_velocityColorMeshes.TryGetValue(i, out var colorViewProjection) ||
                        source.Indices.Length == 0 ||
                        (source.Alpha || fading) != reactiveOnly ||
                        (firstPerson && !source.FirstPersonVisible && source.Attachment?.Weapon == null))
                    {
                        continue;
                    }

                    var state = EnsureVelocityMesh(device, i);
                    if (state == null)
                    {
                        continue;
                    }

                    bool meshValid = valid && state.WasVisible && state.WasOpaque;
                    if (!meshValid)
                    {
                        CopyVelocityPositions(i, state);
                    }
                    _context.UpdateSubresource(state.Positions, state.PreviousBuffer);

                    var constants = new VelocityConstants
                    {
                        CurrentWorld = currentWorld,
                        PreviousWorld = meshValid ? previousWorld : currentWorld,
                        CurrentViewProjection = currentRelativeProjection,
                        PreviousViewProjection = meshValid
                            ? previousRelativeProjection
                            : currentRelativeProjection,
                        ColorViewProjection = colorViewProjection,
                        PreviousCameraOffset = new Vector4(
                            meshValid ? previousCameraPosition - currentCameraPosition : Vector3.Zero,
                            0f),
                        AlphaSettings = new Vector4(
                            source.Surface.UsesAlpha ? 1f : 0f,
                            source.Surface.AlphaCutoff,
                            source.DiffuseColor.W,
                            visibility),
                        OpacityScaleShift = source.Surface.OpacityScaleShift,
                        // Reactive strength is local to arms/weapons, leaving
                        // the rest of the scene's temporal feedback unchanged.
                        MotionSettings = new Vector4(
                            meshValid ? 1f : 0f,
                            source.Attachment?.Weapon != null ? 0.65f : firstPerson ? 0.35f : 0f,
                            width,
                            height)
                    };
                    _context.UpdateSubresource(ref constants, _velocityConstants!);
                    _context.InputAssembler.SetVertexBuffers(
                        0,
                        new VertexBufferBinding(mesh.Vertices, Utilities.SizeOf<SkinnedVertex>(), 0),
                        new VertexBufferBinding(state.PreviousBuffer, Utilities.SizeOf<Vector3>(), 0));
                    _context.InputAssembler.SetIndexBuffer(mesh.Indices, Format.R32_UInt, 0);
                    _context.PixelShader.SetShaderResource(0, mesh.Texture.View);
                    _context.PixelShader.SetShaderResource(1, mesh.OpacityMask.View);
                    _context.Rasterizer.State = source.Surface.DoubleSided ? _doubleSided : _rasterizer;
                    _context.DrawIndexed(source.Indices.Length, 0, 0);
                }
            }
        }
        finally
        {
            _context.ClearState();
            CaptureVelocityPose(device, currentPosition, firstPerson, !fading && matchingMeshDomain);
        }
    }

    private void EnsureVelocityResources(Device device)
    {
        if (_velocityVertex != null)
        {
            return;
        }

        try
        {
            var compiler = new ShaderCompiler(AssetPaths.FromApplication());
            using var vertexCode = compiler.Compile(
                "Characters/CharacterVelocity.hlsl", "VSMain", "vs_4_0");
            using var pixelCode = compiler.Compile(
                "Characters/CharacterVelocity.hlsl", "PSMain", "ps_5_0");
            using var reactiveCode = compiler.Compile(
                "Characters/CharacterVelocity.hlsl", "PSReactive", "ps_5_0");

            _velocityVertex = new VertexShader(device, vertexCode.Bytecode);
            _velocityPixel = new PixelShader(device, pixelCode.Bytecode);
            _velocityReactivePixel = new PixelShader(device, reactiveCode.Bytecode);
            _velocityLayout = new InputLayout(device, vertexCode.Bytecode,
            [
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0),
                new InputElement("POSITION", 1, Format.R32G32B32_Float, 0, 1)
            ]);
            _velocityConstants = new Buffer(
                device, Utilities.SizeOf<VelocityConstants>(), ResourceUsage.Default,
                BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            var depthDescription = DepthStencilStateDescription.Default();
            depthDescription.DepthWriteMask = DepthWriteMask.Zero;
            depthDescription.DepthComparison = Comparison.LessEqual;
            _velocityDepth = new DepthStencilState(device, depthDescription);

            // Preserve reactive masks accumulated by other geometry passes.
            var opaqueBlend = BlendStateDescription.Default();
            opaqueBlend.IndependentBlendEnable = true;
            ConfigureVelocityMaxBlend(ref opaqueBlend, 1);
            _velocityOpaqueBlend = new BlendState(device, opaqueBlend);
            var reactiveBlend = BlendStateDescription.Default();
            ConfigureVelocityMaxBlend(ref reactiveBlend, 0);
            _velocityReactiveBlend = new BlendState(device, reactiveBlend);
        }
        catch
        {
            DisposeVelocity();
            throw;
        }
    }

    private static void ConfigureVelocityMaxBlend(ref BlendStateDescription description, int target)
    {
        description.RenderTarget[target].IsBlendEnabled = true;
        description.RenderTarget[target].SourceBlend = BlendOption.One;
        description.RenderTarget[target].DestinationBlend = BlendOption.One;
        description.RenderTarget[target].BlendOperation = BlendOperation.Maximum;
        description.RenderTarget[target].SourceAlphaBlend = BlendOption.One;
        description.RenderTarget[target].DestinationAlphaBlend = BlendOption.One;
        description.RenderTarget[target].AlphaBlendOperation = BlendOperation.Maximum;
        description.RenderTarget[target].RenderTargetWriteMask = ColorWriteMaskFlags.Red;
    }

    private VelocityMesh? EnsureVelocityMesh(Device device, int index)
    {
        while (_velocityMeshes.Count < _meshes.Count)
        {
            _velocityMeshes.Add(null);
        }
        for (int i = _velocityMeshes.Count - 1; i >= _meshes.Count; i--)
        {
            _velocityMeshes[i]?.Dispose();
            _velocityMeshes.RemoveAt(i);
        }

        var vertices = _pose.Vertices[index];
        var source = _asset.Meshes[index];
        var mesh = _meshes[index];
        var state = _velocityMeshes[index];
        // A rebuilt stream or mesh with the same vertex count still needs a
        // fresh snapshot. CPU skinning retains the array identity each frame.
        if (state != null &&
            (!ReferenceEquals(state.Source, source) ||
             !ReferenceEquals(state.SourceVertices, vertices) ||
             !ReferenceEquals(state.CurrentBuffer, mesh.Vertices) ||
             !ReferenceEquals(state.Indices, source.Indices) ||
             state.Positions.Length != vertices.Length))
        {
            state.Dispose();
            _velocityMeshes[index] = state = null;
        }
        if (state == null && vertices.Length > 0)
        {
            var positions = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                positions[i] = vertices[i].Position;
            }
            state = new VelocityMesh(
                Buffer.Create(device, BindFlags.VertexBuffer, positions), positions,
                source, vertices, mesh.Vertices);
            _velocityMeshes[index] = state;
        }
        return state;
    }

    private void CopyVelocityPositions(int index, VelocityMesh state)
    {
        var vertices = _pose.Vertices[index];
        for (int vertex = 0; vertex < vertices.Length; vertex++)
        {
            state.Positions[vertex] = vertices[vertex].Position;
        }
    }

    private void CaptureVelocityPose(Device device, Vector3 position, bool firstPerson, bool opaque)
    {
        for (int i = 0; i < _meshes.Count; i++)
        {
            var state = EnsureVelocityMesh(device, i);
            if (state == null)
            {
                continue;
            }
            // Snapshot every deformed stream, including hidden clothing,
            // first-person arms and attached weapons. Hidden meshes are marked
            // invalid until they have actually appeared in consecutive frames.
            CopyVelocityPositions(i, state);
            state.WasVisible = _velocityColorMeshes.ContainsKey(i);
            state.WasOpaque = opaque && !_asset.Meshes[i].Alpha;
        }
        _velocityPreviousPosition = position;
        _velocityPreviousYaw = Yaw;
        _velocityPreviousFirstPerson = firstPerson;
        _velocityCapturedFrame = _velocityColorFrame;
        _velocityHasHistory = true;
    }

    private void DisposeVelocity()
    {
        foreach (var mesh in _velocityMeshes)
        {
            mesh?.Dispose();
        }
        _velocityMeshes.Clear();
        _velocityColorMeshes.Clear();
        _velocityOpaqueBlend?.Dispose();
        _velocityOpaqueBlend = null;
        _velocityReactiveBlend?.Dispose();
        _velocityReactiveBlend = null;
        _velocityDepth?.Dispose();
        _velocityDepth = null;
        _velocityConstants?.Dispose();
        _velocityConstants = null;
        _velocityLayout?.Dispose();
        _velocityLayout = null;
        _velocityReactivePixel?.Dispose();
        _velocityReactivePixel = null;
        _velocityPixel?.Dispose();
        _velocityPixel = null;
        _velocityVertex?.Dispose();
        _velocityVertex = null;
        _velocityHasHistory = false;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VelocityConstants
    {
        public Matrix4x4 CurrentWorld;
        public Matrix4x4 PreviousWorld;
        public Matrix4x4 CurrentViewProjection;
        public Matrix4x4 PreviousViewProjection;
        public Matrix4x4 ColorViewProjection;
        public Vector4 PreviousCameraOffset;
        public Vector4 AlphaSettings;
        public Vector4 OpacityScaleShift;
        public Vector4 MotionSettings;
    }

    private sealed class VelocityMesh : IDisposable
    {
        public Buffer PreviousBuffer { get; }
        public Vector3[] Positions { get; }
        public CharacterMesh Source { get; }
        public SkinnedVertex[] SourceVertices { get; }
        public Buffer CurrentBuffer { get; }
        public int[] Indices { get; }
        public bool WasVisible { get; set; }
        public bool WasOpaque { get; set; }

        public VelocityMesh(Buffer previousBuffer, Vector3[] positions,
            CharacterMesh source, SkinnedVertex[] sourceVertices, Buffer currentBuffer)
        {
            PreviousBuffer = previousBuffer;
            Positions = positions;
            Source = source;
            SourceVertices = sourceVertices;
            CurrentBuffer = currentBuffer;
            Indices = source.Indices;
        }

        public void Dispose()
        {
            PreviousBuffer.Dispose();
        }
    }
}
