namespace Editor.Viewport.Roads;

internal sealed partial class RoadRenderer
{
    private float _roadWetness;
    private bool _uniformRoadWetness;

    public void AdvanceWeatherWetness(
        float rainIntensity,
        float deltaSeconds,
        bool uniformWetness)
    {
        _uniformRoadWetness = uniformWetness;

        if (!float.IsFinite(rainIntensity) ||
            !float.IsFinite(deltaSeconds) ||
            deltaSeconds <= 0f)
        {
            return;
        }

        float rain = Math.Clamp(
            rainIntensity,
            0f,
            1f);

        float delta = Math.Clamp(
            deltaSeconds,
            0f,
            0.25f);

        const float soakSeconds = 25f;
        const float drySeconds = 150f;

        if (rain > 0.001f)
        {
            _roadWetness +=
                delta * rain / soakSeconds;
        }
        else
        {
            _roadWetness -=
                delta / drySeconds;
        }

        _roadWetness = Math.Clamp(
            _roadWetness,
            0f,
            1f);
    }
}
