using System.Numerics;

namespace ZoneReborn.World.Characters;

public sealed partial class CharacterPose
{
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
