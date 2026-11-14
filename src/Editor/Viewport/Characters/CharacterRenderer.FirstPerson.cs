using System.Numerics;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Characters;
using ZoneReborn.World.Characters.Locomotion;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Characters;

internal sealed partial class CharacterRenderer
{
    private const float ForearmWeightThreshold = 0.45f;
    private const float ViewModelScale = 0.75f;

    private static readonly Vector3 UnarmedPosition =
        new(0.05f, -0.43f, 0.84f);

    private static readonly Vector3 WeaponPosition =
        new(0.13f, -0.28f, 0.90f);

    private static readonly Vector3 AimingPosition =
        new(0.00f, -0.075f, 0.80f);

    private readonly List<(
        Buffer? Indices,
        int Count,
        bool Weapon)> _firstPersonParts = [];

    private CharacterPose? _firstPersonPose;

    private void BuildFirstPersonParts(Device device)
    {
        _firstPersonPose = new CharacterPose(_asset)
        {
            Weapon = _locomotion.Weapon
        };

        foreach (var mesh in _asset.Meshes)
        {
            bool isWeapon = mesh.Attachment?.Weapon != null;

            int[] indices;

            if (isWeapon)
            {
                indices = mesh.Indices;
            }
            else if (mesh.Attachment == null)
            {
                indices = ExtractForearmTriangles(mesh);
            }
            else
            {
                indices = [];
            }

            Buffer? indexBuffer = null;

            if (indices.Length > 0)
            {
                indexBuffer = Buffer.Create(
                    device,
                    BindFlags.IndexBuffer,
                    indices
                );
            }

            _firstPersonParts.Add((
                indexBuffer,
                indices.Length,
                isWeapon
            ));
        }
    }

    private static int[] ExtractForearmTriangles(
        CharacterMesh mesh)
    {
        bool[] forearmBones = new bool[mesh.Rig.Nodes.Length];

        for (int i = 0; i < forearmBones.Length; i++)
        {
            int nodeIndex = i;

            while (nodeIndex >= 0)
            {
                var node = mesh.Rig.Nodes[nodeIndex];

                if (IsForearmBone(node.Name))
                {
                    forearmBones[i] = true;
                    break;
                }

                nodeIndex = node.Parent;
            }
        }

        float[] weights = new float[mesh.Vertices.Length];

        for (int i = 0; i < mesh.Vertices.Length; i++)
        {
            var vertex = mesh.Vertices[i];

            float weight = 0f;

            if (forearmBones[mesh.Palette[vertex.Bone0]])
            {
                weight += vertex.Weights.X;
            }

            if (forearmBones[mesh.Palette[vertex.Bone1]])
            {
                weight += vertex.Weights.Y;
            }

            if (forearmBones[mesh.Palette[vertex.Bone2]])
            {
                weight += vertex.Weights.Z;
            }

            weights[i] = weight;
        }

        var indices = new List<int>();

        for (int i = 0; i + 2 < mesh.Indices.Length; i += 3)
        {
            int a = mesh.Indices[i];
            int b = mesh.Indices[i + 1];
            int c = mesh.Indices[i + 2];

            if (weights[a] < ForearmWeightThreshold ||
                weights[b] < ForearmWeightThreshold ||
                weights[c] < ForearmWeightThreshold)
            {
                continue;
            }

            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }

        return indices.ToArray();
    }

    private static bool IsForearmBone(string name)
    {
        return name is
            "LeftForeArm" or
            "RightForeArm" or
            "LeftForeArmRoll" or
            "RightForeArmRoll" or
            "LeftHand" or
            "RightHand";
    }

    private void UpdateFirstPersonPose(float aimBlend)
    {
        if (_firstPersonPose == null ||
            _asset.Locomotion is not { } library)
        {
            return;
        }

        _firstPersonPose.Weapon = _locomotion.Weapon;
        _firstPersonPose.AimPitch = 0f;

        bool armed = _locomotion.Weapon.Armed;
        bool crouched = _locomotion.Crouched;

        string posture = crouched ? "crouch" : "stay";
        string equipment = armed ? "incombat" : "unarmed";

        string idleName =
            $"idle_0_{posture}_{equipment}";

        string aimName =
            $"idle_0_{posture}_aim";

        if (!library.Actions.TryGetValue(
                idleName,
                out CharacterAction? idleAction))
        {
            idleAction = library.Idle;
        }

        float aimWeight = _locomotion.Weapon.InHands
            ? Math.Clamp(aimBlend, 0f, 1f)
            : 0f;

        if (aimWeight <= 0.001f ||
            !library.Actions.TryGetValue(
                aimName,
                out CharacterAction? aimAction))
        {
            _firstPersonPose.Apply(
                idleAction,
                idleAction.Clip.FirstFrame,
                1f
            );

            return;
        }

        // Capture the stationary weapon pose as the source.
        // Do not use the moving TPS animator here.
        _firstPersonPose.CaptureBlend(
            idleAction,
            idleAction.Clip.FirstFrame,
            1f,
            0f
        );

        // Blend from stationary weapon handling to ADS.
        // Weapon overlays and hand IK still run in CharacterPose.
        _firstPersonPose.Apply(
            aimAction,
            aimAction.Clip.FirstFrame,
            aimWeight
        );
    }

    private Vector3 GetFirstPersonAnchor(bool weaponInHands)
    {
        if (_firstPersonPose == null)
        {
            return Vector3.Zero;
        }

        if (weaponInHands &&
            _firstPersonPose.TryGetAnimatedWeaponPivot(
                out Matrix4x4 weaponPivot))
        {
            return weaponPivot.Translation;
        }

        if (_firstPersonPose.TryGetAnimatedNodePosition(
                "HP_Rhand",
                out Vector3 position))
        {
            return position;
        }

        if (_firstPersonPose.TryGetAnimatedNodePosition(
                "RightHand",
                out position))
        {
            return position;
        }

        if (_firstPersonPose.TryGetAnimatedNodePosition(
                "Spine3",
                out position))
        {
            return position;
        }

        return new Vector3(0f, 1.4f, 0f);
    }

    private Matrix4x4 CreateFirstPersonTransform(
        float aimBlend,
        bool weaponInHands)
    {
        Vector3 anchor = GetFirstPersonAnchor(
            weaponInHands
        );

        Vector3 target;

        if (weaponInHands)
        {
            target = Vector3.Lerp(
                WeaponPosition,
                AimingPosition,
                aimBlend
            );
        }
        else
        {
            target = UnarmedPosition;
        }

        // The entire animated FPS model is placed in camera
        // space. It is independent of movement yaw, camera yaw
        // and the directional locomotion animations.
        return Matrix4x4.CreateTranslation(-anchor) *
               Matrix4x4.CreateScale(ViewModelScale) *
               Matrix4x4.CreateTranslation(target);
    }

    public void RenderFirstPerson(
        Matrix4x4 projection,
        float firstPersonBlend,
        float aimBlend)
    {
        if (_firstPersonPose == null ||
            _firstPersonParts.Count == 0 ||
            _firstPersonParts.Count != _asset.Meshes.Length ||
            _firstPersonParts.Count != _meshes.Count)
        {
            return;
        }

        float visibility = Math.Clamp(
            (firstPersonBlend - 0.2f) / 0.8f,
            0f,
            1f
        );

        visibility =
            visibility * visibility * (3f - 2f * visibility);

        if (visibility <= 0.001f)
        {
            return;
        }

        UpdateFirstPersonPose(aimBlend);

        bool weaponInHands = _locomotion.Weapon.InHands;
        bool fading = visibility < 0.999f;

        var constants = new Constants
        {
            World = CreateFirstPersonTransform(
                aimBlend,
                weaponInHands
            ),
            ViewProjection = projection
        };

        _context.InputAssembler.InputLayout = _layout;

        _context.InputAssembler.PrimitiveTopology =
            PrimitiveTopology.TriangleList;

        _context.VertexShader.Set(_vertex);
        _context.PixelShader.Set(_pixel);

        _context.VertexShader.SetConstantBuffer(
            0,
            _constants
        );

        _context.PixelShader.SetConstantBuffer(
            0,
            _constants
        );

        _context.PixelShader.SetSampler(
            0,
            _sampler
        );

        var ordered = Enumerable.Range(
            0,
            _firstPersonParts.Count
        ).OrderBy(i => _asset.Meshes[i].Alpha);

        foreach (int i in ordered)
        {
            var part = _firstPersonParts[i];

            if (part.Indices == null || part.Count == 0)
            {
                continue;
            }

            if (part.Weapon && !weaponInHands)
            {
                continue;
            }

            var source = _asset.Meshes[i];
            var mesh = _meshes[i];
            var surface = source.Surface;

            constants.DiffuseColor = source.DiffuseColor;

            constants.ColourR = surface.ColourR;
            constants.ColourG = surface.ColourG;
            constants.ColourB = surface.ColourB;
            constants.ColourA = surface.ColourA;

            constants.PatternScaleShift =
                surface.PatternScaleShift;

            constants.PatternOptions = new Vector4(
                surface.PatternRotation.X,
                surface.PatternRotation.Y,
                surface.ColourByUv2 ? 1f : 0f,
                surface.ColourMaskPath != null &&
                surface.DiffuseMaskPath != null ? 1f : 0f
            );

            constants.OpacityScaleShift =
                surface.OpacityScaleShift;

            constants.SurfaceOptions = new Vector4(
                surface.UsesAlpha ? 1f : 0f,
                surface.AlphaCutoff,
                visibility,
                0f
            );

            _context.UpdateSubresource(
                ref constants,
                _constants
            );

            // The FPS model has its own stationary animated pose.
            // Never upload the TPS locomotion vertices here.
            _context.UpdateSubresource(
                _firstPersonPose.Vertices[i],
                mesh.Vertices
            );

            _context.InputAssembler.SetVertexBuffers(
                0,
                new VertexBufferBinding(
                    mesh.Vertices,
                    Utilities.SizeOf<SkinnedVertex>(),
                    0
                )
            );

            _context.InputAssembler.SetIndexBuffer(
                part.Indices,
                Format.R32_UInt,
                0
            );

            _context.PixelShader.SetShaderResource(
                0,
                mesh.Texture.View
            );

            _context.PixelShader.SetShaderResource(
                1,
                mesh.DiffuseMask.View
            );

            _context.PixelShader.SetShaderResource(
                2,
                mesh.ColourMask.View
            );

            _context.PixelShader.SetShaderResource(
                3,
                mesh.OpacityMask.View
            );

            _context.Rasterizer.State = surface.DoubleSided
                ? _doubleSided
                : _rasterizer;

            bool transparent = fading || source.Alpha;

            _context.OutputMerger.SetDepthStencilState(
                transparent ? _transparentDepth : _depth
            );

            _context.OutputMerger.SetBlendState(
                transparent ? _alpha : null
            );

            _context.DrawIndexed(
                part.Count,
                0,
                0
            );
        }

        for (int slot = 0; slot < 4; slot++)
        {
            _context.PixelShader.SetShaderResource(slot, null);
        }

        _context.OutputMerger.SetBlendState(null);
        _context.OutputMerger.SetDepthStencilState(null);
        _context.Rasterizer.State = null;
    }

    private void DisposeFirstPersonParts()
    {
        foreach (var part in _firstPersonParts)
        {
            part.Indices?.Dispose();
        }

        _firstPersonParts.Clear();
        _firstPersonPose = null;
    }
}
