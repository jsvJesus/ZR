using System.Numerics;

namespace ZoneReborn.World.Characters.Locomotion;

public sealed partial class CharacterLocomotion
{
    private JumpDefinition? _jump;
    private float _jumpWait;
    private float _verticalVelocity;
    private float _jumpPeak;
    private float _landingRemaining;
    private bool _jumpImpulseApplied;

    private void QueueJump(CharacterInput input)
    {
        bool forwardMovement =
            input.Move.Y > 0 &&
            _velocity.Length() >= 0.1f;

        if (forwardMovement)
        {
            float frame = MathF.Floor(Animator.RelativeFrame);
            float frameCount = Animator.Current.Clip.FrameCount;

            float right = JumpDefinition.Right.WaitFrames(
                frame,
                frameCount
            );

            float left = JumpDefinition.Left.WaitFrames(
                frame,
                frameCount
            );

            _jump = right <= left
                ? JumpDefinition.Right
                : JumpDefinition.Left;

            _jumpWait = Math.Min(right, left) /
                        Animator.Current.Clip.FrameRate;
        }
        else
        {
            _jump = JumpDefinition.Stationary;
            _jumpWait = 0;
        }

        State = AnimState.JumpWaiting;
    }

    private void BeginTakeoff()
    {
        _jump ??= JumpDefinition.Stationary;

        Grounded = false;
        State = AnimState.JumpTakeoff;

        _jumpPeak = Position.Y;
        _verticalVelocity = 0;
        _jumpImpulseApplied = false;
        _lastMovingSpeed = 0;

        if (_jump == JumpDefinition.Stationary)
        {
            _velocity = Vector2.Zero;
        }

        Animator.Play(
            _library.Actions[_jump.Up],
            false
        );
    }

    private void BeginFall()
    {
        _jump = JumpDefinition.Fall;

        _verticalVelocity = 0;
        _jumpImpulseApplied = false;
        _jumpPeak = Position.Y;

        State = AnimState.Falling;

        Animator.Play(
            _library.Actions[_jump.Fly],
            true
        );
    }

    private void UpdateAir(
        float dt,
        CharacterInput input,
        float cameraYaw)
    {
        _jump ??= JumpDefinition.Fall;

        bool takeoff = State == AnimState.JumpTakeoff;
        var actionBefore = Animator.Current;

        Animator.Advance(dt, _velocity.Length());

        // Horizontal movement is handled by character physics.
        // Animation RootDelta is not applied again.
        var horizontal = _velocity * dt;

        if (!takeoff)
        {
            var forward = new Vector2(
                MathF.Sin(cameraYaw),
                MathF.Cos(cameraYaw)
            );

            var right = new Vector2(
                forward.Y,
                -forward.X
            );

            float speed = LocomotionSettings.Speed(
                input.Move,
                input.Walk,
                false,
                input.Sprint
            );

            var target =
                (forward * input.Move.Y + right * input.Move.X) *
                speed;

            if (input.Move.LengthSquared() > 0.001f)
            {
                _velocity = Approach(
                    _velocity,
                    target,
                    4f * dt
                );
            }

            horizontal = _velocity * dt;
        }

        var candidate = Position + new Vector3(
            horizontal.X,
            0,
            horizontal.Y
        );

        if (!_surface.TryGetHeight(
            candidate.X,
            candidate.Z,
            out float ground))
        {
            candidate.X = Position.X;
            candidate.Z = Position.Z;

            _velocity = Vector2.Zero;

            if (!_surface.TryGetHeight(
                candidate.X,
                candidate.Z,
                out ground))
            {
                throw new InvalidOperationException(
                    "Персонаж покинул Terrain."
                );
            }
        }

        // Apply the same vertical impulse for every jump type.
        // An animation cue determines when the feet leave the ground.
        if (takeoff && !_jumpImpulseApplied)
        {
            var clip = actionBefore.Clip;

            var cue = clip.Cues.FirstOrDefault(
                c => c.Name == "cJump"
            );

            bool reachedCue =
                cue == null ||
                (Animator.Frame - cue.Frame) * clip.Direction >= 0 ||
                Animator.Complete;

            if (reachedCue)
            {
                _verticalVelocity = MathF.Sqrt(
                    2f *
                    LocomotionSettings.Gravity *
                    LocomotionSettings.JumpHeight
                );

                _jumpImpulseApplied = true;
            }
            else
            {
                // Keep the character on the terrain during
                // the anticipation frames of the jump.
                Position = candidate with { Y = ground };
                return;
            }
        }

        // Physics controls vertical motion for the entire jump.
        candidate.Y +=
            _verticalVelocity * dt -
            0.5f * LocomotionSettings.Gravity * dt * dt;

        _verticalVelocity -= LocomotionSettings.Gravity * dt;

        // Do not allow the character to climb an unwalkable
        // slope by pushing horizontally into it during a jump.
        if (ground > candidate.Y + 0.15f)
        {
            candidate.X = Position.X;
            candidate.Z = Position.Z;

            _velocity = Vector2.Zero;

            _surface.TryGetHeight(
                candidate.X,
                candidate.Z,
                out ground
            );
        }

        _jumpPeak = Math.Max(
            _jumpPeak,
            candidate.Y
        );

        // Landing is controlled by collision and vertical
        // velocity, not by the length of the takeoff animation.
        if (candidate.Y <= ground &&
            _verticalVelocity <= 0)
        {
            Position = candidate with { Y = ground };
            Land(input);
            return;
        }

        Position = candidate;

        if (takeoff && Animator.Complete)
        {
            State = AnimState.JumpAir;

            Animator.Play(
                _library.Actions[_jump.Fly],
                true
            );

            return;
        }

        if (!takeoff && _verticalVelocity <= 0)
        {
            State = AnimState.Falling;

            if (candidate.Y - ground > 0.9f &&
                Animator.Current.Name != _jump.FlyHigh)
            {
                Animator.Play(
                    _library.Actions[_jump.FlyHigh],
                    true
                );
            }
        }
    }

    private void Land(CharacterInput input)
    {
        Grounded = true;
        _verticalVelocity = 0;
        _jumpImpulseApplied = false;

        bool hard =
            _jumpPeak - Position.Y >
            LocomotionSettings.HardLandingHeight;

        bool moving =
            input.Move.LengthSquared() > 0.001f &&
            _velocity.Length() > 0.1f;

        string action = hard
            ? JumpDefinition.HardLanding
            : moving
                ? _jump!.DownMove
                : _jump!.DownStop;

        State = hard
            ? AnimState.LandingHard
            : moving
                ? AnimState.LandingMoving
                : AnimState.LandingIdle;

        _landingRemaining = hard
            ? _jump!.HardLandingTime
            : moving
                ? _jump!.DownMoveTime
                : _jump!.DownStopTime;

        Animator.Play(
            _library.Actions[action],
            false
        );

        if (!moving || hard)
        {
            _velocity = Vector2.Zero;
        }
    }
}
