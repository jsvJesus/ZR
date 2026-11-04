using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZoneReborn.World.Levels.Storage;

namespace ZoneReborn.World.Scene;

/// <summary>Overrides one expanded model instance; source levels and shared prefabs stay intact.</summary>
public static class SceneEditStorage
{
    public const string FileName = "objects.json";
    public const int MaximumAddedObjects = 100_000;
    private const long MaximumDocumentBytes = 64L * 1024 * 1024;

    public static async Task LoadAsync(string directory, SceneData scene, CancellationToken cancellationToken = default)
    {
        string path = Path.Combine(directory, FileName);
        if (!File.Exists(path)) return;
        try
        {
            if (new FileInfo(path).Length > MaximumDocumentBytes) throw new InvalidDataException("Слишком большой файл правок объектов.");
            var document = await LevelJson.ReadAsync<Document>(path, cancellationToken);
            if (document.Format != "ZoneReborn.SceneObjects" || document.Version is not (1 or 2) ||
                document.Level != scene.SourceName || document.Edits == null || document.Edits.Length > 1_000_000)
                throw new InvalidDataException("Неподдерживаемый документ правок объектов или другой исходный уровень.");
            if ((document.Version == 2 && document.Added == null) ||
                (document.Version == 1 && document.Added is { Length: > 0 }) || document.Added?.Length > MaximumAddedObjects)
                throw new InvalidDataException("Некорректный список добавленных объектов.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var replacements = new List<(string Id, SceneInstance? Instance)>();
            foreach (var edit in document.Edits)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (edit == null || string.IsNullOrWhiteSpace(edit.Id) || edit.Id.Length > 1024 || !ids.Add(edit.Id))
                    throw new InvalidDataException("Пустой или повторяющийся ID объекта.");
                var source = scene.Original(edit.Id);
                if (source == null || source.SourceStamp != edit.SourceStamp || source.Model != edit.Model)
                    throw new InvalidDataException($"Исходный объект {edit.Id} изменился или отсутствует. Правки не применены.");
                if (edit.Deleted)
                {
                    if (edit.World != null) throw new InvalidDataException("У удалённого объекта не должно быть трансформации.");
                    replacements.Add((edit.Id, null));
                }
                else replacements.Add((edit.Id, SceneData.Transform(source, ReadMatrix(edit.World))));
            }
            var addedEntries = document.Added ?? [];
            foreach (var entry in addedEntries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || !entry.Id.StartsWith("added/", StringComparison.Ordinal) ||
                    !Guid.TryParseExact(entry.Id[6..], "N", out var identifier) || identifier == Guid.Empty || !ids.Add(entry.Id))
                    throw new InvalidDataException("Некорректный или повторяющийся ID добавленного объекта.");
                if (string.IsNullOrWhiteSpace(entry.Model)) throw new InvalidDataException("Не задана модель добавленного объекта.");
                SceneObjectTransform.ValidateWorld(ReadMatrix(entry.World));
            }
            if ((long)scene.Instances.Count - replacements.Count(change => change.Instance == null) + addedEntries.Length > 1_000_000)
                throw new InvalidDataException("Превышен лимит объектов уровня.");
            // Model/physics parsing may involve large binary resources; do it off the UI thread.
            var additions = await Task.Run(() =>
            {
                var result = new SceneInstance[addedEntries.Length];
                for (int i = 0; i < result.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var entry = addedEntries[i];
                    result[i] = scene.PrepareAddedInstance(entry.Id, entry.Model, ReadMatrix(entry.World), requireLoaded: false);
                }
                return result;
            }, cancellationToken);
            // Apply only after validating the entire file: no partially edited scene.
            scene.LoadOverrides(replacements, additions);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
            JsonException or ArgumentException or OverflowException or System.Xml.XmlException)
        {
            scene.CanSave = false;
            scene.Warnings.Enqueue($"{FileName}: {error.Message} Исходный файл сохранён; перезапись отключена.");
        }
    }

    public static async Task SaveAsync(string directory, SceneData scene)
    {
        var snapshot = scene.CaptureEdits();
        var document = new Document { Level = scene.SourceName, Edits = snapshot.Edits, Added = snapshot.Added };
        string temporary = Path.Combine(directory, $".objects-{Guid.NewGuid():N}.tmp");
        try
        {
            await LevelJson.WriteAsync(temporary, document);
            if (new FileInfo(temporary).Length > MaximumDocumentBytes)
                throw new InvalidDataException("Файл объектов превышает 64 МиБ; исходный файл не перезаписан.");
            File.Move(temporary, Path.Combine(directory, FileName), true);
            // If another edit arrived while writing, it remains dirty.
            scene.MarkSaved(snapshot.Revision);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    internal static float[] MatrixValues(Matrix4x4 world) =>
        [world.M11, world.M12, world.M13, world.M14, world.M21, world.M22, world.M23, world.M24,
         world.M31, world.M32, world.M33, world.M34, world.M41, world.M42, world.M43, world.M44];

    private static Matrix4x4 ReadMatrix(float[]? values)
    {
        if (values == null || values.Length != 16) throw new InvalidDataException("Трансформация должна содержать 16 чисел.");
        return new(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7],
            values[8], values[9], values[10], values[11], values[12], values[13], values[14], values[15]);
    }

    public sealed class Document
    {
        [JsonRequired]
        public string Format { get; init; } = "ZoneReborn.SceneObjects";
        [JsonRequired]
        public int Version { get; init; } = 2;
        [JsonRequired]
        public string Level { get; init; } = "";
        [JsonRequired]
        public SceneObjectOverride[] Edits { get; init; } = [];
        public SceneAddedObject[]? Added { get; init; }
    }
}

public sealed class SceneAddedObject
{
    [JsonRequired]
    public string Id { get; init; } = "";
    [JsonRequired]
    public string Model { get; init; } = "";
    [JsonRequired]
    public float[]? World { get; init; }
}

public sealed class SceneObjectOverride
{
    [JsonRequired]
    public string Id { get; init; } = "";
    [JsonRequired]
    public string SourceStamp { get; init; } = "";
    [JsonRequired]
    public string Model { get; init; } = "";
    [JsonRequired]
    public bool Deleted { get; init; }
    public float[]? World { get; init; }
}
