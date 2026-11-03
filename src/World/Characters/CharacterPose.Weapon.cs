using System.Numerics;
using ZoneReborn.World.Characters.Locomotion;

namespace ZoneReborn.World.Characters;

public sealed partial class CharacterPose
{
    private Matrix4x4 _weaponTransform = Matrix4x4.Identity;
    private CharacterAttachment? _weaponAttachment;
    private sealed record HandChain(int Arm, int Elbow, int ForearmRoll, int Hand, Matrix4x4 InverseAnchor);
    private readonly Dictionary<CharacterRig, (HandChain? Left, HandChain? Right)> _handChains = [];
    private readonly Dictionary<CharacterRig, int[]> _weaponBoneMaps = [];
    private Matrix4x4[] _avatarBeforeHandIk = [];
    private Matrix4x4[] _avatarHandDeltas = [];

    private void InitializeWeaponPose()
    {
        _weaponAttachment = _asset.Meshes.Select(mesh => mesh.Attachment).FirstOrDefault(a => a?.Weapon != null);
        if (_weaponAttachment == null) return;
        var avatar = _weaponAttachment.AvatarRig;
        _handChains.Add(avatar, (FindHandChain(avatar, "Left", "HP_Lhand"), FindHandChain(avatar, "Right", "HP_Rhand")));
        _avatarBeforeHandIk = new Matrix4x4[avatar.Nodes.Length];
        _avatarHandDeltas = new Matrix4x4[avatar.Nodes.Length];
        var avatarIndices = avatar.Nodes.Select((node, index) => (node.Name, index))
            .ToDictionary(pair => pair.Name, pair => pair.index, StringComparer.Ordinal);
        foreach (var rig in _rigs.Keys.Where(rig => rig.Animate && !ReferenceEquals(rig, avatar)))
        {
            var map = new int[rig.Nodes.Length];
            for (int i = 0; i < map.Length; i++)
            {
                var node = rig.Nodes[i];
                // BlendBone transforms are inverse binds specific to this garment.
                // They receive the correction of their parent bone, not another mesh's bind.
                map[i] = !node.Name.Contains("BlendBone", StringComparison.Ordinal)
                    && avatarIndices.TryGetValue(node.Name, out int index)
                    ? index : node.Parent >= 0 ? map[node.Parent] : -1;
            }
            _weaponBoneMaps.Add(rig, map);
        }
    }

    private static HandChain? FindHandChain(CharacterRig rig, string side, string anchorName)
    {
        string armName = side + "Arm", elbowName = side + "ForeArm", rollName = side + "ForeArmRoll", handName = side + "Hand";
        int arm = Array.FindIndex(rig.Nodes, n => n.Name == armName);
        int elbow = Array.FindIndex(rig.Nodes, n => n.Name == elbowName);
        int roll = Array.FindIndex(rig.Nodes, n => n.Name == rollName);
        int hand = Array.FindIndex(rig.Nodes, n => n.Name == handName);
        int anchor = Array.FindIndex(rig.Nodes, n => n.Name == anchorName);
        if (arm < 0 || elbow < 0 || hand < 0) return null;
        var inverse = Matrix4x4.Identity;
        if (anchor >= 0 && !Matrix4x4.Invert(rig.Nodes[anchor].Local, out inverse))
            throw new InvalidDataException($"Non-invertible avatar hand anchor {anchorName}.");
        return new(arm, elbow, roll, hand, inverse);
    }

    private void ApplyWeaponPose(float feetYaw, float torsoYaw)
    {
        var weapon = Weapon;
        var attachment = _weaponAttachment;
        if (attachment?.Weapon is not { } anchors) return;
        if (weapon?.Profile is not { } profile)
        {
            _weaponTransform = attachment.InverseOrigin * _rigs[attachment.AvatarRig][attachment.AvatarNode];
            return;
        }

        foreach (var (rig, matrices) in _rigs)
        {
            if (!rig.Animate) continue;
            for (int i = 0; i < rig.Nodes.Length; i++)
            {
                var node = rig.Nodes[i];
                var local = _locals[rig][i];
                if (weapon.InHands)
                {
                    // Some original grip clips also rotate ForeArmRoll/Hand.
                    // Release the whole layer when the left hand leaves the gun.
                    local = Layer(rig, i, local, profile.LeftGrip, weapon.LeftGripWeight);
                    local = Layer(rig, i, local, profile.RightGrip, 1);
                }
                if (weapon.Overlay is { } overlay)
                    local = Layer(rig, i, local, overlay, weapon.Frame, weapon.OverlayWeight);
                if (node.Name == "Root") local *= Matrix4x4.CreateRotationY(feetYaw);
                matrices[i] = node.Parent < 0 ? local : local * matrices[node.Parent];

                ApplyUpperBodyAim(
                    ref matrices[i],
                    node.Name,
                    torsoYaw
                );
            }
        }

        var avatar = _rigs[attachment.AvatarRig];
        var holster = attachment.InverseOrigin * avatar[attachment.AvatarNode];
        if (!weapon.InHands) { _weaponTransform = holster; return; }

        var carried = anchors.InverseHand * avatar[anchors.AvatarHand];
        float rightCarry = anchors.Legacy ? 1 : weapon.RightCarryWeight;
        var tracked = carried;
        if (!anchors.Legacy)
        {
            var gunAnchor = avatar[anchors.AvatarGun];
            gunAnchor.Translation += Vector3.TransformNormal(profile.GunPosition, gunAnchor);
            tracked = anchors.InverseGun * gunAnchor;
            // GUNRW is the weapon's authored roll pivot, not its mesh origin.
            if (Matrix4x4.Invert(anchors.GunPivot, out var inversePivot))
                tracked = inversePivot * Matrix4x4.CreateRotationZ(profile.GunRootRoll)
                    * Matrix4x4.CreateTranslation(profile.GunRootPosition) * anchors.GunPivot * tracked;
            tracked = ConstrainWeaponReach(attachment.AvatarRig, anchors, profile, tracked);
        }
        _weaponTransform = Blend(carried, tracked, weapon.TrackingWeight * (1 - rightCarry));

        if (weapon.TrackingWeight <= 0) return;
        // Solve once on the complete avatar. Clothing often ends at ForeArm and
        // has no Hand node, so independently solving each exported rig splits cuffs from wrists.
        avatar.CopyTo(_avatarBeforeHandIk, 0);
        if (rightCarry < 1)
            SolveHand(attachment.AvatarRig, false, HandTarget(anchors.RightHand, profile.RightHandPosition,
                profile.RightHandRotation, _weaponTransform), weapon.TrackingWeight * (1 - rightCarry));
        if (anchors.LeftHand is { } left)
            SolveHand(attachment.AvatarRig, true, HandTarget(left, profile.LeftHandPosition,
                profile.LeftHandRotation, _weaponTransform), weapon.LeftGripWeight);
        SynchronizeClothingHands(attachment.AvatarRig);
    }

    private void SynchronizeClothingHands(CharacterRig avatarRig)
    {
        var avatar = _rigs[avatarRig];
        for (int i = 0; i < avatar.Length; i++)
            _avatarHandDeltas[i] = avatar[i] == _avatarBeforeHandIk[i] ? Matrix4x4.Identity
                : Matrix4x4.Invert(_avatarBeforeHandIk[i], out var inverse) ? inverse * avatar[i] : Matrix4x4.Identity;
        foreach (var (rig, map) in _weaponBoneMaps)
        {
            var matrices = _rigs[rig];
            for (int i = 0; i < matrices.Length; i++)
                if (map[i] >= 0) matrices[i] *= _avatarHandDeltas[map[i]];
        }
    }

    private static Matrix4x4 HandTarget(Matrix4x4 anchor, Vector3 position, Vector3 rotation, Matrix4x4 weaponTransform) =>
        Matrix4x4.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z)
        * Matrix4x4.CreateTranslation(position) * anchor * weaponTransform;

    private Matrix4x4 ConstrainWeaponReach(CharacterRig avatarRig, CharacterWeaponAnchors anchors,
        CharacterWeaponProfile profile, Matrix4x4 transform)
    {
        if (!_handChains.TryGetValue(avatarRig, out var hands)) return transform;
        // The gun target must be reachable by both arms. Clamping only the IK
        // wrist leaves the grip visibly ahead during sprint.
        // Project the rigid weapon onto both reach spheres without stretching bones.
        for (int pass = 0; pass < 12; pass++)
        {
            bool moved = ConstrainHandReach(avatarRig, hands.Right, HandTarget(anchors.RightHand,
                profile.RightHandPosition, profile.RightHandRotation, transform), ref transform);
            if (anchors.LeftHand is { } left)
                moved |= ConstrainHandReach(avatarRig, hands.Left, HandTarget(left,
                    profile.LeftHandPosition, profile.LeftHandRotation, transform), ref transform);
            if (!moved) break;
        }
        return transform;
    }

    private bool ConstrainHandReach(CharacterRig rig, HandChain? chain, Matrix4x4 target, ref Matrix4x4 transform)
    {
        if (chain == null) return false;
        var matrices = _rigs[rig];
        var shoulder = matrices[chain.Arm].Translation;
        float reach = Vector3.Distance(shoulder, matrices[chain.Elbow].Translation)
            + Vector3.Distance(matrices[chain.Elbow].Translation, matrices[chain.Hand].Translation) - 0.005f;
        var offset = (chain.InverseAnchor * target).Translation - shoulder;
        float distance = offset.Length();
        if (distance <= reach || distance < 0.0001f || reach <= 0) return false;
        transform.Translation -= offset * ((distance - reach) / distance);
        return true;
    }

    private Matrix4x4 Layer(CharacterRig rig, int index, Matrix4x4 local, string name, float weight)
    {
        if (name.Length == 0 || _asset.Locomotion is not { } library || !library.Actions.TryGetValue(name, out var action)) return local;
        return Layer(rig, index, local, action, action.Clip.FirstFrame, weight);
    }

    private static Matrix4x4 Layer(CharacterRig rig, int index, Matrix4x4 local,
        CharacterAction action, float frame, float weight)
    {
        if (weight <= 0) return local;
        int priority = 0;
        for (int node = index; node >= 0; node = rig.Nodes[node].Parent)
            if (action.Clip.NodePriorities.TryGetValue(rig.Nodes[node].Name, out priority)) break;
        return priority > 0 && action.Clip.Animation.Channels.TryGetValue(rig.Nodes[index].Name, out var channel)
            ? Blend(local, channel.Sample(frame), weight) : local;
    }

    private void SolveHand(CharacterRig rig, bool left, Matrix4x4 target, float weight)
    {
        if (weight <= 0 || !_handChains.TryGetValue(rig, out var hands)
            || (left ? hands.Left : hands.Right) is not { } chain) return;
        int arm = chain.Arm, elbow = chain.Elbow, hand = chain.Hand;
        target = chain.InverseAnchor * target;
        var matrices = _rigs[rig];
        var shoulderPosition = matrices[arm].Translation;
        var elbowPosition = matrices[elbow].Translation;
        var handPosition = matrices[hand].Translation;
        var wanted = Vector3.Lerp(handPosition, target.Translation, weight);
        var requestedPosition = wanted;
        float upper = Vector3.Distance(shoulderPosition, elbowPosition);
        float lower = Vector3.Distance(elbowPosition, handPosition);
        var direction = wanted - shoulderPosition;
        float distance = direction.Length();
        if (upper < 0.0001f || lower < 0.0001f || distance < 0.0001f) return;
        direction /= distance;
        distance = Math.Clamp(distance, MathF.Abs(upper - lower) + 0.0001f, upper + lower - 0.0001f);
        wanted = shoulderPosition + direction * distance;
        var bend = elbowPosition - shoulderPosition;
        bend -= direction * Vector3.Dot(bend, direction);
        if (bend.LengthSquared() < 0.000001f)
        {
            bend = -Vector3.UnitY - direction * Vector3.Dot(-Vector3.UnitY, direction);
            if (bend.LengthSquared() < 0.000001f) bend = Vector3.Cross(direction, Vector3.UnitX);
        }
        bend = Vector3.Normalize(bend);
        float along = (upper * upper - lower * lower + distance * distance) / (2 * distance);
        float height = MathF.Sqrt(Math.Max(0, upper * upper - along * along));
        var wantedElbow = shoulderPosition + direction * along + bend * height;
        RotateSubtree(rig, arm, elbowPosition - shoulderPosition, wantedElbow - shoulderPosition);
        RotateSubtree(rig, elbow, matrices[hand].Translation - matrices[elbow].Translation,
            wanted - matrices[elbow].Translation);

        // AvatarTrackers limits wrist rotation when the grip is over 3 cm away.
        // An unreachable target must not wrench the wrist towards a distant gun.
        if (Vector3.DistanceSquared(matrices[hand].Translation, requestedPosition) > 0.03f * 0.03f) return;
        // Match wrist orientation without stretching the limb past its reachable target.
        var handTarget = Blend(matrices[hand], target, weight);
        handTarget.Translation = matrices[hand].Translation;
        DistributeForearmRoll(rig, chain, handTarget, left ? 0.5f : 1);
        handTarget.Translation = matrices[hand].Translation;
        if (Matrix4x4.Invert(matrices[hand], out var inverseHand))
            TransformSubtree(rig, hand, inverseHand * handTarget);
    }

    private void DistributeForearmRoll(CharacterRig rig, HandChain chain, Matrix4x4 target, float amount)
    {
        if (chain.ForearmRoll < 0) return;
        var matrices = _rigs[rig];
        var pivot = matrices[chain.ForearmRoll].Translation;
        var axis = matrices[chain.Hand].Translation - pivot;
        if (axis.LengthSquared() < 0.000001f || !Matrix4x4.Invert(matrices[chain.Hand], out var inverse)) return;
        if (!Matrix4x4.Decompose(inverse * target, out _, out var rotation, out _)) return;
        axis = Vector3.Normalize(axis);
        var twistVector = axis * Vector3.Dot(new Vector3(rotation.X, rotation.Y, rotation.Z), axis);
        var twist = new Quaternion(twistVector, rotation.W);
        if (twist.LengthSquared() < 0.000001f) return;
        twist = Quaternion.Normalize(twist);
        var roll = Matrix4x4.CreateFromQuaternion(Quaternion.Slerp(Quaternion.Identity, twist, amount));
        // Rotate around Roll -> Hand, retaining both joint positions. The SDK
        // uses node2roll_weight 0.5 on the left and 1 on the right.
        TransformSubtree(rig, chain.ForearmRoll,
            Matrix4x4.CreateTranslation(-pivot) * roll * Matrix4x4.CreateTranslation(pivot));
    }

    private void RotateSubtree(CharacterRig rig, int root, Vector3 from, Vector3 to)
    {
        if (from.LengthSquared() < 0.000001f || to.LengthSquared() < 0.000001f) return;
        from = Vector3.Normalize(from); to = Vector3.Normalize(to);
        float cosine = Math.Clamp(Vector3.Dot(from, to), -1, 1);
        if (cosine > 0.999999f) return;
        var axis = Vector3.Cross(from, to);
        if (axis.LengthSquared() < 0.000001f)
            axis = Vector3.Cross(from, MathF.Abs(from.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX);
        var pivot = _rigs[rig][root].Translation;
        var rotation = Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(axis), MathF.Acos(cosine));
        TransformSubtree(rig, root, Matrix4x4.CreateTranslation(-pivot) * rotation * Matrix4x4.CreateTranslation(pivot));
    }

    private void TransformSubtree(CharacterRig rig, int root, Matrix4x4 delta)
    {
        var matrices = _rigs[rig];
        for (int i = root; i < rig.Nodes.Length; i++)
        {
            int ancestor = i;
            while (ancestor > root) ancestor = rig.Nodes[ancestor].Parent;
            if (ancestor == root) matrices[i] *= delta;
        }
    }
}
