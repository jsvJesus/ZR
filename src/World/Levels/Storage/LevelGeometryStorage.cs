using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Scene;

namespace ZoneReborn.World.Levels.Storage;

/// <summary>Editor save entry point for terrain and the models attached to it.</summary>
public static class LevelGeometryStorage
{
    public static async Task SaveAsync(LoadedLevel level)
    {
        if (level.Objects is { } objects &&
            (objects.HasTransformPreview || objects.IsDirty && !objects.CanSave))
            throw new InvalidOperationException("Завершите перемещение объекта и проверьте доступность сохранения objects.json.");

        // Persist anchors before changing terrain files. If terrain saving is interrupted,
        // AttachTerrain restores these models against the actual heights present on disk.
        if (level.Objects is { IsDirty: true } scene)
            await SceneEditStorage.SaveAsync(level.Directory, scene);
        if (level.TerrainDocument is { } terrain)
            await terrain.SaveAsync();
    }
}
