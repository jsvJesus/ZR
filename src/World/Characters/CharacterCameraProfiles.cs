using System.Numerics;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters.Locomotion;

namespace ZoneReborn.World.Characters;

public sealed record CharacterCameraProfile(
    Vector3 Pivot,
    Vector3 PivotMinPitch,
    Vector3 PivotMaxPitch,
    float Distance,
    float DistanceMinPitch,
    float DistanceMaxPitch)
{
    public (Vector3 Pivot, float Distance) Evaluate(float pitch)
    {
        // player_speed_settings.DCURSOR_LIMITS uses positive-down pitch.
        float factor = Math.Clamp(MathF.Abs(pitch) / (pitch < 0 ? LocomotionSettings.CameraDownPitch : LocomotionSettings.CameraUpPitch), 0, 1);
        return (Vector3.Lerp(Pivot, pitch < 0 ? PivotMaxPitch : PivotMinPitch, factor),
            Distance + ((pitch < 0 ? DistanceMaxPitch : DistanceMinPitch) - Distance) * factor);
    }
}

public sealed class CharacterCameraProfiles
{
    private readonly Dictionary<(string Status, string State), CharacterCameraProfile> _profiles = [];

    public CharacterCameraProfile Resolve(string status, string state)
    {
        // AvatarTrackers._getTrackerKey maps combat sprint to run.
        if (state == "sprint" && status != "bored") state = "run";
        return _profiles[(status, state)];
    }

    internal static CharacterCameraProfiles Read(AssetPaths assets, CancellationToken cancellationToken)
    {
        string path = Path.GetFullPath(Path.Combine(assets.Root, "..", "..", "sdk", "scripts", "common", "data",
            "items_pyson", "tracker_global.pyson"));
        var root = Map(PysonLiteralReader.Read(path, cancellationToken));
        var tracker = Map(root["tracker_global"]);
        var result = new CharacterCameraProfiles();
        foreach (string status in new[] { "bored", "incombat", "aim" })
        {
            foreach (var (key, entry) in Map(tracker[status]))
            {
                if (key is not string state) throw new InvalidDataException($"{path}: invalid camera state.");
                var values = Map(entry);
                result._profiles.Add((status, state), new(
                    Vector(values, "camera_pivotPosition_0pitch"),
                    Vector(values, "camera_pivotPosition_minpitch"),
                    Vector(values, "camera_pivotPosition_maxpitch"),
                    Distance(values, "cam_dist_0pitch"),
                    Distance(values, "cam_dist_minpitch"),
                    Distance(values, "cam_dist_maxpitch")));
            }
        }
        return result;

        Dictionary<object, object?> Map(object? value) => value as Dictionary<object, object?>
            ?? throw new InvalidDataException($"{path}: invalid camera profile dictionary.");

        Vector3 Vector(Dictionary<object, object?> values, string key)
        {
            if (values[key] is not List<object?> components || components.Count != 3)
                throw new InvalidDataException($"{path}: invalid camera vector {key}.");
            return new(Scalar(components[0]), Scalar(components[1]), Scalar(components[2]));
        }

        float Distance(Dictionary<object, object?> values, string key)
        {
            if (values[key] is not List<object?> distances || distances.Count != 3)
                throw new InvalidDataException($"{path}: invalid camera distances {key}.");
            // PlayerAvatar.updateCameraPosition chooses index 0 for normal play.
            float distance = Scalar(distances[0]);
            return distance > 0 ? distance : throw new InvalidDataException($"{path}: invalid camera distance {key}.");
        }

        float Scalar(object? value)
        {
            float number = value switch { long integer => integer, double real => (float)real, _ => float.NaN };
            return float.IsFinite(number) ? number : throw new InvalidDataException($"{path}: invalid camera number.");
        }
    }
}
