using System.Numerics;
using ZoneReborn.World.Characters.Locomotion;

namespace ZoneReborn.World.Characters;

public sealed partial class CharacterPose
{
    private const float FirstPersonSightClearance = 0.10f;

    public float FirstPersonWeight { get; set; }
    public float FirstPersonAimBlend { get; set; }
    public float LookYaw { get; set; }
    public float LookPitch { get; set; }

    private Matrix4x4 FirstPersonLookRotation =>
        Matrix4x4.CreateRotationZ(
            TorsoRoll * LocomotionSettings.FirstPersonRollMultiplier
        ) *
        Matrix4x4.CreateRotationX(-LookPitch) *
        Matrix4x4.CreateRotationY(LookYaw);

    public bool TryGetFirstPersonEye(
        float aimBlend,
        out Vector3 position)
    {
        position = Vector3.Zero;

        if (_asset.AvatarRig is not { } rig ||
            !_rigs.TryGetValue(rig, out var matrices))
        {
            return false;
        }

        int spineIndex = Array.FindIndex(
            rig.Nodes,
            node => node.Name == "Spine3"
        );

        if (spineIndex < 0)
        {
            return false;
        }

        float aim = 0f;

        if (CanTrackFirstPersonWeapon() &&
            _weaponAttachment?.Weapon?.Sight != null &&
            Weapon is { InHands: true } weapon)
        {
            aim = Math.Clamp(aimBlend, 0f, 1f) *
                  weapon.TrackingWeight *
                  (1f - weapon.RightCarryWeight);
        }

        Vector3 normalPivot = new(0f, 0.10f, 0.28f);
        Vector3 aimingPivot = new(-0.05f, 0.10f, 0.28f);

        Vector3 pivot = Vector3.Lerp(
            normalPivot,
            aimingPivot,
            aim
        );

        position = Vector3.Transform(
            pivot,
            matrices[spineIndex]
        );

        Vector3 normalOffset = new(0f, 0f, -0.045f);
        Vector3 aimingOffset = new(0f, 0.05f, -0.045f);

        Vector3 offset = Vector3.Lerp(
            normalOffset,
            aimingOffset,
            aim
        );

        position -= Vector3.TransformNormal(
            offset,
            FirstPersonLookRotation
        );

        return float.IsFinite(position.X) &&
               float.IsFinite(position.Y) &&
               float.IsFinite(position.Z);
    }

    private bool CanTrackFirstPersonWeapon()
    {
        return Weapon is { Relaxed: false, InHands: true } &&
               _weaponAttachment?.Weapon is
               {
                   Legacy: false,
                   Aim: not null
               };
    }

    private Matrix4x4 AlignFirstPersonWeaponSight(
        CharacterWeaponAnchors anchors,
        Matrix4x4 transform)
    {
        float weight =
            Math.Clamp(FirstPersonWeight, 0f, 1f) *
            Math.Clamp(FirstPersonAimBlend, 0f, 1f);

        if (weight <= 0.001f ||
            !CanTrackFirstPersonWeapon() ||
            anchors.Sight is not { } sight ||
            !TryGetFirstPersonEye(
                FirstPersonAimBlend,
                out Vector3 eye))
        {
            return transform;
        }

        Vector3 direction = Vector3.TransformNormal(
            Vector3.UnitZ,
            FirstPersonLookRotation
        );

        if (direction.LengthSquared() < 0.000001f)
        {
            return transform;
        }

        direction = Vector3.Normalize(direction);

        Vector3 target = eye +
                         direction * FirstPersonSightClearance;

        Vector3 sightPosition = (
            sight * transform
        ).Translation;

        Vector3 correction = target - sightPosition;

        if (!float.IsFinite(correction.X) ||
            !float.IsFinite(correction.Y) ||
            !float.IsFinite(correction.Z))
        {
            return transform;
        }

        transform.Translation += correction * weight;

        return transform;
    }

    private bool TryConstrainFirstPersonAimReach(
        CharacterRig rig,
        CharacterWeaponAnchors anchors,
        CharacterWeaponProfile profile,
        ref Matrix4x4 transform)
    {
        if (FirstPersonWeight <= 0.001f ||
            FirstPersonAimBlend <= 0.001f ||
            !CanTrackFirstPersonWeapon() ||
            anchors.Sight == null ||
            !_handChains.TryGetValue(rig, out var hands))
        {
            return false;
        }

        Vector3 direction = Vector3.TransformNormal(
            Vector3.UnitZ,
            FirstPersonLookRotation
        );

        if (direction.LengthSquared() < 0.000001f)
        {
            return true;
        }

        direction = Vector3.Normalize(direction);

        float minimum = float.NegativeInfinity;
        float maximum = float.PositiveInfinity;

        Matrix4x4 rightTarget = HandTarget(
            anchors.RightHand,
            profile.RightHandPosition,
            profile.RightHandRotation,
            transform
        );

        if (!IntersectHandReach(
                rig,
                hands.Right,
                rightTarget,
                direction,
                ref minimum,
                ref maximum))
        {
            return true;
        }

        float correction = Math.Clamp(0f, minimum, maximum);
        if (Weapon is { LeftGripWeight: > 0 } weapon && anchors.LeftHand is { } left)
        {
            Matrix4x4 leftTarget = HandTarget(
                left,
                profile.LeftHandPosition,
                profile.LeftHandRotation,
                transform
            );

            if (IntersectHandReach(
                    rig,
                    hands.Left,
                    leftTarget,
                    direction,
                    ref minimum,
                    ref maximum))
            {
                float bothHands = Math.Clamp(0f, minimum, maximum);
                correction += (bothHands - correction) * weapon.LeftGripWeight;
            }
        }

        if (float.IsFinite(correction))
        {
            transform.Translation +=
                direction * correction;
        }

        return true;
    }

    private bool IntersectHandReach(
        CharacterRig rig,
        HandChain? chain,
        Matrix4x4 target,
        Vector3 direction,
        ref float minimum,
        ref float maximum)
    {
        if (chain == null)
        {
            return true;
        }

        Matrix4x4[] matrices = _rigs[rig];

        Vector3 shoulder = matrices[chain.Arm].Translation;

        float upperArm = Vector3.Distance(
            shoulder,
            matrices[chain.Elbow].Translation
        );

        float forearm = Vector3.Distance(
            matrices[chain.Elbow].Translation,
            matrices[chain.Hand].Translation
        );

        float reach = upperArm + forearm - 0.005f;

        if (reach <= 0f)
        {
            return false;
        }

        Vector3 offset = (
            chain.InverseAnchor * target
        ).Translation - shoulder;

        float along = Vector3.Dot(
            offset,
            direction
        );

        Vector3 sideways = offset - direction * along;

        float radiusSquared =
            reach * reach - sideways.LengthSquared();

        if (radiusSquared < 0f)
        {
            return false;
        }

        float radius = MathF.Sqrt(radiusSquared);

        minimum = MathF.Max(
            minimum,
            -along - radius
        );

        maximum = MathF.Min(
            maximum,
            -along + radius
        );

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
