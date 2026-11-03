using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene;

public static class SourceSceneReader
{
    public static SceneData Read(string sourceLevel, AssetPaths assets, CancellationToken cancellationToken = default)
    {
        var instances = new List<SceneInstance>();
        var warnings = new List<string>();
        var warned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bounds = new Dictionary<string, SceneBounds?>(StringComparer.OrdinalIgnoreCase);
        var prefabs = new Dictionary<string, XElement[]>(StringComparer.OrdinalIgnoreCase);
        var activePrefabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        SceneData? loadedScene = null;
        var physics = new ScenePhysicsCache(assets, message =>
        {
            if (loadedScene == null) Warn(message);
            else loadedScene.Warnings.Enqueue(message);
        });
        string sourceDirectory = Path.GetExtension(sourceLevel).Equals(".xml", StringComparison.OrdinalIgnoreCase)
            ? Path.GetDirectoryName(Path.GetFullPath(sourceLevel))! : Path.GetFullPath(sourceLevel);
        string levelPath = Path.GetExtension(sourceLevel).Equals(".xml", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFullPath(sourceLevel) : Path.Combine(sourceDirectory, "LevelData.xml");
        try
        {
            var entries = Objects(levelPath);
            for (int i = 0; i < entries.Length; i++)
                Expand(entries[i], Matrix4x4.Identity, true, false, 0,
                    i.ToString(CultureInfo.InvariantCulture), Path.GetFileName(sourceDirectory), null);
        }
        catch (Exception error) when (Expected(error)) { Warn($"{levelPath}: {error.Message}"); }
        cancellationToken.ThrowIfCancellationRequested();
        var scene = new SceneData(assets, instances, warnings, physics);
        scene.SourceName = Path.GetFileName(sourceDirectory);
        loadedScene = scene;
        scene.Models.SetSourceDirectory(sourceDirectory);
        return scene;

        void Warn(string text) { if (warned.Add(text)) warnings.Add(text); }

        void Expand(XElement entry, Matrix4x4 parent, bool parentCollision, bool parentPlayerOnly, int depth,
            string id, string parentStamp, string? prefabName)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string file = (string?)entry.Attribute("fileName") ?? "";
            bool prefab = (string?)entry.Attribute("className") == "obj_Prefab";
            if (!prefab && !Path.GetExtension(file).Equals(".sco", StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                string stamp = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                    parentStamp + "\n" + entry.ToString(SaveOptions.DisableFormatting))));
                if (depth > 32) throw new InvalidDataException("Prefab nesting exceeds 32 levels.");
                if (instances.Count >= 1_000_000) throw new InvalidDataException("Scene instance limit exceeded.");
                var gameObject = entry.Element("gameObject");
                Vector3 position = Vector(entry.Element("position"), Vector3.Zero);
                Vector3 rotation = Vector(gameObject?.Element("rotation") ?? entry.Element("rotation"), Vector3.Zero) * (MathF.PI / 180);
                XElement? scaleElement = gameObject?.Element("scale") ?? entry.Element("scale");
                Vector3 scale = scaleElement?.Attribute("val") != null
                    ? new(Number(scaleElement, "val", 1)) : Vector(scaleElement, Vector3.One);
                Matrix4x4 world = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z)
                    * Matrix4x4.CreateTranslation(position) * parent;
                if (!Matrix4x4.Invert(world, out var inverse) || !Finite(world) || !Finite(inverse))
                    throw new InvalidDataException("Scene transform is singular or non-finite.");
                string? physicsFlag = (string?)gameObject?.Attribute("PhysEnable") ?? (string?)entry.Attribute("PhysEnable");
                bool collision = parentCollision && !string.Equals(physicsFlag, "false", StringComparison.OrdinalIgnoreCase) && physicsFlag != "0";
                string? playerFlag = (string?)gameObject?.Attribute("CollisionPlayerOnly");
                bool playerOnly = parentPlayerOnly || string.Equals(playerFlag, "true", StringComparison.OrdinalIgnoreCase) || playerFlag == "1";
                if (prefab)
                {
                    string name = (string?)entry.Element("prefab")?.Attribute("name") ?? file;
                    string relative = name.Replace('\\', '/');
                    if (relative.StartsWith("packs/", StringComparison.OrdinalIgnoreCase)) relative = relative[6..];
                    if (relative.StartsWith("Prefabs/", StringComparison.OrdinalIgnoreCase)) relative = relative[8..];
                    if (!relative.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) relative += ".xml";
                    string path = AssetPathResolver.Resolve(Path.Combine(assets.Root, "Prefabs"), relative);
                    if (!activePrefabs.Add(path)) throw new InvalidDataException($"Prefab cycle: {name}.");
                    try
                    {
                        if (!prefabs.TryGetValue(path, out var children)) prefabs.Add(path, children = Objects(path));
                        for (int i = 0; i < children.Length; i++)
                            Expand(children[i], world, collision, playerOnly, depth + 1,
                                id + "/" + i.ToString(CultureInfo.InvariantCulture), stamp,
                                prefabName == null ? name : prefabName + " / " + name);
                    }
                    finally { activePrefabs.Remove(path); }
                    return;
                }
                string model = SourceModelReader.Normalize(file);
                if (!bounds.TryGetValue(model, out var local))
                {
                    try { local = SourceModelReader.ReadBounds(assets, model, cancellationToken); }
                    catch (Exception error) when (Expected(error)) { Warn($"Model {model}: {error.Message}"); local = null; }
                    bounds[model] = local;
                }
                if (local == null) return;
                SceneBounds localBounds = local.Value;
                if (collision)
                {
                    if (physics.Get(model) is { } physical)
                        localBounds = new(Vector3.Min(localBounds.Minimum, physical.Bounds.Minimum),
                            Vector3.Max(localBounds.Maximum, physical.Bounds.Maximum));
                    else collision = false;
                }
                SceneBounds transformed = localBounds.Transform(world);
                if (!Finite(transformed.Minimum) || !Finite(transformed.Maximum))
                    throw new InvalidDataException("Scene bounds are non-finite.");
                instances.Add(new(model, world, inverse, transformed, collision)
                {
                    PlayerCollisionOnly = playerOnly, Id = id, SourceStamp = stamp,
                    PrefabName = prefabName, LocalBounds = localBounds, VisualLocalBounds = local.Value
                });
            }
            catch (Exception error) when (Expected(error)) { Warn($"Object {file}: {error.Message}"); }
        }

        XElement[] Objects(string path)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var reader = XmlReader.Create(path, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 256L * 1024 * 1024
            });
            return XDocument.Load(reader).Root?.Elements("object").ToArray() ?? [];
        }
    }

    private static bool Expected(Exception error) => error is IOException or UnauthorizedAccessException
        or InvalidDataException or XmlException or FormatException or OverflowException or ArgumentException;

    private static Vector3 Vector(XElement? element, Vector3 fallback) => element == null ? fallback
        : new(Number(element, "x", fallback.X), Number(element, "y", fallback.Y), Number(element, "z", fallback.Z));

    private static float Number(XElement? element, string name, float fallback)
    {
        string? text = (string?)element?.Attribute(name);
        if (text == null) return fallback;
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) && float.IsFinite(number)
            ? number : throw new InvalidDataException($"Invalid scene coordinate {name}.");
    }

    private static bool Finite(Vector3 vector) => float.IsFinite(vector.X) && float.IsFinite(vector.Y) && float.IsFinite(vector.Z);
    private static bool Finite(Matrix4x4 value) => Finite(new Vector3(value.M11, value.M12, value.M13)) && float.IsFinite(value.M14)
        && Finite(new Vector3(value.M21, value.M22, value.M23)) && float.IsFinite(value.M24)
        && Finite(new Vector3(value.M31, value.M32, value.M33)) && float.IsFinite(value.M34)
        && Finite(new Vector3(value.M41, value.M42, value.M43)) && float.IsFinite(value.M44);
}
