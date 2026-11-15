namespace ZoneReborn.World.Vegetation;

public static class VegetationCategory
{
    public static string FromModel(string file)
    {
        var parts = file.Replace('\\', '/').Split('/');
        if (parts.Length < 3 || !parts[0].Equals("speedtree", StringComparison.OrdinalIgnoreCase))
            return "Без категории";
        return parts[1].ToLowerInvariant() switch
        {
            "tree" => "Tree",
            "bush" => "Bush",
            "grass" => "Grass",
            _ => parts[1]
        };
    }

    public static bool HasBarkCollision(string file) => FromModel(file) is "Tree" or "Bush";

    public static VegetationScatterKind ScatterKind(string file) => FromModel(file) switch
    {
        "Tree" => VegetationScatterKind.Tree,
        "Bush" => VegetationScatterKind.Bush,
        "Reed" => VegetationScatterKind.Reed,
        _ => VegetationScatterKind.Grass
    };
}
