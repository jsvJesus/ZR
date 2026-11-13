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
}
