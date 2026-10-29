using System.Numerics;

namespace ZoneReborn.World.Characters.Locomotion;

public enum AnimState
{
    Idle, Walk, Run, Sprint, CrouchIdle, CrouchWalk, CrouchRun,
    Crouching, StandingUp, Stopping, TurningLeft, TurningRight,
    JumpWaiting, JumpTakeoff, JumpAir, Falling, LandingMoving, LandingIdle, LandingHard
}

// Numeric IDs are the original scripts/common/capabilities.xml values.
public enum AnimationCap { Unarmed = 0, AutogunArmed = 1, Crouching = 2, Injured = 3, Aiming = 4, Dead = 5, Crawl = 14, Bored = 16, Tired = 17 }
public enum SupportFoot { Left, Right }

public readonly record struct CharacterInput(
    Vector2 Move, bool Walk, bool Sprint, bool Crouch, bool JumpPressed, bool ToggleCrouchPressed);

public readonly record struct LocomotionSnapshot(
    AnimState State, string Action, float Frame, float Speed, bool Grounded, bool Crouched);

// Ported from player_speed_settings.pyc_dis; no weapon is equipped in editor Play.
public static class LocomotionSettings
{
    public const float Gravity = 9.8f;
    public const float Acceleration = 150f;
    public const float SprintAcceleration = 300f;
    public const float BodyTwistSpeed = 15f;
    public const float MaximumSlopeDegrees = 40f;
    public const float PostureTransitionSpeed = 0.3f;
    public const float HardLandingHeight = 1.5f;

    public const float DegreesToRadians = MathF.PI / 180f;
    public const float MovingTurnSpeed = 240f * DegreesToRadians;
    public const float StandingTurnThreshold = 60f * DegreesToRadians;
    public const float CrouchTurnThreshold = 45f * DegreesToRadians;
    public const float MaximumTurnAngle = 75f * DegreesToRadians;
    public const float MaximumFootYaw = 35f * DegreesToRadians;
    public const float MaximumTorsoYaw = 70f * DegreesToRadians;

    public const float JumpHeight = 0.9f;

    public static float Speed(Vector2 move, bool walk, bool crouch, bool sprint)
    {
        if (sprint && !crouch && !walk && move.Y > 0) return 6.3f;
        if (crouch)
            return move.Y < 0 ? (walk ? 1.2f : 1f) : move.Y == 0 && move.X != 0 ? (walk ? 1.4f : 1.2f) : walk ? 1.5f : 2.8f;
        return move.Y < 0 ? (walk ? 1.2f : 2.7f) : move.Y == 0 && move.X != 0 ? (walk ? 1.3f : 2.7f) : walk ? 1.3f : 3.6f;
    }
}
