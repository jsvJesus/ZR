using System.Diagnostics;
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
    private const float ViewModelScale = 0.82f;
    private const float ViewModelMeshThreshold = 0.32f;

    private const float MotionResponse = 11f;
    private const float ReferenceInfluence = 0.10f;

    private static readonly Vector3 UnarmedPosition =
        new(0.03f, -0.30f, 0.84f);

    private static readonly Vector3 WeaponPosition =
        new(0.10f, -0.21f, 0.84f);

    private static readonly Vector3 AimPosition =
        new(0.00f, -0.075f, 0.82f);

    private readonly List<(
        Buffer? Indices,
        int Count,
        bool Weapon)> _firstPersonParts = [];

    private readonly List<SkinnedVertex[]> _firstPersonVertices = [];

    private CharacterPose? _firstPersonReferencePose;

    private long _lastFirstPersonTick;
    private float _movementBlend;
    private float _sprintBlend;
    private float _jumpBlend;

    private void BuildFirstPersonParts(Device device)
    {
        DisposeFirstPersonParts();

        _firstPersonReferencePose = new CharacterPose(_asset)
        {
            Weapon = _locomotion.Weapon
        };

        foreach (CharacterMesh mesh in _asset.Meshes)
        {
            bool isWeapon = mesh.Attachment?.Weapon != null;
            bool isViewModelMesh = !isWeapon && IsViewModelMesh(mesh);

            int[] indices = isWeapon || isViewModelMesh
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

            _firstPersonVertices.Add(
                new SkinnedVertex[mesh.Vertices.Length]
            );
        }
    }

    private static bool IsViewModelMesh(CharacterMesh mesh)
    {
        if (mesh.Attachment != null ||
            mesh.Vertices.Length == 0 ||
            mesh.Rig.Nodes.Length == 0)
        {
            return false;
        }

        bool[] viewModelBones = new bool[mesh.Rig.Nodes.Length];

        for (int i = 0; i < viewModelBones.Length; i++)
        {
            int nodeIndex = i;

            while ((uint)nodeIndex < (uint)mesh.Rig.Nodes.Length)
            {
                var node = mesh.Rig.Nodes[nodeIndex];

                if (node.Name is
                    "LeftForeArm" or
                    "RightForeArm" or
                    "LeftForeArmRoll" or
                    "RightForeArmRoll" or
                    "LeftHand" or
                    "RightHand")
                {
                    viewModelBones[i] = true;
                    break;
                }

                nodeIndex = node.Parent;
            }
        }

        int influencedVertices = 0;

        foreach (var vertex in mesh.Vertices)
        {
            float weight = 0f;

            if (IsViewModelInfluence(
                    mesh,
                    viewModelBones,
                    vertex.Bone0))
            {
                weight += vertex.Weights.X;
            }

            if (IsViewModelInfluence(
                    mesh,
                    viewModelBones,
                    vertex.Bone1))
            {
                weight += vertex.Weights.Y;
            }

            if (IsViewModelInfluence(
                    mesh,
                    viewModelBones,
                    vertex.Bone2))
            {
                weight += vertex.Weights.Z;
            }

            if (weight >= 0.35f)
            {
                influencedVertices++;
            }
        }

        return influencedVertices >=
            mesh.Vertices.Length * ViewModelMeshThreshold;
    }

    private static bool IsViewModelInfluence(
        CharacterMesh mesh,
        bool[] viewModelBones,
        int boneIndex)
    {
        if ((uint)boneIndex >= (uint)mesh.Palette.Length)
        {
            return false;
        }

        int nodeIndex = mesh.Palette[boneIndex];

        return (uint)nodeIndex < (uint)viewModelBones.Length &&
               viewModelBones[nodeIndex];
    }

    private float GetWeaponBlend()
    {
        float tracking = Math.Clamp(
            _locomotion.Weapon.TrackingWeight,
            0f,
            1f
        );

        if (_locomotion.Weapon.InHands)
        {
            return MathF.Max(tracking, 1f);
        }

        if (_locomotion.Weapon.Armed)
        {
            return MathF.Max(tracking, 0.001f);
        }

        return tracking;
    }

    private bool HasWeaponViewModel()
    {
        return GetWeaponBlend() > 0.001f ||
               _locomotion.Weapon.Armed ||
               _locomotion.Weapon.InHands;
    }

    private bool IsWeaponTransitioning()
    {
        float tracking = Math.Clamp(
            _locomotion.Weapon.TrackingWeight,
            0f,
            1f
        );

        return tracking > 0.001f &&
               tracking < 0.999f;
    }

    private bool IsAirborne()
    {
        return _locomotion.Snapshot.State is
            AnimState.JumpWaiting or
            AnimState.JumpTakeoff or
            AnimState.JumpAir or
            AnimState.Falling or
            AnimState.LandingMoving or
            AnimState.LandingIdle or
            AnimState.LandingHard;
    }

    private void UpdateReferencePose(float aimBlend)
    {
        if (_firstPersonReferencePose == null ||
            _asset.Locomotion is not { } library)
        {
            return;
        }

        var pose = _firstPersonReferencePose;

        pose.Weapon = _locomotion.Weapon;
        pose.AimPitch = 0f;

        string posture = _locomotion.Crouched
            ? "crouch"
            : "stay";

        string equipment = HasWeaponViewModel()
            ? "incombat"
            : "unarmed";

        string idleName = $"idle_0_{posture}_{equipment}";

        if (!library.Actions.TryGetValue(
                idleName,
                out CharacterAction? idle))
        {
            idle = library.Idle;
        }

        float aim = HasWeaponViewModel()
            ? Math.Clamp(aimBlend, 0f, 1f)
            : 0f;

        string aimName = $"idle_0_{posture}_aim";

        if (aim > 0.001f &&
            library.Actions.TryGetValue(
                aimName,
                out CharacterAction? aimAction))
        {
            pose.CaptureBlend(
                idle,
                idle.Clip.FirstFrame,
                1f,
                0f
            );

            pose.Apply(
                aimAction,
                aimAction.Clip.FirstFrame,
                aim
            );
        }
        else
        {
            pose.Apply(
                idle,
                idle.Clip.FirstFrame,
                1f
            );
        }
    }

    private static Vector3 GetPoseAnchor(
        CharacterPose pose,
        bool weaponVisible)
    {
        if (weaponVisible &&
            pose.TryGetAnimatedWeaponPivot(
                out Matrix4x4 weaponPivot))
        {
            return weaponPivot.Translation;
        }

        if (pose.TryGetAnimatedNodePosition(
                "Spine3",
                out Vector3 spine))
        {
            return spine;
        }

        if (pose.TryGetAnimatedNodePosition(
                "Spine2",
                out spine))
        {
            return spine;
        }

        return new Vector3(0f, 1.4f, 0f);
    }

    private Matrix4x4 GetMovementCompensation(
        bool weaponVisible,
        bool useReferenceStabilization,
        out bool valid)
    {
        valid = false;

        if (!weaponVisible ||
            !useReferenceStabilization ||
            _firstPersonReferencePose == null)
        {
            return Matrix4x4.Identity;
        }

        if (!_pose.TryGetAnimatedWeaponPivot(
                out Matrix4x4 animatedPivot) ||
            !_firstPersonReferencePose.TryGetAnimatedWeaponPivot(
                out Matrix4x4 referencePivot))
        {
            return Matrix4x4.Identity;
        }

        if (!Matrix4x4.Invert(
                animatedPivot,
                out Matrix4x4 inverseAnimated))
        {
            return Matrix4x4.Identity;
        }

        valid = true;
        return inverseAnimated * referencePivot;
    }

    private float UpdateMotionBlends()
    {
        long now = Stopwatch.GetTimestamp();

        float deltaTime = _lastFirstPersonTick == 0
            ? 1f / 60f
            : (float)Stopwatch.GetElapsedTime(
                _lastFirstPersonTick,
                now
            ).TotalSeconds;

        _lastFirstPersonTick = now;

        deltaTime = Math.Clamp(deltaTime, 0f, 0.05f);

        var snapshot = _locomotion.Snapshot;
        bool airborne = IsAirborne();

        bool moving = snapshot.Grounded &&
            snapshot.Speed > 0.12f &&
            !airborne;

        bool sprinting =
            moving &&
            snapshot.State == AnimState.Sprint;

        float response =
            1f - MathF.Exp(-MotionResponse * deltaTime);

        _movementBlend +=
            ((moving ? 1f : 0f) - _movementBlend) *
            response;

        _sprintBlend +=
            ((sprinting ? 1f : 0f) - _sprintBlend) *
            response;

        _jumpBlend +=
            ((airborne ? 1f : 0f) - _jumpBlend) *
            response;

        return Math.Clamp(
            snapshot.Speed / 3.6f,
            0f,
            1.5f
        );
    }

    private Matrix4x4 CreateFirstPersonTransform(
        float aimBlend,
        bool weaponVisible,
        float weaponBlend,
        float movementSpeed)
    {
        Vector3 anchor = GetPoseAnchor(
            _firstPersonReferencePose!,
            weaponVisible
        );

        Vector3 armedTarget = Vector3.Lerp(
            WeaponPosition,
            AimPosition,
            Math.Clamp(aimBlend, 0f, 1f)
        );

        Vector3 target = Vector3.Lerp(
            UnarmedPosition,
            armedTarget,
            weaponBlend
        );

        float movement = _movementBlend *
            Math.Clamp(movementSpeed, 0f, 1f) *
            (1f - 0.85f * aimBlend);

        float phase =
            _locomotion.Animator.Phase * MathF.Tau;

        float horizontalBob =
            MathF.Sin(phase) * 0.010f * movement;

        float verticalBob =
            -MathF.Abs(MathF.Cos(phase)) * 0.012f * movement;

        float forwardBob =
            MathF.Sin(phase * 2f) * 0.004f * movement;

        target += new Vector3(
            horizontalBob,
            verticalBob,
            forwardBob
        );

        target += new Vector3(
            0f,
            -0.085f,
            -0.050f
        ) * _sprintBlend;

        target += new Vector3(
            0f,
            -0.055f,
            -0.030f
        ) * _jumpBlend;

        float roll =
            MathF.Sin(phase) * 0.006f * movement;

        float pitch =
            -0.035f * _sprintBlend +
            0.020f * _jumpBlend;

        return Matrix4x4.CreateTranslation(-anchor) *
               Matrix4x4.CreateScale(ViewModelScale) *
               Matrix4x4.CreateRotationX(pitch) *
               Matrix4x4.CreateRotationZ(roll) *
               Matrix4x4.CreateTranslation(target);
    }

    private void UpdateFirstPersonVertices(
        int meshIndex,
        Matrix4x4 compensation,
        float referenceBlend)
    {
        var target = _firstPersonVertices[meshIndex];

        if (_firstPersonReferencePose == null ||
            referenceBlend <= 0.001f)
        {
            Array.Copy(
                _pose.Vertices[meshIndex],
                target,
                target.Length
            );

            return;
        }

        var reference = _firstPersonReferencePose.Vertices[meshIndex];
        var animated = _pose.Vertices[meshIndex];

        for (int i = 0; i < target.Length; i++)
        {
            var referenceVertex = reference[i];
            var animatedVertex = animated[i];

            Vector3 compensatedPosition = Vector3.Transform(
                animatedVertex.Position,
                compensation
            );

            Vector3 compensatedNormal = Vector3.TransformNormal(
                animatedVertex.Normal,
                compensation
            );

            Vector3 blendedNormal = Vector3.Lerp(
                referenceVertex.Normal,
                compensatedNormal,
                referenceBlend
            );

            if (blendedNormal.LengthSquared() > 0.000001f)
            {
                blendedNormal = Vector3.Normalize(blendedNormal);
            }
            else
            {
                blendedNormal = referenceVertex.Normal;
            }

            target[i] = new SkinnedVertex(
                Vector3.Lerp(
                    referenceVertex.Position,
                    compensatedPosition,
                    referenceBlend
                ),
                blendedNormal,
                referenceVertex.Uv,
                referenceVertex.Uv2
            );
        }
    }

    public void RenderFirstPerson(
        Matrix4x4 projection,
        float firstPersonBlend,
        float aimBlend)
    {
        if (_firstPersonReferencePose == null ||
            _firstPersonParts.Count == 0 ||
            _firstPersonParts.Count != _asset.Meshes.Length ||
            _firstPersonParts.Count != _meshes.Count ||
            _firstPersonVertices.Count != _asset.Meshes.Length)
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

        bool weaponVisible = HasWeaponViewModel();
        float weaponBlend = GetWeaponBlend();

        bool useReferenceStabilization =
            weaponVisible &&
            !IsAirborne() &&
            !IsWeaponTransitioning();

        bool fading = visibility < 0.999f;

        UpdateReferencePose(aimBlend);

        float movementSpeed = UpdateMotionBlends();

        Matrix4x4 compensation = GetMovementCompensation(
            weaponVisible,
            useReferenceStabilization,
            out bool hasCompensation
        );

        float referenceBlend = hasCompensation
            ? ReferenceInfluence * (1f - 0.70f * aimBlend)
            : 0f;

        var constants = new Constants
        {
            World = CreateFirstPersonTransform(
                aimBlend,
                weaponVisible,
                weaponBlend,
                movementSpeed
            ),
            ViewProjection = projection
        };

        _context.InputAssembler.InputLayout = _layout;
        _context.InputAssembler.PrimitiveTopology =
            PrimitiveTopology.TriangleList;

        _context.VertexShader.Set(_vertex);
        _context.PixelShader.Set(_pixel);

        _context.VertexShader.SetConstantBuffer(0, _constants);
        _context.PixelShader.SetConstantBuffer(0, _constants);
        _context.PixelShader.SetSampler(0, _sampler);

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

            if (part.Weapon && !weaponVisible)
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

            UpdateFirstPersonVertices(
                i,
                compensation,
                referenceBlend
            );

            _context.UpdateSubresource(ref constants, _constants);
            _context.UpdateSubresource(
                _firstPersonVertices[i],
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
        _firstPersonVertices.Clear();

        _firstPersonReferencePose = null;
        _lastFirstPersonTick = 0;

        _movementBlend = 0f;
        _sprintBlend = 0f;
        _jumpBlend = 0f;
    }
}
