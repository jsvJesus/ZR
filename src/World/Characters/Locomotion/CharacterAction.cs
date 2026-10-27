using System.Numerics;
using ZoneReborn.World.Characters.Formats;

namespace ZoneReborn.World.Characters.Locomotion;

public sealed record AnimationCue(float Frame, string Name);

public sealed record CharacterClip(string Name, SourceAnimation Animation, float FrameRate,
    float FirstFrame, float LastFrame, AnimationCue[] Cues, IReadOnlyDictionary<string, int> NodePriorities)
{
    public float Direction => LastFrame >= FirstFrame ? 1 : -1;
    public float FrameCount => MathF.Abs(LastFrame - FirstFrame);
    public float Duration => FrameCount / FrameRate;
    public Vector3 RootPosition(float frame) => Animation.Channels.TryGetValue("Root", out var channel)
        ? channel.SamplePosition(frame) : Vector3.Zero;
    public float NaturalSpeed
    {
        get
        {
            var delta = RootPosition(LastFrame) - RootPosition(FirstFrame);
            return new Vector2(delta.X, delta.Z).Length() / Duration;
        }
    }
    public float RootYaw(float frame)
    {
        if (!Animation.Channels.TryGetValue("Root", out var channel)) return 0;
        // The source rig's local Y axis points forward after the export basis transform.
        var forward = Vector3.TransformNormal(Vector3.UnitY, channel.Sample(frame));
        return MathF.Atan2(forward.X, forward.Z);
    }
}

public sealed record ActionMatch(float MinSpeed, float MaxSpeed, float MinYaw, float MaxYaw,
    float MinAux, float MaxAux, int[] CapsOn, int[] CapsOff)
{
    public bool Matches(float speed, float yawDegrees, float auxiliaryDegrees, IReadOnlySet<int> caps) =>
        speed >= MinSpeed && speed <= MaxSpeed && AngleInRange(yawDegrees, MinYaw, MaxYaw)
        && AngleInRange(auxiliaryDegrees, MinAux, MaxAux) && CapsMatch(caps);
    public bool CapsMatch(IReadOnlySet<int> caps) => CapsOn.All(caps.Contains) && !CapsOff.Any(caps.Contains);
    public static bool AngleInRange(float angle, float minimum, float maximum)
    {
        if (maximum - minimum >= 359.99f) return true;
        for (int offset = -2; offset <= 2; offset++)
            if (angle + offset * 360 >= minimum && angle + offset * 360 <= maximum) return true;
        return false;
    }
}

public sealed record CharacterAction(string Name, CharacterClip Clip, ActionMatch? Trigger, ActionMatch? Cancel,
    float BlendIn, float BlendOut, bool Movement, bool Impacting, bool IgnoreGravity, bool ScalePlaybackSpeed,
    bool StopLeft, bool StopRight, float MinimumPreviousTime, int Track, bool FeetFollowDirection)
{
    public bool Stop => StopLeft || StopRight;
    public bool Turn => Name.StartsWith("turn_", StringComparison.Ordinal);
    public bool ExtractRoot => Movement || Impacting || Turn;
}
