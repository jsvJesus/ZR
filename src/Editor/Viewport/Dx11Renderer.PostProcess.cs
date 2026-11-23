using System;
using System.Numerics;
using Editor.Rendering.Shaders;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Assets;

namespace Editor.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private Texture2D? _hdrSceneTexture;
        private ShaderResourceView? _hdrSceneResource;
        private Texture2D? _ambientTexture;
        private RenderTargetView? _ambientTarget;
        private ShaderResourceView? _ambientResource;
        private Texture2D? _composedTexture;
        private RenderTargetView? _composedTarget;
        private ShaderResourceView? _composedResource;
        private RenderTargetView? _backBufferTarget;

        private ShaderResourceView? _depthShaderResource;

        private Texture2D? _normalTexture;
        private RenderTargetView? _normalTarget;
        private ShaderResourceView? _normalResource;

        private Texture2D? _roughnessTexture;
        private RenderTargetView? _roughnessTarget;
        private ShaderResourceView? _roughnessResource;

        private Texture2D? _motionTexture;
        private RenderTargetView? _motionTarget;
        private ShaderResourceView? _motionResource;

        private PostProcessManager? _postProcessManager;

        internal ShaderResourceView? SceneColorResource =>
            _hdrSceneResource;

        internal ShaderResourceView? SceneDepthResource =>
            _firstPersonDepthActive ? _firstPersonDepth?.Resource ?? _depthShaderResource : _depthShaderResource;

        internal ShaderResourceView? SceneNormalResource =>
            _normalResource;

        internal ShaderResourceView? SceneRoughnessResource =>
            _roughnessResource;

        internal ShaderResourceView? SceneMotionResource =>
            _motionResource;

        private void InitializePostProcessing()
        {
            var compiler = new ShaderCompiler(
                AssetPaths.FromApplication());

            _postProcessManager = new PostProcessManager(
                _device,
                compiler);

            _firstPersonDepth = new FirstPersonDepthBuffer(
                _device,
                compiler);
        }

        private void CreateRenderTargetResources()
        {
            try
            {
                using (var backBuffer =
                    Texture2D.FromSwapChain<Texture2D>(
                        _swapChain,
                        0))
                {
                    _backBufferTarget = new RenderTargetView(
                        _device,
                        backBuffer);
                }

                (
                    _hdrSceneTexture,
                    _target,
                    _hdrSceneResource
                ) = CreateColorSurface(
                    Format.R16G16B16A16_Float);

                (_ambientTexture, _ambientTarget, _ambientResource) =
                    CreateColorSurface(Format.R16G16B16A16_Float);
                (_composedTexture, _composedTarget, _composedResource) =
                    CreateColorSurface(Format.R16G16B16A16_Float);

                _depthTexture = new Texture2D(
                    _device,
                    new Texture2DDescription
                    {
                        Width = _width,
                        Height = _height,
                        ArraySize = 1,
                        MipLevels = 1,
                        Format = Format.R32_Typeless,
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        BindFlags =
                            BindFlags.DepthStencil |
                            BindFlags.ShaderResource,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = ResourceOptionFlags.None
                    });

                _depthTarget = new DepthStencilView(
                    _device,
                    _depthTexture,
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

                _depthShaderResource = new ShaderResourceView(
                    _device,
                    _depthTexture,
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

                (
                    _normalTexture,
                    _normalTarget,
                    _normalResource
                ) = CreateColorSurface(
                    Format.R16G16B16A16_Float);

                (
                    _roughnessTexture,
                    _roughnessTarget,
                    _roughnessResource
                ) = CreateColorSurface(
                    Format.R8_UNorm);

                (
                    _motionTexture,
                    _motionTarget,
                    _motionResource
                ) = CreateColorSurface(
                    Format.R16G16_Float);
            }
            catch
            {
                DisposeRenderTargetResources();
                throw;
            }
        }

        private (
            Texture2D Texture,
            RenderTargetView Target,
            ShaderResourceView Resource
        ) CreateColorSurface(Format format)
        {
            var texture = new Texture2D(
                _device,
                new Texture2DDescription
                {
                    Width = _width,
                    Height = _height,
                    ArraySize = 1,
                    MipLevels = 1,
                    Format = format,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags =
                        BindFlags.RenderTarget |
                        BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None
                });

            try
            {
                var target = new RenderTargetView(
                    _device,
                    texture);

                try
                {
                    var resource = new ShaderResourceView(
                        _device,
                        texture);

                    return (texture, target, resource);
                }
                catch
                {
                    target.Dispose();
                    throw;
                }
            }
            catch
            {
                texture.Dispose();
                throw;
            }
        }

        private void RenderPostProcessing(
            Matrix4x4 viewProjection,
            Vector3 cameraPosition,
            Vector3 sunDirection)
        {
            if (_postProcessManager == null ||
                _backBufferTarget == null ||
                _hdrSceneResource == null ||
                _ambientResource == null ||
                _composedTarget == null ||
                _composedResource == null ||
                _depthShaderResource == null ||
                _motionResource == null ||
                _normalTarget == null ||
                _normalResource == null ||
                _roughnessTarget == null ||
                _motionTarget == null)
            {
                return;
            }

            float contactStrength =
                _environmentSettings.ShadowsEnabled &&
                !_objectPreview &&
                sunDirection.Y > 0.04f
                    ? 0.3f
                    : 0f;

            _postProcessManager.ToneMappingEnabled = _environmentSettings.ToneMappingEnabled;
            _postProcessManager.Exposure = _environmentSettings.Exposure;
            _postProcessManager.BloomIntensity = _environmentSettings.BloomIntensity;
            _postProcessManager.BloomThreshold = _environmentSettings.BloomThreshold;
            _postProcessManager.ColorGradingStrength = _environmentSettings.ColorGradingStrength;
            _postProcessManager.GradingPreset = _environmentSettings.ColorGradingPreset;
            _postProcessManager.SkyDitherStrength = _environmentSettings.SkyDitherStrength;

            _postProcessManager.AoEnabled = _environmentSettings.AoEnabled;
            _postProcessManager.AoIntensity = _environmentSettings.AoIntensity;
            _postProcessManager.AoRadius = _environmentSettings.AoRadius;
            _postProcessManager.AoQuality = _environmentSettings.AoQuality;

            _postProcessManager.TaaEnabled = _environmentSettings.TaaEnabled && !_objectPreview;
            _postProcessManager.TaaFeedback = _environmentSettings.TaaFeedback;
            _postProcessManager.TaaJitterPixels = _taaJitterPixels;

            bool firstPersonDepth =
                _firstPersonDepthActive &&
                _firstPersonDepth?.Resource != null;

            Matrix4x4 activeViewProjection = firstPersonDepth
                ? _firstPersonViewProjection
                : viewProjection;

            Action<Matrix4x4, Vector3, bool> skeletalVelocity =
                (previousViewProjection, previousCameraPosition, historyValid) =>
                {
                    if (_character == null ||
                        _motionTarget == null)
                    {
                        return;
                    }

                    DepthStencilView? depthTarget = firstPersonDepth
                        ? _firstPersonDepth?.DepthTarget
                        : _depthTarget;

                    if (depthTarget == null)
                    {
                        return;
                    }

                    bool firstPersonMeshes =
                        firstPersonDepth &&
                        _playFirstPersonBlend >= 0.8f;

                    _character.RenderVelocity(
                        _device,
                        _motionTarget,
                        depthTarget,
                        activeViewProjection,
                        previousViewProjection,
                        cameraPosition,
                        previousCameraPosition,
                        firstPersonMeshes,
                        historyValid,
                        _width,
                        _height);
                };

            _postProcessManager.Render(
                _context,
                _width,
                _height,
                activeViewProjection,
                cameraPosition,
                sunDirection,
                contactStrength,
                _hdrSceneResource,
                _ambientResource,
                _composedTarget,
                _composedResource,
                firstPersonDepth
                    ? _firstPersonDepth!.Resource!
                    : _depthShaderResource,
                _normalTarget,
                _normalResource,
                _roughnessTarget,
                _motionTarget,
                _motionResource,
                _backBufferTarget,
                _shadowConstantBuffer,
                _shadowShaderView,
                _shadowSampler,
                _skyShadowConstantBuffer,
                _skyShadowShaderView,
                _skyShadowSampler,
                firstPersonDepth,
                skeletalVelocity);
        }

        private void ResetPostProcessHistory()
        {
            _postProcessManager?.ResetHistory();
        }

        private void DisposePostProcessing()
        {
            _postProcessManager?.Dispose();
            _postProcessManager = null;
        }

        private void DisposeRenderTargetResources()
        {
            _firstPersonDepthActive = false;
            _ambientResource?.Dispose(); _ambientResource = null;
            _ambientTarget?.Dispose(); _ambientTarget = null;
            _ambientTexture?.Dispose(); _ambientTexture = null;
            _composedResource?.Dispose(); _composedResource = null;
            _composedTarget?.Dispose(); _composedTarget = null;
            _composedTexture?.Dispose(); _composedTexture = null;
            _motionResource?.Dispose();
            _motionResource = null;

            _motionTarget?.Dispose();
            _motionTarget = null;

            _motionTexture?.Dispose();
            _motionTexture = null;

            _roughnessResource?.Dispose();
            _roughnessResource = null;

            _roughnessTarget?.Dispose();
            _roughnessTarget = null;

            _roughnessTexture?.Dispose();
            _roughnessTexture = null;

            _normalResource?.Dispose();
            _normalResource = null;

            _normalTarget?.Dispose();
            _normalTarget = null;

            _normalTexture?.Dispose();
            _normalTexture = null;

            _depthShaderResource?.Dispose();
            _depthShaderResource = null;

            _depthTarget?.Dispose();
            _depthTarget = null;

            _depthTexture?.Dispose();
            _depthTexture = null;

            _hdrSceneResource?.Dispose();
            _hdrSceneResource = null;

            _target?.Dispose();
            _target = null;

            _hdrSceneTexture?.Dispose();
            _hdrSceneTexture = null;

            _backBufferTarget?.Dispose();
            _backBufferTarget = null;
        }
    }
}
