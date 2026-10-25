namespace ZoneReborn.World.Levels.Import;

public sealed record LevelImportProgress(string Level, int CompletedTiles, int TotalTiles)
{
    public int Percentage => TotalTiles == 0 ? 0 : CompletedTiles * 100 / TotalTiles;
}

