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
        private Buffer _frameBuffer = null!;

        private SamplerState? _colorSampler;
        private HdrBloom? _bloom;
        private ColorLutAtlas? _lut;

        private Matrix4x4 _previousViewProjection;
        private bool _hasPreviousFrame;
        private bool _disposed;

        public bool ToneMappingEnabled { get; set; } = true;
        public float Exposure { get; set; } = 1f;

        public float BloomIntensity { get; set; } = 0.2f;
        public float BloomThreshold { get; set; } = 1f;

        public float ColorGradingStrength { get; set; } = 0.4f;
        public ColorGradingPreset GradingPreset { get; set; } =
            ColorGradingPreset.Atmospheric;

        public float SkyDitherStrength { get; set; } = 1f;

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

                _bloom = new HdrBloom(device, compiler);
                _lut = new ColorLutAtlas(device);
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
        }

        public void ReleaseSizeDependentResources()
        {
            _bloom?.ReleaseTargets();
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
            ShaderResourceView depth,
            RenderTargetView normal,
            ShaderResourceView normalResource,
            RenderTargetView roughness,
            RenderTargetView motion,
            RenderTargetView backBuffer,
            Buffer? sunShadowConstants,
            ShaderResourceView? sunShadowResource,
            SamplerState? sunShadowSampler)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(PostProcessManager));
            }

            if (width <= 0 || height <= 0)
            {
                return;
            }

            if (!Matrix4x4.Invert(
                    viewProjection,
                    out Matrix4x4 inverseViewProjection))
            {
                ResetHistory();
                return;
            }

            Matrix4x4 previousViewProjection =
                _hasPreviousFrame
                    ? _previousViewProjection
                    : viewProjection;

            bool contactAvailable =
                contactStrength > 0f &&
                sunShadowConstants != null &&
                sunShadowResource != null &&
                sunShadowSampler != null;

            _lut?.SetPreset(GradingPreset);

            ShaderResourceView? bloomResource = null;

            if (BloomIntensity > 0.0001f && _bloom != null)
            {
                bloomResource = _bloom.Render(
                    context,
                    width,
                    height,
                    hdrScene,
                    BloomThreshold);
            }

            var constants = new FrameConstants
            {
                InverseViewProjection = inverseViewProjection,
                PreviousViewProjection = previousViewProjection,
                CurrentViewProjection = viewProjection,
                Parameters = new Vector4(
                    width,
                    height,
                    Math.Clamp(Exposure, 0.01f, 16f),
                    ToneMappingEnabled ? 1f : 0f),
                Camera = new Vector4(
                    cameraPosition,
                    Math.Clamp(contactStrength, 0f, 1f)),
                Sun = new Vector4(
                    sunDirection,
                    contactAvailable ? 1f : 0f),
                GradeSettings = new Vector4(
                    Math.Clamp(BloomIntensity, 0f, 5f),
                    Math.Clamp(ColorGradingStrength, 0f, 1f),
                    Math.Clamp(SkyDitherStrength, 0f, 2f),
                    bloomResource != null ? 1f : 0f)
            };

            context.ClearState();

            try
            {
                context.UpdateSubresource(
                    ref constants,
                    _frameBuffer);

                context.Rasterizer.SetViewport(
                    0,
                    0,
                    width,
                    height);

                context.InputAssembler.InputLayout = null;

                context.InputAssembler.PrimitiveTopology =
                    PrimitiveTopology.TriangleList;

                context.VertexShader.Set(
                    _fullscreenVertex);

                context.PixelShader.SetConstantBuffer(
                    0,
                    _frameBuffer);

                // Stage 1: surface reconstruction.
                context.OutputMerger.SetRenderTargets(
                    null,
                    normal,
                    roughness,
                    motion);

                context.PixelShader.SetShaderResource(
                    0,
                    depth);

                context.PixelShader.Set(
                    _surfacePixel);

                context.Draw(3, 0);

                context.PixelShader.SetShaderResource(
                    0,
                    null);

                // Stage 2: HDR composition and presentation.
                context.OutputMerger.SetRenderTargets(
                    null,
                    backBuffer);

                context.PixelShader.SetShaderResource(
                    0,
                    hdrScene);

                context.PixelShader.SetShaderResource(
                    1,
                    depth);

                context.PixelShader.SetShaderResource(
                    2,
                    normalResource);

                context.PixelShader.SetShaderResource(
                    3,
                    bloomResource);

                context.PixelShader.SetShaderResource(
                    4,
                    _lut?.View);

                context.PixelShader.SetSampler(
                    0,
                    _colorSampler);

                if (contactAvailable)
                {
                    context.PixelShader.SetConstantBuffer(
                        2,
                        sunShadowConstants);

                    context.PixelShader.SetShaderResource(
                        7,
                        sunShadowResource);

                    context.PixelShader.SetSampler(
                        7,
                        sunShadowSampler);
                }

                context.PixelShader.Set(
                    _presentPixel);

                context.Draw(3, 0);

                _previousViewProjection = viewProjection;
                _hasPreviousFrame = true;
            }
            finally
            {
                context.ClearState();
            }
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

            _bloom?.Dispose();
            _bloom = null;

            _colorSampler?.Dispose();
            _colorSampler = null;

            _frameBuffer?.Dispose();
            _presentPixel?.Dispose();
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
        }
    }
}
