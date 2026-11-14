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
    private const float ViewModelScale = 0.82f;
    private const float HandMeshThreshold = 0.60f;

    private static readonly Vector3 UnarmedPosition =
        new(0.05f, -0.32f, 0.85f);

    private static readonly Vector3 WeaponPosition =
        new(0.12f, -0.23f, 0.85f);

    private static readonly Vector3 AimPosition =
        new(0.0f, -0.08f, 0.85f);

    private readonly List<(
        Buffer? Indices,
        int Count,
        bool Weapon)> _firstPersonParts = [];

    private void BuildFirstPersonParts(Device device)
    {
        DisposeFirstPersonParts();

        foreach (CharacterMesh mesh in _asset.Meshes)
        {
            bool isWeapon = mesh.Attachment?.Weapon != null;
            bool isHandMesh = !isWeapon && IsHandMesh(mesh);

            int[] indices = isWeapon || isHandMesh
                ? mesh.Indices
                : [];

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

    private static bool IsHandMesh(CharacterMesh mesh)
    {
        if (mesh.Attachment != null ||
            mesh.Vertices.Length == 0 ||
            mesh.Rig.Nodes.Length == 0)
        {
            return false;
        }

        bool[] handBones = new bool[mesh.Rig.Nodes.Length];

        for (int i = 0; i < handBones.Length; i++)
        {
            int nodeIndex = i;

            while (nodeIndex >= 0 &&
                   nodeIndex < mesh.Rig.Nodes.Length)
            {
                var node = mesh.Rig.Nodes[nodeIndex];

                if (node.Name is "LeftHand" or "RightHand")
                {
                    handBones[i] = true;
                    break;
                }

                nodeIndex = node.Parent;
            }
        }

        int handVertices = 0;

        foreach (var vertex in mesh.Vertices)
        {
            float weight = 0f;

            if (IsHandInfluence(
                    mesh,
                    handBones,
                    vertex.Bone0))
            {
                weight += vertex.Weights.X;
            }

            if (IsHandInfluence(
                    mesh,
                    handBones,
                    vertex.Bone1))
            {
                weight += vertex.Weights.Y;
            }

            if (IsHandInfluence(
                    mesh,
                    handBones,
                    vertex.Bone2))
            {
                weight += vertex.Weights.Z;
            }

            if (weight >= 0.5f)
            {
                handVertices++;
            }
        }

        return handVertices >=
            mesh.Vertices.Length * HandMeshThreshold;
    }

    private static bool IsHandInfluence(
        CharacterMesh mesh,
        bool[] handBones,
        int boneIndex)
    {
        if ((uint)boneIndex >= (uint)mesh.Palette.Length)
        {
            return false;
        }

        int nodeIndex = mesh.Palette[boneIndex];

        return (uint)nodeIndex < (uint)handBones.Length &&
               handBones[nodeIndex];
    }

    private Vector3 GetAnimatedViewAnchor(
        bool weaponInHands)
    {
        if (weaponInHands &&
            _pose.TryGetAnimatedWeaponPivot(
                out Matrix4x4 weaponPivot))
        {
            return weaponPivot.Translation;
        }

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

        return new Vector3(
            0f,
            1.4f,
            0f
        );
    }

    private Matrix4x4 CreateFirstPersonTransform(
        float aimBlend,
        bool weaponInHands)
    {
        Vector3 anchor = GetAnimatedViewAnchor(
            weaponInHands
        );

        Vector3 target = weaponInHands
            ? Vector3.Lerp(
                WeaponPosition,
                AimPosition,
                Math.Clamp(aimBlend, 0f, 1f))
            : UnarmedPosition;

        return Matrix4x4.CreateTranslation(-anchor) *
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

        if (visibility <= 0.001f)
        {
            return;
        }

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

        _context.PixelShader.SetSampler(0, _sampler);

        var ordered = Enumerable.Range(
            0,
            _firstPersonParts.Count
        ).OrderBy(i => _asset.Meshes[i].Alpha);

        foreach (int i in ordered)
        {
            var part = _firstPersonParts[i];

            if (part.Indices == null ||
                part.Count == 0)
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

            // Reuse the actual animated pose.
            // This includes running, jumping, landing,
            // weapon overlays and hand IK.
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
    }
}
