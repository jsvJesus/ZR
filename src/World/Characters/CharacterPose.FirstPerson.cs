using System.Numerics;
using ZoneReborn.World.Characters.Locomotion;

namespace ZoneReborn.World.Characters;

public sealed partial class CharacterPose
{
    public float FirstPersonWeight { get; set; }
    public float FirstPersonAimBlend { get; set; }
    public float LookYaw { get; set; }
    public float LookPitch { get; set; }

    private Matrix4x4 FirstPersonLookRotation =>
        Matrix4x4.CreateRotationZ(TorsoRoll * LocomotionSettings.FirstPersonRollMultiplier)
        * Matrix4x4.CreateRotationX(-LookPitch) * Matrix4x4.CreateRotationY(LookYaw);

    public bool TryGetFirstPersonEye(float aimBlend, out Vector3 position)
    {
        position = Vector3.Zero;
        if (_asset.AvatarRig is not { } rig || !_rigs.TryGetValue(rig, out var matrices))
        {
            return false;
        }

        int spineIndex = Array.FindIndex(rig.Nodes, node => node.Name == "Spine3");
        if (spineIndex < 0)
        {
            return false;
        }

        // player_speed_settings.FPS_CAMERA. These offsets belong to the
        // animated Spine3 frame, not to world axes or the collision capsule.
        float aim = CanTrackFirstPersonWeapon() && _weaponAttachment?.Weapon?.Sight != null &&
            Weapon is { InHands: true } weapon
            ? Math.Clamp(aimBlend, 0, 1) * weapon.TrackingWeight * (1 - weapon.RightCarryWeight)
            : 0;
        var pivot = Vector3.Lerp(new Vector3(0, 0.1f, 0.28f), new Vector3(-0.05f, 0.1f, 0.28f), aim);
        position = Vector3.Transform(pivot, matrices[spineIndex]);
        var look = FirstPersonLookRotation;
        var offset = Vector3.Lerp(new Vector3(0, 0, -0.045f), new Vector3(0, 0.05f, -0.045f), aim);
        position -= Vector3.TransformNormal(offset, look);

        return float.IsFinite(position.X) && float.IsFinite(position.Y) && float.IsFinite(position.Z);
    }

    private bool CanTrackFirstPersonWeapon() =>
        Weapon?.Relaxed != true && _weaponAttachment?.Weapon is { Legacy: false, Aim: not null };

    private Matrix4x4 TrackFirstPersonWeapon(CharacterWeaponAnchors anchors, Matrix4x4 transform, Vector3 gunPosition)
    {
        float weight = Math.Clamp(FirstPersonWeight, 0, 1);
        if (weight <= 0.001f || !CanTrackFirstPersonWeapon() || anchors.Aim is not { } aim)
        {
            return transform;
        }

        // GunNode.lookAt corrects the direction with the shortest rotation.
        // Preserve animated roll and GUNRW profile offsets instead of replacing
        // the complete orientation. Both hand IKs receive the same rigid target.
        var from = Vector3.TransformNormal(Vector3.UnitZ, aim * transform);
        if (from.LengthSquared() < 0.000001f)
        {
            return transform;
        }

        from = Vector3.Normalize(from);
        var look = FirstPersonLookRotation;
        var to = Vector3.TransformNormal(Vector3.UnitZ, look);
        float cosine = Math.Clamp(Vector3.Dot(from, to), -1, 1);
        var delta = Matrix4x4.Identity;
        if (cosine < 0.999999f)
        {
            var axis = Vector3.Cross(from, to);
            if (axis.LengthSquared() < 0.000001f)
            {
                axis = Vector3.Cross(from, MathF.Abs(from.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX);
            }

            delta = Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(axis), MathF.Acos(cosine) * weight);
        }

        // The original direction tracker rotates HP_GUNR, preserving its origin.
        // HP_GUNRW is a child pivot displaced along the gun; using it here makes
        // pitch/yaw corrections orbit the entire weapon toward/away from the eye.
        return transform * Matrix4x4.CreateTranslation(-gunPosition) * delta * Matrix4x4.CreateTranslation(gunPosition);
    }

    private Matrix4x4 AlignFirstPersonWeaponSight(CharacterWeaponAnchors anchors, Matrix4x4 transform)
    {
        float weight = Math.Clamp(FirstPersonWeight, 0, 1) * Math.Clamp(FirstPersonAimBlend, 0, 1);
        if (weight > 0.001f && CanTrackFirstPersonWeapon() && anchors.Sight is { } sight &&
            TryGetFirstPersonEye(FirstPersonAimBlend, out var eye))
        {
            // GunNode._apply_fps_mode moves the sight target to the skeleton
            // camera before the hand trackers. Account for the final GUNRW
            // profile here without making the camera follow a weapon pivot.
            transform.Translation += (eye - (sight * transform).Translation) * weight;
        }

        return transform;
    }

    private bool TryConstrainFirstPersonAimReach(CharacterRig rig, CharacterWeaponAnchors anchors,
        CharacterWeaponProfile profile, ref Matrix4x4 transform)
    {
        if (FirstPersonWeight <= 0.001f || FirstPersonAimBlend <= 0.001f ||
            !CanTrackFirstPersonWeapon() || anchors.Sight == null ||
            !_handChains.TryGetValue(rig, out var hands))
        {
            return false;
        }

        var look = FirstPersonLookRotation;
        var direction = Vector3.TransformNormal(Vector3.UnitZ, look);
        float minimum = float.NegativeInfinity;
        float maximum = float.PositiveInfinity;
        if (!IntersectHandReach(rig, hands.Right, HandTarget(anchors.RightHand,
                profile.RightHandPosition, profile.RightHandRotation, transform), direction, ref minimum, ref maximum) ||
            (anchors.LeftHand is { } left && !IntersectHandReach(rig, hands.Left, HandTarget(left,
                profile.LeftHandPosition, profile.LeftHandRotation, transform), direction, ref minimum, ref maximum)))
        {
            return false;
        }

        // Moving along the sight line preserves screen-centre alignment while
        // bringing both grips within reach. Do not project the gun sideways.
        transform.Translation += direction * Math.Clamp(0, minimum, maximum);
        return true;
    }

    private bool IntersectHandReach(CharacterRig rig, HandChain? chain, Matrix4x4 target,
        Vector3 direction, ref float minimum, ref float maximum)
    {
        if (chain == null)
        {
            return true;
        }

        var matrices = _rigs[rig];
        var shoulder = matrices[chain.Arm].Translation;
        float reach = Vector3.Distance(shoulder, matrices[chain.Elbow].Translation)
            + Vector3.Distance(matrices[chain.Elbow].Translation, matrices[chain.Hand].Translation) - 0.005f;
        var offset = (chain.InverseAnchor * target).Translation - shoulder;
        float along = Vector3.Dot(offset, direction);
        float radiusSquared = reach * reach - (offset - direction * along).LengthSquared();
        if (reach <= 0 || radiusSquared < 0)
        {
            return false;
        }

        float radius = MathF.Sqrt(radiusSquared);
        minimum = MathF.Max(minimum, -along - radius);
        maximum = MathF.Min(maximum, -along + radius);
        return minimum <= maximum;
    }

    public bool TryGetAnimatedNodePosition(
        string nodeName,
        out Vector3 position)
    {
        position = Vector3.Zero;

        if (_asset.AvatarRig is not { } rig ||
            !_rigs.TryGetValue(rig, out var matrices))
        {
            return false;
        }

        for (int i = 0; i < rig.Nodes.Length; i++)
        {
            if (!string.Equals(
                    rig.Nodes[i].Name,
                    nodeName,
                    StringComparison.Ordinal))
            {
                continue;
            }

            position = matrices[i].Translation;
            return true;
        }

        return false;
    }

    public bool TryGetAnimatedWeaponPivot(
        out Matrix4x4 pivot)
    {
        pivot = Matrix4x4.Identity;

        if (_weaponAttachment?.Weapon is not { } anchors ||
            Weapon?.InHands != true)
        {
            return false;
        }

        pivot = anchors.GunPivot * _weaponTransform;

        return float.IsFinite(pivot.M41) &&
               float.IsFinite(pivot.M42) &&
               float.IsFinite(pivot.M43);
    }
}
