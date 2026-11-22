using System;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace Editor.Viewport.Lighting
{
    internal enum LocalShadowKind
    {
        Spot,
        Point
    }

    internal sealed class LocalShadowMap : IDisposable
    {
        private readonly DepthStencilView[] _depthViews;
        private Texture2D? _texture;
        private ShaderResourceView? _shaderResource;

        public LocalShadowKind Kind { get; }
        public int Resolution { get; }
        public int FaceCount => _depthViews.Length;

        public ShaderResourceView ShaderResource =>
            _shaderResource ??
            throw new ObjectDisposedException(nameof(LocalShadowMap));

        public LocalShadowMap(
            Device device,
            LocalShadowKind kind,
            int resolution = 0)
        {
            Kind = kind;

            Resolution = resolution > 0
                ? Math.Clamp(resolution, 128, 2048)
                : kind == LocalShadowKind.Point
                    ? 512
                    : 1024;

            int faceCount = kind == LocalShadowKind.Point
                ? 6
                : 1;

            _depthViews = new DepthStencilView[faceCount];

            try
            {
                _texture = new Texture2D(
                    device,
                    new Texture2DDescription
                    {
                        Width = Resolution,
                        Height = Resolution,
                        MipLevels = 1,
                        ArraySize = faceCount,
                        Format = Format.R32_Typeless,
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        BindFlags =
                            BindFlags.DepthStencil |
                            BindFlags.ShaderResource,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = kind == LocalShadowKind.Point
                            ? ResourceOptionFlags.TextureCube
                            : ResourceOptionFlags.None
                    });

                for (int index = 0; index < faceCount; index++)
                {
                    var description = new DepthStencilViewDescription
                    {
                        Format = Format.D32_Float
                    };

                    if (kind == LocalShadowKind.Point)
                    {
                        description.Dimension =
                            DepthStencilViewDimension.Texture2DArray;

                        description.Texture2DArray =
                            new DepthStencilViewDescription.Texture2DArrayResource
                            {
                                FirstArraySlice = index,
                                ArraySize = 1,
                                MipSlice = 0
                            };
                    }
                    else
                    {
                        description.Dimension =
                            DepthStencilViewDimension.Texture2D;

                        description.Texture2D =
                            new DepthStencilViewDescription.Texture2DResource
                            {
                                MipSlice = 0
                            };
                    }

                    _depthViews[index] = new DepthStencilView(
                        device,
                        _texture,
                        description);
                }

                var resourceDescription =
                    new ShaderResourceViewDescription
                    {
                        Format = Format.R32_Float
                    };

                if (kind == LocalShadowKind.Point)
                {
                    resourceDescription.Dimension =
                        ShaderResourceViewDimension.TextureCube;

                    resourceDescription.TextureCube =
                        new ShaderResourceViewDescription.TextureCubeResource
                        {
                            MostDetailedMip = 0,
                            MipLevels = 1
                        };
                }
                else
                {
                    resourceDescription.Dimension =
                        ShaderResourceViewDimension.Texture2D;

                    resourceDescription.Texture2D =
                        new ShaderResourceViewDescription.Texture2DResource
                        {
                            MostDetailedMip = 0,
                            MipLevels = 1
                        };
                }

                _shaderResource = new ShaderResourceView(
                    device,
                    _texture,
                    resourceDescription);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public DepthStencilView GetDepthView(int face = 0)
        {
            if (_texture == null)
            {
                throw new ObjectDisposedException(
                    nameof(LocalShadowMap));
            }

            if ((uint)face >= (uint)_depthViews.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(face));
            }

            return _depthViews[face];
        }

        public void Dispose()
        {
            _shaderResource?.Dispose();
            _shaderResource = null;

            foreach (var view in _depthViews)
            {
                view?.Dispose();
            }

            _texture?.Dispose();
            _texture = null;
        }
    }
}
