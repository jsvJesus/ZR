using System.Numerics;

namespace ZoneReborn.World.Characters.Locomotion;

public sealed class CharacterAnimator
{
    private readonly IReadOnlyDictionary<string, CharacterAction> _actions;

    private CharacterClip? _speedReference;
    private float _time;
    private float _blendTime;
    private float _blendDuration = 0.001f;
    private float _baseTime;
    private bool _loop;

    private float BlendWeight =>
        Math.Clamp(_blendTime / _blendDuration, 0, 1);

    public CharacterPose Pose { get; }
    public CharacterAction Current { get; private set; }

    public float Frame => Current.Clip.IsStatic
        ? Current.Clip.FirstFrame
        : Current.Clip.FirstFrame +
          (_loop
              ? _time % Current.Clip.Duration
              : Math.Min(_time, Current.Clip.Duration)) *
          Current.Clip.FrameRate *
          Current.Clip.Direction;

    public float RelativeFrame =>
        (Frame - Current.Clip.FirstFrame) *
        Current.Clip.Direction;

    public float Phase => Current.Clip.IsStatic
        ? 0
        : RelativeFrame / Current.Clip.FrameCount;

    public float Elapsed { get; private set; }
    public bool Complete => !_loop && _time >= Current.Clip.Duration;

    public Vector3 RootDelta { get; private set; }
    public float YawDelta { get; private set; }

    public SupportFoot Foot { get; private set; }
    public float FeetYaw { get; set; }
    public float TorsoYaw { get; set; }

    public event Action<string>? Cue;

    public CharacterAnimator(CharacterAsset asset)
    {
        var library = asset.Locomotion ??
            throw new InvalidOperationException(
                "Character has no locomotion actions."
            );

        _actions = library.Actions;

        Pose = new CharacterPose(asset);
        Current = library.Idle;

        _speedReference = FindSpeedReference(Current);

        _loop = true;
        _blendTime = _blendDuration;

        Pose.Apply(Current, Frame, 1);
        UpdateFoot();
    }

    public void Play(
        CharacterAction action,
        bool loop,
        bool preservePhase = false,
        float resumeFrame = -1)
    {
        if (ReferenceEquals(action, Current) &&
            loop == _loop &&
            resumeFrame < 0 &&
            !Complete)
        {
            return;
        }

        float phase = preservePhase
            ? MatchFootPhase(action.Clip)
            : 0;

        Pose.CaptureBlend(
            Current,
            Frame,
            BlendWeight,
            _baseTime
        );

        _blendDuration = Math.Max(
            0.001f,
            Math.Max(Current.BlendOut, action.BlendIn)
        );

        _blendTime = 0;

        Current = action;
        _speedReference = FindSpeedReference(action);

        _loop = loop;

        _time = resumeFrame >= 0
            ? Math.Clamp(
                resumeFrame,
                0,
                action.Clip.FrameCount
            ) / action.Clip.FrameRate
            : phase * action.Clip.Duration;

        Elapsed = 0;
        RootDelta = Vector3.Zero;
        YawDelta = 0;

        UpdateFoot();
    }

    public void Advance(float deltaTime, float speed)
    {
        float before = Frame;

        Elapsed += deltaTime;
        _baseTime += deltaTime;

        if (Current.Clip.IsStatic)
        {
            _time = Math.Min(
                _time + deltaTime,
                Current.Clip.Duration
            );

            RootDelta = Vector3.Zero;
            YawDelta = 0;
            _blendTime += deltaTime;

            return;
        }

        float scale = PlaybackScale(speed);
        float oldTime = _time;

        _time += deltaTime * scale;

        float after = Frame;

        int loops = _loop
            ? (int)MathF.Floor(_time / Current.Clip.Duration) -
              (int)MathF.Floor(oldTime / Current.Clip.Duration)
            : 0;

        var clip = Current.Clip;

        RootDelta =
            clip.RootPosition(after) -
            clip.RootPosition(before) +
            (clip.RootPosition(clip.LastFrame) -
             clip.RootPosition(clip.FirstFrame)) * loops;

        YawDelta = MathF.IEEERemainder(
            clip.RootYaw(after) -
            clip.RootYaw(before) +
            MathF.IEEERemainder(
                clip.RootYaw(clip.LastFrame) -
                clip.RootYaw(clip.FirstFrame),
                MathF.Tau
            ) * loops,
            MathF.Tau
        );

        int firstCycle = _loop
            ? (int)MathF.Floor(oldTime / clip.Duration)
            : 0;

        int lastCycle = _loop
            ? (int)MathF.Floor(_time / clip.Duration)
            : 0;

        for (int cycle = firstCycle; cycle <= lastCycle; cycle++)
        {
            foreach (var cue in clip.Cues)
            {
                float cueTime =
                    cycle * clip.Duration +
                    (cue.Frame - clip.FirstFrame) *
                    clip.Direction / clip.FrameRate;

                if (cueTime <= oldTime || cueTime > _time)
                {
                    continue;
                }

                if (ContactFoot(cue.Name) is { } foot)
                {
                    Foot = foot;
                }

                Cue?.Invoke(cue.Name);
            }
        }

        _blendTime += deltaTime;
    }

    public void EvaluatePose()
    {
        Pose.Apply(
            Current,
            Frame,
            BlendWeight,
            FeetYaw,
            TorsoYaw,
            _baseTime
        );
    }

    private float PlaybackScale(float speed)
    {
        if (!Current.ScalePlaybackSpeed ||
            !Current.Movement ||
            Current.Clip.NaturalSpeed <= 0.01f)
        {
            return 1f;
        }

        if (_speedReference is { } reference &&
            reference.NaturalSpeed > 0.01f)
        {
            float scale =
                speed / reference.NaturalSpeed *
                Current.Clip.Duration / reference.Duration;

            return Math.Clamp(scale, 0.05f, 3f);
        }

        return Math.Clamp(
            speed / Current.Clip.NaturalSpeed,
            0.05f,
            3f
        );
    }

    private CharacterClip? FindSpeedReference(CharacterAction action)
    {
        if (!action.Movement ||
            !action.ScalePlaybackSpeed ||
            action.Track != 0)
        {
            return null;
        }

        string name = action.Name;

        int first = name.IndexOf('_');

        if (first < 0)
        {
            return null;
        }

        int second = name.IndexOf('_', first + 1);

        if (second < 0)
        {
            return null;
        }

        string direction = name[(first + 1)..second];

        if (direction is not ("left" or "right" or "back"))
        {
            return null;
        }

        string forwardName =
            name[..(first + 1)] +
            "forward" +
            name[second..];

        if (_actions.TryGetValue(
                forwardName,
                out var reference) &&
            reference.Movement &&
            reference.ScalePlaybackSpeed &&
            reference.Clip.NaturalSpeed > 0.01f)
        {
            return reference.Clip;
        }

        return null;
    }

    private static SupportFoot? ContactFoot(string name)
    {
        return name.StartsWith(
            "cfootprint_l",
            StringComparison.Ordinal
        )
            ? SupportFoot.Left
            : name.StartsWith(
                "cfootprint_r",
                StringComparison.Ordinal
            )
                ? SupportFoot.Right
                : null;
    }

    private void UpdateFoot()
    {
        var cues = Current.Clip.Cues
            .Where(c => ContactFoot(c.Name) != null)
            .ToArray();

        if (cues.Length == 0)
        {
            return;
        }

        var previous = cues.LastOrDefault(c =>
            (c.Frame - Frame) *
            Current.Clip.Direction <= 0
        );

        if (previous == null && _loop)
        {
            previous = cues[^1];
        }

        if (previous != null)
        {
            Foot = ContactFoot(previous.Name)!.Value;
        }
    }

    private float MatchFootPhase(CharacterClip next)
    {
        var source = Current.Clip;

        if (source.IsStatic || next.IsStatic)
        {
            return 0;
        }

        var contacts = source.Cues
            .Where(c => ContactFoot(c.Name) != null)
            .ToArray();

        var targets = next.Cues
            .Where(c => ContactFoot(c.Name) != null)
            .ToArray();

        if (contacts.Length < 2 || targets.Length < 2)
        {
            return Phase;
        }

        float phase = Phase;

        float CuePhase(
            CharacterClip clip,
            AnimationCue cue)
        {
            return (cue.Frame - clip.FirstFrame) *
                   clip.Direction / clip.FrameCount;
        }

        float Cycle(float value)
        {
            return value - MathF.Floor(value);
        }

        var previous = contacts.MinBy(c =>
            Cycle(phase - CuePhase(source, c))
        )!;

        float distance = Cycle(
            phase - CuePhase(source, previous)
        );

        var following = contacts.FirstOrDefault(c =>
            ContactFoot(c.Name) != ContactFoot(previous.Name)
        );

        var target = targets.FirstOrDefault(c =>
            ContactFoot(c.Name) == ContactFoot(previous.Name)
        );

        var targetFollowing = targets.FirstOrDefault(c =>
            ContactFoot(c.Name) != ContactFoot(previous.Name)
        );

        if (following == null ||
            target == null ||
            targetFollowing == null)
        {
            return phase;
        }

        float interval = Cycle(
            CuePhase(source, following) -
            CuePhase(source, previous)
        );

        if (interval < 0.0001f)
        {
            return phase;
        }

        float targetInterval = Cycle(
            CuePhase(next, targetFollowing) -
            CuePhase(next, target)
        );

        return Cycle(
            CuePhase(next, target) +
            distance / interval * targetInterval
        );
    }
}
