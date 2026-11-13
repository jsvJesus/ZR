using System.Numerics;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Characters;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Characters;

internal sealed partial class CharacterRenderer
{
    private const float ArmWeightThreshold = 0.45f;
    private const float ViewModelScale = 0.82f;

    private static readonly Vector3 UnarmedPosition =
        new(0.10f, -0.22f, 0.95f);

    private static readonly Vector3 WeaponPosition =
        new(0.10f, -0.14f, 0.98f);

    private static readonly Vector3 AimingPosition =
        new(0.00f, -0.055f, 0.93f);

    private readonly List<(
        Buffer? Indices,
        int Count,
        bool Weapon)> _firstPersonParts = [];

    private void BuildFirstPersonParts(Device device)
    {
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
                indices = ExtractArmTriangles(mesh);
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

    private static int[] ExtractArmTriangles(CharacterMesh mesh)
    {
        bool[] armBones = new bool[mesh.Rig.Nodes.Length];

        for (int i = 0; i < armBones.Length; i++)
        {
            int nodeIndex = i;

            while (nodeIndex >= 0)
            {
                var node = mesh.Rig.Nodes[nodeIndex];

                if (IsArmBone(node.Name))
                {
                    armBones[i] = true;
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

            if (armBones[mesh.Palette[vertex.Bone0]])
            {
                weight += vertex.Weights.X;
            }

            if (armBones[mesh.Palette[vertex.Bone1]])
            {
                weight += vertex.Weights.Y;
            }

            if (armBones[mesh.Palette[vertex.Bone2]])
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

            if (weights[a] < ArmWeightThreshold ||
                weights[b] < ArmWeightThreshold ||
                weights[c] < ArmWeightThreshold)
            {
                continue;
            }

            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }

        return indices.ToArray();
    }

    private static bool IsArmBone(string name)
    {
        return name is
            "LeftArm" or
            "RightArm" or
            "LeftForeArm" or
            "RightForeArm" or
            "LeftForeArmRoll" or
            "RightForeArmRoll" or
            "LeftHand" or
            "RightHand";
    }

    private Vector3 GetSpinePosition()
    {
        if (_pose.TryGetAnimatedNodePosition(
                "Spine3",
                out Vector3 position))
        {
            return position;
        }

        if (_pose.TryGetAnimatedNodePosition(
                "Spine2",
                out position))
        {
            return position;
        }

        if (_pose.TryGetAnimatedNodePosition(
                "Spine1",
                out position))
        {
            return position;
        }

        return new Vector3(
            0f,
            _locomotion.CameraHeight - _pose.FloorOffset,
            0f
        );
    }

    private Vector3 GetWeaponAnchor(Vector3 fallback)
    {
        if (_pose.TryGetAnimatedNodePosition(
                "HP_GUNR",
                out Vector3 position))
        {
            return position;
        }

        if (_pose.TryGetAnimatedNodePosition(
                "HP_Rhand",
                out position))
        {
            return position;
        }

        if (_pose.TryGetAnimatedNodePosition(
                "RightHand",
                out position))
        {
            return position;
        }

        return fallback;
    }

    private Matrix4x4 CreateFirstPersonTransform(
        float aimBlend)
    {
        Vector3 spine = GetSpinePosition();
        Vector3 weaponAnchor = GetWeaponAnchor(spine);

        float weaponBlend = _locomotion.Weapon.InHands
            ? Math.Clamp(
                _locomotion.Weapon.TrackingWeight,
                0f,
                1f
            )
            : 0f;

        Vector3 anchor = Vector3.Lerp(
            spine,
            weaponAnchor,
            weaponBlend
        );

        Vector3 weaponPosition = Vector3.Lerp(
            WeaponPosition,
            AimingPosition,
            aimBlend
        );

        Vector3 target = Vector3.Lerp(
            UnarmedPosition,
            weaponPosition,
            weaponBlend
        );

        // Remove the avatar's world-facing body twist.
        // Camera rotation is already represented by the game view.
        float torsoYaw = _locomotion.Animator.TorsoYaw;

        // The world animation pitches the upper body toward
        // the camera. Compensate it in the viewmodel pass.
        float aimPitch = _pose.AimPitch;

        return Matrix4x4.CreateTranslation(-anchor) *
               Matrix4x4.CreateRotationY(-torsoYaw) *
               Matrix4x4.CreateRotationX(aimPitch) *
               Matrix4x4.CreateScale(ViewModelScale) *
               Matrix4x4.CreateTranslation(target);
    }

    public void RenderFirstPerson(
        Matrix4x4 projection,
        float firstPersonBlend,
        float aimBlend)
    {
        if (_firstPersonParts.Count == 0 ||
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

        if (visibility < 0.001f)
        {
            return;
        }

        bool fading = visibility < 0.999f;
        bool weaponInHands = _locomotion.Weapon.InHands;

        var constants = new Constants
        {
            World = CreateFirstPersonTransform(aimBlend),
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

            _context.UpdateSubresource(
                _pose.Vertices[i],
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
            _context.PixelShader.SetShaderResource(
                slot,
                null
            );
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
    }
}
