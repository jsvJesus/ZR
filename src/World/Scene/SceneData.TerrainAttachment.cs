using System.Numerics;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Scene;

public sealed partial class SceneData
{
    private TerrainDocument? _attachedTerrain;
    private TerrainSurface? _terrainSurface;
    private readonly List<SceneInstance> _pendingTerrainObjects = [];

    public void AttachTerrain(LoadedLevel level)
    {
        if (_attachedTerrain != null)
        {
            _attachedTerrain.HeightsChanging -= PrepareTerrainChange;
            _attachedTerrain.HeightsChanged -= CommitTerrainChange;
        }
        _pendingTerrainObjects.Clear();
        _attachedTerrain = level.TerrainDocument;
        _terrainSurface = new TerrainSurface(level);
        RestoreTerrainAttachments();
        if (_attachedTerrain != null)
        {
            _attachedTerrain.HeightsChanging += PrepareTerrainChange;
            _attachedTerrain.HeightsChanged += CommitTerrainChange;
        }
    }

    private void RestoreTerrainAttachments()
    {
        if (!CanSave || _attachedTerrain == null) return;
        var restored = new List<SceneInstance>();
        try
        {
            foreach (var instance in _instances)
            {
                // Original models without overrides stay lazy: do not read the whole heightfield.
                if (instance.TerrainAnchor == null) continue;
                var projected = ProjectToTerrain(instance);
                if (projected.World != instance.World) restored.Add(projected);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            CanSave = false;
            Warnings.Enqueue($"Не удалось восстановить привязку моделей к Terrain: {error.Message} Перезапись объектов отключена.");
            return;
        }

        if (restored.Count == 0) return;
        foreach (var instance in restored) _current[instance.Id] = instance;
        RebuildIndex();
        _revision++;
    }

    public void SetFollowTerrain(string id, bool follow)
    {
        RequireEditable();
        var before = Find(id) ?? throw new InvalidOperationException("Объект не найден или уже удалён.");
        if (before.FollowTerrain == follow) return;
        Record(new(before, before with { FollowTerrain = follow, TerrainAnchor = null }));
    }

    public static bool CanFollowTerrain(SceneInstance instance) =>
        !instance.IsEnvironmentCollision && !instance.PlayerCollisionOnly;

    private SceneInstance EnsureTerrainAnchor(SceneInstance instance) =>
        instance.TerrainAnchor != null ? instance : ReanchorTerrain(instance);

    private SceneInstance ReanchorTerrain(SceneInstance instance)
    {
        if (!instance.FollowTerrain || !CanFollowTerrain(instance) || _terrainSurface == null)
            return instance with { TerrainAnchor = null };

        var center = instance.Bounds.Center;
        return _terrainSurface.TryGetHeight(center.X, center.Z, out float height)
            ? instance with { TerrainAnchor = new(height, instance.World.M42) }
            : instance with { TerrainAnchor = null };
    }

    private SceneInstance ProjectToTerrain(SceneInstance instance, Func<int, int, float>? sample = null)
    {
        if (!instance.FollowTerrain || !CanFollowTerrain(instance) || _terrainSurface == null)
            return instance;
        instance = EnsureTerrainAnchor(instance);
        if (instance.TerrainAnchor is not { } anchor) return instance;
        var center = instance.Bounds.Center;
        float height;
        bool found = sample == null
            ? _terrainSurface.TryGetHeight(center.X, center.Z, out height)
            : _terrainSurface.TryGetHeight(center.X, center.Z, sample, out height);
        if (!found) return instance;

        var world = instance.World;
        world.M42 = (float)((double)anchor.WorldY + height - anchor.Height);
        return world == instance.World ? instance : Transform(instance, world);
    }

    private void PrepareTerrainChange(TerrainHeightChange change)
    {
        _pendingTerrainObjects.Clear();
        var area = new SceneBounds(
            new Vector3(change.MinimumX, -2e7f, change.MinimumZ),
            new Vector3(change.MaximumX, 2e7f, change.MaximumZ));

        // Query the existing spatial index; do not scan/rebuild the entire scene per brush stamp.
        foreach (var instance in Query(area))
        {
            if (!instance.FollowTerrain || !CanFollowTerrain(instance)) continue;
            var center = instance.Bounds.Center;
            if (center.X < change.MinimumX || center.X > change.MaximumX ||
                center.Z < change.MinimumZ || center.Z > change.MaximumZ) continue;

            var projected = ProjectToTerrain(instance, change.SampleHeight);
            if (projected.World != instance.World) _pendingTerrainObjects.Add(projected);
        }

        if (_pendingTerrainObjects.Count > 0)
        {
            // Validate before terrain samples change, including read-only scenes and active gizmos.
            RequireEditable();
        }
    }

    private void CommitTerrainChange()
    {
        if (_pendingTerrainObjects.Count == 0) return;
        foreach (var projected in _pendingTerrainObjects)
        {
            var previous = _current[projected.Id];
            Unindex(previous);
            _current[projected.Id] = projected;
            _instances[_instanceSlots[projected.Id]] = projected;
            Index(projected);
        }
        _pendingTerrainObjects.Clear();
        _collisionDirty = true;
        _revision++;
        // Terrain history owns this movement. Object history is preserved and projected on restore.
        Changed?.Invoke();
    }
}
