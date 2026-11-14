using System.Numerics;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Characters.Locomotion;

public sealed partial class CharacterLocomotion
{
    private readonly LocomotionLibrary _library;
    private readonly ActionMatcher _matcher;
    private readonly TerrainSurface _surface;
    private readonly HashSet<int> _caps = [(int)AnimationCap.Unarmed];

    private Vector2 _velocity;
    private bool _crouchToggle;
    private bool _crouched;
    private float _lastDirection;
    private float _lastMovingSpeed;
    private float _lastCameraYaw;
    private float _cameraHeight = 1.15f;
    private float _cameraBaseY;

    public CharacterAnimator Animator { get; }
    public CharacterWeaponController Weapon { get; }
    public Vector3 Position { get; private set; }
    public float Yaw { get; private set; }
    public AnimState State { get; private set; } = AnimState.Idle;
    public bool Grounded { get; private set; } = true;
    public bool Crouched => _crouched;
    public float CameraHeight => _cameraHeight;
    public Vector3 CameraAnchor => Position with { Y = _cameraBaseY };
    public IReadOnlySet<int> MatchCaps => _caps;
    public bool IsAiming => _caps.Contains((int)AnimationCap.Aiming) &&
        Weapon.State == CharacterWeaponState.Held && Grounded && !IsLanding && !IsPostureTransition &&
        State != AnimState.JumpWaiting;

    public bool TryGetFirstPersonEye(float aimBlend, out Vector3 position)
    {
        if (!Animator.Pose.TryGetFirstPersonEye(aimBlend, out position))
        {
            return false;
        }

        position = Vector3.Transform(position, Matrix4x4.CreateRotationY(Yaw)) +
            Position + Vector3.UnitY * Animator.Pose.FloorOffset;
        return true;
    }

    public LocomotionSnapshot Snapshot => new(
        State,
        Animator.Current.Name,
        Animator.RelativeFrame,
        _velocity.Length(),
        Grounded,
        _crouched
    );

    public CharacterLocomotion(
        CharacterAsset asset,
        TerrainSurface surface,
        Vector3 position,
        float yaw)
    {
        _library = asset.Locomotion ??
                   throw new InvalidOperationException("Missing locomotion library.");

        _matcher = new ActionMatcher(_library);
        _surface = surface;

        Position = position;
        Yaw = yaw;
        _lastCameraYaw = yaw;
        _cameraBaseY = position.Y;

        Animator = new CharacterAnimator(asset);
        Weapon = new CharacterWeaponController(asset.Weapon, _library);
        Animator.Pose.Weapon = Weapon;
    }

    public void Update(float deltaTime, CharacterInput input, float cameraYaw, float cameraPitch = 0f)
    {
        if (!float.IsFinite(deltaTime) || deltaTime <= 0)
        {
            return;
        }

        deltaTime = Math.Min(deltaTime, 0.08f);

        if (input.ToggleCrouchPressed)
        {
            _crouchToggle = !_crouchToggle;
        }

        if (input.Move.LengthSquared() > 1)
        {
            input = input with
            {
                Move = Vector2.Normalize(input.Move)
            };
        }

        bool wantsCrouch = input.Crouch || _crouchToggle;
        if (_crouched && !wantsCrouch && !_surface.CanOccupy(Position, StandingHeight)) wantsCrouch = true;

        Weapon.Advance(
            deltaTime,
            IsFreeGroundState &&
            wantsCrouch == _crouched &&
            !input.JumpPressed,
            _crouched,
            input.Sprint &&
            !(input.AimPressed && Weapon.InHands) &&
            input.Move.Y > 0 &&
            !input.Walk
        );

        UpdateWeaponCaps(input.AimPressed);

        if (Weapon.IsChanging) wantsCrouch = _crouched;

        float cameraDelta = Wrap(cameraYaw - _lastCameraYaw);
        _lastCameraYaw = cameraYaw;

        if (Grounded && !IsPostureTransition && !IsLanding && input.Move.LengthSquared() > 0.001f)
        {
            // Camera steering changes the movement basis, not the local gait.
            // Transport velocity into that basis before accelerating, so a camera
            // turn alone does not slow sprint or select a strafe/back run.
            if (Animator.Current.Movement)
            {
                float sine = MathF.Sin(cameraDelta);
                float cosine = MathF.Cos(cameraDelta);
                _velocity = new Vector2(
                    _velocity.X * cosine + _velocity.Y * sine,
                    _velocity.Y * cosine - _velocity.X * sine
                );
            }

            Yaw = Wrap(cameraYaw);
        }

        if (input.JumpPressed &&
            Grounded &&
            !_crouched &&
            !wantsCrouch &&
            !Weapon.IsChanging &&
            IsFreeGroundState)
        {
            QueueJump(input);
        }

        int steps = Math.Max(
            1,
            (int)MathF.Ceiling(deltaTime * 120f)
        );

        float dt = deltaTime / steps;

        for (int i = 0; i < steps; i++)
        {
            Step(dt, input, cameraYaw, wantsCrouch);
        }

        UpdateBodyTwist(deltaTime, cameraYaw);

        float targetCameraHeight = _crouched ? 0.8f : 1.15f;

        _cameraHeight +=
            (targetCameraHeight - _cameraHeight) *
            (1f - MathF.Exp(-10f * deltaTime));

        _cameraBaseY +=
            (Position.Y - _cameraBaseY) *
            (1f - MathF.Exp(-8f * deltaTime));

        Weapon.UpdatePose(
            deltaTime,
            Animator.Current,
            !Grounded || IsLanding,
            Animator.Pose.FirstPersonWeight > 0.001f
                ? IsAiming
                : _caps.Contains((int)AnimationCap.Aiming)
        );

        float targetPitch =
            Weapon.InHands &&
            Grounded &&
            !IsPostureTransition &&
            !IsLanding
                ? Math.Clamp(cameraPitch, -1.15f, 0.65f)
                : 0f;

        float pitchBlend =
            1f - MathF.Exp(-12f * deltaTime);

        Animator.Pose.AimPitch +=
            (targetPitch - Animator.Pose.AimPitch) *
            pitchBlend;

        Animator.Pose.LookYaw = Wrap(cameraYaw - Yaw);
        Animator.Pose.LookPitch = Math.Clamp(cameraPitch, -1.15f, 0.65f);
        Animator.EvaluatePose();
    }

    public void ReleaseInput()
    {
        if (State != AnimState.JumpWaiting)
        {
            return;
        }

        _jump = null;
        _jumpWait = 0;

        State = _crouched
            ? AnimState.CrouchIdle
            : AnimState.Idle;
    }

    private bool IsLanding =>
        State is AnimState.LandingIdle
            or AnimState.LandingMoving
            or AnimState.LandingHard;

    private bool IsPostureTransition =>
        State is AnimState.Crouching
            or AnimState.StandingUp;

    private bool IsFreeGroundState =>
        Grounded &&
        !IsLanding &&
        !IsPostureTransition &&
        !IsTurning &&
        State != AnimState.Stopping &&
        State != AnimState.JumpWaiting;

    private void Step(
        float dt,
        CharacterInput input,
        float cameraYaw,
        bool wantsCrouch)
    {
        if (State is AnimState.JumpTakeoff
            or AnimState.JumpAir
            or AnimState.Falling)
        {
            UpdateAir(dt, input, cameraYaw);
            return;
        }

        bool moving = input.Move.LengthSquared() > 0.001f;

        if (State == AnimState.JumpWaiting)
        {
            if (wantsCrouch)
            {
                _jump = null;
                _jumpWait = 0;

                State = _crouched
                    ? AnimState.CrouchIdle
                    : AnimState.Idle;
            }
            else
            {
                _jumpWait -= dt;

                if (_jumpWait <= 0)
                {
                    BeginTakeoff(input);
                    UpdateAir(dt, input, cameraYaw);
                    return;
                }
            }
        }

        if (IsPostureTransition && Animator.Complete)
        {
            State = _crouched
                ? AnimState.CrouchIdle
                : AnimState.Idle;
        }

        if (Grounded &&
            !IsLanding &&
            !IsPostureTransition &&
            State != AnimState.JumpWaiting &&
            wantsCrouch != _crouched)
        {
            SetCrouch(wantsCrouch);
        }

        if (IsPostureTransition)
        {
            _velocity = Vector2.Zero;
            Animator.Advance(dt, 0);
            return;
        }

        if (IsTurning)
        {
            if (moving)
            {
                State = _crouched
                    ? AnimState.CrouchIdle
                    : AnimState.Idle;
            }
            else
            {
                _velocity = Vector2.Zero;
                AdvanceTurn(dt, cameraYaw);
                return;
            }
        }

        if (State == AnimState.Stopping)
        {
            if (moving || Animator.Complete)
            {
                State = _crouched
                    ? AnimState.CrouchIdle
                    : AnimState.Idle;
            }
            else
            {
                AdvanceStop(dt);
                return;
            }
        }

        if (!moving &&
            _lastMovingSpeed > 0.1f &&
            !IsLanding &&
            State != AnimState.JumpWaiting)
        {
            var stop = _matcher.Stop(
                _lastMovingSpeed,
                _lastDirection,
                _caps,
                Animator.Foot,
                Animator.Elapsed
            );

            _lastMovingSpeed = 0;

            if (stop != null)
            {
                State = AnimState.Stopping;
                _velocity = Vector2.Zero;

                Animator.Play(stop, false);
                AdvanceStop(dt);
                return;
            }
        }

        // Moving yaw uses the same view basis as velocity and action matching.
        // The 45-degree turn track is used only on the spot.
        // Landing keeps its authored root displacement in the landing direction.
        if (moving && !IsLanding)
        {
            Yaw = Wrap(cameraYaw);
        }

        bool aiming = _caps.Contains(
            (int)AnimationCap.Aiming
        );

        float speed = moving
            ? LocomotionSettings.Speed(
                input.Move,
                input.Walk,
                _crouched,
                input.Sprint && !aiming,
                aiming
            )
            : 0;

        var forward = new Vector2(
            MathF.Sin(cameraYaw),
            MathF.Cos(cameraYaw)
        );

        var right = new Vector2(
            forward.Y,
            -forward.X
        );

        var wantedVelocity =
            (forward * input.Move.Y + right * input.Move.X) * speed;

        if (State == AnimState.LandingHard)
        {
            wantedVelocity = Vector2.Zero;
        }

        float acceleration =
            input.Sprint && !_crouched && !aiming
                ? LocomotionSettings.SprintAcceleration
                : LocomotionSettings.Acceleration;

        _velocity = Approach(
            _velocity,
            wantedVelocity,
            acceleration * dt
        );

        var previousPosition = Position;
        var displacement = _velocity * dt;

        if (IsLanding)
        {
            Animator.Advance(dt, _velocity.Length());
            var root = Vector3.TransformNormal(Animator.RootDelta, Matrix4x4.CreateRotationY(Yaw));
            var promotion = new Vector2(root.X, root.Z);
            if (Animator.Current.Impacting && promotion.LengthSquared() > displacement.LengthSquared())
                displacement = promotion;
        }

        if (!MoveOnGround(displacement))
        {
            _velocity = Vector2.Zero;
        }

        if (!Grounded)
        {
            BeginFall();
            Animator.Advance(dt, _velocity.Length());
            return;
        }

        _velocity = new Vector2(
            Position.X - previousPosition.X,
            Position.Z - previousPosition.Z
        ) / dt;

        float actualSpeed = _velocity.Length();

        float direction = actualSpeed > 0.01f
            ? Wrap(
                MathF.Atan2(_velocity.X, _velocity.Y) - Yaw
            ) * 180f / MathF.PI
            : 0;

        if (moving && actualSpeed > 0.1f)
        {
            _lastDirection = direction;
            _lastMovingSpeed = actualSpeed;
        }

        if (IsLanding)
        {
            _landingRemaining -= dt;

            if (_landingRemaining <= 0)
            {
                int resumeFrame = _jump?.ResumeFrame ?? -1;

                _jump = null;

                State = _crouched
                    ? AnimState.CrouchIdle
                    : AnimState.Idle;

                var next = _matcher.Select(
                    actualSpeed,
                    direction,
                    _caps,
                    Animator.Current
                );

                Animator.Play(
                    next,
                    true,
                    resumeFrame: moving ? resumeFrame : -1
                );
            }

            return;
        }

        if (!moving &&
            actualSpeed < 0.1f &&
            State != AnimState.JumpWaiting)
        {
            if (TryStartTurn(cameraYaw))
            {
                AdvanceTurn(dt, cameraYaw);
                return;
            }
        }

        var action = _matcher.Select(
            actualSpeed,
            direction,
            _caps,
            Animator.Current
        );

        if (_crouched &&
            Weapon.InHands &&
            action.Name == "run_forward_crouch_incombat")
        {
            action = _library.Actions["walk_forward_crouch_incombat"];
        }

        bool preservePhase =
            Animator.Current.Movement &&
            action.Movement;

        Animator.Play(action, true, preservePhase);

        if (State != AnimState.JumpWaiting)
        {
            if (actualSpeed < 0.1f)
            {
                State = _crouched
                    ? AnimState.CrouchIdle
                    : AnimState.Idle;
            }
            else if (_crouched)
            {
                State = action.Name.StartsWith(
                    "run_",
                    StringComparison.Ordinal
                )
                    ? AnimState.CrouchRun
                    : AnimState.CrouchWalk;
            }
            else if (action.Name.StartsWith(
                "sprint_",
                StringComparison.Ordinal))
            {
                State = AnimState.Sprint;
            }
            else if (action.Name.StartsWith(
                "run_",
                StringComparison.Ordinal))
            {
                State = AnimState.Run;
            }
            else
            {
                State = AnimState.Walk;
            }
        }

        Animator.Advance(dt, actualSpeed);
    }

    private void AdvanceStop(float dt)
    {
        Animator.Advance(dt, 0);
        var promotion = Vector3.TransformNormal(
            Animator.RootDelta,
            Matrix4x4.CreateRotationY(Yaw + Animator.FeetYaw)
        );
        MoveOnGround(new Vector2(promotion.X, promotion.Z));
        _velocity = Vector2.Zero;
        if (!Grounded) BeginFall();
    }

    private void SetCrouch(bool value)
    {
        _crouched = value;

        if (value)
        {
            _caps.Add((int)AnimationCap.Crouching);
        }
        else
        {
            _caps.Remove((int)AnimationCap.Crouching);
        }

        if (_velocity.Length() >
            LocomotionSettings.PostureTransitionSpeed)
        {
            return;
        }

        State = value
            ? AnimState.Crouching
            : AnimState.StandingUp;

        _lastMovingSpeed = 0;

        string action = value
            ? $"pose_stay_to_crouch_{(Weapon.Armed ? "incombat" : "unarmed")}"
            : $"pose_crouch_to_stay_{(Weapon.Armed ? "incombat" : "unarmed")}";

        Animator.Play(_library.Actions[action], false);
    }

    public void RestoreWeaponInHands()
    {
        Weapon.RestoreHeld();
        UpdateWeaponCaps();
        Animator.Play(_matcher.Idle(_caps), true);
        Animator.EvaluatePose();
    }

    private void UpdateWeaponCaps(bool aimPressed = false)
    {
        _caps.Remove((int)AnimationCap.Unarmed);
        _caps.Remove((int)AnimationCap.AutogunArmed);
        _caps.Remove((int)AnimationCap.Aiming);
        _caps.Remove((int)AnimationCap.Bored);
        _caps.Remove((int)AnimationCap.WeaponRightHand);

        _caps.Add(
            (int)(Weapon.Armed
                ? AnimationCap.AutogunArmed
                : AnimationCap.Unarmed)
        );

        if (!Weapon.Armed)
        {
            return;
        }

        if (aimPressed &&
            Weapon.State == CharacterWeaponState.Held &&
            Grounded &&
            !IsLanding &&
            !IsPostureTransition)
        {
            _caps.Add((int)AnimationCap.Aiming);
        }

        if (Weapon.Relaxed)
        {
            _caps.Add((int)AnimationCap.Bored);
        }

        if (Weapon.Relaxed &&
            Weapon.Definition?.ResolveProfile(
                false,
                true,
                true,
                inCombat: false
            ).ToAttachWeapon == "Right")
        {
            _caps.Add((int)AnimationCap.WeaponRightHand);
        }
    }

    private void UpdateBodyTwist(float dt, float cameraYaw)
    {
        bool moving = _velocity.LengthSquared() > 0.01f;

        float feetTarget = State == AnimState.Stopping && Animator.Current.FeetFollowDirection
            ? Animator.FeetYaw : 0;

        if (Grounded &&
            !IsTurning &&
            !IsPostureTransition &&
            moving &&
            Animator.Current.Movement &&
            Animator.Current.FeetFollowDirection)
        {
            var clip = Animator.Current.Clip;

            var natural =
                clip.RootPosition(clip.LastFrame) -
                clip.RootPosition(clip.FirstFrame);

            float naturalYaw = MathF.Atan2(
                natural.X,
                natural.Z
            );

            float movementYaw = MathF.Atan2(
                _velocity.X,
                _velocity.Y
            );

            feetTarget = Math.Clamp(
                Wrap(movementYaw - Yaw - naturalYaw),
                -LocomotionSettings.MaximumFootYaw,
                LocomotionSettings.MaximumFootYaw
            );
        }

        Animator.FeetYaw = ApproachAngle(
            Animator.FeetYaw,
            feetTarget,
            19f * dt
        );

        float torsoTarget = 0;

        if (Grounded && !IsPostureTransition)
        {
            float delta = Wrap(
                cameraYaw - Yaw - Animator.FeetYaw
            );

            float maximumTwist =
                45f * LocomotionSettings.DegreesToRadians;

            torsoTarget = Math.Clamp(
                delta,
                -maximumTwist,
                maximumTwist
            );
        }

        Animator.TorsoYaw = ApproachAngle(
            Animator.TorsoYaw,
            torsoTarget,
            6f * dt
        );
    }

    private bool MoveOnGround(Vector2 displacement)
    {
        if (_surface.HasSceneObjects || _surface.HasTerrainHoles) return MoveAmongObjects(displacement);
        var candidate = Position + new Vector3(
            displacement.X,
            0,
            displacement.Y
        );

        if (!_surface.TryGetHeight(
            candidate.X,
            candidate.Z,
            out float height))
        {
            return false;
        }

        float distance = displacement.Length();
        float rise = height - Position.Y;

        float maximumRise =
            distance *
            MathF.Tan(
                LocomotionSettings.MaximumSlopeDegrees *
                MathF.PI / 180f
            ) + 0.001f;

        if (distance > 0.00001f && rise > maximumRise)
        {
            if (_surface.TryGetHeight(
                    Position.X + 0.1f,
                    Position.Z,
                    out float hx) &&
                _surface.TryGetHeight(
                    Position.X,
                    Position.Z + 0.1f,
                    out float hz))
            {
                var gradient = new Vector2(
                    hx - Position.Y,
                    hz - Position.Y
                );

                if (gradient.LengthSquared() <= 0.000001f)
                {
                    return false;
                }

                gradient = Vector2.Normalize(gradient);

                displacement -= gradient *
                    Math.Max(
                        0,
                        Vector2.Dot(displacement, gradient)
                    );

                candidate = Position + new Vector3(
                    displacement.X,
                    0,
                    displacement.Y
                );

                if (!_surface.TryGetHeight(
                    candidate.X,
                    candidate.Z,
                    out height))
                {
                    return false;
                }

                maximumRise =
                    displacement.Length() *
                    MathF.Tan(
                        LocomotionSettings.MaximumSlopeDegrees *
                        MathF.PI / 180f
                    ) + 0.001f;

                if (height - Position.Y > maximumRise)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        if (Position.Y - height > 0.2f)
        {
            Position = candidate;
            Grounded = false;
        }
        else
        {
            Position = candidate with { Y = height };
        }

        return true;
    }

    private static Vector2 Approach(
        Vector2 current,
        Vector2 target,
        float maximum)
    {
        var delta = target - current;

        if (delta.LengthSquared() <= maximum * maximum)
        {
            return target;
        }

        return current + Vector2.Normalize(delta) * maximum;
    }

    private static float Wrap(float radians)
    {
        return MathF.IEEERemainder(radians, MathF.Tau);
    }

    private static float ApproachAngle(
        float current,
        float target,
        float maximum)
    {
        float delta = Wrap(target - current);

        return Wrap(
            current + Math.Clamp(delta, -maximum, maximum)
        );
    }
}
