using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZoneReborn.World.Vegetation;

public static class GroundCoverStorage
{
    public const string FileName = "groundcover.json";
    private const long MaximumFileBytes = 64L * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static async Task<GroundCoverDocument> LoadAsync(string directory, CancellationToken token = default)
    {
        var document = new GroundCoverDocument();
        string path = Path.Combine(directory, FileName);
        if (!File.Exists(path)) return document;
        try
        {
            if (new FileInfo(path).Length > MaximumFileBytes)
                throw new InvalidDataException("Слишком большой файл groundcover.json.");
            await using var stream = File.OpenRead(path);
            var stored = await JsonSerializer.DeserializeAsync<StoredDocument>(stream, Options, token)
                ?? throw new InvalidDataException("Пустой файл groundcover.json.");
            if (stored.Format != "ZoneReborn.GroundCover" || stored.Version != 1 || stored.Settings == null ||
                stored.Cells == null || stored.Cells.Length > GroundCoverDocument.MaximumOverrides)
                throw new InvalidDataException("Неподдерживаемый документ groundcover.json.");
            GroundCoverSettings.Validate(stored.Settings);
            var cells = new GroundCoverCell[stored.Cells.Length];
            var unique = new HashSet<(int, int)>();
            for (int index = 0; index < cells.Length; index++)
            {
                token.ThrowIfCancellationRequested();
                int[] cell = stored.Cells[index];
                if (cell == null || cell.Length != 3 || cell[0] is < -10_000_000 or > 10_000_000 ||
                    cell[1] is < -10_000_000 or > 10_000_000 || cell[2] is < 0 or > 1 ||
                    !unique.Add((cell[0], cell[1])))
                    throw new InvalidDataException("Некорректная или повторяющаяся ячейка groundcover.json.");
                cells[index] = new(cell[0], cell[1], cell[2] != 0);
            }
            document.Load(stored.Settings, cells);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            InvalidDataException or JsonException or ArgumentException or NotSupportedException or OverflowException)
        {
            document.CanSave = false;
            document.Warnings.Add($"{FileName}: {error.Message} Исходный файл сохранён; изменения отключены.");
        }
        return document;
    }

    public static async Task SaveAsync(string directory, GroundCoverDocument document, CancellationToken token = default)
    {
        var snapshot = document.Capture();
        GroundCoverSettings.Validate(snapshot.Settings);
        var stored = new StoredDocument
        {
            Format = "ZoneReborn.GroundCover", Version = 1, Settings = snapshot.Settings,
            Cells = snapshot.Cells.Select(cell => new[] { cell.X, cell.Z, cell.Enabled ? 1 : 0 }).ToArray()
        };
        string target = Path.Combine(directory, FileName);
        string temporary = Path.Combine(directory, $".groundcover-{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, stored, Options, token);
                await stream.FlushAsync(token);
                stream.Flush(flushToDisk: true);
            }
            token.ThrowIfCancellationRequested();
            if (File.Exists(target)) File.Replace(temporary, target, null);
            else File.Move(temporary, target);
            // Edits after the captured snapshot remain dirty after an asynchronous save.
            document.MarkSaved(snapshot.Revision);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private sealed class StoredDocument
    {
        [JsonRequired] public string Format { get; init; } = "";
        [JsonRequired] public int Version { get; init; }
        [JsonRequired] public GroundCoverSettings Settings { get; init; } = null!;
        [JsonRequired] public int[][] Cells { get; init; } = null!;
    }
}
