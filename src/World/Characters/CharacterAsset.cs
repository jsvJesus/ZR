using System.Numerics;
using System.Runtime.InteropServices;
using ZoneReborn.World.Characters.Formats;
using ZoneReborn.World.Characters.Locomotion;

namespace ZoneReborn.World.Characters;

public sealed record SkeletonNode(string Name, int Parent, Matrix4x4 Local);
public sealed record CharacterRig(SkeletonNode[] Nodes);
public sealed record CharacterMesh(SourceVertex[] Vertices, int[] Indices, string DiffusePath, CharacterRig Rig, int[] Palette, bool Alpha);
public sealed record CharacterAsset(CharacterMesh[] Meshes, SourceAnimation Animation, float FrameRate)
{
    public LocomotionLibrary? Locomotion { get; init; }
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct SkinnedVertex(Vector3 Position, Vector3 Normal, Vector2 Uv);

public sealed class CharacterPose
{
    private readonly CharacterAsset _asset;
    private readonly Dictionary<CharacterRig, Matrix4x4[]> _rigs;
    private readonly Dictionary<CharacterRig, Matrix4x4[]> _locals;
    private readonly Dictionary<CharacterRig, Matrix4x4[]> _blendFrom;
    public SkinnedVertex[][] Vertices { get; }
    public float FloorOffset { get; }

    public CharacterPose(CharacterAsset asset)
    {
        _asset = asset;
        _rigs = asset.Meshes.Select(m => m.Rig).Distinct().ToDictionary(r => r, r => new Matrix4x4[r.Nodes.Length]);
        _locals = _rigs.Keys.ToDictionary(r => r, r => new Matrix4x4[r.Nodes.Length]);
        _blendFrom = _rigs.Keys.ToDictionary(r => r, r => new Matrix4x4[r.Nodes.Length]);
        Vertices = asset.Meshes.Select(m => new SkinnedVertex[m.Vertices.Length]).ToArray();
        Update(0);
        FloorOffset = -Vertices.SelectMany(v => v).Min(v => v.Position.Y);
    }

    public void Update(double seconds)
    {
        float frame = (float)(seconds * _asset.FrameRate % _asset.Animation.Frames);
        Evaluate(_asset.Animation, frame, null, 1);
    }

    public void BeginBlend()
    {
        foreach (var rig in _rigs.Keys) _locals[rig].CopyTo(_blendFrom[rig], 0);
    }

    public void Apply(CharacterAction action, float frame, float blend, float feetYaw = 0, float torsoYaw = 0) =>
        Evaluate(action.Clip.Animation, frame, action, blend, feetYaw, torsoYaw);

    private void Evaluate(SourceAnimation animation, float frame, CharacterAction? action, float blend, float feetYaw = 0, float torsoYaw = 0)
    {
        foreach (var (rig, matrices) in _rigs)
        {
            for (int i = 0; i < rig.Nodes.Length; i++)
            {
                var node = rig.Nodes[i];
                var local = animation.Channels.TryGetValue(node.Name, out var channel) ? channel.Sample(frame) : node.Local;
                if (node.Name == "Root" && action?.ExtractRoot == true)
                {
                    // The controller consumes this motion in world space, once.
                    local.Translation = Vector3.Zero;
                    if (action.Turn)
                    {
                        float yaw = MathF.IEEERemainder(action.Clip.RootYaw(frame) - action.Clip.RootYaw(action.Clip.FirstFrame), MathF.Tau);
                        local *= Matrix4x4.CreateRotationY(-yaw);
                    }
                }
                if (blend < 1) local = Blend(_blendFrom[rig][i], local, blend);
                _locals[rig][i] = local;
                if (node.Name == "Root") local *= Matrix4x4.CreateRotationY(feetYaw);
                // Exported BlendBone nodes already contain inverse bind transforms.
                matrices[i] = node.Parent < 0 ? local : local * matrices[node.Parent];
                if (node.Name == "Spine2" && torsoYaw != 0)
                {
                    var pivot = matrices[i].Translation;
                    matrices[i] *= Matrix4x4.CreateRotationY(torsoYaw);
                    matrices[i].Translation = pivot;
                }
            }
        }
        for (int meshIndex = 0; meshIndex < _asset.Meshes.Length; meshIndex++)
        {
            var mesh = _asset.Meshes[meshIndex];
            var matrices = _rigs[mesh.Rig];
            var target = Vertices[meshIndex];
            for (int i = 0; i < target.Length; i++)
            {
                var v = mesh.Vertices[i];
                var a = matrices[mesh.Palette[v.Bone0]];
                var b = matrices[mesh.Palette[v.Bone1]];
                var c = matrices[mesh.Palette[v.Bone2]];
                var position = Vector3.Transform(v.Position, a) * v.Weights.X
                    + Vector3.Transform(v.Position, b) * v.Weights.Y + Vector3.Transform(v.Position, c) * v.Weights.Z;
                var normal = Vector3.TransformNormal(v.Normal, a) * v.Weights.X
                    + Vector3.TransformNormal(v.Normal, b) * v.Weights.Y + Vector3.TransformNormal(v.Normal, c) * v.Weights.Z;
                target[i] = new(position, normal.LengthSquared() > 0.00001f ? Vector3.Normalize(normal) : Vector3.UnitY, v.Uv);
            }
        }
    }

    private static Matrix4x4 Blend(Matrix4x4 from, Matrix4x4 to, float amount)
    {
        if (from == to) return to;
        if (!Matrix4x4.Decompose(from, out var fromScale, out var fromRotation, out var fromPosition)
            || !Matrix4x4.Decompose(to, out var toScale, out var toRotation, out var toPosition))
            return Matrix4x4.Lerp(from, to, amount);
        return Matrix4x4.CreateScale(Vector3.Lerp(fromScale, toScale, amount))
            * Matrix4x4.CreateFromQuaternion(Quaternion.Slerp(fromRotation, toRotation, amount))
            * Matrix4x4.CreateTranslation(Vector3.Lerp(fromPosition, toPosition, amount));
    }
}
