using System.Collections.Concurrent;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene;

// Physics owns its own geometry and never depends on visual streaming or material loading.
public sealed class ScenePhysicsCache(AssetPaths assets, Action<string> warning)
{
    private readonly ConcurrentDictionary<string, Lazy<ScenePhysicsMesh?>> _meshes = new(StringComparer.OrdinalIgnoreCase);

    public ScenePhysicsMesh? Get(string model) => _meshes.GetOrAdd(model, file => new Lazy<ScenePhysicsMesh?>(
        () => Load(file), LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    private ScenePhysicsMesh? Load(string model)
    {
        try { return SourcePhysicsReader.Read(assets, model, warning: warning); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException
            or FormatException or OverflowException or ArgumentException or System.Xml.XmlException)
        {
            warning($"Physics {model}: {exception.Message}");
            return null;
        }
    }
}
