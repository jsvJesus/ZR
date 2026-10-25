using System.IO;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Textures.Dds;
using Device = SharpDX.Direct3D11.Device;

namespace Editor.Viewport.Terrain;

internal sealed class TerrainTexture : IDisposable
{
    public ShaderResourceView View { get; }

    public TerrainTexture(Device device, DdsImage image) : this(device, image.Width, image.Height,
        image.Format switch
        {
            DdsPixelFormat.Bc1 => Format.BC1_UNorm,
            DdsPixelFormat.Bc3 => Format.BC3_UNorm,
            DdsPixelFormat.Rgba8 => Format.R8G8B8A8_UNorm,
            _ => throw new InvalidDataException("Unsupported Terrain texture format.")
        }, image.MipLevels) { }

    private TerrainTexture(Device device, int width, int height, Format format, IReadOnlyList<DdsMipLevel> mips)
    {
        var pins = new GCHandle[mips.Count];
        try
        {
            var boxes = new DataBox[mips.Count];
            for (int index = 0; index < mips.Count; index++)
            {
                pins[index] = GCHandle.Alloc(mips[index].Data, GCHandleType.Pinned);
                boxes[index] = new DataBox(pins[index].AddrOfPinnedObject(), mips[index].RowPitch, mips[index].Data.Length);
            }
            using var texture = new Texture2D(device, new Texture2DDescription
            {
                Width = width, Height = height, ArraySize = 1, MipLevels = mips.Count,
                Format = format, SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Immutable, BindFlags = BindFlags.ShaderResource
            }, boxes);
            View = new ShaderResourceView(device, texture);
        }
        finally
        {
            foreach (var pin in pins) if (pin.IsAllocated) pin.Free();
        }
    }

    public static TerrainTexture Bc1(Device device, byte[] data) => new(device, 256, 256, Format.BC1_UNorm,
        new[] { new DdsMipLevel(256, 256, 512, data) });

    public static TerrainTexture WeightSum(Device device, float[] weights)
    {
        var bytes = new byte[weights.Length * sizeof(float)];
        System.Buffer.BlockCopy(weights, 0, bytes, 0, bytes.Length);
        return new TerrainTexture(device, 256, 256, Format.R32_Float,
            new[] { new DdsMipLevel(256, 256, 256 * sizeof(float), bytes) });
    }

    public static TerrainTexture Solid(Device device, byte red, byte green, byte blue) => new(device,
        new DdsImage(1, 1, DdsPixelFormat.Rgba8,
            new[] { new DdsMipLevel(1, 1, 4, new[] { red, green, blue, (byte)255 }) }));

    public void Dispose() => View.Dispose();
}
