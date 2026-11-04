using System.Numerics;

namespace ZoneReborn.World.Characters.Locomotion;

public sealed partial class CharacterLocomotion
{
    private const float JumpCueFallbackDelay = 0.08f;

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

    private void BeginTakeoff(CharacterInput input)
    {
        _jump ??= JumpDefinition.Stationary;

        Grounded = false;
        State = AnimState.JumpTakeoff;

        _jumpPeak = Position.Y;
        _verticalVelocity = 0;
        _jumpImpulseApplied = false;
        _lastMovingSpeed = 0;

        if (_jump == JumpDefinition.Stationary &&
            input.Move.LengthSquared() <= 0.001f)
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

        if (input.Move.LengthSquared() > 0.001f)
        {
            Yaw = Wrap(cameraYaw);
        }

        bool takeoff = State == AnimState.JumpTakeoff;
        var actionBefore = Animator.Current;

        Animator.Advance(dt, _velocity.Length());

        var horizontal = _velocity * dt;

        if (takeoff &&
            (_jump == JumpDefinition.Right ||
             _jump == JumpDefinition.Left))
        {
            var root = Vector3.TransformNormal(
                Animator.RootDelta,
                Matrix4x4.CreateRotationY(Yaw)
            );

            var promotion = new Vector2(root.X, root.Z);

            if (promotion.LengthSquared() >
                horizontal.LengthSquared())
            {
                horizontal = promotion;
            }
        }

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
                (forward * input.Move.Y +
                 right * input.Move.X) * speed;

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

        if (!GroundBelow(
            candidate.X,
            candidate.Z,
            Position.Y + 0.15f,
            out float ground))
        {
            candidate.X = Position.X;
            candidate.Z = Position.Z;
            horizontal = Vector2.Zero;
            _velocity = Vector2.Zero;

            if (!GroundBelow(
                candidate.X,
                candidate.Z,
                Position.Y + 0.15f,
                out ground))
            {
                throw new InvalidOperationException(
                    "Персонаж покинул Terrain."
                );
            }
        }

        if (takeoff && !_jumpImpulseApplied)
        {
            var clip = actionBefore.Clip;

            var cue = clip.Cues.FirstOrDefault(
                c => c.Name == "cJump"
            );

            bool reachedCue = cue != null
                ? (Animator.Frame - cue.Frame) *
                    clip.Direction >= 0 || Animator.Complete
                : Animator.Elapsed >= JumpCueFallbackDelay ||
                    Animator.Complete;

            if (!reachedCue)
            {
                Position = _surface.MoveCharacter(
                    Position,
                    candidate with { Y = ground },
                    CollisionHeight
                );

                return;
            }

            _verticalVelocity = MathF.Sqrt(
                2f *
                LocomotionSettings.Gravity *
                LocomotionSettings.JumpHeight
            );

            _jumpImpulseApplied = true;
        }

        float verticalDisplacement =
            _verticalVelocity * dt -
            0.5f * LocomotionSettings.Gravity * dt * dt;

        _verticalVelocity -= LocomotionSettings.Gravity * dt;

        candidate.Y = Position.Y + verticalDisplacement;

        if (ground > candidate.Y + 0.15f)
        {
            candidate.X = Position.X;
            candidate.Z = Position.Z;
            horizontal = Vector2.Zero;
            _velocity = Vector2.Zero;

            GroundBelow(
                candidate.X,
                candidate.Z,
                Position.Y + 0.15f,
                out ground
            );
        }

        bool descending =
            _verticalVelocity <= 0 &&
            verticalDisplacement <= 0;

        if (_surface.HasSceneObjects)
        {
            var horizontalDelta = new Vector3(
                horizontal.X,
                0,
                horizontal.Y
            );

            if (verticalDisplacement > 0)
            {
                var verticalTarget = Position +
                    Vector3.UnitY * verticalDisplacement;

                var elevated = _surface.MoveCharacter(
                    Position,
                    verticalTarget,
                    CollisionHeight
                );

                if (elevated.Y <
                    verticalTarget.Y - 0.001f &&
                    _verticalVelocity > 0)
                {
                    _verticalVelocity = 0;
                }

                candidate = _surface.MoveCharacter(
                    elevated,
                    elevated + horizontalDelta,
                    CollisionHeight
                );
            }
            else
            {
                var advanced = _surface.MoveCharacter(
                    Position,
                    Position + horizontalDelta,
                    CollisionHeight
                );

                candidate = _surface.MoveCharacter(
                    advanced,
                    advanced +
                    Vector3.UnitY * verticalDisplacement,
                    CollisionHeight
                );
            }

            GroundBelow(
                candidate.X,
                candidate.Z,
                Math.Max(Position.Y, candidate.Y) + 0.02f,
                out ground
            );
        }

        _jumpPeak = Math.Max(
            _jumpPeak,
            candidate.Y
        );

        if (descending &&
            TryGroundSupport(
                candidate,
                _surface.HasSceneObjects ? 0.02f : 0,
                0.15f,
                out var landingSupport,
                out float landingSurfaceY))
        {
            Position = landingSupport;
            _supportSurfaceY = landingSurfaceY;
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

        if (!hard && _jump == JumpDefinition.Stationary)
        {
            _landingRemaining = Animator.Current.Clip.Duration;
        }

        if (!moving || hard)
        {
            _velocity = Vector2.Zero;
        }
    }
}
