using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain;

public readonly record struct TerrainContactProperties(
    int MaterialIndex,
    TerrainSurfaceType SurfaceType,
    float Wetness,
    float Friction,
    float Hardness,
    string FootstepSound);

public sealed partial class TerrainDocument
{
    private readonly float[] _weatherWetness = new float[256];

    public float MaterialWetness(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= _weatherWetness.Length)
        {
            return 0f;
        }

        return _weatherWetness[layerIndex];
    }

    public void AdvanceWeatherWetness(
        float rainIntensity,
        float deltaSeconds)
    {
        if (!float.IsFinite(rainIntensity) ||
            !float.IsFinite(deltaSeconds) ||
            deltaSeconds <= 0f)
        {
            return;
        }

        float rain = Math.Clamp(rainIntensity, 0f, 1f);
        float delta = Math.Clamp(deltaSeconds, 0f, 0.25f);

        foreach (var layer in layers)
        {
            if (layer.Index < 0 || layer.Index >= _weatherWetness.Length)
            {
                continue;
            }

            var settings = layer.Settings.Weather;

            if (!settings.ReactToRain)
            {
                _weatherWetness[layer.Index] = 0f;
                continue;
            }

            float current = _weatherWetness[layer.Index];

            if (rain > 0.001f)
            {
                float soakSeconds = Math.Clamp(
                    settings.SoakSeconds,
                    1f,
                    3600f);

                current += delta * rain / soakSeconds;
            }
            else
            {
                float drySeconds = Math.Clamp(
                    settings.DrySeconds,
                    1f,
                    7200f);

                current -= delta / drySeconds;
            }

            _weatherWetness[layer.Index] = Math.Clamp(
                current,
                0f,
                1f);
        }
    }

    public bool TryGetContactProperties(
        float worldX,
        float worldZ,
        out TerrainContactProperties result)
    {
        result = default;

        if (!float.IsFinite(worldX) ||
            !float.IsFinite(worldZ) ||
            !HasSurface(worldX, worldZ))
        {
            return false;
        }

        var descriptor = Descriptor;

        int tileX = Math.Clamp(
            (int)(worldX / (256f * descriptor.CellSize)),
            0,
            descriptor.TilesX - 1);

        int tileZ = Math.Clamp(
            (int)(worldZ / (256f * descriptor.CellSize)),
            0,
            descriptor.TilesZ - 1);

        var tile = Raw(tileX, tileZ);

        float pixelSize =
            256f / 248f * descriptor.CellSize;

        int pixelX = Math.Clamp(
            (int)MathF.Round(
                (worldX - tileX * 256f * descriptor.CellSize)
                / pixelSize + 3.5f),
            0,
            255);

        int pixelZ = Math.Clamp(
            (int)MathF.Round(
                (worldZ - tileZ * 256f * descriptor.CellSize)
                / pixelSize + 3.5f),
            0,
            255);

        var weights = new List<(int Index, float Weight)>();
        float sum = 0f;

        foreach (byte index in tile.ActiveLayers)
        {
            if (index == 0 || index >= layers.Count)
            {
                continue;
            }

            int group = (index - 1) / 3;
            int channel = (index - 1) % 3;

            var mask = tile.Masks.FirstOrDefault(
                item => item.Group == group);

            if (mask == null)
            {
                continue;
            }

            var sample = TerrainMaskCodec.Sample(
                mask.Data,
                pixelX,
                pixelZ);

            float weight = channel switch
            {
                0 => sample.X,
                1 => sample.Y,
                _ => sample.Z
            };

            weights.Add((index, weight));
            sum += weight;
        }

        int dominantIndex = 0;
        float dominantWeight = Math.Max(1f - sum, 0f);

        foreach (var entry in weights)
        {
            float normalizedWeight =
                entry.Weight / Math.Max(1f, sum);

            if (normalizedWeight > dominantWeight)
            {
                dominantWeight = normalizedWeight;
                dominantIndex = entry.Index;
            }
        }

        var layer = layers[dominantIndex];
        var physics = layer.Settings.Physics;
        var weather = layer.Settings.Weather;

        float wetness = MaterialWetness(dominantIndex);

        float friction = physics.Friction +
            (weather.WetFriction - physics.Friction) * wetness;

        string sound = wetness >= 0.45f &&
            !string.IsNullOrWhiteSpace(weather.WetFootstepSound)
                ? weather.WetFootstepSound
                : physics.FootstepSound;

        result = new TerrainContactProperties(
            dominantIndex,
            layer.Settings.SurfaceType,
            wetness,
            friction,
            physics.Hardness,
            sound);

        return true;
    }
}
