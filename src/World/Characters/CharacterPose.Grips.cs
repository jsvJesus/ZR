using System.Numerics;
using ZoneReborn.World.Characters.Locomotion;

namespace ZoneReborn.World.Characters;

public sealed partial class CharacterPose
{
    private readonly GripLayer _leftGrip = new();
    private readonly GripLayer _rightGrip = new();

    public void AdvanceWeaponLayers(float deltaTime)
    {
        if (_asset.Locomotion is not { } library) return;
        _leftGrip.Advance(Weapon?.Profile?.LeftGrip ?? "", library, deltaTime);
        _rightGrip.Advance(Weapon?.Profile?.RightGrip ?? "", library, deltaTime);
    }

    // A grip is a separate original track (10/11), with its own priorities and
    // blend times. Save the evaluated layer, including a partially faded grip,
    // so reversing bored/combat does not restart from an old finger pose.
    private sealed class GripLayer
    {
        private readonly record struct GripNode(Matrix4x4 Transform, float Weight);
        private readonly Dictionary<CharacterRig, GripNode[]> _current = [];
        private readonly Dictionary<CharacterRig, GripNode[]> _from = [];
        private CharacterAction? _action;
        private string _name = "";
        private float _time;
        private float _duration = 0.001f;

        public void Advance(string name, LocomotionLibrary library, float deltaTime)
        {
            if (name != _name)
            {
                CharacterAction? next = null;
                if (name.Length != 0 && !library.Actions.TryGetValue(name, out next))
                    throw new InvalidDataException($"Missing original hand grip action {name}.");
                foreach (var (rig, pose) in _current) _from[rig] = (GripNode[])pose.Clone();
                _duration = MathF.Max(0.001f, MathF.Max(_action?.BlendOut ?? 0, next?.BlendIn ?? 0));
                _action = next;
                _name = name;
                _time = 0;
            }
            _time += deltaTime;
        }

        public Matrix4x4 Apply(CharacterRig rig, int index, Matrix4x4 local, float weight)
        {
            if (!_current.TryGetValue(rig, out var pose))
                _current.Add(rig, pose = new GripNode[rig.Nodes.Length]);

            var from = _from.TryGetValue(rig, out var previous) ? previous[index] : default;
            float blend = Math.Clamp(_time / _duration, 0, 1);
            bool hasTarget = _action != null && LayerPriority(rig, index, _action) > 0 &&
                _action.Clip.Animation.Channels.ContainsKey(rig.Nodes[index].Name);
            float targetWeight = hasTarget ? 1f : 0f;
            float layerWeight = from.Weight * (1 - blend) + targetWeight * blend;
            var transform = hasTarget
                ? _action!.Clip.Animation.Channels[rig.Nodes[index].Name].Sample(_action.Clip.FirstFrame)
                : from.Transform;
            if (from.Weight > 0 && hasTarget && layerWeight > 0)
                transform = Blend(from.Transform, transform, blend / layerWeight);
            pose[index] = new(transform, layerWeight);
            return layerWeight * weight > 0 ? Blend(local, transform, layerWeight * weight) : local;
        }
    }

    private static int LayerPriority(CharacterRig rig, int index, CharacterAction action)
    {
        for (int node = index; node >= 0; node = rig.Nodes[node].Parent)
            if (action.Clip.NodePriorities.TryGetValue(rig.Nodes[node].Name, out int priority)) return priority;
        return 0;
    }
}
