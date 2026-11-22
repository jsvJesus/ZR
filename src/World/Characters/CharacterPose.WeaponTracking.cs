using System.Numerics;

namespace ZoneReborn.World.Characters;

public sealed partial class CharacterPose
{
    public Vector3? ViewAimTarget { get; set; }

    public bool TryGetWeaponTrackerPosition(out Vector3 position)
    {
        position = default;
        if (_weaponAttachment?.Weapon is not { } anchors || Weapon?.Profile is not { } profile) return false;
        var gun = _rigs[_weaponAttachment.AvatarRig][anchors.AvatarGun];
        position = gun.Translation + Vector3.TransformNormal(profile.GunPosition, gun);
        return true;
    }

    private Matrix4x4 TrackWeaponDirection(CharacterRig rig, CharacterWeaponAnchors anchors,
        CharacterWeaponProfile profile, Matrix4x4 gun)
    {
        var desired = Vector3.TransformNormal(Vector3.UnitZ, FirstPersonLookRotation);
        if (ViewAimTarget is { } target && Weapon?.Relaxed == false)
        {
            // _calcFixlookDir retains the sight system's transverse offset
            // relative to HP_GUNR, rather than treating the tracker as a muzzle.
            var offsetPosition = anchors.Aim is { } aim ? (aim * anchors.InverseGun).Translation : Vector3.Zero;
            offsetPosition.Z = 0;
            var offset = target - Vector3.Transform(offsetPosition, gun);
            if (offset.LengthSquared() > 0.000001f)
            {
                // ADS uses the sight/eye ray directly; TPS converges towards
                // the same point that projects under the white camera dot.
                desired = Vector3.Lerp(Vector3.Normalize(offset), desired, Math.Clamp(FirstPersonWeight, 0, 1));
            }
        }

        desired = LimitWeaponDirection(rig, profile, desired);
        var forward = Vector3.TransformNormal(Vector3.UnitZ, gun);
        return RotateWeaponDirection(gun, forward, desired, gun.Translation, 1);
    }

    private Vector3 LimitWeaponDirection(CharacterRig rig, CharacterWeaponProfile profile, Vector3 desired)
    {
        int limitIndex = Array.FindIndex(rig.Nodes, node => node.Name == "HP_GUNR_LIMIT");
        if (limitIndex < 0 || !Matrix4x4.Invert(_rigs[rig][limitIndex], out var inverseLimit)) return desired;
        // Clamp in the animated HP_GUNR_LIMIT space, before GUNRW child roll.
        var local = Vector3.TransformNormal(desired, inverseLimit);
        float yaw = Math.Clamp(MathF.Atan2(local.X, local.Z), profile.GunYawLimits.X, profile.GunYawLimits.Y);
        float pitch = Math.Clamp(MathF.Atan2(-local.Y, new Vector2(local.X, local.Z).Length()),
            profile.GunPitchLimits.X, profile.GunPitchLimits.Y);
        local = new(MathF.Sin(yaw) * MathF.Cos(pitch), -MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch));
        return Vector3.Normalize(Vector3.TransformNormal(local, _rigs[rig][limitIndex]));
    }

    private Matrix4x4 ConvergeThirdPersonWeapon(CharacterRig rig, CharacterWeaponAnchors anchors,
        CharacterWeaponProfile profile, Matrix4x4 transform, Vector3 pivot)
    {
        float weight = 1 - Math.Clamp(FirstPersonWeight, 0, 1);
        if (weight <= 0 || Weapon?.Relaxed != false || anchors.Aim is not { } aim || ViewAimTarget is not { } target)
            return transform;
        // GUNRW and reach correction move HP_AIMSYS after the tracker rotates.
        // Re-evaluate its actual ray while retaining authored limits and arm reach.
        for (int pass = 0; pass < 3; pass++)
        {
            var aimTransform = aim * transform;
            var desired = target - aimTransform.Translation;
            if (desired.LengthSquared() < 0.000001f) break;
            desired = LimitWeaponDirection(rig, profile, Vector3.Normalize(desired));
            transform = RotateWeaponDirection(transform, Vector3.TransformNormal(Vector3.UnitZ, aimTransform),
                desired, pivot, weight);
            transform = ConstrainWeaponReach(rig, anchors, profile, transform);
        }
        return transform;
    }

    private static Matrix4x4 RotateWeaponDirection(Matrix4x4 transform, Vector3 forward, Vector3 desired, Vector3 pivot, float weight)
    {
        forward = Vector3.Normalize(forward);
        desired = Vector3.Normalize(desired);
        float cosine = Math.Clamp(Vector3.Dot(forward, desired), -1, 1);
        if (cosine > 0.999999f) return transform;
        var axis = Vector3.Cross(forward, desired);
        if (axis.LengthSquared() < 0.000001f)
            axis = Vector3.Cross(forward, MathF.Abs(forward.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX);
        return transform * Matrix4x4.CreateTranslation(-pivot)
            * Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(axis), MathF.Acos(cosine) * weight)
            * Matrix4x4.CreateTranslation(pivot);
    }
}
