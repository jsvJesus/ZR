using System.Numerics;
using System.Runtime.InteropServices;
using ZoneReborn.World.Characters.Formats;

namespace ZoneReborn.World.Characters;

public sealed record SkeletonNode(string Name, int Parent, Matrix4x4 Local);
public sealed record CharacterRig(SkeletonNode[] Nodes);
public sealed record CharacterMesh(SourceVertex[] Vertices, int[] Indices, string DiffusePath, CharacterRig Rig, int[] Palette, bool Alpha);
public sealed record CharacterAsset(CharacterMesh[] Meshes, SourceAnimation Animation, float FrameRate);

[StructLayout(LayoutKind.Sequential)]
public readonly record struct SkinnedVertex(Vector3 Position, Vector3 Normal, Vector2 Uv);

public sealed class CharacterPose
{
    private readonly CharacterAsset _asset;
    private readonly Dictionary<CharacterRig, Matrix4x4[]> _rigs;
    public SkinnedVertex[][] Vertices { get; }
    public float FloorOffset { get; }

    public CharacterPose(CharacterAsset asset)
    {
        _asset = asset;
        _rigs = asset.Meshes.Select(m => m.Rig).Distinct().ToDictionary(r => r, r => new Matrix4x4[r.Nodes.Length]);
        Vertices = asset.Meshes.Select(m => new SkinnedVertex[m.Vertices.Length]).ToArray();
        Update(0);
        FloorOffset = -Vertices.SelectMany(v => v).Min(v => v.Position.Y);
    }

    public void Update(double seconds)
    {
        float frame = (float)(seconds * _asset.FrameRate % _asset.Animation.Frames);
        foreach (var (rig, matrices) in _rigs)
        {
            for (int i = 0; i < rig.Nodes.Length; i++)
            {
                var node = rig.Nodes[i];
                var local = _asset.Animation.Channels.TryGetValue(node.Name, out var channel) ? channel.Sample(frame) : node.Local;
                // Exported BlendBone nodes already contain inverse bind transforms.
                matrices[i] = node.Parent < 0 ? local : local * matrices[node.Parent];
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
}
