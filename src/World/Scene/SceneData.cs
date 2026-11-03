using System.Collections.Concurrent;
using System.Numerics;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene;

public sealed class SceneData
{
    private const int CellSize = 64;
    private readonly Dictionary<(int X, int Z), List<SceneInstance>> _cells = [];
    private readonly List<SceneInstance> _large = [];
    public IReadOnlyList<SceneInstance> Instances { get; }
    public ConcurrentQueue<string> Warnings { get; } = new();
    public SceneModelCache Models { get; }
    public ScenePhysicsCache Physics { get; }
    public SceneCollision Collision { get; }

    public SceneData(AssetPaths assets, IReadOnlyList<SceneInstance> instances, IEnumerable<string>? warnings = null,
        ScenePhysicsCache? physics = null)
    {
        Instances = instances;
        if (warnings != null) foreach (string warning in warnings) Warnings.Enqueue(warning);
        Models = new SceneModelCache(assets, warning => Warnings.Enqueue(warning));
        Physics = physics ?? new ScenePhysicsCache(assets, warning => Warnings.Enqueue(warning));
        foreach (var instance in instances)
        {
            int x0 = Cell(instance.Bounds.Minimum.X), x1 = Cell(instance.Bounds.Maximum.X);
            int z0 = Cell(instance.Bounds.Minimum.Z), z1 = Cell(instance.Bounds.Maximum.Z);
            if ((long)(x1 - x0 + 1) * (z1 - z0 + 1) > 4096) { _large.Add(instance); continue; }
            for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
            {
                if (!_cells.TryGetValue((x, z), out var cell)) _cells.Add((x, z), cell = []);
                cell.Add(instance);
            }
        }
        Collision = new SceneCollision(this);
    }

    public IEnumerable<SceneInstance> Query(SceneBounds bounds)
    {
        var seen = new HashSet<SceneInstance>(ReferenceEqualityComparer.Instance);
        for (int z = Cell(bounds.Minimum.Z); z <= Cell(bounds.Maximum.Z); z++)
        for (int x = Cell(bounds.Minimum.X); x <= Cell(bounds.Maximum.X); x++)
            if (_cells.TryGetValue((x, z), out var cell))
                foreach (var instance in cell)
                    if (seen.Add(instance) && instance.Bounds.Intersects(bounds)) yield return instance;
        foreach (var instance in _large) if (instance.Bounds.Intersects(bounds)) yield return instance;
    }

    public void Preload(Vector3 center, float radius, CancellationToken cancellationToken = default)
    {
        var extent = new Vector3(radius);
        foreach (string model in Query(new(center - extent, center + extent)).Select(instance => instance.Model)
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            Models.Get(model);
            Physics.Get(model);
        }
    }

    private static int Cell(float value) => (int)MathF.Floor(value / CellSize);
}
