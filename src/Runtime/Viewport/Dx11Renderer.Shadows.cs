using System;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Runtime.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private const int ShadowCascadeCount = 4;
        private const int ShadowResolution = 2048;
        private const int SkyShadowResolution = 1024;
        private const float CascadeSplitLambda = 0.65f;
        private const float CascadeBlendFraction = 0.12f;

        private Texture2D? _shadowTexture;
        private readonly DepthStencilView?[] _shadowDepthViews =
            new DepthStencilView?[ShadowCascadeCount];

        private readonly Matrix4x4[] _shadowMatrices =
            new Matrix4x4[ShadowCascadeCount];
        private readonly RasterizerState?[] _sunShadowRasterizers =
            new RasterizerState?[ShadowCascadeCount];
        private readonly float[] _shadowDepthRanges = new float[ShadowCascadeCount];

        private ShaderResourceView? _shadowShaderView;
        private SamplerState? _shadowSampler;
        private Buffer? _shadowConstantBuffer;

        private Vector4 _shadowSplits;
        private Vector4 _shadowDepthBias;
        private float _shadowNearPlane;

        private Texture2D? _skyShadowTexture;
        private DepthStencilView? _skyShadowDepthView;
        private ShaderResourceView? _skyShadowShaderView;
        private SamplerState? _skyShadowSampler;
        private Buffer? _skyShadowConstantBuffer;

        private void InitializeShadowResources()
        {
            try
            {
                CreateCascadedShadowMap();

                CreateShadowMap(
                    SkyShadowResolution,
                    out _skyShadowTexture,
                    out _skyShadowDepthView,
                    out _skyShadowShaderView);

                _shadowSampler = CreateComparisonSampler(linear: false);
                _skyShadowSampler = CreateComparisonSampler();

                _shadowConstantBuffer = new Buffer(
                    _device,
                    Utilities.SizeOf<ShadowConstants>(),
                    ResourceUsage.Default,
                    BindFlags.ConstantBuffer,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.None,
                    0);

                _skyShadowConstantBuffer = new Buffer(
                    _device,
                    Utilities.SizeOf<SkyShadowConstants>(),
                    ResourceUsage.Default,
                    BindFlags.ConstantBuffer,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.None,
                    0);
            }
            catch
            {
                DisposeShadowResources();
                throw;
            }
        }

        private void CreateCascadedShadowMap()
        {
            _shadowTexture = new Texture2D(
                _device,
                new Texture2DDescription
                {
                    Width = ShadowResolution,
                    Height = ShadowResolution,
                    MipLevels = 1,
                    ArraySize = ShadowCascadeCount,
                    Format = Format.R32_Typeless,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags =
                        BindFlags.DepthStencil |
                        BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None
                });

            for (int index = 0; index < ShadowCascadeCount; index++)
            {
                _shadowDepthViews[index] = new DepthStencilView(
                    _device,
                    _shadowTexture,
                    new DepthStencilViewDescription
                    {
                        Format = Format.D32_Float,
                        Dimension = DepthStencilViewDimension.Texture2DArray,
                        Texture2DArray =
                            new DepthStencilViewDescription.Texture2DArrayResource
                            {
                                MipSlice = 0,
                                FirstArraySlice = index,
                                ArraySize = 1
                            }
                    });
            }

            _shadowShaderView = new ShaderResourceView(
                _device,
                _shadowTexture,
                new ShaderResourceViewDescription
                {
                    Format = Format.R32_Float,
                    Dimension = ShaderResourceViewDimension.Texture2DArray,
                    Texture2DArray =
                        new ShaderResourceViewDescription.Texture2DArrayResource
                        {
                            MostDetailedMip = 0,
                            MipLevels = 1,
                            FirstArraySlice = 0,
                            ArraySize = ShadowCascadeCount
                        }
                });
        }

        private void CreateShadowMap(
            int resolution,
            out Texture2D texture,
            out DepthStencilView depthView,
            out ShaderResourceView shaderView)
        {
            texture = new Texture2D(
                _device,
                new Texture2DDescription
                {
                    Width = resolution,
                    Height = resolution,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.R32_Typeless,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags =
                        BindFlags.DepthStencil |
                        BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None
                });

            try
            {
                depthView = new DepthStencilView(
                    _device,
                    texture,
                    new DepthStencilViewDescription
                    {
                        Format = Format.D32_Float,
                        Dimension = DepthStencilViewDimension.Texture2D,
                        Texture2D =
                            new DepthStencilViewDescription.Texture2DResource
                            {
                                MipSlice = 0
                            }
                    });

                try
                {
                    shaderView = new ShaderResourceView(
                        _device,
                        texture,
                        new ShaderResourceViewDescription
                        {
                            Format = Format.R32_Float,
                            Dimension = ShaderResourceViewDimension.Texture2D,
                            Texture2D =
                                new ShaderResourceViewDescription.Texture2DResource
                                {
                                    MostDetailedMip = 0,
                                    MipLevels = 1
                                }
                        });
                }
                catch
                {
                    depthView.Dispose();
                    throw;
                }
            }
            catch
            {
                texture.Dispose();
                throw;
            }
        }

        private SamplerState CreateComparisonSampler(bool linear = true)
        {
            return new SamplerState(
                _device,
                new SamplerStateDescription
                {
                    Filter = linear ? Filter.ComparisonMinMagLinearMipPoint : Filter.ComparisonMinMagMipPoint,
                    AddressU = TextureAddressMode.Border,
                    AddressV = TextureAddressMode.Border,
                    AddressW = TextureAddressMode.Border,
                    BorderColor =
                        new SharpDX.Mathematics.Interop.RawColor4(
                            1f, 1f, 1f, 1f),
                    ComparisonFunction = Comparison.LessEqual,
                    MinimumLod = 0,
                    MaximumLod = float.MaxValue
                });
        }

        private void RenderEnvironmentShadows(
            FreeCamera camera,
            Vector3 sunDirection,
            float farPlane,
            bool playing)
        {
            if (_shadowShaderView == null ||
                _shadowConstantBuffer == null ||
                _skyShadowDepthView == null ||
                _skyShadowShaderView == null ||
                _skyShadowConstantBuffer == null)
            {
                return;
            }

            // Prevent DSV/SRV conflicts with the previous frame.
            _context.PixelShader.SetShaderResource(7, null);
            _context.PixelShader.SetShaderResource(23, null);

            SetShadowConstants(
                false,
                _environmentSettings.ShadowStrength,
                camera.LookDirection);

            SetSkyShadowConstants(
                Matrix4x4.Identity,
                false);

            if (!_environmentSettings.ShadowsEnabled ||
                _objectPreview)
            {
                return;
            }

            if (sunDirection.Y > 0.04f)
            {
                RenderSunCascades(
                    camera,
                    sunDirection,
                    farPlane,
                    playing);
            }

            RenderSkyShadowMap(camera);

            if (sunDirection.Y > 0.04f)
            {
                _context.PixelShader.SetShaderResource(
                    7,
                    _shadowShaderView);

                _context.PixelShader.SetSampler(
                    7,
                    _shadowSampler);
            }
        }

        private void RenderSunCascades(
            FreeCamera camera,
            Vector3 sunDirection,
            float farPlane,
            bool playing)
        {
            float nearPlane = playing
                ? 0.15f - 0.14f *
                    Math.Clamp(_playFirstPersonBlend, 0f, 1f)
                : 0.5f;

            float maximumDistance = Math.Clamp(
                _environmentSettings.ShadowDistance,
                25f,
                400f);

            float shadowDistance = Math.Min(
                farPlane,
                maximumDistance);

            if (shadowDistance <= nearPlane + 0.01f)
            {
                return;
            }

            float fieldOfView = playing
                ? _playFieldOfView
                : 1.05f;

            float aspectRatio = (float)_width / _height;
            float tangent = MathF.Tan(fieldOfView * 0.5f);

            Vector3 forward = Vector3.Normalize(
                camera.LookDirection);

            Vector3 lightForward = -sunDirection;

            Vector3 reference = MathF.Abs(lightForward.Y) > 0.99f
                ? Vector3.UnitZ
                : Vector3.UnitY;

            Vector3 lightRight = Vector3.Normalize(
                Vector3.Cross(reference, lightForward));

            Vector3 lightUp = Vector3.Cross(
                lightForward,
                lightRight);

            float previousSplit = nearPlane;
            float previousCascadeStart = nearPlane;
            _shadowNearPlane = nearPlane;

            for (int index = 0; index < ShadowCascadeCount; index++)
            {
                float fraction =
                    (index + 1f) / ShadowCascadeCount;

                float logarithmic =
                    nearPlane *
                    MathF.Pow(shadowDistance / nearPlane, fraction);

                float uniform =
                    nearPlane +
                    (shadowDistance - nearPlane) * fraction;

                float split = uniform + (logarithmic - uniform) * CascadeSplitLambda;

                if (index == ShadowCascadeCount - 1)
                {
                    split = shadowDistance;
                }

                // The next cascade must cover the preceding cascade's blend band.
                float cascadeNear = index == 0 ? nearPlane : MathF.Max(
                    nearPlane,
                    previousSplit - MathF.Max(
                        0.5f,
                        (previousSplit - previousCascadeStart) * CascadeBlendFraction));

                float middle = (cascadeNear + split) * 0.5f;

                Vector3 center =
                    camera.Position + forward * middle;

                float horizontal = split * tangent * aspectRatio;
                float vertical = split * tangent;

                float radius = MathF.Sqrt(
                    (split - middle) * (split - middle) +
                    horizontal * horizontal +
                    vertical * vertical);

                radius = MathF.Ceiling((radius * 1.05f + 2f) * 8f)
                    / 8f;

                // Snap projection center to shadow texel boundaries.
                float texelSize =
                    (radius * 2f) / ShadowResolution;

                float centerX = Vector3.Dot(
                    center,
                    lightRight);

                float centerY = Vector3.Dot(
                    center,
                    lightUp);

                float snappedX =
                    MathF.Round(centerX / texelSize) * texelSize;

                float snappedY =
                    MathF.Round(centerY / texelSize) * texelSize;

                center +=
                    lightRight * (snappedX - centerX) +
                    lightUp * (snappedY - centerY);

                float casterExtension = Math.Clamp(
                    split * 0.5f + 32f,
                    40f,
                    240f);

                float lightOffset =
                    radius + casterExtension + 8f;

                Vector3 lightPosition =
                    center + sunDirection * lightOffset;

                Matrix4x4 lightView = CreateShadowView(
                    lightPosition,
                    lightForward);

                float lightFarPlane = lightOffset + radius + casterExtension + 16f;
                float depthRange = lightFarPlane - 0.1f;
                RasterizerState shadowRasterizer = SunShadowRasterizer(index, depthRange);

                Matrix4x4 lightProjection =
                    CreateShadowProjection(
                        radius,
                        0.1f,
                        lightFarPlane);

                Matrix4x4 lightViewProjection =
                    lightView * lightProjection;

                _shadowMatrices[index] = lightViewProjection;

                switch (index)
                {
                    case 0:
                        _shadowSplits.X = split;
                        _shadowDepthBias.X = 0.002f / depthRange;
                        break;
                    case 1:
                        _shadowSplits.Y = split;
                        _shadowDepthBias.Y = 0.002f / depthRange;
                        break;
                    case 2:
                        _shadowSplits.Z = split;
                        _shadowDepthBias.Z = 0.002f / depthRange;
                        break;
                    case 3:
                        _shadowSplits.W = split;
                        _shadowDepthBias.W = 0.002f / depthRange;
                        break;
                }

                var depthView = _shadowDepthViews[index];

                if (depthView == null)
                {
                    previousCascadeStart = previousSplit;
                    previousSplit = split;
                    continue;
                }

                _context.OutputMerger.SetRenderTargets(depthView);

                _context.ClearDepthStencilView(
                    depthView,
                    DepthStencilClearFlags.Depth,
                    1f,
                    0);

                _context.Rasterizer.SetViewport(
                    0,
                    0,
                    ShadowResolution,
                    ShadowResolution);

                try
                {
                    _terrain?.Render(
                        camera,
                        lightViewProjection,
                        shadowRasterizer);

                    Vector3 casterCenter =
                        center +
                        sunDirection * casterExtension * 0.35f;

                    _objects?.RenderShadowCasters(
                        casterCenter,
                        radius + casterExtension,
                        lightViewProjection,
                        shadowRasterizer);

                    _trees?.Render(
                        camera,
                        lightViewProjection,
                        playing,
                        shadowPass: true,
                        shadowForward: lightForward,
                        shadowRasterizer: shadowRasterizer);

                    if (_character != null || _npcRenderers.Count > 0)
                    {
                        Matrix4x4 relativeProjection =
                            Matrix4x4.CreateTranslation(
                                camera.Position) *
                            lightViewProjection;

                        _character?.RenderShadow(
                            relativeProjection,
                            camera.Position,
                            shadowRasterizer);

                        foreach (NpcPreviewEntry preview in
                                 _npcRenderers.Values)
                        {
                            preview.Renderer.RenderShadow(
                                relativeProjection,
                                camera.Position,
                                shadowRasterizer);
                        }
                    }
                }
                finally
                {
                    RestoreRenderTarget();
                }

                previousCascadeStart = previousSplit;
                previousSplit = split;
            }

            SetShadowConstants(
                true,
                _environmentSettings.ShadowStrength,
                forward);
        }

        private RasterizerState SunShadowRasterizer(int cascade, float depthRange)
        {
            if (_sunShadowRasterizers[cascade] == null ||
                _shadowDepthRanges[cascade] != depthRange)
            {
                // Positive bias moves casters away from the light (LessEqual, D32_Float).
                // Bound grazing-angle displacement to 2 cm in every cascade.
                var replacement = new RasterizerState(_device, new RasterizerStateDescription
                {
                    FillMode = FillMode.Solid,
                    CullMode = CullMode.None,
                    IsDepthClipEnabled = true,
                    DepthBias = 16,
                    SlopeScaledDepthBias = 1.0f,
                    DepthBiasClamp = 0.02f / depthRange
                });

                _sunShadowRasterizers[cascade]?.Dispose();
                _sunShadowRasterizers[cascade] = replacement;
                _shadowDepthRanges[cascade] = depthRange;
            }

            return _sunShadowRasterizers[cascade]!;
        }

        private void RenderSkyShadowMap(FreeCamera camera)
        {
            if (_skyShadowDepthView == null)
            {
                return;
            }

            float distance = Math.Clamp(
                MathF.Max(
                    _environmentSettings.ShadowDistance * 1.5f,
                    72f),
                72f,
                400f);

            float skyTexelSize =
                distance * 2f / SkyShadowResolution;

            Vector3 center = camera.Position;

            center.X = MathF.Round(
                center.X / skyTexelSize) * skyTexelSize;

            center.Z = MathF.Round(
                center.Z / skyTexelSize) * skyTexelSize;

            Matrix4x4 lightView = CreateShadowView(
                center + Vector3.UnitY * 512f,
                -Vector3.UnitY);

            Matrix4x4 lightProjection = CreateShadowProjection(
                distance,
                0.1f,
                1024f);

            Matrix4x4 lightViewProjection =
                lightView * lightProjection;

            _context.OutputMerger.SetRenderTargets(
                _skyShadowDepthView);

            _context.ClearDepthStencilView(
                _skyShadowDepthView,
                DepthStencilClearFlags.Depth,
                1f,
                0);

            _context.Rasterizer.SetViewport(
                0,
                0,
                SkyShadowResolution,
                SkyShadowResolution);

            try
            {
                _objects?.RenderShadowCasters(
                    center,
                    distance + 40f,
                    lightViewProjection);
            }
            finally
            {
                RestoreRenderTarget();
            }

            SetSkyShadowConstants(
                lightViewProjection,
                true);

            _context.PixelShader.SetShaderResource(
                23,
                _skyShadowShaderView);

            _context.PixelShader.SetSampler(
                8,
                _skyShadowSampler);
        }

        private void RestoreRenderTarget()
        {
            _context.OutputMerger.SetRenderTargets(
                _depthTarget,
                _target,
                _ambientTarget);

            _context.Rasterizer.SetViewport(
                0,
                0,
                _width,
                _height);

            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(null);
            _context.Rasterizer.State = null;
        }

        private static Matrix4x4 CreateShadowProjection(
            float extent,
            float nearPlane,
            float farPlane)
        {
            float depth = farPlane - nearPlane;

            return new Matrix4x4
            {
                M11 = 1f / extent,
                M22 = 1f / extent,
                M33 = 1f / depth,
                M43 = -nearPlane / depth,
                M44 = 1f
            };
        }

        private static Matrix4x4 CreateShadowView(
            Vector3 position,
            Vector3 forward)
        {
            forward = Vector3.Normalize(forward);

            Vector3 reference =
                MathF.Abs(forward.Y) > 0.99f
                    ? Vector3.UnitZ
                    : Vector3.UnitY;

            Vector3 right = Vector3.Normalize(
                Vector3.Cross(reference, forward));

            Vector3 up = Vector3.Cross(
                forward,
                right);

            return new Matrix4x4
            {
                M11 = right.X,
                M12 = up.X,
                M13 = forward.X,
                M14 = 0f,

                M21 = right.Y,
                M22 = up.Y,
                M23 = forward.Y,
                M24 = 0f,

                M31 = right.Z,
                M32 = up.Z,
                M33 = forward.Z,
                M34 = 0f,

                M41 = -Vector3.Dot(right, position),
                M42 = -Vector3.Dot(up, position),
                M43 = -Vector3.Dot(forward, position),
                M44 = 1f
            };
        }

        private void SetShadowConstants(
            bool enabled,
            float strength,
            Vector3 cameraForward)
        {
            var constants = new ShadowConstants
            {
                Matrix0 = _shadowMatrices[0],
                Matrix1 = _shadowMatrices[1],
                Matrix2 = _shadowMatrices[2],
                Matrix3 = _shadowMatrices[3],
                Parameters = new Vector4(
                    enabled ? 1f : 0f,
                    Math.Clamp(strength, 0f, 1f),
                    ShadowResolution,
                    CascadeBlendFraction),
                Splits = _shadowSplits,
                CameraForward = new Vector4(
                    Vector3.Normalize(cameraForward),
                    _shadowNearPlane),
                DepthBias = _shadowDepthBias
            };

            _context.UpdateSubresource(
                ref constants,
                _shadowConstantBuffer!);

            _context.PixelShader.SetConstantBuffer(
                2,
                _shadowConstantBuffer);
        }

        private void SetSkyShadowConstants(
            Matrix4x4 matrix,
            bool enabled)
        {
            var constants = new SkyShadowConstants
            {
                ViewProjection = matrix,
                Parameters = new Vector4(
                    enabled ? 1f : 0f,
                    SkyShadowResolution,
                    0.0002f,
                    0f)
            };

            _context.UpdateSubresource(
                ref constants,
                _skyShadowConstantBuffer!);

            _context.PixelShader.SetConstantBuffer(
                3,
                _skyShadowConstantBuffer);
        }

        private void DisposeShadowResources()
        {
            if (_context != null)
            {
                _context.PixelShader.SetShaderResource(7, null);
                _context.PixelShader.SetShaderResource(23, null);
            }

            _skyShadowConstantBuffer?.Dispose();
            _skyShadowConstantBuffer = null;

            _skyShadowSampler?.Dispose();
            _skyShadowSampler = null;

            _skyShadowShaderView?.Dispose();
            _skyShadowShaderView = null;

            _skyShadowDepthView?.Dispose();
            _skyShadowDepthView = null;

            _skyShadowTexture?.Dispose();
            _skyShadowTexture = null;

            _shadowConstantBuffer?.Dispose();
            _shadowConstantBuffer = null;

            _shadowSampler?.Dispose();
            _shadowSampler = null;

            _shadowShaderView?.Dispose();
            _shadowShaderView = null;

            for (int index = 0; index < ShadowCascadeCount; index++)
            {
                _shadowDepthViews[index]?.Dispose();
                _shadowDepthViews[index] = null;
                _sunShadowRasterizers[index]?.Dispose();
                _sunShadowRasterizers[index] = null;
            }

            _shadowTexture?.Dispose();
            _shadowTexture = null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ShadowConstants
        {
            public Matrix4x4 Matrix0;
            public Matrix4x4 Matrix1;
            public Matrix4x4 Matrix2;
            public Matrix4x4 Matrix3;
            public Vector4 Parameters;
            public Vector4 Splits;
            public Vector4 CameraForward;
            public Vector4 DepthBias;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SkyShadowConstants
        {
            public Matrix4x4 ViewProjection;
            public Vector4 Parameters;
        }
    }
}
