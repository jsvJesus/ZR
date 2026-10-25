using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain.Storage;

public static class TerrainOverviewWriter
{
    public static void Write(string path, TerrainOverviewData overview)
    {
        using var writer = new BinaryWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write));
        writer.Write("ZROVER01"u8);
        writer.Write(overview.Width);
        writer.Write(overview.Height);
        writer.Write(overview.Step);
        foreach (ushort value in overview.Heights) writer.Write(value);
    }
}

