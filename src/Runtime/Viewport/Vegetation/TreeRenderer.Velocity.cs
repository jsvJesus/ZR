using System.Numerics;
using System.Runtime.InteropServices;
using Runtime.Rendering.Shaders;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Vegetation;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Runtime.Viewport.Vegetation
{
    internal sealed partial class TreeRenderer
    {
        private VertexShader _velocityVertex = null!;
        private PixelShader _velocityPixel = null!;
        private InputLayout _velocityLayout = null!;
        private Buffer _velocityConstants = null!;
        private DepthStencilState _velocityDepth = null!;
        private BlendState _velocityBlend = null!;

        // Manual and generated instances have independent ID namespaces. Only actual
        // color draws enter these maps; absence or a different LOD breaks history.
        private Dictionary<(Guid Id, bool Generated, int Part), VelocityDraw> _previousVelocityDraws = [];
        private Dictionary<(Guid Id, bool Generated, int Part), VelocityDraw> _currentVelocityDraws = [];
        private bool _velocityColorPending;

        private readonly record struct VelocityDraw(
            TreeAsset Asset, ModelResources Model, int Part, int Lod,
            Matrix4x4 World, Matrix4x4 ColorViewProjection, Vector4 CameraRight, Vector4 CameraUp);

        [StructLayout(LayoutKind.Sequential)]
        private struct VelocityConstants
        {
            public Matrix4x4 World, PreviousWorld, ViewProjection, PreviousViewProjection;
            public Matrix4x4 ColorViewProjection;
            public Vector4 CameraRight, CameraUp, PreviousCameraRight, PreviousCameraUp;
            // x: exact color alpha cutoff, y: compatible history, z: leaf/frond
            // material reactivity, w: billboard orientation change.
            public Vector4 Parameters;
            public Vector4 ViewportSize;
        }

        private void InitializeVelocity(ShaderCompiler compiler)
        {
            using var vs = compiler.Compile("Vegetation/TreeVelocity.hlsl", "VSMain", "vs_4_0");
            using var ps = compiler.Compile("Vegetation/TreeVelocity.hlsl", "PSMain", "ps_5_0");
            _velocityVertex = new(_device, vs.Bytecode);
            _velocityPixel = new(_device, ps.Bytecode);
            _velocityLayout = new(_device, vs.Bytecode,
                [new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                 new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                 new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0),
                 new InputElement("TEXCOORD", 1, Format.R32G32B32A32_Float, 32, 0),
                 new InputElement("TEXCOORD", 2, Format.R32G32_Float, 48, 0)]);
            _velocityConstants = new(_device, Utilities.SizeOf<VelocityConstants>(), ResourceUsage.Default,
                BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            var depth = DepthStencilStateDescription.Default();
            depth.DepthWriteMask = DepthWriteMask.Zero;
            depth.DepthComparison = Comparison.LessEqual;
            _velocityDepth = new(_device, depth);

            // Motion replaces the visible surface; reactivity also contains
            // translucent layers accumulated by other renderers and can only grow.
            var blend = BlendStateDescription.Default();
            blend.IndependentBlendEnable = true;
            blend.RenderTarget[0].IsBlendEnabled = false;
            blend.RenderTarget[1].IsBlendEnabled = true;
            blend.RenderTarget[1].SourceBlend = BlendOption.One;
            blend.RenderTarget[1].DestinationBlend = BlendOption.One;
            blend.RenderTarget[1].BlendOperation = BlendOperation.Maximum;
            blend.RenderTarget[1].SourceAlphaBlend = BlendOption.One;
            blend.RenderTarget[1].DestinationAlphaBlend = BlendOption.One;
            blend.RenderTarget[1].AlphaBlendOperation = BlendOperation.Maximum;
            blend.RenderTarget[1].RenderTargetWriteMask = ColorWriteMaskFlags.Red;
            _velocityBlend = new(_device, blend);
        }

        private void BeginVelocityColorPass()
        {
            // A color pass that never reached velocity cannot become valid history
            // later (for example while temporal rendering was disabled).
            if (_velocityColorPending) _currentVelocityDraws.Clear();
            (_previousVelocityDraws, _currentVelocityDraws) = (_currentVelocityDraws, _previousVelocityDraws);
            _currentVelocityDraws.Clear();
            _velocityColorPending = true;
        }

        private void RecordVelocityDraw(TreeInstance tree, bool generated, TreeAsset asset,
            ModelResources model, int part, int lod, Constants constants)
        {
            _currentVelocityDraws[(tree.Id, generated, part)] = new(
                asset, model, part, lod, constants.World, constants.ViewProjection,
                constants.CameraRight, constants.CameraUp);
        }

        public void RenderVelocity(
            RenderTargetView motionTarget,
            RenderTargetView reactiveTarget,
            DepthStencilView depthTarget,
            Matrix4x4 currentViewProjection,
            Matrix4x4 previousViewProjection,
            Vector3 cameraPosition,
            bool historyValid,
            int width,
            int height)
        {
            // The owner calls this even when the color renderer was skipped, so
            // returning vegetation cannot inherit geometry from an older frame.
            if (!_velocityColorPending)
            {
                _previousVelocityDraws.Clear();
                _currentVelocityDraws.Clear();
                return;
            }
            _velocityColorPending = false;
            if (width <= 0 || height <= 0)
            {
                _previousVelocityDraws.Clear();
                _currentVelocityDraws.Clear();
                return;
            }

            _context.OutputMerger.SetTargets(depthTarget, motionTarget, reactiveTarget);
            _context.OutputMerger.SetDepthStencilState(_velocityDepth);
            _context.OutputMerger.SetBlendState(_velocityBlend);
            _context.Rasterizer.SetViewport(0, 0, width, height);
            _context.Rasterizer.State = _rasterizer;
            _context.InputAssembler.InputLayout = _velocityLayout;
            _context.VertexShader.Set(_velocityVertex);
            _context.VertexShader.SetConstantBuffer(0, _velocityConstants);
            _context.PixelShader.Set(_velocityPixel);
            _context.PixelShader.SetConstantBuffer(0, _velocityConstants);
            _context.PixelShader.SetSampler(0, _sampler);

            foreach (var (key, draw) in _currentVelocityDraws)
            {
                bool valid = historyValid && _previousVelocityDraws.TryGetValue(key, out var previous) &&
                    ReferenceEquals(previous.Asset, draw.Asset) && ReferenceEquals(previous.Model, draw.Model) &&
                    previous.Lod == draw.Lod && ContinuousVelocityWorld(previous.World, draw.World,
                        MathF.Max(draw.Asset.Minimum.Length(), draw.Asset.Maximum.Length())
                            * MathF.Abs(draw.CameraRight.W));
                // Never read previous matrices/bases until their geometry is known
                // to match. New geometry uses current reconstruction and w=0.
                var prior = valid ? _previousVelocityDraws[key] : draw;
                var part = draw.Asset.Parts[draw.Part];
                var gpu = draw.Model.Parts[draw.Part];
                float materialReactive = part.Kind switch
                {
                    TreePartKind.Leaves or TreePartKind.Billboard => 0.3f,
                    TreePartKind.Fronds => 0.2f,
                    _ => 0f
                };
                float orientationChange = Math.Clamp(Math.Max(
                    Vector3.Distance(new(draw.CameraRight.X, draw.CameraRight.Y, draw.CameraRight.Z),
                        new(prior.CameraRight.X, prior.CameraRight.Y, prior.CameraRight.Z)),
                    Vector3.Distance(new(draw.CameraUp.X, draw.CameraUp.Y, draw.CameraUp.Z),
                        new(prior.CameraUp.X, prior.CameraUp.Y, prior.CameraUp.Z))), 0f, 1f);
                var constants = new VelocityConstants
                {
                    World = draw.World,
                    PreviousWorld = prior.World,
                    ViewProjection = currentViewProjection,
                    PreviousViewProjection = valid ? previousViewProjection : currentViewProjection,
                    ColorViewProjection = draw.ColorViewProjection,
                    CameraRight = draw.CameraRight,
                    CameraUp = draw.CameraUp,
                    PreviousCameraRight = prior.CameraRight,
                    PreviousCameraUp = prior.CameraUp,
                    Parameters = new(part.Kind == TreePartKind.Branches ? 0f : 0.4f,
                        valid ? 1f : 0f, materialReactive, orientationChange),
                    ViewportSize = new(width, height, 0f, 0f)
                };
                _context.UpdateSubresource(ref constants, _velocityConstants);
                _context.InputAssembler.PrimitiveTopology = part.Kind is TreePartKind.Branches or TreePartKind.Fronds
                    ? PrimitiveTopology.TriangleStrip : PrimitiveTopology.TriangleList;
                _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(gpu.Vertices, 56, 0));
                _context.InputAssembler.SetIndexBuffer(gpu.Indices[draw.Lod], Format.R32_UInt, 0);
                _context.PixelShader.SetShaderResource(0, Texture(part.Diffuse, false).View);
                _context.DrawIndexed(part.Lods[draw.Lod].Length, 0, 0);
            }

            _context.PixelShader.SetShaderResource(0, null);
            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(null);
            _context.Rasterizer.State = null;
        }

        private static bool ContinuousVelocityWorld(Matrix4x4 previous, Matrix4x4 current, float radius)
        {
            // Most vegetation stays still. Avoid decomposition for the same pose,
            // while still rejecting singular/nonfinite transforms as history.
            float determinant = current.GetDeterminant();
            if (!float.IsFinite(determinant) || MathF.Abs(determinant) <= 0.000000000001f ||
                !float.IsFinite(current.M41) || !float.IsFinite(current.M42) || !float.IsFinite(current.M43))
                return false;
            if (previous == current) return true;

            float previousDeterminant = previous.GetDeterminant();
            if (!float.IsFinite(previousDeterminant) || MathF.Abs(previousDeterminant) <= 0.000000000001f ||
                !float.IsFinite(previous.M41) || !float.IsFinite(previous.M42) || !float.IsFinite(previous.M43) ||
                MathF.Sign(previousDeterminant) != MathF.Sign(determinant) ||
                !Matrix4x4.Decompose(previous, out Vector3 oldScale, out System.Numerics.Quaternion oldRotation,
                    out Vector3 oldPosition) ||
                !Matrix4x4.Decompose(current, out Vector3 scale, out System.Numerics.Quaternion rotation,
                    out Vector3 position))
                return false;

            float reach = MathF.Max(4f, radius * 2f);
            if (!float.IsFinite(reach) || !(Vector3.DistanceSquared(position, oldPosition) <= reach * reach) ||
                !(MathF.Abs(System.Numerics.Quaternion.Dot(oldRotation, rotation)) >= 0.7071067f))
                return false;
            for (int axis = 0; axis < 3; axis++)
            {
                float a = MathF.Abs(oldScale[axis]), b = MathF.Abs(scale[axis]);
                if (!(a > 0.00001f && b / a >= 0.5f && b / a <= 2f)) return false;
            }
            return true;
        }

        private void DisposeVelocity()
        {
            _previousVelocityDraws.Clear();
            _currentVelocityDraws.Clear();
            _velocityBlend?.Dispose();
            _velocityDepth?.Dispose();
            _velocityConstants?.Dispose();
            _velocityLayout?.Dispose();
            _velocityPixel?.Dispose();
            _velocityVertex?.Dispose();
        }
    }
}
