using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters;

namespace Editor.Characters;

internal static class CharacterOutfitStore
{
    private static string FilePath(AssetPaths assets)
    {
        string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            Path.GetFullPath(assets.Root).ToUpperInvariant())))[..16];
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZoneReborn", "Editor", $"character-{key}.json");
    }

    public static bool Exists(AssetPaths assets) => File.Exists(FilePath(assets));

    public static CharacterOutfit? Read(AssetPaths assets)
    {
        string path = FilePath(assets);
        if (!File.Exists(path)) return null;
        var outfit = JsonSerializer.Deserialize<CharacterOutfit>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Сохранённый комплект пуст.");
        if (outfit.ItemTypes == null) throw new InvalidDataException("В комплекте нет списка вещей.");
        return outfit;
    }

    // A small settings commit on the UI thread, after the GPU swap. It must
    // finish before another mode/Stop/Close event can cancel the load session.
    public static void Write(AssetPaths assets, CharacterOutfit outfit)
    {
        string path = FilePath(assets), temporary = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(outfit,
                new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
