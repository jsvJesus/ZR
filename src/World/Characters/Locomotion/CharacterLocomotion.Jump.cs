using System.Numerics;

namespace ZoneReborn.World.Characters.Locomotion;

public sealed partial class CharacterLocomotion
{
    private JumpDefinition? _jump;
    private float _jumpWait;
    private float _verticalVelocity;
    private float _jumpPeak;
    private float _landingRemaining;
    private bool _stationaryImpulse;

    private void QueueJump(CharacterInput input)
    {
        if (input.Move.Y > 0 && _velocity.Length() >= 0.1f)
        {
            float frame = MathF.Floor(Animator.RelativeFrame), count = Animator.Current.Clip.FrameCount;
            float right = JumpDefinition.Right.WaitFrames(frame, count), left = JumpDefinition.Left.WaitFrames(frame, count);
            _jump = right <= left ? JumpDefinition.Right : JumpDefinition.Left;
            _jumpWait = Math.Min(right, left) / Animator.Current.Clip.FrameRate;
        }
        else { _jump = JumpDefinition.Stationary; _jumpWait = 0; }
        State = AnimState.JumpWaiting;
    }

    private void BeginTakeoff()
    {
        Grounded = false;
        State = AnimState.JumpTakeoff;
        _jumpPeak = Position.Y;
        _verticalVelocity = 0;
        _stationaryImpulse = false;
        _lastMovingSpeed = 0;
        Animator.Play(_library.Actions[_jump!.Up], false);
        // avatar_jump unlocks vertical camera LOCK_CAM_TIME=0.5 after the takeoff action.
        // Skip the lock when the forward terrain rises, as _needLockVerticalCamForJump does.
        bool clear = true;
        var forward = new Vector3(MathF.Sin(Yaw), 0, MathF.Cos(Yaw));
        for (int i = 1; i < 20; i++)
        {
            var probe = Position + forward * (4f * i / 20);
            if (_surface.TryGetHeight(probe.X, probe.Z, out float height) && height - Position.Y > 0.4f)
            { clear = false; break; }
        }
        if (clear) _cameraLockRemaining = Animator.Current.Clip.Duration + 0.5f;
    }

    private void BeginFall()
    {
        _jump = JumpDefinition.Fall;
        _verticalVelocity = 0;
        _jumpPeak = Position.Y;
        State = AnimState.Falling;
        Animator.Play(_library.Actions[_jump.Fly], true);
    }

    private void UpdateAir(float dt, CharacterInput input, float cameraYaw)
    {
        _jump ??= JumpDefinition.Fall;
        var actionBefore = Animator.Current;
        Animator.Advance(dt, _velocity.Length());
        var promotion = Vector3.TransformNormal(Animator.RootDelta, Matrix4x4.CreateRotationY(Yaw));
        var horizontal = _velocity * dt;
        bool takeoff = State == AnimState.JumpTakeoff;
        if (takeoff && _jump != JumpDefinition.Stationary)
        {
            // vel_or_imp=True in the source: choose the larger movement, never add both.
            var promoted = new Vector2(promotion.X, promotion.Z);
            if (promoted.LengthSquared() > horizontal.LengthSquared()) horizontal = promoted;
        }
        if (!takeoff)
        {
            // Existing world geometry is Terrain. Air steering is deliberately weaker than ground acceleration.
            var forward = new Vector2(MathF.Sin(cameraYaw), MathF.Cos(cameraYaw));
            var right = new Vector2(forward.Y, -forward.X);
            var target = (forward * input.Move.Y + right * input.Move.X)
                * LocomotionSettings.Speed(input.Move, input.Walk, false, input.Sprint);
            if (input.Move.LengthSquared() > 0.001f) _velocity = Approach(_velocity, target, 4 * dt);
            horizontal = _velocity * dt;
        }
        var candidate = Position + new Vector3(horizontal.X, 0, horizontal.Y);
        if (!_surface.TryGetHeight(candidate.X, candidate.Z, out float ground))
        {
            candidate.X = Position.X; candidate.Z = Position.Z;
            _velocity = Vector2.Zero;
            if (!_surface.TryGetHeight(candidate.X, candidate.Z, out ground)) throw new InvalidOperationException("Персонаж покинул Terrain.");
        }
        if (takeoff && _jump != JumpDefinition.Stationary)
        {
            candidate.Y += promotion.Y;
            _verticalVelocity = promotion.Y / dt;
        }
        else
        {
            if (takeoff && !_stationaryImpulse)
            {
                var cue = actionBefore.Clip.Cues.FirstOrDefault(c => c.Name == "cJump");
                if (Animator.Frame >= (cue?.Frame ?? actionBefore.Clip.FirstFrame))
                {
                    // The native idle impulse is not in the decompiled PlayerAvatar.
                    // Derive height from the original running takeoff rather than inventing a second jump height.
                    var reference = _library.Actions[JumpDefinition.Right.Up].Clip;
                    float height = Math.Max(0.1f, reference.RootPosition(reference.LastFrame).Y - reference.RootPosition(reference.FirstFrame).Y);
                    _verticalVelocity = MathF.Sqrt(2 * LocomotionSettings.Gravity * height);
                    _stationaryImpulse = true;
                }
            }
            if (!takeoff || _stationaryImpulse)
            {
                candidate.Y += _verticalVelocity * dt - 0.5f * LocomotionSettings.Gravity * dt * dt;
                _verticalVelocity -= LocomotionSettings.Gravity * dt;
            }
        }
        // Reject the upward part of an unwalkable bank instead of teleporting onto it mid-jump.
        if (ground > candidate.Y + 0.15f)
        {
            candidate.X = Position.X; candidate.Z = Position.Z;
            _velocity = Vector2.Zero;
            _surface.TryGetHeight(candidate.X, candidate.Z, out ground);
        }
        if (takeoff && !Animator.Complete && !_stationaryImpulse && candidate.Y < ground)
            candidate.Y = ground;
        _jumpPeak = Math.Max(_jumpPeak, candidate.Y);
        if (candidate.Y <= ground && _verticalVelocity <= 0 && (!takeoff || Animator.Complete || _stationaryImpulse))
        {
            Position = candidate with { Y = ground };
            Land(input);
            return;
        }
        Position = candidate;
        if (takeoff && Animator.Complete)
        {
            State = AnimState.JumpAir;
            Animator.Play(_library.Actions[_jump.Fly], true);
        }
        else if (!takeoff && _verticalVelocity <= 0)
        {
            State = AnimState.Falling;
            if (candidate.Y - ground > 0.9f && Animator.Current.Name != _jump.FlyHigh)
                Animator.Play(_library.Actions[_jump.FlyHigh], true);
        }
    }

    private void Land(CharacterInput input)
    {
        Grounded = true;
        _verticalVelocity = 0;
        bool hard = _jumpPeak - Position.Y > LocomotionSettings.HardLandingHeight;
        bool moving = input.Move.LengthSquared() > 0.001f && _velocity.Length() > 0.1f;
        string action = hard ? JumpDefinition.HardLanding : moving ? _jump!.DownMove : _jump!.DownStop;
        State = hard ? AnimState.LandingHard : moving ? AnimState.LandingMoving : AnimState.LandingIdle;
        _landingRemaining = hard ? _jump!.HardLandingTime : moving ? _jump!.DownMoveTime : _jump!.DownStopTime;
        Animator.Play(_library.Actions[action], false);
        if (!moving || hard) _velocity = Vector2.Zero;
    }
}
