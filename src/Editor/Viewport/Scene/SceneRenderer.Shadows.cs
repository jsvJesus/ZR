using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Editor.Rendering.Shaders;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Scene;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Scene;

internal sealed partial class SceneRenderer
{
    private PixelShader? _shadowPixel;
    private RasterizerState? _shadowRasterizer;

    private void InitializeShadowPipeline(AssetPaths assets)
    {
        var compiler = new ShaderCompiler(assets);

        using var pixelCode = compiler.Compile(
            "Scene/SceneShadow.hlsl",
            "PSMain",
            "ps_4_0");

        _shadowPixel = new PixelShader(
            _device,
            pixelCode.Bytecode);

        _shadowRasterizer = new RasterizerState(
            _device,
            new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                IsDepthClipEnabled = true,
                DepthBias = 80,
                SlopeScaledDepthBias = 1.0f,
                DepthBiasClamp = 0f
            });
    }

    public void RenderShadowCasters(
        Vector3 center,
        float radius,
        Matrix4x4 viewProjection)
    {
        if (_disposed ||
            _shadowPixel == null ||
            _shadowRasterizer == null)
        {
            return;
        }

        float reach = Math.Clamp(
            radius,
            1f,
            512f);

        var extent = new Vector3(reach);

        Vector4[] planes = FrustumPlanes(
            viewProjection);

        var groups = new Dictionary<
            (string Model, bool Mirrored),
            DrawGroup>();

        foreach (SceneInstance instance in _scene.Query(
                     new SceneBounds(
                         center - extent,
                         center + extent)))
        {
            if (instance.IsEnvironmentCollision)
            {
                continue;
            }

            if (!Visible(instance.Bounds, planes))
            {
                continue;
            }

            bool mirrored =
                instance.World.GetDeterminant() < 0f;

            var key = (instance.Model, mirrored);

            if (!groups.TryGetValue(
                    key,
                    out DrawGroup? group))
            {
                group = new DrawGroup(
                    instance.Model,
                    mirrored);

                groups.Add(key, group);
            }

            group.Instances.Add(instance);

            group.DistanceSquared = MathF.Min(
                group.DistanceSquared,
                Vector3.DistanceSquared(
                    center,
                    instance.Bounds.Center));
        }

        DrawGroup[] ordered = groups.Values
            .OrderBy(group => group.DistanceSquared)
            .ToArray();

        _worlds.Clear();

        int uploadedModels = 0;
        long uploadedBytes = 0;

        foreach (DrawGroup group in ordered)
        {
            if (!_models.TryGetValue(
                    group.Model,
                    out ModelResources? model))
            {
                if (_failedModels.Contains(group.Model))
                {
                    continue;
                }

                if (!_scene.Models.TryGet(
                        group.Model,
                        out SceneModel source))
                {
                    _scene.Models.Request(group.Model);
                    continue;
                }

                long bytes = GeometryBytes(source);

                if (uploadedModels >= 2 ||
                    (uploadedBytes > 0 &&
                     uploadedBytes + bytes > ModelFrameBytes))
                {
                    continue;
                }

                if (!MakeModelRoom(bytes))
                {
                    continue;
                }

                try
                {
                    model = new ModelResources(
                        _device,
                        source,
                        bytes)
                    {
                        LastUsed = _frame
                    };

                    _models.Add(group.Model, model);

                    _modelBytes += bytes;
                    uploadedBytes += bytes;
                    uploadedModels++;
                }
                catch (Exception exception)
                {
                    _failedModels.Add(group.Model);

                    Warn(
                        $"Shadow model {group.Model}: " +
                        exception.Message);

                    continue;
                }
            }

            model.LastUsed = _frame;

            group.Resources = model;
            group.Start = _worlds.Count;

            foreach (SceneInstance instance in group.Instances)
            {
                if (_worlds.Count >= MaximumInstances)
                {
                    break;
                }

                _worlds.Add(instance.World);
            }

            group.Count = _worlds.Count - group.Start;
        }

        if (_worlds.Count == 0)
        {
            return;
        }

        _context.MapSubresource(
            _instances,
            MapMode.WriteDiscard,
            SharpDX.Direct3D11.MapFlags.None,
            out DataStream stream);

        try
        {
            using (stream)
            {
                stream.WriteRange(_worlds.ToArray());
            }
        }
        finally
        {
            _context.UnmapSubresource(_instances, 0);
        }

        try
        {
            _context.InputAssembler.InputLayout = _layout;

            _context.InputAssembler.PrimitiveTopology =
                PrimitiveTopology.TriangleList;

            _context.VertexShader.Set(_vertex);

            _context.VertexShader.SetConstantBuffer(
                0,
                _constants);

            _context.PixelShader.SetConstantBuffer(
                0,
                _constants);

            _context.PixelShader.SetSampler(
                0,
                _sampler);

            _context.OutputMerger.SetDepthStencilState(
                _depth);

            _context.OutputMerger.SetBlendState(null);

            _context.Rasterizer.State =
                _shadowRasterizer;

            foreach (DrawGroup group in ordered)
            {
                if (group.Resources == null ||
                    group.Count == 0)
                {
                    continue;
                }

                foreach (MeshResources mesh in
                         group.Resources.Meshes)
                {
                    if (mesh.Material.Transparent)
                    {
                        continue;
                    }

                    DrawShadowMesh(
                        mesh,
                        group.Start,
                        group.Count,
                        viewProjection);
                }
            }
        }
        finally
        {
            ResetBindings();
        }
    }

    private void DrawShadowMesh(
        MeshResources mesh,
        int start,
        int count,
        Matrix4x4 viewProjection)
    {
        SceneMaterial material = mesh.Material;

        var constants = new Constants
        {
            ViewProjection = viewProjection,
            Color = material.Color,
            CameraPosition = Vector4.Zero,
            Parameters = new Vector4(
                material.AlphaTest ? 0.4f : 0f,
                0f,
                0f,
                0f)
        };

        _context.UpdateSubresource(
            ref constants,
            _constants);

        _context.InputAssembler.SetVertexBuffers(
            0,
            new VertexBufferBinding(
                mesh.Vertices,
                48,
                0),
            new VertexBufferBinding(
                _instances,
                64,
                0));

        _context.InputAssembler.SetIndexBuffer(
            mesh.Indices,
            Format.R32_UInt,
            0);

        if (material.AlphaTest)
        {
            var diffuse = Texture(
                material.Diffuse,
                _white,
                out _);

            _context.PixelShader.Set(_shadowPixel);

            _context.PixelShader.SetShaderResource(
                0,
                diffuse.View);
        }
        else
        {
            _context.PixelShader.Set(null);
        }

        _context.DrawIndexedInstanced(
            mesh.IndexCount,
            count,
            0,
            0,
            start);
    }

    private void DisposeShadowPipeline()
    {
        _shadowRasterizer?.Dispose();
        _shadowPixel?.Dispose();

        _shadowRasterizer = null;
        _shadowPixel = null;
    }
}
