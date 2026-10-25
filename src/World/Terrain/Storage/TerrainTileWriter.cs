using System.Text;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain.Storage;

public static class TerrainTileWriter
{
    public static void Write(string path, TerrainTileData tile)
    {
        using var writer = new BinaryWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write), Encoding.UTF8);
        writer.Write("ZRTERR01"u8);
        writer.Write(tile.X);
        writer.Write(tile.Z);
        writer.Write(tile.Heights.Length);
        foreach (ushort height in tile.Heights) writer.Write(height);
        writer.Write(tile.Normals.Length);
        writer.Write(tile.Normals);
        writer.Write(tile.ActiveLayers.Length);
        writer.Write(tile.ActiveLayers);
        writer.Write(tile.Masks.Count);
        foreach (var mask in tile.Masks)
        {
            writer.Write(mask.Group);
            writer.Write(mask.Data.Length);
            writer.Write(mask.Data);
        }
    }
}

