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

namespace Editor.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private const int ShadowResolution = 2048;
        private const int SkyShadowResolution = 1024;

        private Texture2D? _shadowTexture;
        private DepthStencilView? _shadowDepthView;
        private ShaderResourceView? _shadowShaderView;
        private SamplerState? _shadowSampler;
        private Buffer? _shadowConstantBuffer;

        private Texture2D? _skyShadowTexture;
        private DepthStencilView? _skyShadowDepthView;
        private ShaderResourceView? _skyShadowShaderView;
        private SamplerState? _skyShadowSampler;
        private Buffer? _skyShadowConstantBuffer;

        private void InitializeShadowResources()
        {
            CreateShadowMap(
                ShadowResolution,
                out _shadowTexture,
                out _shadowDepthView,
                out _shadowShaderView);

            CreateShadowMap(
                SkyShadowResolution,
                out _skyShadowTexture,
                out _skyShadowDepthView,
                out _skyShadowShaderView);

            _shadowSampler = CreateComparisonSampler();
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
                        BindFlags.ShaderResource
                });

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

        private SamplerState CreateComparisonSampler()
        {
            return new SamplerState(
                _device,
                new SamplerStateDescription
                {
                    Filter = Filter.ComparisonMinMagLinearMipPoint,
                    AddressU = TextureAddressMode.Border,
                    AddressV = TextureAddressMode.Border,
                    AddressW = TextureAddressMode.Border,
                    BorderColor =
                        new SharpDX.Mathematics.Interop.RawColor4(
                            1f,
                            1f,
                            1f,
                            1f),
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
            if (_shadowDepthView == null ||
                _shadowShaderView == null ||
                _shadowConstantBuffer == null ||
                _skyShadowDepthView == null ||
                _skyShadowShaderView == null ||
                _skyShadowConstantBuffer == null)
            {
                return;
            }

            _context.PixelShader.SetShaderResource(7, null);
            _context.PixelShader.SetShaderResource(23, null);

            var settings = _environmentSettings;

            SetShadowConstants(
                Matrix4x4.Identity,
                false,
                settings.ShadowStrength);

            SetSkyShadowConstants(
                Matrix4x4.Identity,
                false);

            if (!settings.ShadowsEnabled || _objectPreview)
            {
                return;
            }

            if (sunDirection.Y > 0.04f)
            {
                RenderSunShadowMap(
                    camera,
                    sunDirection,
                    farPlane,
                    playing);
            }

            RenderSkyShadowMap(camera);
        }

        private void RenderSunShadowMap(
            FreeCamera camera,
            Vector3 sunDirection,
            float farPlane,
            bool playing)
        {
            float distance = Math.Clamp(
                _environmentSettings.ShadowDistance,
                25f,
                200f);

            Vector3 center = camera.Position +
                camera.LookDirection * distance * 0.25f;

            float lightOffset = MathF.Max(
                distance * 4f,
                150f);

            Vector3 lightPosition =
                center + sunDirection * lightOffset;

            Matrix4x4 lightView = CreateShadowView(
                lightPosition,
                -sunDirection);

            float nearPlane = 0.1f;
            float farShadowPlane =
                lightOffset * 2f + distance * 3f;

            Matrix4x4 lightProjection =
                CreateShadowProjection(
                    distance,
                    nearPlane,
                    farShadowPlane);

            Matrix4x4 lightViewProjection =
                lightView * lightProjection;

            _context.OutputMerger.SetRenderTargets(
                _shadowDepthView);

            _context.ClearDepthStencilView(
                _shadowDepthView!,
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
                    lightViewProjection);

                _roads?.Render(
                    camera.Position,
                    lightViewProjection,
                    farPlane,
                    playing);

                _objects?.RenderShadowCasters(
                    center,
                    distance * 3f,
                    lightViewProjection);

                _trees?.Render(
                    camera,
                    lightViewProjection,
                    playing);

                if (_character != null)
                {
                    Matrix4x4 relativeShadowProjection =
                        Matrix4x4.CreateTranslation(
                            camera.Position) *
                        lightViewProjection;

                    _character.Render(
                        relativeShadowProjection,
                        camera.Position);
                }
            }
            finally
            {
                RestoreRenderTarget();
            }

            SetShadowConstants(
                lightViewProjection,
                true,
                _environmentSettings.ShadowStrength);

            _context.PixelShader.SetShaderResource(
                7,
                _shadowShaderView);

            _context.PixelShader.SetSampler(
                7,
                _shadowSampler);
        }

        private void RenderSkyShadowMap(FreeCamera camera)
        {
            float distance = Math.Clamp(
                MathF.Max(
                    _environmentSettings.ShadowDistance * 1.5f,
                    72f),
                72f,
                200f);

            Vector3 center = camera.Position;

            Vector3 lightPosition = center +
                Vector3.UnitY * 512f;

            Matrix4x4 lightView = CreateShadowView(
                lightPosition,
                -Vector3.UnitY);

            Matrix4x4 lightProjection =
                CreateShadowProjection(
                    distance,
                    0.1f,
                    1024f);

            Matrix4x4 lightViewProjection =
                lightView * lightProjection;

            _context.OutputMerger.SetRenderTargets(
                _skyShadowDepthView);

            _context.ClearDepthStencilView(
                _skyShadowDepthView!,
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
                _target);

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
            Matrix4x4 matrix,
            bool enabled,
            float strength)
        {
            var constants = new ShadowConstants
            {
                ViewProjection = matrix,
                Parameters = new Vector4(
                    enabled ? 1f : 0f,
                    Math.Clamp(strength, 0f, 1f),
                    ShadowResolution,
                    0.0001f)
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
                    0.0001f,
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
            _skyShadowSampler?.Dispose();
            _skyShadowShaderView?.Dispose();
            _skyShadowDepthView?.Dispose();
            _skyShadowTexture?.Dispose();

            _shadowConstantBuffer?.Dispose();
            _shadowSampler?.Dispose();
            _shadowShaderView?.Dispose();
            _shadowDepthView?.Dispose();
            _shadowTexture?.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ShadowConstants
        {
            public Matrix4x4 ViewProjection;
            public Vector4 Parameters;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SkyShadowConstants
        {
            public Matrix4x4 ViewProjection;
            public Vector4 Parameters;
        }
    }
}
