namespace ZoneReborn.World.Terrain.Import.Configuration;

internal sealed class TerrainScript
{
    public Dictionary<string, string> Settings { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Dictionary<string, string>> Layers { get; } = [];

    public static TerrainScript Read(string path)
    {
        var result = new TerrainScript();
        Dictionary<string, string>? layer = null;
        bool expectBrace = false, hasBase = false;
        int lineNumber = 0;
        foreach (string sourceLine in File.ReadLines(path))
        {
            lineNumber++;
            string line = sourceLine.Trim();
            if (line.Length == 0 || line.StartsWith("//") || line.StartsWith('#')) continue;
            if (line is "base_layer" or "layer")
            {
                if (layer != null || expectBrace || (line == "base_layer" && hasBase) ||
                    (line == "layer" && !hasBase))
                    throw new InvalidDataException($"Нарушен порядок слоёв Terrain, строка {lineNumber}.");
                if (line == "base_layer") hasBase = true;
                layer = new(StringComparer.OrdinalIgnoreCase);
                result.Layers.Add(layer);
                expectBrace = true;
                continue;
            }
            if (line == "{")
            {
                if (!expectBrace) throw new InvalidDataException("Неожиданное начало блока Terrain.");
                expectBrace = false;
                continue;
            }
            if (line == "}")
            {
                if (layer == null || expectBrace) throw new InvalidDataException("Неожиданное завершение блока Terrain.");
                layer = null;
                continue;
            }
            int colon = line.IndexOf(':');
            if (colon <= 0 || expectBrace) throw new InvalidDataException($"Некорректная строка Terrain: {lineNumber}.");
            string key = line[..colon].Trim(), value = line[(colon + 1)..].Trim();
            if (value.StartsWith('"'))
            {
                if (value.Length < 2 || !value.EndsWith('"'))
                    throw new InvalidDataException($"Незакрытая строка Terrain: {lineNumber}.");
                value = value[1..^1];
            }
            if (!(layer ?? result.Settings).TryAdd(key, value))
                throw new InvalidDataException($"Повторяющееся поле Terrain: {key}.");
        }
        if (!hasBase || layer != null || expectBrace) throw new InvalidDataException("Неполное описание слоёв Terrain.");
        return result;
    }
}

