using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Editor.Environment;
using Editor.Rendering.Shaders;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport
{
    internal sealed class PostProcessManager : IDisposable
    {
        private VertexShader _fullscreenVertex = null!;
        private PixelShader _surfacePixel = null!;
        private PixelShader _presentPixel = null!;
        private PixelShader _composePixel = null!;
        private Buffer _frameBuffer = null!;

        private SamplerState? _colorSampler;
        private HdrBloom? _bloom;
        private ColorLutAtlas? _lut;
        private GtaoEffect? _ao;
        private TaaEffect? _taa;

        private Matrix4x4 _previousViewProjection;
        private bool _hasPreviousFrame;
        private bool _previousFirstPersonDepth;
        private bool _disposed;

        public bool ToneMappingEnabled { get; set; } = true;
        public float Exposure { get; set; } = 1f;

        public float BloomIntensity { get; set; } = 0.2f;
        public float BloomThreshold { get; set; } = 1f;

        public float ColorGradingStrength { get; set; } = 0.4f;
        public ColorGradingPreset GradingPreset { get; set; } =
            ColorGradingPreset.Atmospheric;

        public float SkyDitherStrength { get; set; } = 1f;

        public bool AoEnabled { get; set; } = true;
        public float AoIntensity { get; set; } = 0.65f;
        public float AoRadius { get; set; } = 1.2f;
        public int AoQuality { get; set; } = 2;

        public bool TaaEnabled { get; set; } = true;
        public float TaaFeedback { get; set; } = 0.88f;

        public PostProcessManager(
            Device device,
            ShaderCompiler compiler)
        {
            try
            {
                using var vertexCode = compiler.Compile(
                    "PostProcess/Frame.hlsl",
                    "VSMain",
                    "vs_4_0");

                using var surfaceCode = compiler.Compile(
                    "PostProcess/Frame.hlsl",
                    "PSSurface",
                    "ps_4_0");

                using var presentCode = compiler.Compile(
                    "PostProcess/Frame.hlsl",
                    "PSPresent",
                    "ps_5_0");

                _fullscreenVertex = new VertexShader(
                    device,
                    vertexCode.Bytecode);

                _surfacePixel = new PixelShader(
                    device,
                    surfaceCode.Bytecode);

                _presentPixel = new PixelShader(
                    device,
                    presentCode.Bytecode);

                using var composeCode = compiler.Compile("PostProcess/Frame.hlsl", "PSComposite", "ps_5_0");
                _composePixel = new PixelShader(device, composeCode.Bytecode);

                _frameBuffer = new Buffer(
                    device,
                    Utilities.SizeOf<FrameConstants>(),
                    ResourceUsage.Default,
                    BindFlags.ConstantBuffer,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.None,
                    0);

                _colorSampler = new SamplerState(
                    device,
                    new SamplerStateDescription
                    {
                        Filter = Filter.MinMagMipLinear,
                        AddressU = TextureAddressMode.Clamp,
                        AddressV = TextureAddressMode.Clamp,
                        AddressW = TextureAddressMode.Clamp,
                        ComparisonFunction = Comparison.Never,
                        MinimumLod = 0,
                        MaximumLod = float.MaxValue
                    });

                // Construct
                _bloom = new HdrBloom(device, compiler);
                _lut = new ColorLutAtlas(device);
                _ao = new GtaoEffect(device, compiler);
                _taa = new TaaEffect(device, compiler);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void ResetHistory()
        {
            _hasPreviousFrame = false;
            _previousViewProjection = Matrix4x4.Identity;
            _ao?.ResetHistory();
            _taa?.ResetHistory();
        }

        public void ReleaseSizeDependentResources()
        {
            _bloom?.ReleaseTargets();
            _ao?.ReleaseTargets();
            _taa?.ReleaseTargets();
            ResetHistory();
        }

        public void Render(
            DeviceContext context,
            int width,
            int height,
            Matrix4x4 viewProjection,
            Vector3 cameraPosition,
            Vector3 sunDirection,
            float contactStrength,
            ShaderResourceView hdrScene,
            ShaderResourceView ambient,
            RenderTargetView composedTarget,
            ShaderResourceView composedResource,
            ShaderResourceView depth,
            RenderTargetView normal,
            ShaderResourceView normalResource,
            RenderTargetView roughness,
            RenderTargetView motion,
            ShaderResourceView motionResource,
            RenderTargetView backBuffer,
            Buffer? sunShadowConstants,
            ShaderResourceView? sunShadowResource,
            SamplerState? sunShadowSampler,
            Buffer? skyShadowConstants,
            ShaderResourceView? skyShadowResource,
            SamplerState? skyShadowSampler,
            bool firstPersonDepth)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PostProcessManager));
            if (width <= 0 || height <= 0) return;

            if (!Matrix4x4.Invert(viewProjection, out Matrix4x4 inverseViewProjection))
            {
                ResetHistory();
                return;
            }

            if (_hasPreviousFrame && firstPersonDepth != _previousFirstPersonDepth)
                ResetHistory();

            Matrix4x4 previousViewProjection = _hasPreviousFrame
                ? _previousViewProjection : viewProjection;
            bool contactAvailable = contactStrength > 0f &&
                sunShadowConstants != null && sunShadowResource != null && sunShadowSampler != null;
            _lut?.SetPreset(GradingPreset);

            var constants = new FrameConstants
            {
                InverseViewProjection = inverseViewProjection,
                PreviousViewProjection = previousViewProjection,
                CurrentViewProjection = viewProjection,
                AoSettings = new Vector4(Math.Clamp(AoIntensity, 0f, 2f), AoEnabled ? 1f : 0f, 0f, 0f),
                Parameters = new Vector4(width, height, Math.Clamp(Exposure, 0.01f, 16f),
                    ToneMappingEnabled ? 1f : 0f),
                Camera = new Vector4(cameraPosition, Math.Clamp(contactStrength, 0f, 1f)),
                Sun = new Vector4(sunDirection, contactAvailable ? 1f : 0f),
                GradeSettings = new Vector4(Math.Clamp(BloomIntensity, 0f, 5f),
                    Math.Clamp(ColorGradingStrength, 0f, 1f),
                    Math.Clamp(SkyDitherStrength, 0f, 2f), 0f)
            };

            context.ClearState();
            try
            {
                context.UpdateSubresource(ref constants, _frameBuffer);
                BindFullscreen(context, width, height);
                context.OutputMerger.SetRenderTargets(null, normal, roughness, motion);
                context.PixelShader.SetShaderResource(0, depth);
                context.PixelShader.Set(_surfacePixel);
                context.Draw(3, 0);
                context.PixelShader.SetShaderResource(0, null);

                ShaderResourceView? aoResource = null;
                if (AoEnabled && _ao != null)
                {
                    aoResource = _ao.Render(context, width, height, inverseViewProjection,
                        viewProjection, previousViewProjection, cameraPosition,
                        depth, normalResource, AoRadius, AoQuality);
                }
                else
                {
                    _ao?.ResetHistory();
                }

                // AO changes only the captured ambient contribution. Compose before
                // bloom so occluded ambient cannot return as an unoccluded glow.
                BindFullscreen(context, width, height);
                context.OutputMerger.SetRenderTargets(null, composedTarget);
                context.PixelShader.SetShaderResource(0, hdrScene);
                context.PixelShader.SetShaderResource(1, depth);
                context.PixelShader.SetShaderResource(2, normalResource);
                context.PixelShader.SetShaderResource(5, aoResource);
                context.PixelShader.SetShaderResource(6, ambient);
                context.PixelShader.SetConstantBuffer(3, skyShadowConstants);
                context.PixelShader.SetShaderResource(23, skyShadowResource);
                context.PixelShader.SetSampler(8, skyShadowSampler);
                context.PixelShader.Set(_composePixel);
                context.Draw(3, 0);
                context.ClearState();

                ShaderResourceView resolvedColor =
                    composedResource;

                if (TaaEnabled && _taa != null)
                {
                    float effectiveFeedback = Math.Clamp(
                        TaaFeedback,
                        0f,
                        0.96f);

                    // FPS skeleton and weapon poses currently have
                    // no independent per-vertex velocity.
                    // Use a conservative history weight.
                    if (firstPersonDepth)
                    {
                        effectiveFeedback *= 0.35f;
                    }

                    resolvedColor = _taa.Render(
                        context,
                        width,
                        height,
                        composedResource,
                        depth,
                        motionResource,
                        inverseViewProjection,
                        viewProjection,
                        previousViewProjection,
                        effectiveFeedback);
                }
                else
                {
                    _taa?.ResetHistory();
                }

                ShaderResourceView? bloomResource = null;
                if (BloomIntensity > 0.0001f && _bloom != null)
                {
                    bloomResource = _bloom.Render(
                        context,
                        width,
                        height,
                        resolvedColor,
                        BloomThreshold);
                }

                constants.GradeSettings.W = bloomResource != null ? 1f : 0f;
                context.UpdateSubresource(ref constants, _frameBuffer);
                BindFullscreen(context, width, height);
                context.OutputMerger.SetRenderTargets(null, backBuffer);
                context.PixelShader.SetShaderResource(0, resolvedColor);
                context.PixelShader.SetShaderResource(1, depth);
                context.PixelShader.SetShaderResource(2, normalResource);
                context.PixelShader.SetShaderResource(3, bloomResource);
                context.PixelShader.SetShaderResource(4, _lut?.View);
                context.PixelShader.SetSampler(0, _colorSampler);
                if (contactAvailable)
                {
                    context.PixelShader.SetConstantBuffer(2, sunShadowConstants);
                    context.PixelShader.SetShaderResource(7, sunShadowResource);
                    context.PixelShader.SetSampler(7, sunShadowSampler);
                }
                context.PixelShader.Set(_presentPixel);
                context.Draw(3, 0);

                _previousViewProjection = viewProjection;
                _previousFirstPersonDepth = firstPersonDepth;
                _hasPreviousFrame = true;
            }
            finally
            {
                context.ClearState();
            }
        }

        private void BindFullscreen(DeviceContext context, int width, int height)
        {
            context.Rasterizer.SetViewport(0, 0, width, height);
            context.InputAssembler.InputLayout = null;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.VertexShader.Set(_fullscreenVertex);
            context.PixelShader.SetConstantBuffer(0, _frameBuffer);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _lut?.Dispose();
            _lut = null;

            _taa?.Dispose();
            _taa = null;

            _ao?.Dispose();
            _ao = null;

            _bloom?.Dispose();
            _bloom = null;

            _colorSampler?.Dispose();
            _colorSampler = null;

            _frameBuffer?.Dispose();
            _presentPixel?.Dispose();
            _composePixel?.Dispose();
            _surfacePixel?.Dispose();
            _fullscreenVertex?.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FrameConstants
        {
            public Matrix4x4 InverseViewProjection;
            public Matrix4x4 PreviousViewProjection;
            public Matrix4x4 CurrentViewProjection;
            public Vector4 Parameters;
            public Vector4 Camera;
            public Vector4 Sun;
            public Vector4 GradeSettings;
            public Vector4 AoSettings;
        }
    }
}
