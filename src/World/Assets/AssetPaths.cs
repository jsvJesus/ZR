namespace ZoneReborn.World.Assets;

public sealed class AssetPaths
{
    public string Root { get; }
    public string ImportLevels => Path.Combine(Root, "import", "levels");
    public string Levels => Path.Combine(Root, "levels");
    public string Materials => Path.Combine(Root, "materials");
    public string Shaders => Path.Combine(Root, "shaders");

    public AssetPaths(string root) => Root = Path.GetFullPath(root);

    public static AssetPaths FromApplication() => new(Path.Combine(AppContext.BaseDirectory, "packs"));
}

