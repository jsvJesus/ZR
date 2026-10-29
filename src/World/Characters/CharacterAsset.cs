using System.Numerics;
using System.Runtime.InteropServices;
using ZoneReborn.World.Characters.Formats;
using ZoneReborn.World.Characters.Locomotion;

namespace ZoneReborn.World.Characters;

public sealed record SkeletonNode(
    string Name,
    int Parent,
    Matrix4x4 Local
);

public sealed record CharacterRig(SkeletonNode[] Nodes);

public sealed record CharacterMesh(
    SourceVertex[] Vertices,
    int[] Indices,
    string DiffusePath,
    CharacterRig Rig,
    int[] Palette,
    bool Alpha
);

public sealed record CharacterAsset(
    CharacterMesh[] Meshes,
    SourceAnimation Animation,
    float FrameRate)
{
    public LocomotionLibrary? Locomotion { get; init; }
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct SkinnedVertex(
    Vector3 Position,
    Vector3 Normal,
    Vector2 Uv
);

public sealed class CharacterPose
{
    private readonly CharacterAsset _asset;
    private readonly Dictionary<CharacterRig, Matrix4x4[]> _rigs;
    private readonly Dictionary<CharacterRig, Matrix4x4[]> _locals;
    private readonly Dictionary<CharacterRig, Matrix4x4[]> _blendFrom;
    private readonly Vector3 _rootReference;

    public SkinnedVertex[][] Vertices { get; }
    public float FloorOffset { get; }

    public CharacterPose(CharacterAsset asset)
    {
        _asset = asset;

        _rigs = asset.Meshes
            .Select(m => m.Rig)
            .Distinct()
            .ToDictionary(
                rig => rig,
                rig => new Matrix4x4[rig.Nodes.Length]
            );

        _locals = _rigs.Keys.ToDictionary(
            rig => rig,
            rig => new Matrix4x4[rig.Nodes.Length]
        );

        _blendFrom = _rigs.Keys.ToDictionary(
            rig => rig,
            rig => new Matrix4x4[rig.Nodes.Length]
        );

        _rootReference = asset.Animation.Channels.TryGetValue(
            "Root",
            out var root)
                ? root.SamplePosition(0)
                : Vector3.Zero;

        Vertices = asset.Meshes
            .Select(mesh => new SkinnedVertex[mesh.Vertices.Length])
            .ToArray();

        Update(0);

        FloorOffset = -Vertices
            .SelectMany(vertices => vertices)
            .Min(vertex => vertex.Position.Y);
    }

    public void Update(double seconds)
    {
        float frame = (float)(
            seconds * _asset.FrameRate %
            _asset.Animation.Frames
        );

        Evaluate(
            _asset.Animation,
            frame,
            null,
            1
        );
    }

    public void BeginBlend()
    {
        foreach (var rig in _rigs.Keys)
        {
            _locals[rig].CopyTo(
                _blendFrom[rig],
                0
            );
        }
    }

    public void Apply(
        CharacterAction action,
        float frame,
        float blend,
        float feetYaw = 0,
        float torsoYaw = 0)
    {
        Evaluate(
            action.Clip.Animation,
            frame,
            action,
            blend,
            feetYaw,
            torsoYaw
        );
    }

    private void Evaluate(
        SourceAnimation animation,
        float frame,
        CharacterAction? action,
        float blend,
        float feetYaw = 0,
        float torsoYaw = 0)
    {
        foreach (var (rig, matrices) in _rigs)
        {
            for (int i = 0; i < rig.Nodes.Length; i++)
            {
                var node = rig.Nodes[i];

                var local = animation.Channels.TryGetValue(
                    node.Name,
                    out var channel)
                        ? channel.Sample(frame)
                        : node.Local;

                if (node.Name == "Root" &&
                    action?.ExtractRoot == true)
                {
                    // Root translation has already been consumed
                    // by the movement or jump controller.
                    // Keep the original skeleton reference.
                    local.Translation = _rootReference;

                    if (action.Turn)
                    {
                        float yaw = MathF.IEEERemainder(
                            action.Clip.RootYaw(frame) -
                            action.Clip.RootYaw(
                                action.Clip.FirstFrame
                            ),
                            MathF.Tau
                        );

                        local *= Matrix4x4.CreateRotationY(-yaw);
                    }
                }

                if (blend < 1)
                {
                    local = Blend(
                        _blendFrom[rig][i],
                        local,
                        blend
                    );
                }

                _locals[rig][i] = local;

                if (node.Name == "Root")
                {
                    local *= Matrix4x4.CreateRotationY(feetYaw);
                }

                // Exported BlendBone nodes contain
                // inverse bind transforms.
                matrices[i] = node.Parent < 0
                    ? local
                    : local * matrices[node.Parent];

                if (node.Name == "Spine2" && torsoYaw != 0)
                {
                    var pivot = matrices[i].Translation;

                    matrices[i] *= Matrix4x4.CreateRotationY(
                        torsoYaw
                    );

                    matrices[i].Translation = pivot;
                }
            }
        }

        for (int meshIndex = 0;
             meshIndex < _asset.Meshes.Length;
             meshIndex++)
        {
            var mesh = _asset.Meshes[meshIndex];

            var matrices = _rigs[mesh.Rig];
            var target = Vertices[meshIndex];

            for (int i = 0; i < target.Length; i++)
            {
                var vertex = mesh.Vertices[i];

                var a = matrices[
                    mesh.Palette[vertex.Bone0]
                ];

                var b = matrices[
                    mesh.Palette[vertex.Bone1]
                ];

                var c = matrices[
                    mesh.Palette[vertex.Bone2]
                ];

                var position =
                    Vector3.Transform(vertex.Position, a) *
                    vertex.Weights.X +
                    Vector3.Transform(vertex.Position, b) *
                    vertex.Weights.Y +
                    Vector3.Transform(vertex.Position, c) *
                    vertex.Weights.Z;

                var normal =
                    Vector3.TransformNormal(vertex.Normal, a) *
                    vertex.Weights.X +
                    Vector3.TransformNormal(vertex.Normal, b) *
                    vertex.Weights.Y +
                    Vector3.TransformNormal(vertex.Normal, c) *
                    vertex.Weights.Z;

                target[i] = new SkinnedVertex(
                    position,
                    normal.LengthSquared() > 0.00001f
                        ? Vector3.Normalize(normal)
                        : Vector3.UnitY,
                    vertex.Uv
                );
            }
        }
    }

    private static Matrix4x4 Blend(
        Matrix4x4 from,
        Matrix4x4 to,
        float amount)
    {
        if (from == to)
        {
            return to;
        }

        if (!Matrix4x4.Decompose(
                from,
                out var fromScale,
                out var fromRotation,
                out var fromPosition) ||
            !Matrix4x4.Decompose(
                to,
                out var toScale,
                out var toRotation,
                out var toPosition))
        {
            return Matrix4x4.Lerp(
                from,
                to,
                amount
            );
        }

        var scale = Vector3.Lerp(
            fromScale,
            toScale,
            amount
        );

        var rotation = Quaternion.Slerp(
            fromRotation,
            toRotation,
            amount
        );

        var position = Vector3.Lerp(
            fromPosition,
            toPosition,
            amount
        );

        return Matrix4x4.CreateScale(scale) *
               Matrix4x4.CreateFromQuaternion(rotation) *
               Matrix4x4.CreateTranslation(position);
    }
}
