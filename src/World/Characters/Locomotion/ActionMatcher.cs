namespace ZoneReborn.World.Characters.Locomotion;

public sealed class ActionMatcher
{
    private readonly LocomotionLibrary _library;
    public ActionMatcher(LocomotionLibrary library) => _library = library;

    public CharacterAction Select(float speed, float directionDegrees, IReadOnlySet<int> caps, CharacterAction current)
    {
        var candidate = _library.MovementActions.FirstOrDefault(a => a.Trigger!.Matches(speed, directionDegrees, 0, caps));
        if (candidate != null) return candidate;
        // A non-degenerate cancel region can retain an action across a trigger gap.
        if (!current.Stop && current.Cancel is { } cancel && cancel.MaxSpeed > cancel.MinSpeed
            && cancel.MaxYaw > cancel.MinYaw && cancel.Matches(speed, directionDegrees, 0, caps)
            && current.Trigger?.CapsMatch(caps) == true) return current;
        // Preserve a sensible ground pose if the source's directional intervals have a gap.
        float cardinal = MathF.Abs(directionDegrees) <= 45 ? 0 : MathF.Abs(directionDegrees) >= 135 ? 180 : directionDegrees > 0 ? 90 : -90;
        return _library.MovementActions.FirstOrDefault(a => a.Trigger!.Matches(speed, cardinal, 0, caps))
            ?? _library.Actions[caps.Contains((int)AnimationCap.Crouching) ? "idle_0_crouch_unarmed" : "idle_0_stay_unarmed"];
    }

    public CharacterAction? Stop(float speed, float directionDegrees, IReadOnlySet<int> caps, SupportFoot foot, float previousTime) =>
        _library.StopActions.FirstOrDefault(a => a.Trigger?.Matches(speed, directionDegrees, 0, caps) == true
            && (foot == SupportFoot.Left ? a.StopLeft : a.StopRight) && previousTime >= a.MinimumPreviousTime);
}
