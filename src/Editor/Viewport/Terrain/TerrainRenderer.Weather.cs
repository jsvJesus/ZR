namespace Editor.Viewport.Terrain;

internal sealed partial class TerrainRenderer
{
    public void UpdateWeatherWetness(
        float rainIntensity,
        float deltaSeconds)
    {
        _level.TerrainDocument?.AdvanceWeatherWetness(
            rainIntensity,
            deltaSeconds);
    }
}
