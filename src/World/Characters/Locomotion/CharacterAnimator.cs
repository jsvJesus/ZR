using System.Numerics;

namespace ZoneReborn.World.Characters.Locomotion;

public sealed class CharacterAnimator
{
    private float _time;
    private float _blendTime;
    private float _blendDuration = 0.001f;
    private bool _loop;
    public CharacterPose Pose { get; }
    public CharacterAction Current { get; private set; }
    public float Frame => Current.Clip.IsStatic ? Current.Clip.FirstFrame
        : Current.Clip.FirstFrame + (_loop ? _time % Current.Clip.Duration : Math.Min(_time, Current.Clip.Duration)) * Current.Clip.FrameRate * Current.Clip.Direction;
    public float RelativeFrame => (Frame - Current.Clip.FirstFrame) * Current.Clip.Direction;
    public float Phase => Current.Clip.IsStatic ? 0 : RelativeFrame / Current.Clip.FrameCount;
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
        Pose = new CharacterPose(asset);
        Current = asset.Locomotion?.Idle ?? throw new InvalidOperationException("Character has no locomotion actions.");
        _loop = true;
        Pose.Apply(Current, Frame, 1);
    }

    public void Play(CharacterAction action, bool loop, bool preservePhase = false, float resumeFrame = -1)
    {
        if (ReferenceEquals(action, Current) && loop == _loop && resumeFrame < 0) return;
        float phase = preservePhase ? Phase : 0;
        Pose.BeginBlend();
        _blendDuration = Math.Max(0.001f, Math.Max(Current.BlendOut, action.BlendIn));
        _blendTime = 0;
        Current = action;
        _loop = loop;
        _time = resumeFrame >= 0 ? Math.Clamp(resumeFrame, 0, action.Clip.FrameCount) / action.Clip.FrameRate : phase * action.Clip.Duration;
        Elapsed = 0;
        RootDelta = Vector3.Zero; YawDelta = 0;
    }

    public void Advance(float deltaTime, float speed)
    {
        float before = Frame;
        Elapsed += deltaTime;
        if (Current.Clip.IsStatic)
        {
            _time = Math.Min(_time + deltaTime, Current.Clip.Duration);
            RootDelta = Vector3.Zero;
            YawDelta = 0;
            _blendTime += deltaTime;
            return;
        }
        float scale = Current.ScalePlaybackSpeed && Current.Movement && Current.Clip.NaturalSpeed > 0.01f
            ? Math.Clamp(speed / Current.Clip.NaturalSpeed, 0.05f, 3f) : 1;
        float oldTime = _time;
        _time += deltaTime * scale;
        float after = Frame;
        int loops = _loop ? (int)MathF.Floor(_time / Current.Clip.Duration) - (int)MathF.Floor(oldTime / Current.Clip.Duration) : 0;
        var clip = Current.Clip;
        RootDelta = clip.RootPosition(after) - clip.RootPosition(before)
            + (clip.RootPosition(clip.LastFrame) - clip.RootPosition(clip.FirstFrame)) * loops;
        YawDelta = MathF.IEEERemainder(clip.RootYaw(after) - clip.RootYaw(before), MathF.Tau);
        foreach (var cue in clip.Cues)
        {
            float cueTime = (cue.Frame - clip.FirstFrame) * clip.Direction;
            float fromTime = (before - clip.FirstFrame) * clip.Direction, toTime = (after - clip.FirstFrame) * clip.Direction;
            bool crossed = loops > 0 ? cueTime > fromTime || cueTime <= toTime : cueTime > fromTime && cueTime <= toTime;
            if (!crossed) continue;
            if (cue.Name == "cfootprint_l") Foot = SupportFoot.Left;
            else if (cue.Name == "cfootprint_r") Foot = SupportFoot.Right;
            Cue?.Invoke(cue.Name);
        }
        _blendTime += deltaTime;
    }

    // Skin once per display frame, not once per physics substep.
    public void EvaluatePose() => Pose.Apply(Current, Frame, Math.Clamp(_blendTime / _blendDuration, 0, 1), FeetYaw, TorsoYaw);
}
