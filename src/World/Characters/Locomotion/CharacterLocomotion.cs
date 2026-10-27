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
    private float _turnStartYaw;
    private float _turnSign;
    private float _cameraHeight = 1.15f;
    private float _cameraBaseY;
    private float _cameraLockRemaining;

    public CharacterAnimator Animator { get; }
    public Vector3 Position { get; private set; }
    public float Yaw { get; private set; }
    public AnimState State { get; private set; } = AnimState.Idle;
    public bool Grounded { get; private set; } = true;
    public bool Crouched => _crouched;
    public float CameraHeight => _cameraHeight;
    public Vector3 CameraAnchor => Position with { Y = _cameraBaseY };
    public IReadOnlySet<int> MatchCaps => _caps;
    public LocomotionSnapshot Snapshot => new(State, Animator.Current.Name, Animator.RelativeFrame, _velocity.Length(), Grounded, _crouched);

    public CharacterLocomotion(CharacterAsset asset, TerrainSurface surface, Vector3 position, float yaw)
    {
        _library = asset.Locomotion ?? throw new InvalidOperationException("Missing locomotion library.");
        _matcher = new(_library); _surface = surface;
        Position = position; Yaw = yaw;
        _cameraBaseY = position.Y;
        Animator = new CharacterAnimator(asset);
    }

    public void Update(float deltaTime, CharacterInput input, float cameraYaw)
    {
        if (!float.IsFinite(deltaTime) || deltaTime <= 0) return;
        deltaTime = Math.Min(deltaTime, 0.08f);
        if (input.ToggleCrouchPressed) _crouchToggle = !_crouchToggle;
        if (input.Move.LengthSquared() > 1) input = input with { Move = Vector2.Normalize(input.Move) };
        bool wantsCrouch = input.Crouch || _crouchToggle;
        if (input.JumpPressed && Grounded && !_crouched && !wantsCrouch && IsFreeGroundState) QueueJump(input);
        int steps = Math.Max(1, (int)MathF.Ceiling(deltaTime * 120));
        for (int i = 0; i < steps; i++) Step(deltaTime / steps, input, cameraYaw, wantsCrouch);
        UpdateBodyTwist(deltaTime, cameraYaw);
        _cameraHeight += ((_crouched ? 0.8f : 1.15f) - _cameraHeight) * (1 - MathF.Exp(-10 * deltaTime));
        _cameraLockRemaining = Math.Max(0, _cameraLockRemaining - deltaTime);
        if (_cameraLockRemaining <= 0) _cameraBaseY += (Position.Y - _cameraBaseY) * (1 - MathF.Exp(-8 * deltaTime));
        Animator.EvaluatePose();
    }

    public void ReleaseInput()
    {
        // Focus loss must not leave a queued jump or crouch key active.
        if (State == AnimState.JumpWaiting) { _jump = null; State = _crouched ? AnimState.CrouchIdle : AnimState.Idle; }
    }

    private bool IsLanding => State is AnimState.LandingIdle or AnimState.LandingMoving or AnimState.LandingHard;
    private bool IsPostureTransition => State is AnimState.Crouching or AnimState.StandingUp;
    private bool IsTurning => State is AnimState.TurningLeft or AnimState.TurningRight;
    private bool IsFreeGroundState => Grounded && !IsLanding && !IsPostureTransition && !IsTurning && State != AnimState.Stopping;

    private void Step(float dt, CharacterInput input, float cameraYaw, bool wantsCrouch)
    {
        if (State is AnimState.JumpTakeoff or AnimState.JumpAir or AnimState.Falling)
        {
            UpdateAir(dt, input, cameraYaw);
            return;
        }
        bool moving = input.Move.LengthSquared() > 0.001f;
        if (State == AnimState.JumpWaiting)
        {
            if (wantsCrouch) { _jump = null; State = AnimState.Idle; }
            else if ((_jumpWait -= dt) <= 0) { BeginTakeoff(); UpdateAir(dt, input, cameraYaw); return; }
        }
        if (IsPostureTransition && Animator.Complete) State = _crouched ? AnimState.CrouchIdle : AnimState.Idle;
        if (Grounded && !IsLanding && !IsPostureTransition && State != AnimState.JumpWaiting && wantsCrouch != _crouched)
            SetCrouch(wantsCrouch);
        if (IsPostureTransition)
        {
            _velocity = Vector2.Zero;
            Animator.Advance(dt, 0);
            return;
        }
        if (IsTurning)
        {
            if (moving) State = _crouched ? AnimState.CrouchIdle : AnimState.Idle;
            else
            {
                Animator.Advance(dt, 0);
                var clip = Animator.Current.Clip;
                float curve = MathF.Abs(Wrap(clip.RootYaw(Animator.Frame) - clip.RootYaw(clip.FirstFrame)));
                float total = MathF.Abs(Wrap(clip.RootYaw(clip.LastFrame) - clip.RootYaw(clip.FirstFrame)));
                Yaw = Wrap(_turnStartYaw + _turnSign * (total > 0.05f ? curve : MathF.PI / 2 * Animator.Phase));
                if (Animator.Complete) State = _crouched ? AnimState.CrouchIdle : AnimState.Idle;
                return;
            }
        }
        if (State == AnimState.Stopping)
        {
            if (moving || Animator.Complete) State = _crouched ? AnimState.CrouchIdle : AnimState.Idle;
            else
            {
                Animator.Advance(dt, 0);
                var promotion = Vector3.TransformNormal(Animator.RootDelta, Matrix4x4.CreateRotationY(Yaw + Animator.FeetYaw));
                MoveOnGround(new Vector2(promotion.X, promotion.Z));
                _velocity = Vector2.Zero;
                if (!Grounded) BeginFall();
                return;
            }
        }
        if (!moving && _lastMovingSpeed > 0.1f && !IsLanding && State != AnimState.JumpWaiting)
        {
            var stop = _matcher.Stop(_lastMovingSpeed, _lastDirection, _caps, Animator.Foot, Animator.Elapsed);
            _lastMovingSpeed = 0;
            if (stop != null)
            {
                State = AnimState.Stopping; _velocity = Vector2.Zero;
                Animator.Play(stop, false);
                Animator.Advance(dt, 0);
                return;
            }
        }
        if (moving) Yaw = ApproachAngle(Yaw, cameraYaw, LocomotionSettings.BodyTwistSpeed * dt);
        float speed = moving ? LocomotionSettings.Speed(input.Move, input.Walk, _crouched, input.Sprint) : 0;
        var forward = new Vector2(MathF.Sin(cameraYaw), MathF.Cos(cameraYaw));
        var right = new Vector2(forward.Y, -forward.X);
        var wantedVelocity = (forward * input.Move.Y + right * input.Move.X) * speed;
        if (State == AnimState.LandingHard) wantedVelocity = Vector2.Zero;
        float acceleration = input.Sprint && !_crouched ? LocomotionSettings.SprintAcceleration : LocomotionSettings.Acceleration;
        _velocity = Approach(_velocity, wantedVelocity, acceleration * dt);
        var previous = Position;
        if (!MoveOnGround(_velocity * dt)) _velocity = Vector2.Zero;
        if (!Grounded)
        {
            BeginFall();
            Animator.Advance(dt, _velocity.Length());
            return;
        }
        _velocity = new Vector2(Position.X - previous.X, Position.Z - previous.Z) / dt;
        float actualSpeed = _velocity.Length();
        float direction = actualSpeed > 0.01f ? Wrap(MathF.Atan2(_velocity.X, _velocity.Y) - Yaw) * 180 / MathF.PI : 0;
        if (moving && actualSpeed > 0.1f) { _lastDirection = direction; _lastMovingSpeed = actualSpeed; }
        if (IsLanding)
        {
            Animator.Advance(dt, actualSpeed);
            if ((_landingRemaining -= dt) <= 0)
            {
                int resume = _jump?.ResumeFrame ?? -1;
                _jump = null;
                State = _crouched ? AnimState.CrouchIdle : AnimState.Idle;
                var next = _matcher.Select(actualSpeed, direction, _caps, Animator.Current);
                Animator.Play(next, true, resumeFrame: moving ? resume : -1);
            }
            return;
        }
        if (!moving && actualSpeed < 0.1f && State != AnimState.JumpWaiting)
        {
            float angle = Wrap(cameraYaw - Yaw);
            // new_turn.py: unarmed STAY 90 degrees / CROUCH 65 degrees.
            if (MathF.Abs(angle) > (_crouched ? 65 : 90) * MathF.PI / 180)
            {
                _turnStartYaw = Yaw; _turnSign = MathF.Sign(angle);
                string name = $"turn_{(angle > 0 ? "right" : "left")}90_{(_crouched ? "crouch" : "stay")}_unarmed";
                State = angle > 0 ? AnimState.TurningRight : AnimState.TurningLeft;
                Animator.Play(_library.Actions[name], false);
                Animator.Advance(dt, 0);
                return;
            }
        }
        var action = _matcher.Select(actualSpeed, direction, _caps, Animator.Current);
        bool preservePhase = Animator.Current.Movement && action.Movement;
        Animator.Play(action, true, preservePhase);
        if (State != AnimState.JumpWaiting)
            State = actualSpeed < 0.1f ? (_crouched ? AnimState.CrouchIdle : AnimState.Idle)
                : _crouched ? (action.Name.StartsWith("run_", StringComparison.Ordinal) ? AnimState.CrouchRun : AnimState.CrouchWalk)
                : action.Name.StartsWith("sprint_", StringComparison.Ordinal) ? AnimState.Sprint
                : action.Name.StartsWith("run_", StringComparison.Ordinal) ? AnimState.Run : AnimState.Walk;
        Animator.Advance(dt, actualSpeed);
    }

    private void SetCrouch(bool value)
    {
        _crouched = value;
        if (value) _caps.Add((int)AnimationCap.Crouching); else _caps.Remove((int)AnimationCap.Crouching);
        if (_velocity.Length() > LocomotionSettings.PostureTransitionSpeed) return;
        State = value ? AnimState.Crouching : AnimState.StandingUp;
        Animator.Play(_library.Actions[value ? "pose_stay_to_crouch_unarmed" : "pose_crouch_to_stay_unarmed"], false);
    }

    private void UpdateBodyTwist(float dt, float cameraYaw)
    {
        float target = Animator.FeetYaw;
        if (!Grounded || IsTurning || IsPostureTransition) target = 0;
        else if (_velocity.LengthSquared() > 0.01f && Animator.Current.Movement && Animator.Current.FeetFollowDirection)
        {
            var clip = Animator.Current.Clip;
            var natural = clip.RootPosition(clip.LastFrame) - clip.RootPosition(clip.FirstFrame);
            float naturalYaw = MathF.Atan2(natural.X, natural.Z);
            target = Math.Clamp(Wrap(MathF.Atan2(_velocity.X, _velocity.Y) - Yaw - naturalYaw), -1.4f, 1.4f);
        }
        // player_speed_settings.MODEL_SMOOTH: STAY_FOOT_TWIST=19, STAY_BODY_TWIST=15.
        Animator.FeetYaw = ApproachAngle(Animator.FeetYaw, target, 19 * dt);
        float torsoTarget = Grounded ? Math.Clamp(Wrap(cameraYaw - Yaw - Animator.FeetYaw), -85 * MathF.PI / 180, 85 * MathF.PI / 180) : 0;
        Animator.TorsoYaw = ApproachAngle(Animator.TorsoYaw, torsoTarget, LocomotionSettings.BodyTwistSpeed * dt);
    }

    private bool MoveOnGround(Vector2 displacement)
    {
        var candidate = Position + new Vector3(displacement.X, 0, displacement.Y);
        if (!_surface.TryGetHeight(candidate.X, candidate.Z, out float height)) return false;
        float distance = displacement.Length();
        float rise = height - Position.Y;
        if (distance > 0.00001f && rise > distance * MathF.Tan(LocomotionSettings.MaximumSlopeDegrees * MathF.PI / 180) + 0.001f)
        {
            // Keep the contour component when approaching an unwalkable slope diagonally.
            if (_surface.TryGetHeight(Position.X + 0.1f, Position.Z, out float hx)
                && _surface.TryGetHeight(Position.X, Position.Z + 0.1f, out float hz))
            {
                var gradient = new Vector2(hx - Position.Y, hz - Position.Y);
                if (gradient.LengthSquared() > 0.000001f)
                {
                    gradient = Vector2.Normalize(gradient);
                    displacement -= gradient * Math.Max(0, Vector2.Dot(displacement, gradient));
                    candidate = Position + new Vector3(displacement.X, 0, displacement.Y);
                    if (!_surface.TryGetHeight(candidate.X, candidate.Z, out height)
                        || height - Position.Y > displacement.Length() * MathF.Tan(LocomotionSettings.MaximumSlopeDegrees * MathF.PI / 180) + 0.001f) return false;
                }
                else return false;
            }
            else return false;
        }
        if (Position.Y - height > 0.2f)
        {
            Position = candidate;
            Grounded = false;
        }
        else Position = candidate with { Y = height };
        return true;
    }

    private static Vector2 Approach(Vector2 current, Vector2 target, float maximum)
    {
        var delta = target - current;
        return delta.LengthSquared() <= maximum * maximum ? target : current + Vector2.Normalize(delta) * maximum;
    }
    private static float Wrap(float radians) => MathF.IEEERemainder(radians, MathF.Tau);
    private static float ApproachAngle(float current, float target, float maximum) => Wrap(current + Math.Clamp(Wrap(target - current), -maximum, maximum));
}
