using System.Numerics;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainDocument
{
    private const int PaintOverviewStep = 8;
    private readonly HashSet<(int X, int Z)> _paintOverviewDirty = [];
    private readonly HashSet<(int X, int Z)> _paintedTiles = [];
    private byte[]? _paintOverview;
    private bool _paintOverviewLoaded;
    public int PaintOverviewWidth => Descriptor.SamplesX / PaintOverviewStep;
    public int PaintOverviewHeight => Descriptor.SamplesZ / PaintOverviewStep;

    private void InvalidatePaintOverview()
    {
        LoadPaintOverview();
        _paintOverviewDirty.UnionWith(_paintedTiles);
    }

    private void LoadPaintOverview()
    {
        if (_paintOverviewLoaded) return;
        _paintOverviewLoaded = true;
        if (Descriptor.HasSourceMaterialLayout)
            _paintedTiles.UnionWith(manifest.Tiles.Select(tile => (tile.X, tile.Z)));
        string path = AssetPathResolver.Resolve(directory, Descriptor.PaintOverviewFile);
        if (!File.Exists(path)) { _paintOverviewDirty.UnionWith(_paintedTiles); return; }
        using var reader = new BinaryReader(File.OpenRead(path));
        if (!reader.ReadBytes(8).AsSpan().SequenceEqual("ZRPAINT1"u8)
            || reader.ReadInt32() != PaintOverviewWidth || reader.ReadInt32() != PaintOverviewHeight)
            throw new InvalidDataException("Некорректный обзор покраски Terrain.");
        int size = checked(PaintOverviewWidth * PaintOverviewHeight * 4);
        _paintOverview = reader.ReadBytes(size);
        if (_paintOverview.Length != size || reader.BaseStream.Position != reader.BaseStream.Length)
            throw new InvalidDataException("Неполный обзор покраски Terrain.");
        for (int z = 0; z < PaintOverviewHeight; z++)
        for (int x = 0; x < PaintOverviewWidth; x++)
            if (_paintOverview[(z * PaintOverviewWidth + x) * 4 + 3] != 0)
                _paintedTiles.Add((x / 32, z / 32));
    }

    public byte[]? BuildPaintOverview()
    {
        LoadPaintOverview();
        if (_paintOverviewDirty.Count == 0) return _paintOverview;
        _paintOverview ??= new byte[checked(PaintOverviewWidth * PaintOverviewHeight * 4)];
        foreach (var key in _paintOverviewDirty)
        {
            var tile = Raw(key.X, key.Z);
            var active = tile.ActiveLayers.Where(index => index > 0 && index < layers.Count).ToHashSet();
            for (int z = 0; z < 32; z++)
            for (int x = 0; x < 32; x++)
            {
                // Average several mask samples so a narrow stroke remains visible at distance.
                Vector3 color = Vector3.Zero;
                float total = 0;
                for (int dz = 1; dz < 8; dz += 2)
                for (int dx = 1; dx < 8; dx += 2)
                {
                    int px = Math.Clamp((int)((x * 8 + dx) * 248f / 256 + 4), 0, 255);
                    int pz = Math.Clamp((int)((z * 8 + dz) * 248f / 256 + 4), 0, 255);
                    Vector3 sample = Vector3.Zero;
                    float weight = 0, knownWeight = 0;
                    foreach (var mask in tile.Masks)
                    {
                        Vector3 rgb = TerrainMaskCodec.Sample(mask.Data, px, pz);
                        for (int channel = 0; channel < 3; channel++)
                        {
                            int index = mask.Group * 3 + channel + 1;
                            if (index >= layers.Count || !active.Contains((byte)index)) continue;
                            float w = channel == 0 ? rgb.X : channel == 1 ? rgb.Y : rgb.Z;
                            weight += w;
                            var average = layers[index].AverageColor;
                            // Older levels already have a baked far image, but no per-layer mean colors.
                            // Keep that background instead of replacing it with a made-up tint.
                            if (average.Length != 3 || !average.All(float.IsFinite)) continue;
                            sample += new Vector3(average[0], average[1], average[2]) * w; knownWeight += w;
                        }
                    }
                    color += sample / Math.Max(1, weight); total += knownWeight / Math.Max(1, weight);
                }
                int offset = ((key.Z * 32 + z) * PaintOverviewWidth + key.X * 32 + x) * 4;
                if (total > 0) color /= total;
                _paintOverview[offset] = Byte(color.X);
                _paintOverview[offset + 1] = Byte(color.Y);
                _paintOverview[offset + 2] = Byte(color.Z);
                _paintOverview[offset + 3] = Byte(total / 16);
            }
            _paintedTiles.Add(key);
        }
        _paintOverviewDirty.Clear(); return _paintOverview;

        static byte Byte(float value) => (byte)Math.Clamp((int)MathF.Round(value * 255), 0, 255);
    }

    private void WritePaintOverview(string path, byte[] pixels)
    {
        using var writer = new BinaryWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write));
        writer.Write("ZRPAINT1"u8); writer.Write(PaintOverviewWidth); writer.Write(PaintOverviewHeight); writer.Write(pixels);
    }
}
