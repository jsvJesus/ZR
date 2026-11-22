using System.Numerics;

namespace ZoneReborn.World.Characters.Locomotion;

public sealed partial class CharacterLocomotion
{
    public Vector3 ViewRayOrigin(Vector3 cameraPosition, Vector3 lookDirection)
    {
        // SO _get_collide_pos starts the camera ray 0.4m beyond the weapon's
        // projected depth, so a nearby obstruction cannot aim the gun backwards.
        if (!Animator.Pose.TryGetWeaponTrackerPosition(out var localOrigin)) return cameraPosition;
        var gunOrigin = Vector3.Transform(localOrigin, Matrix4x4.CreateRotationY(Yaw))
            + Position + Vector3.UnitY * Animator.Pose.FloorOffset;
        float depth = MathF.Max(0, Vector3.Dot(gunOrigin - cameraPosition, lookDirection) + 0.4f);
        return cameraPosition + lookDirection * depth;
    }

    public void UpdateViewPose(Vector3 target, float firstPersonBlend, float aimBlend)
    {
        var pose = Animator.Pose;
        pose.FirstPersonWeight = firstPersonBlend;
        pose.FirstPersonAimBlend = aimBlend;
        pose.ViewAimTarget = Vector3.Transform(target - Position - Vector3.UnitY * pose.FloorOffset,
            Matrix4x4.CreateRotationY(-Yaw));
        Animator.EvaluatePose();
    }
}
