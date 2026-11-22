using System.Numerics;

namespace ZoneReborn.World.Characters.Locomotion;

public enum AnimState
{
    Idle, Walk, Run, Sprint, CrouchIdle, CrouchWalk, CrouchRun,
    Crouching, StandingUp, Stopping, TurningLeft, TurningRight,
    JumpWaiting, JumpTakeoff, JumpAir, Falling, LandingMoving, LandingIdle, LandingHard
}

// Numeric IDs are the original scripts/common/capabilities.xml values.
public enum AnimationCap
{
    Unarmed = 0,
    AutogunArmed = 1,
    Crouching = 2,
    Injured = 3,
    Aiming = 4,
    Dead = 5,
    Crawl = 14,
    Bored = 16,
    Tired = 17,
    WeaponRightHand = 24
}

public enum SupportFoot
{
    Left,
    Right
}

public readonly record struct CharacterInput(
    Vector2 Move,
    bool Walk,
    bool Sprint,
    bool Crouch,
    bool JumpPressed,
    bool ToggleCrouchPressed,
    bool AimPressed = false,
    float Lean = 0f,
    bool FirePressed = false
);

public readonly record struct LocomotionSnapshot(
    AnimState State,
    string Action,
    float Frame,
    float Speed,
    bool Grounded,
    bool Crouched
);

// Ported from player_speed_settings.pyc_dis.
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
    public const float StandingTurnThreshold = 45f * DegreesToRadians;
    public const float CrouchTurnThreshold = 45f * DegreesToRadians;
    public const float TurnAngle = 45f * DegreesToRadians;
    public const float MaximumFootYaw = 35f * DegreesToRadians;
    public const float MaximumTorsoYaw = 70f * DegreesToRadians;
    public const float MaximumTorsoRoll = 38.1f * DegreesToRadians;
    public const float FirstPersonRollMultiplier = 0.2f;
    public const float CameraDownPitch = 85f * DegreesToRadians;
    public const float FirstPersonDownPitch = 88f * DegreesToRadians;
    public const float CameraUpPitch = 65f * DegreesToRadians;
    public const float BodyDownPitch = 75f * DegreesToRadians;
    public const float BodyUpPitch = 60f * DegreesToRadians;

    public const float JumpHeight = 0.9f;

    public static float Speed(
        Vector2 move,
        bool walk,
        bool crouch,
        bool sprint,
        bool aiming = false,
        bool inCombat = false)
    {
        bool back = move.Y < 0;
        bool strafe = move.Y == 0 && move.X != 0;
        if (aiming)
        {
            if (crouch) return 0.7f;
            if (!walk) return 1f;
            return back ? 0.4f : strafe ? 0.6f : 1f;
        }

        if (crouch)
        {
            if (walk) return back ? 1.2f : strafe || inCombat ? 1.4f : 1.5f;
            if (back) return inCombat ? 1.2f : 1f;
            if (strafe) return inCombat ? 1.3f : 1.2f;
            return 2.8f;
        }

        if (sprint && !walk && move.Y > 0)
        {
            return 6.3f;
        }

        if (inCombat)
        {
            if (walk) return back || strafe ? 1f : 1.6f;
            return back || strafe ? 2.85f : 3.65f;
        }

        if (walk) return back ? 1.2f : 1.3f;
        return back || strafe ? 2.7f : 3.6f;
    }
}
