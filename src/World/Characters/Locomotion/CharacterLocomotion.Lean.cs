using System.Numerics;

namespace ZoneReborn.World.Characters.Locomotion;

public sealed partial class CharacterLocomotion
{
    private const float LeanResponse = 12f;
    private const float LeanClearance = 0.15f;

    private void UpdateTorsoRoll(float deltaTime, float input)
    {
        bool allowed = Grounded && !IsLanding && !IsPostureTransition &&
            State is not (AnimState.Sprint or AnimState.JumpWaiting) && !Weapon.Relaxed;
        float target = allowed && float.IsFinite(input)
            ? Math.Clamp(input, -1, 1) * LocomotionSettings.MaximumTorsoRoll
            : 0;
        float angle = TorsoRoll + (target - TorsoRoll) * (1 - MathF.Exp(-LeanResponse * deltaTime));
        if (MathF.Abs(angle) < 0.0001f)
        {
            angle = 0;
        }

        if (angle != 0 && _surface.HasSceneObjects)
        {
            angle = ConstrainTorsoRoll(angle);
        }

        Animator.Pose.TorsoRoll = angle;
    }

    private float ConstrainTorsoRoll(float angle)
    {
        // Evaluate only skeleton matrices for collision probes. The real clip,
        // transition blend and feet/torso yaw stay unchanged; skinning and hand
        // IK run once afterward, using the final permitted angle.
        Animator.Pose.TorsoRoll = 0;
        Animator.EvaluatePose(skin: false);
        if (!TryGetFirstPersonEye(0, out var neutralEye) ||
            !TryGetFirstPersonEye(1, out var neutralAimEye))
        {
            return angle;
        }

        if (IsTorsoRollClear(angle, neutralEye, neutralAimEye))
        {
            return angle;
        }

        float clear = 0;
        float blocked = 1;
        for (int i = 0; i < 6; i++)
        {
            float fraction = (clear + blocked) * 0.5f;
            if (IsTorsoRollClear(angle * fraction, neutralEye, neutralAimEye))
            {
                clear = fraction;
            }
            else
            {
                blocked = fraction;
            }
        }

        return angle * clear;
    }

    private bool IsTorsoRollClear(float angle, Vector3 neutralEye, Vector3 neutralAimEye)
    {
        Animator.Pose.TorsoRoll = angle;
        Animator.EvaluatePose(skin: false);
        return TryGetFirstPersonEye(0, out var eye) &&
            TryGetFirstPersonEye(1, out var aimEye) &&
            IsLeanSweepClear(neutralEye, eye) && IsLeanSweepClear(neutralAimEye, aimEye);
    }

    private bool IsLeanSweepClear(Vector3 origin, Vector3 target)
    {
        var offset = target - origin;
        float distance = offset.Length();
        if (distance < 0.0001f)
        {
            return true;
        }

        // CameraDistance already subtracts 0.15 m from a scene hit. Extend the
        // query by the same clearance to catch walls just beyond the eye target.
        return _surface.CameraDistance(origin, offset / distance, distance + LeanClearance) >= distance;
    }
}
