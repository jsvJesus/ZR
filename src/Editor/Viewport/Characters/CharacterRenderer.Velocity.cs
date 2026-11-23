using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Editor.Rendering.Shaders;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Characters;

internal sealed partial class CharacterRenderer
{
    private readonly List<VelocityMesh?> _velocityMeshes = [];

    private Device? _velocityDevice;
    private VertexShader? _velocityVertex;
    private PixelShader? _velocityPixel;
    private InputLayout? _velocityLayout;
    private Buffer? _velocityConstants;
    private DepthStencilState? _velocityDepth;

    private bool _velocityHasHistory;
    private bool _velocityPreviousFirstPerson;

    private Vector3 _velocityPreviousPosition;
    private float _velocityPreviousYaw;

    public void RenderVelocity(
        Device device,
        RenderTargetView motionTarget,
        DepthStencilView depthTarget,
        Matrix4x4 currentViewProjection,
        Matrix4x4 previousViewProjection,
        Vector3 currentCameraPosition,
        Vector3 previousCameraPosition,
        bool firstPerson,
        bool historyValid,
        int width,
        int height)
    {
        EnsureVelocityResources(device);

        Vector3 currentPosition = Position +
            new Vector3(0f, _pose.FloorOffset, 0f);

        bool valid =
            historyValid &&
            _velocityHasHistory &&
            _velocityPreviousFirstPerson == firstPerson &&
            Vector3.DistanceSquared(
                currentPosition,
                _velocityPreviousPosition) < 16f;

        Matrix4x4 currentRelativeProjection =
            Matrix4x4.CreateTranslation(
                currentCameraPosition) *
            currentViewProjection;

        Matrix4x4 previousRelativeProjection =
            Matrix4x4.CreateTranslation(
                previousCameraPosition) *
            previousViewProjection;

        Matrix4x4 currentWorld =
            Matrix4x4.CreateRotationY(Yaw) *
            Matrix4x4.CreateTranslation(
                currentPosition - currentCameraPosition);

        Matrix4x4 previousWorld =
            Matrix4x4.CreateRotationY(
                _velocityPreviousYaw) *
            Matrix4x4.CreateTranslation(
                _velocityPreviousPosition -
                previousCameraPosition);

        try
        {
            bool visible = firstPerson ||
                CalculateVisibility(
                    currentCameraPosition) >= 0.999f;

            if (!valid || !visible)
            {
                return;
            }

            _context.ClearState();

            _context.OutputMerger.SetRenderTargets(
                depthTarget,
                motionTarget);

            _context.OutputMerger.SetDepthStencilState(
                _velocityDepth);

            _context.OutputMerger.SetBlendState(null);

            _context.Rasterizer.SetViewport(
                0,
                0,
                width,
                height);

            _context.InputAssembler.InputLayout =
                _velocityLayout;

            _context.InputAssembler.PrimitiveTopology =
                PrimitiveTopology.TriangleList;

            _context.VertexShader.Set(
                _velocityVertex);

            _context.VertexShader.SetConstantBuffer(
                0,
                _velocityConstants);

            _context.PixelShader.Set(
                _velocityPixel);

            _context.PixelShader.SetConstantBuffer(
                0,
                _velocityConstants);

            _context.PixelShader.SetSampler(
                0,
                _sampler);

            for (int i = 0; i < _meshes.Count; i++)
            {
                var source = _asset.Meshes[i];
                var mesh = _meshes[i];
                var state = _velocityMeshes[i];

                if (state == null ||
                    source.Indices.Length == 0 ||
                    source.Alpha)
                {
                    continue;
                }

                if (firstPerson &&
                    !source.FirstPersonVisible &&
                    source.Attachment?.Weapon == null)
                {
                    continue;
                }

                if (state.Positions.Length !=
                    _pose.Vertices[i].Length)
                {
                    continue;
                }

                _context.UpdateSubresource(
                    state.Positions,
                    state.PreviousBuffer);

                var constants = new VelocityConstants
                {
                    CurrentWorld = currentWorld,
                    PreviousWorld = previousWorld,

                    CurrentViewProjection =
                        currentRelativeProjection,

                    PreviousViewProjection =
                        previousRelativeProjection,

                    AlphaSettings = new Vector4(
                        source.Surface.UsesAlpha ? 1f : 0f,
                        source.Surface.AlphaCutoff,
                        0f,
                        0f),

                    OpacityScaleShift =
                        source.Surface.OpacityScaleShift
                };

                _context.UpdateSubresource(
                    ref constants,
                    _velocityConstants!);

                _context.InputAssembler.SetVertexBuffers(
                    0,
                    new VertexBufferBinding(
                        mesh.Vertices,
                        Utilities.SizeOf<SkinnedVertex>(),
                        0),
                    new VertexBufferBinding(
                        state.PreviousBuffer,
                        Utilities.SizeOf<Vector3>(),
                        0));

                _context.InputAssembler.SetIndexBuffer(
                    mesh.Indices,
                    Format.R32_UInt,
                    0);

                _context.PixelShader.SetShaderResource(
                    0,
                    mesh.Texture.View);

                _context.PixelShader.SetShaderResource(
                    1,
                    mesh.OpacityMask.View);

                _context.Rasterizer.State =
                    source.Surface.DoubleSided
                        ? _doubleSided
                        : _rasterizer;

                _context.DrawIndexed(
                    source.Indices.Length,
                    0,
                    0);
            }
        }
        finally
        {
            _context.ClearState();

            CaptureVelocityPose(
                device,
                currentPosition,
                firstPerson);
        }
    }

    private void EnsureVelocityResources(Device device)
    {
        if (_velocityVertex != null)
        {
            return;
        }

        _velocityDevice = device;

        try
        {
            var compiler = new ShaderCompiler(
                AssetPaths.FromApplication());

            using var vertexCode = compiler.Compile(
                "Characters/CharacterVelocity.hlsl",
                "VSMain",
                "vs_4_0");

            using var pixelCode = compiler.Compile(
                "Characters/CharacterVelocity.hlsl",
                "PSMain",
                "ps_5_0");

            _velocityVertex = new VertexShader(
                device,
                vertexCode.Bytecode);

            _velocityPixel = new PixelShader(
                device,
                pixelCode.Bytecode);

            _velocityLayout = new InputLayout(
                device,
                vertexCode.Bytecode,
                [
                    new InputElement(
                        "POSITION",
                        0,
                        Format.R32G32B32_Float,
                        0,
                        0),

                    new InputElement(
                        "TEXCOORD",
                        0,
                        Format.R32G32_Float,
                        24,
                        0),

                    new InputElement(
                        "POSITION",
                        1,
                        Format.R32G32B32_Float,
                        0,
                        1)
                ]);

            _velocityConstants = new Buffer(
                device,
                Utilities.SizeOf<VelocityConstants>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            var depthDescription =
                DepthStencilStateDescription.Default();

            depthDescription.DepthWriteMask =
                DepthWriteMask.Zero;

            depthDescription.DepthComparison =
                Comparison.LessEqual;

            _velocityDepth = new DepthStencilState(
                device,
                depthDescription);

            for (int i = 0; i < _meshes.Count; i++)
            {
                _velocityMeshes.Add(
                    CreateVelocityMesh(
                        device,
                        i));
            }
        }
        catch
        {
            DisposeVelocity();
            throw;
        }
    }

    private VelocityMesh? CreateVelocityMesh(
        Device device,
        int index)
    {
        var vertices = _pose.Vertices[index];

        if (vertices.Length == 0)
        {
            return null;
        }

        var positions = new Vector3[
            vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            positions[i] = vertices[i].Position;
        }

        Buffer previousBuffer = Buffer.Create(
            device,
            BindFlags.VertexBuffer,
            positions);

        return new VelocityMesh(
            previousBuffer,
            positions);
    }

    private void CaptureVelocityPose(
        Device device,
        Vector3 position,
        bool firstPerson)
    {
        for (int i = 0; i < _meshes.Count; i++)
        {
            var vertices = _pose.Vertices[i];
            var state = _velocityMeshes[i];

            if (state == null ||
                state.Positions.Length != vertices.Length)
            {
                state?.Dispose();

                state = CreateVelocityMesh(
                    device,
                    i);

                _velocityMeshes[i] = state;
            }

            if (state == null)
            {
                continue;
            }

            for (int vertex = 0;
                 vertex < vertices.Length;
                 vertex++)
            {
                state.Positions[vertex] =
                    vertices[vertex].Position;
            }
        }

        _velocityPreviousPosition = position;
        _velocityPreviousYaw = Yaw;
        _velocityPreviousFirstPerson = firstPerson;
        _velocityHasHistory = true;
    }

    private void DisposeVelocity()
    {
        foreach (var mesh in _velocityMeshes)
        {
            mesh?.Dispose();
        }

        _velocityMeshes.Clear();

        _velocityDepth?.Dispose();
        _velocityDepth = null;

        _velocityConstants?.Dispose();
        _velocityConstants = null;

        _velocityLayout?.Dispose();
        _velocityLayout = null;

        _velocityPixel?.Dispose();
        _velocityPixel = null;

        _velocityVertex?.Dispose();
        _velocityVertex = null;

        _velocityDevice = null;
        _velocityHasHistory = false;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VelocityConstants
    {
        public Matrix4x4 CurrentWorld;
        public Matrix4x4 PreviousWorld;

        public Matrix4x4 CurrentViewProjection;
        public Matrix4x4 PreviousViewProjection;

        public Vector4 AlphaSettings;
        public Vector4 OpacityScaleShift;
    }

    private sealed class VelocityMesh : IDisposable
    {
        public Buffer PreviousBuffer { get; }
        public Vector3[] Positions { get; }

        public VelocityMesh(
            Buffer previousBuffer,
            Vector3[] positions)
        {
            PreviousBuffer = previousBuffer;
            Positions = positions;
        }

        public void Dispose()
        {
            PreviousBuffer.Dispose();
        }
    }
}
