namespace ZoneReborn.World.Textures.Dds
{
    public sealed record DdsImage(
        int Width,
        int Height,
        DdsPixelFormat Format,
        IReadOnlyList<DdsMipLevel> MipLevels);
}
