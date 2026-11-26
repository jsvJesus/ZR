using System.Numerics;
using System.Runtime.InteropServices;
using Runtime.Rendering.Shaders;
using Runtime.Viewport.Terrain;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Scene;
using Buffer = SharpDX.Direct3D11.Buffer;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Runtime.Viewport.Scene;

internal sealed partial class SceneRenderer
{
    private const int VelocityInstanceStride = 144;
    private readonly List<VelocityInstance> _velocityInstances = [];
    private readonly List<VelocityDraw> _velocityDraws = [];
    private Dictionary<(string Id, string Model), VelocityHistory> _previousVelocity = [];
    private Dictionary<(string Id, string Model), VelocityHistory> _currentVelocity = [];
    private readonly HashSet<(string Id, string Model)> _duplicateVelocityIds = [];
    private VelocityStream[] _velocityUpload = [];
    private VertexShader? _velocityVertex;
    private PixelShader? _velocityPixel, _reactivePixel;
    private InputLayout? _velocityLayout;
    private Buffer? _velocityConstants, _velocityBuffer;
    private BlendState? _velocityBlend, _reactiveBlend;
    private long _velocitySubmittedFrame = -1;

    private void BeginVelocityFrame()
    {
        _velocityDraws.Clear();
        _velocityInstances.Clear();
    }

    private void RecordVelocityDraw(MeshResources mesh, TerrainTexture diffuse, TerrainTexture normal,
        int start, int count, bool mirrored, Constants constants)
    {
        _velocityDraws.Add(new(mesh, diffuse, start, count, mirrored,
            mesh.Material.DoubleSided || ForceCutout, constants));
        // Texture arrivals/mip replacement and ForceCutout changes invalidate that
        // surface, instead of borrowing history from an incompatible coverage mask.
        int signature = HashCode.Combine(mesh, diffuse, normal, constants.Color,
            constants.Parameters.X, constants.Parameters.Y, constants.CameraPosition.W,
            mesh.Material.DoubleSided || ForceCutout);
        Span<VelocityInstance> instances = CollectionsMarshal.AsSpan(_velocityInstances);
        for (int index = start; index < start + count; index++)
        {
            instances[index].Appearance ^= signature;
            instances[index].DrawCount++;
        }
    }

    public void RenderVelocity(RenderTargetView motionTarget, RenderTargetView reactiveTarget,
        DepthStencilView depthTarget, Matrix4x4 currentViewProjection, Matrix4x4 previousViewProjection,
        Vector3 cameraPosition, bool historyValid, int width, int height)
    {
        if (_disposed) return;
        if (_velocitySubmittedFrame == _frame)
        {
            // No new color token: a skipped renderer frame must break history,
            // including an empty groundcover/domain that later becomes visible.
            _previousVelocity.Clear();
            return;
        }
        _velocitySubmittedFrame = _frame;
        _currentVelocity.Clear();
        _duplicateVelocityIds.Clear();
        if (_velocityUpload.Length < _velocityInstances.Count)
            _velocityUpload = new VelocityStream[Math.Min(MaximumInstances,
                Math.Max(_velocityInstances.Count, _velocityUpload.Length * 2))];
        for (int index = 0; index < _velocityInstances.Count; index++)
        {
            VelocityInstance instance = _velocityInstances[index];
            var key = (instance.Instance.Id, instance.Instance.Model);
            if (instance.DrawCount == 0) continue;
            var history = new VelocityHistory(instance.Instance.World, instance.Resources,
                instance.Instance.SourceStamp, instance.Appearance, instance.DrawCount, _frame);
            if (!_currentVelocity.TryAdd(key, history)) _duplicateVelocityIds.Add(key);
        }
        foreach (var key in _duplicateVelocityIds) _currentVelocity.Remove(key);
        for (int index = 0; index < _velocityInstances.Count; index++)
        {
            VelocityInstance instance = _velocityInstances[index];
            var key = (instance.Instance.Id, instance.Instance.Model);
            Matrix4x4 current = instance.Instance.World;
            bool valid = historyValid && instance.DrawCount > 0 && !string.IsNullOrEmpty(key.Id)
                && !_duplicateVelocityIds.Contains(key)
                && _previousVelocity.TryGetValue(key, out VelocityHistory previous)
                && previous.Frame == _frame - 1 && ReferenceEquals(previous.Resources, instance.Resources)
                && previous.SourceStamp == instance.Instance.SourceStamp
                && previous.Appearance == instance.Appearance && previous.DrawCount == instance.DrawCount
                && ContinuousWorld(previous.World, current, instance.Instance.Bounds.Radius);
            // Never feed an uninitialized previous transform into the vertex shader.
            Matrix4x4 previousWorld = valid ? _previousVelocity[key].World : current;
            _velocityUpload[index] = new() { Current = current, Previous = previousWorld,
                History = new Vector4(valid ? 1 : 0, 0, 0, 0) };
        }
        try
        {
            if (_velocityDraws.Count == 0 || width <= 0 || height <= 0) return;
            EnsureVelocityPipeline();
            _context.MapSubresource(_velocityBuffer!, MapMode.WriteDiscard,
                SharpDX.Direct3D11.MapFlags.None, out DataStream stream);
            try { using (stream) stream.WriteRange(_velocityUpload, 0, _velocityInstances.Count); }
            finally { _context.UnmapSubresource(_velocityBuffer!, 0); }
            _context.Rasterizer.SetViewport(0, 0, width, height);
            _context.InputAssembler.InputLayout = _velocityLayout;
            _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _context.VertexShader.Set(_velocityVertex);
            _context.VertexShader.SetConstantBuffer(0, _velocityConstants);
            _context.PixelShader.SetConstantBuffer(0, _velocityConstants);
            _context.PixelShader.SetSampler(0, _sampler);
            _context.OutputMerger.SetDepthStencilState(_depthRead);
            _context.OutputMerger.SetTargets(depthTarget, motionTarget, reactiveTarget);
            _context.OutputMerger.SetBlendState(_velocityBlend);
            _context.PixelShader.Set(_velocityPixel);
            foreach (VelocityDraw draw in _velocityDraws)
                if (draw.Constants.Parameters.Y < 0.5f)
                    DrawVelocity(draw, currentViewProjection, previousViewProjection, historyValid, width, height);
            _context.OutputMerger.SetTargets(depthTarget, reactiveTarget);
            _context.OutputMerger.SetBlendState(_reactiveBlend);
            _context.PixelShader.Set(_reactivePixel);
            foreach (VelocityDraw draw in _velocityDraws)
                if (draw.Constants.Parameters.Y >= 0.5f)
                    DrawVelocity(draw, currentViewProjection, previousViewProjection, historyValid, width, height);
        }
        catch (Exception exception)
        {
            // Do not accept a failed geometric pass as next frame's surface history.
            _currentVelocity.Clear();
            Warn($"Scene velocity rendering failed: {exception.Message}");
        }
        finally
        {
            (_previousVelocity, _currentVelocity) = (_currentVelocity, _previousVelocity);
            // Two dictionaries are reused; neither retains objects hidden for multiple frames.
            _currentVelocity.Clear();
            ResetBindings();
            _context.OutputMerger.ResetTargets();
        }
    }

    private void DrawVelocity(VelocityDraw draw, Matrix4x4 currentViewProjection,
        Matrix4x4 previousViewProjection, bool historyValid, int width, int height)
    {
        Constants color = draw.Constants;
        var constants = new VelocityConstants
        {
            CurrentViewProjection = currentViewProjection,
            PreviousViewProjection = previousViewProjection,
            ColorViewProjection = color.ViewProjection,
            MaterialColor = color.Color,
            CameraPosition = color.CameraPosition,
            Parameters = new Vector4(color.Parameters.X, color.Parameters.Y, historyValid ? 1 : 0, 0),
            ViewportSize = new Vector4(width, height, 0, 0)
        };
        _context.UpdateSubresource(ref constants, _velocityConstants!);
        // These are the exact material/cutout/cull settings captured by the color draw.
        _context.Rasterizer.State = draw.TwoSided ? (draw.Mirrored ? _mirroredTwoSided : _twoSided)
            : draw.Mirrored ? _mirroredBack : _back;
        _context.InputAssembler.SetVertexBuffers(0,
            new VertexBufferBinding(draw.Mesh.Vertices, 48, 0),
            new VertexBufferBinding(_velocityBuffer, VelocityInstanceStride, 0));
        _context.InputAssembler.SetIndexBuffer(draw.Mesh.Indices, Format.R32_UInt, 0);
        _context.PixelShader.SetShaderResource(0, draw.Diffuse.View);
        _context.DrawIndexedInstanced(draw.Mesh.IndexCount, draw.Count, 0, 0, draw.Start);
    }

    private void EnsureVelocityPipeline()
    {
        if (_velocityVertex != null) return;
        try
        {
            var compiler = new ShaderCompiler(_assets);
            using var vertexCode = compiler.Compile("Scene/SceneVelocity.hlsl", "VSMain", "vs_4_0");
            using var pixelCode = compiler.Compile("Scene/SceneVelocity.hlsl", "PSMain", "ps_4_0");
            using var reactiveCode = compiler.Compile("Scene/SceneVelocity.hlsl", "PSReactive", "ps_4_0");
            _velocityLayout = new(_device, vertexCode.Bytecode,
                [new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                 new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0),
                 new InputElement("WORLD", 0, Format.R32G32B32A32_Float, 0, 1, InputClassification.PerInstanceData, 1),
                 new InputElement("WORLD", 1, Format.R32G32B32A32_Float, 16, 1, InputClassification.PerInstanceData, 1),
                 new InputElement("WORLD", 2, Format.R32G32B32A32_Float, 32, 1, InputClassification.PerInstanceData, 1),
                 new InputElement("WORLD", 3, Format.R32G32B32A32_Float, 48, 1, InputClassification.PerInstanceData, 1),
                 new InputElement("PREVIOUSWORLD", 0, Format.R32G32B32A32_Float, 64, 1, InputClassification.PerInstanceData, 1),
                 new InputElement("PREVIOUSWORLD", 1, Format.R32G32B32A32_Float, 80, 1, InputClassification.PerInstanceData, 1),
                 new InputElement("PREVIOUSWORLD", 2, Format.R32G32B32A32_Float, 96, 1, InputClassification.PerInstanceData, 1),
                 new InputElement("PREVIOUSWORLD", 3, Format.R32G32B32A32_Float, 112, 1, InputClassification.PerInstanceData, 1),
                 new InputElement("HISTORY", 0, Format.R32G32B32A32_Float, 128, 1, InputClassification.PerInstanceData, 1)]);
            _velocityPixel = new(_device, pixelCode.Bytecode);
            _reactivePixel = new(_device, reactiveCode.Bytecode);
            _velocityConstants = new(_device, Utilities.SizeOf<VelocityConstants>(), ResourceUsage.Default,
                BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _velocityBuffer = new(_device, MaximumInstances * VelocityInstanceStride, ResourceUsage.Dynamic,
                BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            var blend = BlendStateDescription.Default();
            blend.IndependentBlendEnable = true;
            var reactive = blend.RenderTarget[1];
            reactive.IsBlendEnabled = true;
            reactive.SourceBlend = reactive.DestinationBlend = BlendOption.One;
            reactive.BlendOperation = BlendOperation.Maximum;
            reactive.SourceAlphaBlend = reactive.DestinationAlphaBlend = BlendOption.One;
            reactive.AlphaBlendOperation = BlendOperation.Maximum;
            reactive.RenderTargetWriteMask = ColorWriteMaskFlags.All;
            blend.RenderTarget[1] = reactive;
            _velocityBlend = new(_device, blend);
            blend.RenderTarget[0] = reactive;
            _reactiveBlend = new(_device, blend);
            // Publish the readiness sentinel only when every lazy resource exists.
            _velocityVertex = new(_device, vertexCode.Bytecode);
        }
        catch { DisposeVelocityPipeline(); throw; }
    }

    private static bool ContinuousWorld(Matrix4x4 previous, Matrix4x4 current, float radius)
    {
        // The common 60k-groundcover case avoids matrix decomposition entirely.
        if (previous == current) return float.IsFinite(current.GetDeterminant());
        if (!Matrix4x4.Decompose(previous, out Vector3 oldScale, out Quaternion oldRotation, out Vector3 oldPosition)
            || !Matrix4x4.Decompose(current, out Vector3 scale, out Quaternion rotation, out Vector3 position)) return false;
        if (!float.IsFinite(previous.GetDeterminant()) || !float.IsFinite(current.GetDeterminant())) return false;
        if (MathF.Sign(previous.GetDeterminant()) != MathF.Sign(current.GetDeterminant())) return false;
        float reach = MathF.Max(4, radius * 2);
        if (!(Vector3.DistanceSquared(position, oldPosition) <= reach * reach)
            || !(MathF.Abs(Quaternion.Dot(oldRotation, rotation)) >= 0.7071067f)) return false;
        for (int axis = 0; axis < 3; axis++)
        {
            float a = MathF.Abs(oldScale[axis]), b = MathF.Abs(scale[axis]);
            if (!(a > 0.00001f && b / a >= 0.5f && b / a <= 2)) return false;
        }
        return true;
    }

    private void DisposeVelocityPipeline()
    {
        _velocityVertex?.Dispose(); _velocityVertex = null;
        _velocityPixel?.Dispose(); _velocityPixel = null;
        _reactivePixel?.Dispose(); _reactivePixel = null;
        _velocityLayout?.Dispose(); _velocityLayout = null;
        _velocityConstants?.Dispose(); _velocityConstants = null;
        _velocityBuffer?.Dispose(); _velocityBuffer = null;
        _velocityBlend?.Dispose(); _velocityBlend = null;
        _reactiveBlend?.Dispose(); _reactiveBlend = null;
        _previousVelocity.Clear(); _currentVelocity.Clear();
        _velocityDraws.Clear(); _velocityInstances.Clear(); _velocityUpload = [];
        _duplicateVelocityIds.Clear();
    }

    private struct VelocityInstance(SceneInstance instance, ModelResources resources)
    {
        public SceneInstance Instance = instance;
        public ModelResources Resources = resources;
        public int Appearance;
        public int DrawCount;
    }

    private readonly record struct VelocityDraw(MeshResources Mesh, TerrainTexture Diffuse,
        int Start, int Count, bool Mirrored, bool TwoSided, Constants Constants);
    private readonly record struct VelocityHistory(Matrix4x4 World, ModelResources Resources,
        string SourceStamp, int Appearance, int DrawCount, long Frame);

    [StructLayout(LayoutKind.Sequential)]
    private struct VelocityStream
    {
        public Matrix4x4 Current, Previous;
        public Vector4 History;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VelocityConstants
    {
        public Matrix4x4 CurrentViewProjection, PreviousViewProjection, ColorViewProjection;
        public Vector4 MaterialColor, CameraPosition, Parameters, ViewportSize;
    }
}
