namespace ZoneReborn.World.Levels.Import;

public sealed record LevelImportReport(
    int ImporterVersion,
    string SourceLevel,
    DateTimeOffset ImportedAtUtc,
    int TileCount,
    int LayerCount,
    IReadOnlyList<string> Notes);

