using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Editor.Environment;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;

namespace Editor.Viewport
{
    internal sealed class ColorLutAtlas : IDisposable
    {
        private const int CubeSize = 32;
        private const int AtlasWidth = CubeSize * CubeSize;
        private const int AtlasHeight = CubeSize;

        private readonly Device _device;

        private ShaderResourceView? _view;
        private ColorGradingPreset? _currentPreset;
        private bool _disposed;

        public ShaderResourceView View =>
            _view ?? throw new InvalidOperationException(
                "Color LUT is not initialized.");

        public ColorLutAtlas(Device device)
        {
            _device = device;
            SetPreset(ColorGradingPreset.Atmospheric);
        }

        public void SetPreset(ColorGradingPreset preset)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(ColorLutAtlas));
            }

            if (_currentPreset == preset && _view != null)
            {
                return;
            }

            byte[] pixels = BuildAtlas(preset);

            GCHandle handle = GCHandle.Alloc(
                pixels,
                GCHandleType.Pinned);

            ShaderResourceView? replacement = null;

            try
            {
                var description = new Texture2DDescription
                {
                    Width = AtlasWidth,
                    Height = AtlasHeight,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.R8G8B8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Immutable,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None
                };

                var box = new DataBox(
                    handle.AddrOfPinnedObject(),
                    AtlasWidth * 4,
                    pixels.Length);

                using var texture = new Texture2D(
                    _device,
                    description,
                    new[] { box });

                replacement = new ShaderResourceView(
                    _device,
                    texture);
            }
            finally
            {
                handle.Free();
            }

            _view?.Dispose();
            _view = replacement;
            _currentPreset = preset;
        }

        private static byte[] BuildAtlas(
            ColorGradingPreset preset)
        {
            byte[] pixels = new byte[
                AtlasWidth * AtlasHeight * 4];

            for (int blue = 0; blue < CubeSize; blue++)
            {
                for (int green = 0; green < CubeSize; green++)
                {
                    for (int red = 0; red < CubeSize; red++)
                    {
                        var color = new Vector3(
                            red / 31f,
                            green / 31f,
                            blue / 31f);

                        color = ApplyPreset(
                            color,
                            preset);

                        int x = blue * CubeSize + red;
                        int y = green;

                        int offset =
                            (y * AtlasWidth + x) * 4;

                        pixels[offset] = ToByte(color.X);
                        pixels[offset + 1] = ToByte(color.Y);
                        pixels[offset + 2] = ToByte(color.Z);
                        pixels[offset + 3] = 255;
                    }
                }
            }

            return pixels;
        }

        private static Vector3 ApplyPreset(
            Vector3 color,
            ColorGradingPreset preset)
        {
            if (preset == ColorGradingPreset.Neutral)
            {
                return color;
            }

            float saturation = preset switch
            {
                ColorGradingPreset.Atmospheric => 0.9f,
                ColorGradingPreset.Cold => 0.82f,
                ColorGradingPreset.Warm => 0.96f,
                ColorGradingPreset.ZoneBleak => 0.76f,
                _ => 1f
            };

            float contrast = preset switch
            {
                ColorGradingPreset.Atmospheric => 1.08f,
                ColorGradingPreset.Cold => 1.06f,
                ColorGradingPreset.Warm => 1.04f,
                ColorGradingPreset.ZoneBleak => 1.11f,
                _ => 1f
            };

            float luminance =
                color.X * 0.2126f +
                color.Y * 0.7152f +
                color.Z * 0.0722f;

            color = new Vector3(luminance) +
                (color - new Vector3(luminance)) *
                saturation;

            color = (color - new Vector3(0.5f)) *
                contrast + new Vector3(0.5f);

            float shadow = MathF.Pow(
                1f - luminance,
                2f);

            float highlight = MathF.Pow(
                luminance,
                2f);

            switch (preset)
            {
                case ColorGradingPreset.Atmospheric:
                    color *= new Vector3(
                        1f - shadow * 0.035f + highlight * 0.018f,
                        1f + shadow * 0.012f,
                        1f + shadow * 0.055f - highlight * 0.025f);
                    break;

                case ColorGradingPreset.Cold:
                    color *= new Vector3(
                        1f - shadow * 0.075f,
                        1f + shadow * 0.012f,
                        1f + shadow * 0.095f);
                    break;

                case ColorGradingPreset.Warm:
                    color *= new Vector3(
                        1f + highlight * 0.065f,
                        1f + highlight * 0.025f,
                        1f - shadow * 0.07f);
                    break;
                case ColorGradingPreset.ZoneBleak:
                {
                    color *= new Vector3(
                        1f - shadow * 0.055f,
                        1f + shadow * 0.008f,
                        1f + shadow * 0.025f);

                    float greenDominance = MathF.Max(
                        0f,
                        color.Y - (color.X + color.Z) * 0.5f);

                    color.Y -= greenDominance * 0.13f;

                    color = Vector3.Lerp(
                        color,
                        color * new Vector3(
                            0.94f,
                            0.96f,
                            0.98f),
                        0.25f);

                    break;
                }
            }

            return Vector3.Clamp(
                color,
                Vector3.Zero,
                Vector3.One);
        }

        private static byte ToByte(float value)
        {
            return (byte)Math.Clamp(
                (int)MathF.Round(
                    Math.Clamp(value, 0f, 1f) * 255f),
                0,
                255);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _view?.Dispose();
            _view = null;
        }
    }
}
