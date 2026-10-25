using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain.Import;

internal sealed class TerrainOverviewBuilder
{
    private const int Step = 32;
    private readonly TerrainDescriptor descriptor;
    private readonly int width;
    private readonly int height;
    private readonly ushort[] heights;

    public TerrainOverviewBuilder(TerrainDescriptor descriptor)
    {
        this.descriptor = descriptor;
        width = descriptor.SamplesX / Step + 1;
        height = descriptor.SamplesZ / Step + 1;
        heights = new ushort[width * height];
    }

    public void Add(TerrainTileData tile)
    {
        for (int z = 0; z < 256; z += Step)
        for (int x = 0; x < 256; x += Step)
            heights[(tile.Z * 256 / Step + z / Step) * width + tile.X * 256 / Step + x / Step] =
                tile.Heights[z * 256 + x];
        if (tile.X == descriptor.TilesX - 1)
            for (int z = 0; z < 256; z += Step)
                heights[(tile.Z * 256 / Step + z / Step) * width + width - 1] = tile.Heights[z * 256 + 255];
        if (tile.Z == descriptor.TilesZ - 1)
            for (int x = 0; x < 256; x += Step)
                heights[(height - 1) * width + tile.X * 256 / Step + x / Step] = tile.Heights[255 * 256 + x];
        if (tile.X == descriptor.TilesX - 1 && tile.Z == descriptor.TilesZ - 1)
            heights[^1] = tile.Heights[^1];
    }

    public TerrainOverviewData Build() => new(width, height, Step, heights);
}

