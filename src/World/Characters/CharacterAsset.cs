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

public sealed record CharacterRig(SkeletonNode[] Nodes)
{
    public bool Animate { get; init; } = true;
}

public sealed record CharacterAttachment(CharacterRig AvatarRig, int AvatarNode, Matrix4x4 InverseOrigin)
{
    public CharacterWeaponAnchors? Weapon { get; init; }
}

public sealed record CharacterWeaponAnchors(int AvatarHand, int AvatarGun,
    Matrix4x4 InverseHand, Matrix4x4 InverseGun, Matrix4x4 GunPivot,
    Matrix4x4 RightHand, Matrix4x4? LeftHand, bool Legacy)
{
    public Matrix4x4? Aim { get; init; }
    public Matrix4x4? Sight { get; init; }
    public Matrix4x4? Barrel { get; init; }
}

public sealed record CharacterMesh(
    SourceVertex[] Vertices,
    int[] Indices,
    string DiffusePath,
    CharacterRig Rig,
    int[] Palette,
    bool Alpha
)
{
    public CharacterAttachment? Attachment { get; init; }
    public bool FirstPersonVisible { get; init; } = true;
    public Vector4 DiffuseColor { get; init; } = Vector4.One;
    public CharacterSurfaceMaterial Surface { get; init; } = new();
}

public sealed record CharacterAsset(
    CharacterMesh[] Meshes,
    SourceAnimation Animation,
    float FrameRate)
{
    public LocomotionLibrary? Locomotion { get; init; }
    public CharacterRig? AvatarRig { get; init; }
    public CharacterWeaponDefinition? Weapon { get; init; }
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct SkinnedVertex(
    Vector3 Position,
    Vector3 Normal,
    Vector2 Uv,
    Vector2 Uv2
);

public sealed partial class CharacterPose
{
    private readonly CharacterAsset _asset;
    private readonly Dictionary<CharacterRig, Matrix4x4[]> _rigs;
    private readonly Dictionary<CharacterRig, Matrix4x4[]> _locals;
    private readonly Dictionary<CharacterRig, Matrix4x4[]> _blendFrom;
    private readonly Dictionary<CharacterRig, int[]> _priorities;
    private readonly Vector3 _rootReference;

    public SkinnedVertex[][] Vertices { get; }
    public float FloorOffset { get; }
    public CharacterWeaponController? Weapon { get; set; }

    public CharacterPose(CharacterAsset asset)
    {
        _asset = asset;

        _rigs = asset.Meshes
            .Select(m => m.Rig)
            .Concat(asset.AvatarRig is { } avatarRig ? new[] { avatarRig } : Array.Empty<CharacterRig>())
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

        _priorities = _rigs.Keys.ToDictionary(
            rig => rig,
            rig => new int[rig.Nodes.Length]
        );

        _rootReference = asset.Animation.Channels.TryGetValue(
            "Root",
            out var root)
                ? root.SamplePosition(0)
                : Vector3.Zero;

        InitializeWeaponPose();

        Vertices = asset.Meshes
            .Select(mesh => new SkinnedVertex[mesh.Vertices.Length])
            .ToArray();

        Update(0);

        // Attached equipment must never move the avatar's ground reference.
        FloorOffset = -Enumerable.Range(0, asset.Meshes.Length)
            .Where(index => asset.Meshes[index].Attachment == null)
            .SelectMany(index => Vertices[index])
            .Select(vertex => vertex.Position.Y)
            .DefaultIfEmpty(0)
            .Min();
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

    public void CaptureBlend(CharacterAction action, float frame, float blend, float baseTime)
    {
        Evaluate(action.Clip.Animation, frame, action, blend, baseTime: baseTime, skin: false);
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
        float torsoYaw = 0,
        float baseTime = 0,
        bool skin = true)
    {
        Evaluate(
            action.Clip.Animation,
            frame,
            action,
            blend,
            feetYaw,
            torsoYaw,
            baseTime,
            skin,
            includeWeaponLayers: !skin
        );
    }

    private void Evaluate(
        SourceAnimation animation,
        float frame,
        CharacterAction? action,
        float blend,
        float feetYaw = 0,
        float torsoYaw = 0,
        float baseTime = 0,
        bool skin = true,
        bool includeWeaponLayers = false)
    {
        CharacterClip? baseClip = action?.Turn == true && action.Track != 0
            ? _asset.Locomotion?.Actions[$"idle_0_{(action.Name.Contains("_crouch_", StringComparison.Ordinal) ? "crouch" : "stay")}_{(Weapon?.Armed == true ? "incombat" : "unarmed")}"].Clip
            : null;
        float baseFrame = baseClip == null ? 0 : baseClip.FirstFrame
            + baseTime % baseClip.Duration * baseClip.FrameRate * baseClip.Direction;

        foreach (var (rig, matrices) in _rigs)
        {
            var priorities = _priorities[rig];
            for (int i = 0; i < rig.Nodes.Length; i++)
            {
                var node = rig.Nodes[i];

                // An attachment uses its own exported bind skeleton. Matching
                // names such as Root must not pick up avatar animation channels.
                if (!rig.Animate)
                {
                    _locals[rig][i] = node.Local;
                    matrices[i] = node.Parent < 0 ? node.Local : node.Local * matrices[node.Parent];
                    continue;
                }

                int priority = node.Parent < 0 ? 0 : priorities[node.Parent];
                if (action != null && action.Clip.NodePriorities.TryGetValue(node.Name, out int authoredPriority))
                    priority = authoredPriority;
                priorities[i] = priority;

                // Turn actions are overlays. A zero-priority subtree (Hips/Spine,
                // arms, attachments) continues the unarmed idle underneath it.
                bool useBase = baseClip != null && priority <= 0;
                var source = useBase ? baseClip!.Animation : animation;
                float sourceFrame = useBase ? baseFrame : frame;

                var local = source.Channels.TryGetValue(
                    node.Name,
                    out var channel)
                        ? channel.Sample(sourceFrame)
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

                        var reference = local.Translation;
                        local *= Matrix4x4.CreateRotationY(-yaw);
                        local.Translation = reference;
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

                ApplyUpperBodyAim(
                    ref matrices[i],
                    node.Name,
                    torsoYaw
                );
            }
        }

        if (!skin)
        {
            // Collision previews need the same spine as the rendered pose,
            // including track-12 equip/holster layers. CaptureBlend keeps its
            // original base-local snapshot by leaving this option disabled.
            if (includeWeaponLayers) ApplyWeaponLayers(feetYaw, torsoYaw);
            return;
        }

        ApplyWeaponPose(feetYaw, torsoYaw);

        for (int meshIndex = 0;
             meshIndex < _asset.Meshes.Length;
             meshIndex++)
        {
            var mesh = _asset.Meshes[meshIndex];

            var matrices = _rigs[mesh.Rig];
            var target = Vertices[meshIndex];
            var attachment = mesh.Attachment;
            var attachmentTransform = attachment == null ? Matrix4x4.Identity
                : attachment.Weapon != null ? _weaponTransform
                : attachment.InverseOrigin * _rigs[attachment.AvatarRig][attachment.AvatarNode];

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

                if (attachment != null)
                {
                    position = Vector3.Transform(position, attachmentTransform);
                    normal = Vector3.TransformNormal(normal, attachmentTransform);
                }

                target[i] = new SkinnedVertex(
                    position,
                    normal.LengthSquared() > 0.00001f
                        ? Vector3.Normalize(normal)
                        : Vector3.UnitY,
                    vertex.Uv,
                    vertex.Uv2
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
