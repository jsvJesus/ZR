using ZoneReborn.World.Terrain;
using ZoneReborn.World.Terrain.Import;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Levels.Import;

/// <summary>Preserves the old surface and enclosed holes; only exterior empty cells accept new ground.</summary>
internal sealed class TerrainExpansion
{
    private readonly TerrainDescriptor _old;
    private readonly IReadOnlyDictionary<(int X, int Z), TerrainTileData> _tiles;
    private readonly TerrainHoles _holes;
    private readonly bool[] _exterior;
    public int OffsetX { get; }
    public int OffsetZ { get; }

    public TerrainExpansion(TerrainDescriptor old, IReadOnlyDictionary<(int X, int Z), TerrainTileData> tiles,
        TerrainHoles holes, RasterizedTerrain raster, CancellationToken token)
    {
        _old = old; _tiles = tiles; _holes = holes;
        OffsetX = (int)MathF.Round((old.SourceOffsetX - raster.Offset.X) / old.CellSize);
        OffsetZ = (int)MathF.Round((old.SourceOffsetZ - raster.Offset.Y) / old.CellSize);
        if (OffsetX < 0 || OffsetZ < 0 || OffsetX % 256 != 0 || OffsetZ % 256 != 0)
            throw new InvalidDataException("Не удалось совместить старую и новую сетки Terrain.");
        _exterior = Exterior(holes, token);
    }

    public bool ProtectedCell(int x, int z)
    {
        x -= OffsetX; z -= OffsetZ;
        return x >= 0 && z >= 0 && x < _holes.Width && z < _holes.Height && !_exterior[z * _holes.Width + x];
    }

    public bool ProtectedVertex(int x, int z) => ProtectedCell(x, z) || ProtectedCell(x - 1, z)
        || ProtectedCell(x, z - 1) || ProtectedCell(x - 1, z - 1);

    public bool OldHole(int x, int z) => _holes.Contains(x - OffsetX, z - OffsetZ);

    public ushort OldHeight(int x, int z)
    {
        x -= OffsetX; z -= OffsetZ;
        return _tiles[(x / 256, z / 256)].Heights[(z % 256) * 256 + x % 256];
    }

    public TerrainMaterialTile Materials(int tx, int tz, TerrainMaterialTile source)
    {
        var masks = source.Masks.ToDictionary(mask => mask.Group, mask => (byte[])mask.Data.Clone());
        var active = source.ActiveLayers.ToHashSet();
        for (int z = 0; z < 256; z++)
        for (int x = 0; x < 256; x++)
        {
            // Convert the bordered mask texel center to the old sample grid.
            float oldX = tx * 256 + (x - 3.5f) * 256 / 248f - OffsetX;
            float oldZ = tz * 256 + (z - 3.5f) * 256 / 248f - OffsetZ;
            if (!TouchesProtected(oldX, oldZ)) continue;
            // Keep the mapped old tile's own border: its GPU sampler used that border,
            // even when the texel center was technically in the neighboring tile.
            int ox = Math.Clamp(tx - OffsetX / 256, 0, _old.TilesX - 1);
            int oz = Math.Clamp(tz - OffsetZ / 256, 0, _old.TilesZ - 1);
            var tile = _tiles[(ox, oz)];
            int px = x + (tx - OffsetX / 256 - ox) * 248;
            int pz = z + (tz - OffsetZ / 256 - oz) * 248;
            if (px < 0 || pz < 0 || px > 255 || pz > 255) continue;
            int offset = (z * 256 + x) * 4;
            foreach (byte[] pixels in masks.Values) { pixels[offset] = 0; pixels[offset + 1] = 0; pixels[offset + 2] = 0; }
            foreach (var mask in tile.Masks)
            {
                var rgb = TerrainMaskCodec.Sample(mask.Data, px, pz);
                for (int channel = 0; channel < 3; channel++)
                {
                    int index = mask.Group * 3 + channel + 1;
                    if (index >= 255 || !tile.ActiveLayers.Contains((byte)index)) continue;
                    if (!masks.TryGetValue(mask.Group, out var pixels)) masks.Add(mask.Group, pixels = new byte[256 * 256 * 4]);
                    float value = channel == 0 ? rgb.X : channel == 1 ? rgb.Y : rgb.Z;
                    pixels[offset + channel] = (byte)Math.Clamp((int)MathF.Round(value * 255), 0, 255);
                    active.Add((byte)index);
                }
            }
        }
        return new(active.Order().ToArray(), masks.OrderBy(pair => pair.Key).Select(pair => new TerrainMask(pair.Key, pair.Value)).ToArray());
    }

    private bool TouchesProtected(float x, float z)
    {
        // A linearly filtered texel contributes up to one full texel spacing away.
        const float support = 256f / 248;
        int x0 = Math.Max(0, (int)MathF.Floor(x - support));
        int x1 = Math.Min(_holes.Width - 1, (int)MathF.Floor(x + support));
        int z0 = Math.Max(0, (int)MathF.Floor(z - support));
        int z1 = Math.Min(_holes.Height - 1, (int)MathF.Floor(z + support));
        for (int cz = z0; cz <= z1; cz++)
        for (int cx = x0; cx <= x1; cx++)
            if (!_exterior[cz * _holes.Width + cx]) return true;
        return false;
    }

    private static bool[] Exterior(TerrainHoles holes, CancellationToken token)
    {
        var exterior = new bool[checked(holes.Width * holes.Height)];
        var queue = new int[exterior.Length]; int head = 0, tail = 0;
        for (int x = 0; x < holes.Width; x++) { Visit(x, 0); Visit(x, holes.Height - 1); }
        for (int z = 0; z < holes.Height; z++) { Visit(0, z); Visit(holes.Width - 1, z); }
        while (head < tail)
        {
            if ((head & 4095) == 0) token.ThrowIfCancellationRequested();
            int position = queue[head++], x = position % holes.Width, z = position / holes.Width;
            if (x > 0) Visit(x - 1, z); if (x + 1 < holes.Width) Visit(x + 1, z);
            if (z > 0) Visit(x, z - 1); if (z + 1 < holes.Height) Visit(x, z + 1);
        }
        return exterior;

        void Visit(int x, int z)
        {
            int index = z * holes.Width + x;
            if (exterior[index] || !holes.Contains(x, z)) return;
            exterior[index] = true; queue[tail++] = index;
        }
    }
}
