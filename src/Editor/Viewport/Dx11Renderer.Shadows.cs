using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Editor.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private const int ShadowResolution = 1024;

        private Texture2D? _shadowTexture;
        private DepthStencilView? _shadowDepthView;
        private ShaderResourceView? _shadowShaderView;
        private SamplerState? _shadowSampler;
        private Buffer? _shadowConstantBuffer;

        private void InitializeShadowResources()
        {
            _shadowTexture = new Texture2D(
                _device,
                new Texture2DDescription
                {
                    Width = ShadowResolution,
                    Height = ShadowResolution,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.R32_Typeless,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags =
                        BindFlags.DepthStencil |
                        BindFlags.ShaderResource
                });

            _shadowDepthView = new DepthStencilView(
                _device,
                _shadowTexture,
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

            _shadowShaderView = new ShaderResourceView(
                _device,
                _shadowTexture,
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

            _shadowSampler = new SamplerState(
                _device,
                new SamplerStateDescription
                {
                    Filter = Filter.ComparisonMinMagLinearMipPoint,
                    AddressU = TextureAddressMode.Border,
                    AddressV = TextureAddressMode.Border,
                    AddressW = TextureAddressMode.Border,
                    BorderColor = new SharpDX.Mathematics.Interop.RawColor4(
                        1f,
                        1f,
                        1f,
                        1f),
                    ComparisonFunction = Comparison.LessEqual,
                    MinimumLod = 0,
                    MaximumLod = float.MaxValue
                });

            _shadowConstantBuffer = new Buffer(
                _device,
                Utilities.SizeOf<ShadowConstants>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);
        }

        private void RenderEnvironmentShadows(
            FreeCamera camera,
            Vector3 sunDirection,
            float farPlane,
            bool playing)
        {
            if (_shadowDepthView == null ||
                _shadowShaderView == null ||
                _shadowSampler == null ||
                _shadowConstantBuffer == null)
            {
                return;
            }

            _context.PixelShader.SetShaderResource(7, null);

            var settings = _environmentSettings;

            if (!settings.ShadowsEnabled ||
                sunDirection.Y <= 0.04f ||
                _terrain == null ||
                _objectPreview)
            {
                SetShadowConstants(
                    Matrix4x4.Identity,
                    false,
                    settings.ShadowStrength);

                return;
            }

            float distance = Math.Clamp(
                settings.ShadowDistance,
                10f,
                200f);

            Vector3 center = camera.Position +
                camera.LookDirection * distance * 0.25f;

            float lightOffset = MathF.Max(
                distance * 2f,
                100f);

            Vector3 lightPosition = center +
                sunDirection * lightOffset;

            Matrix4x4 lightView = CreateShadowView(
                lightPosition,
                -sunDirection);

            float nearPlane = 0.1f;
            float farShadowPlane = lightOffset * 2f + distance;
            float extent = distance;

            var lightProjection = new Matrix4x4
            {
                M11 = 1f / extent,
                M22 = 1f / extent,
                M33 = 1f / (farShadowPlane - nearPlane),
                M43 = -nearPlane / (farShadowPlane - nearPlane),
                M44 = 1f
            };

            Matrix4x4 lightViewProjection =
                lightView * lightProjection;

            // Во время построения карты глубины не читаем её как текстуру.
            SetShadowConstants(
                lightViewProjection,
                false,
                settings.ShadowStrength);

            _context.OutputMerger.SetRenderTargets(
                _shadowDepthView);

            _context.ClearDepthStencilView(
                _shadowDepthView,
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

                _objects?.Render(
                    camera,
                    lightViewProjection,
                    MathF.Min(farPlane, distance * 3f));

                _trees?.Render(
                    camera,
                    lightViewProjection,
                    playing);

                if (_character != null)
                {
                    Matrix4x4 relativeShadowProjection =
                        Matrix4x4.CreateTranslation(camera.Position) *
                        lightViewProjection;

                    _character.Render(
                        relativeShadowProjection,
                        camera.Position);
                }
            }
            finally
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

            SetShadowConstants(
                lightViewProjection,
                true,
                settings.ShadowStrength);

            _context.PixelShader.SetShaderResource(
                7,
                _shadowShaderView);

            _context.PixelShader.SetSampler(
                7,
                _shadowSampler);
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
                    0.0025f)
            };

            _context.UpdateSubresource(
                ref constants,
                _shadowConstantBuffer!);

            _context.PixelShader.SetConstantBuffer(
                2,
                _shadowConstantBuffer);
        }

        private static Matrix4x4 CreateShadowView(
            Vector3 position,
            Vector3 forward)
        {
            forward = Vector3.Normalize(forward);

            Vector3 reference = MathF.Abs(forward.Y) > 0.99f
                ? Vector3.UnitZ
                : Vector3.UnitY;

            Vector3 right = Vector3.Normalize(
                Vector3.Cross(reference, forward));

            Vector3 up = Vector3.Cross(forward, right);

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

        private void DisposeShadowResources()
        {
            _context?.PixelShader.SetShaderResource(7, null);

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
    }
}
