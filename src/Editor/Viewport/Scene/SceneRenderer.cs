using System.Numerics;
using System.Runtime.InteropServices;
using Editor.Rendering.Shaders;
using Editor.Viewport.Terrain;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Textures.Dds;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Scene;

/// <summary>Streams shared scene geometry; instances only contribute one world matrix.</summary>
internal sealed class SceneRenderer : IDisposable
{
    private const long ModelCacheBytes = 128L * 1024 * 1024;
    private const long TextureCacheBytes = 128L * 1024 * 1024;
    private const long ModelFrameBytes = 16L * 1024 * 1024;
    private const long TextureFrameBytes = 4L * 1024 * 1024;
    private const int MaximumInstances = 65536;
    // Shared across scene replacement: an old in-flight file read cannot overlap another
    // large DDS decode. Completed requests only retain their selected resident mip chain.
    private static readonly SemaphoreSlim TextureDecoder = new(1, 1);
    private readonly Device _device;
    private readonly DeviceContext _context;
    private readonly SceneData _scene;
    private readonly Dictionary<string, ModelResources> _models = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TextureResources> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Task<TextureResult>> _pendingTextures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TextureDemand> _textureDemands = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TextureMipInfo[]> _textureInfo = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _failedModels = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _failedTextures = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _warnings = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource _stop = new();
    private readonly List<Matrix4x4> _worlds = [];
    private readonly List<TransparentDraw> _transparent = [];
    private readonly HashSet<string> _visibleObjects = new(StringComparer.Ordinal);
    private VertexShader _vertex = null!;
    private PixelShader _pixel = null!;
    private InputLayout _layout = null!;
    private Buffer _constants = null!;
    private Buffer _instances = null!;
    private SamplerState _sampler = null!;
    private RasterizerState _back = null!, _mirroredBack = null!, _twoSided = null!, _mirroredTwoSided = null!;
    private DepthStencilState _depth = null!, _depthRead = null!;
    private BlendState _alpha = null!;
    private TerrainTexture _white = null!, _flatNormal = null!;
    private long _modelBytes, _textureBytes, _frame;
    private bool _disposed, _preparedFrame;

    public SceneRenderer(Device device, DeviceContext context, AssetPaths assets, SceneData scene)
    {
        _device = device; _context = context; _scene = scene;
        try
        {
            var compiler = new ShaderCompiler(assets);
            using var vs = compiler.Compile("Scene/Scene.hlsl", "VSMain", "vs_4_0");
            using var ps = compiler.Compile("Scene/Scene.hlsl", "PSMain", "ps_4_0");
            _vertex = new(device, vs.Bytecode); _pixel = new(device, ps.Bytecode);
            // SceneVertex = position(12), normal(12), uv(8), tangent(16). System.Numerics
            // matrices use row vectors: the instance stream contains four unchanged rows.
            _layout = new(device, vs.Bytecode,
                [new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                 new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                 new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0),
                 new InputElement("TANGENT", 0, Format.R32G32B32A32_Float, 32, 0),
                 new InputElement("WORLD", 0, Format.R32G32B32A32_Float, 0, 1, InputClassification.PerInstanceData, 1),
                 new InputElement("WORLD", 1, Format.R32G32B32A32_Float, 16, 1, InputClassification.PerInstanceData, 1),
                 new InputElement("WORLD", 2, Format.R32G32B32A32_Float, 32, 1, InputClassification.PerInstanceData, 1),
                 new InputElement("WORLD", 3, Format.R32G32B32A32_Float, 48, 1, InputClassification.PerInstanceData, 1)]);
            _constants = new(device, Utilities.SizeOf<Constants>(), ResourceUsage.Default,
                BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _instances = new(device, MaximumInstances * 64, ResourceUsage.Dynamic,
                BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            _sampler = new(device, new SamplerStateDescription
            {
                Filter = Filter.Anisotropic, MaximumAnisotropy = 8,
                AddressU = TextureAddressMode.Wrap, AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap, MinimumLod = 0, MaximumLod = float.MaxValue,
                ComparisonFunction = Comparison.Never
            });
            var raster = new RasterizerStateDescription
                { FillMode = FillMode.Solid, CullMode = CullMode.Back, IsDepthClipEnabled = true };
            _back = new(device, raster);
            raster.IsFrontCounterClockwise = true; _mirroredBack = new(device, raster);
            raster.CullMode = CullMode.None; _mirroredTwoSided = new(device, raster);
            raster.IsFrontCounterClockwise = false; _twoSided = new(device, raster);
            var depth = DepthStencilStateDescription.Default();
            depth.DepthComparison = Comparison.LessEqual;
            _depth = new(device, depth);
            depth.DepthWriteMask = DepthWriteMask.Zero; _depthRead = new(device, depth);
            var blend = BlendStateDescription.Default();
            blend.RenderTarget[0].IsBlendEnabled = true;
            blend.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            blend.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            blend.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blend.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            blend.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
            blend.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            blend.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            _alpha = new(device, blend);
            _white = TerrainTexture.Solid(device, 255, 255, 255);
            _flatNormal = TerrainTexture.Solid(device, 128, 128, 255);
        }
        catch { Dispose(); throw; }
    }

    public void Render(FreeCamera camera, Matrix4x4 viewProjection, float farPlane)
    {
        if (_disposed) return;
        _preparedFrame = false;
        _frame++;
        _visibleObjects.Clear();
        // Hard visibility bounds also bound the number of spatial cells visited on large maps.
        float distance = float.IsFinite(farPlane) ? Math.Clamp(farPlane, 1, 1800) : 1800;
        var extent = new Vector3(distance);
        var planes = FrustumPlanes(viewProjection);
        var groups = new Dictionary<(string Model, bool Mirrored), DrawGroup>();
        foreach (var instance in _scene.Query(new(camera.Position - extent, camera.Position + extent)))
        {
            float radius = instance.Bounds.Radius;
            float distanceSquared = Vector3.DistanceSquared(camera.Position, instance.Bounds.Center);
            if (distanceSquared > (distance + radius) * (distance + radius) || !Visible(instance.Bounds, planes)) continue;
            bool mirrored = instance.World.GetDeterminant() < 0;
            var key = (instance.Model, mirrored);
            if (!groups.TryGetValue(key, out var group))
                groups.Add(key, group = new(instance.Model, mirrored));
            group.Instances.Add(instance);
            group.DistanceSquared = Math.Min(group.DistanceSquared, distanceSquared);
        }
        var ordered = groups.Values.OrderBy(g => g.DistanceSquared).ToArray();
        long uploaded = 0;
        int uploadedModels = 0;
        _worlds.Clear(); _transparent.Clear();
        foreach (var group in ordered)
        {
            if (!_models.TryGetValue(group.Model, out var model))
            {
                if (_failedModels.Contains(group.Model)) continue;
                if (!_scene.Models.TryGet(group.Model, out var source))
                { _scene.Models.Request(group.Model); continue; }
                long bytes = GeometryBytes(source);
                // A valid large model gets a whole frame's upload allowance instead of being
                // permanently omitted. The cache can temporarily grow for one oversized asset.
                if (uploadedModels >= 4 || (uploaded > 0 && uploaded + bytes > ModelFrameBytes)) continue;
                if (!MakeModelRoom(bytes)) continue;
                if (bytes > ModelFrameBytes)
                    Warn($"Scene model {group.Model}: uploading {bytes / (1024 * 1024)} MiB in one frame.");
                try
                {
                    model = new(_device, source, bytes) { LastUsed = _frame };
                    _models.Add(group.Model, model); _modelBytes += bytes;
                    uploaded += bytes; uploadedModels++;
                }
                catch (Exception exception)
                {
                    _failedModels.Add(group.Model);
                    Warn($"Scene model {group.Model}: GPU upload failed: {exception.Message}");
                    continue;
                }
            }
            model.LastUsed = _frame;
            group.Resources = model;
            group.Start = _worlds.Count;
            foreach (var instance in group.Instances)
            {
                if (_worlds.Count == MaximumInstances) break;
                int offset = _worlds.Count;
                _worlds.Add(instance.World);
                _visibleObjects.Add(instance.Id);
                foreach (var mesh in model.Meshes)
                    if (mesh.Material.Transparent)
                        _transparent.Add(new(mesh, offset, group.Mirrored,
                            Vector3.DistanceSquared(camera.Position, instance.Bounds.Center)));
            }
            group.Count = _worlds.Count - group.Start;
        }
        try
        {
            PrepareTextures(ordered);
            UploadReadyTextures();
            RequestTextures();
            if (_worlds.Count == 0) return;
            _context.MapSubresource(_instances, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out DataStream stream);
            try { using (stream) stream.WriteRange(_worlds.ToArray()); }
            finally { _context.UnmapSubresource(_instances, 0); }
            BindPipeline();
            _context.OutputMerger.SetDepthStencilState(_depth);
            _context.OutputMerger.SetBlendState(null);
            foreach (var group in ordered)
            {
                if (group.Resources == null || group.Count == 0) continue;
                foreach (var mesh in group.Resources.Meshes)
                    if (!mesh.Material.Transparent)
                        Draw(mesh, group.Start, group.Count, group.Mirrored, camera.Position, viewProjection);
            }
            // Defer glass until trees/characters have populated opaque depth. Mesh interiors
            // remain in source triangle order, as with other forward-rendered assets.
            _transparent.Sort((a, b) => b.DistanceSquared.CompareTo(a.DistanceSquared));
            _preparedFrame = true;
        }
        catch (Exception exception) { Warn($"Scene rendering failed: {exception.Message}"); }
        finally { ResetBindings(); }
    }

    public SceneInstance? Pick(Vector3 origin, Vector3 direction, float maximumDistance)
    {
        if (_disposed || !float.IsFinite(maximumDistance) || maximumDistance <= 0) return null;
        float nearest = Math.Min(maximumDistance, 1800);
        var end = origin + direction * nearest;
        var candidates = new List<(SceneInstance Instance, float Near)>();
        foreach (var instance in _scene.Query(new(Vector3.Min(origin, end), Vector3.Max(origin, end))))
        {
            if (!_visibleObjects.Contains(instance.Id) || !_models.ContainsKey(instance.Model)) continue;
            if (SceneVisualPicker.Intersects(instance.VisualLocalBounds.Transform(instance.World), origin,
                direction, nearest, out float near)) candidates.Add((instance, near));
        }
        candidates.Sort((a, b) => a.Near.CompareTo(b.Near));
        SceneInstance? picked = null;
        foreach (var (instance, near) in candidates)
        {
            if (near > nearest) break;
            if (!_scene.Models.TryGet(instance.Model, out var model))
            {
                _scene.Models.Request(instance.Model);
                continue;
            }
            if (!SceneVisualPicker.Pick(model, instance.InverseWorld, origin, direction, nearest, out float distance))
                continue;
            nearest = distance;
            picked = instance;
        }
        return picked;
    }

    public void RenderTransparent(FreeCamera camera, Matrix4x4 viewProjection)
    {
        if (_disposed || !_preparedFrame) return;
        _preparedFrame = false;
        if (_transparent.Count == 0) return;
        try
        {
            // Other renderers overwrite the pipeline between passes; only the scene's
            // prepared world-matrix stream and GPU models persist across those draws.
            BindPipeline();
            _context.OutputMerger.SetDepthStencilState(_depthRead);
            _context.OutputMerger.SetBlendState(_alpha);
            foreach (var draw in _transparent)
                Draw(draw.Mesh, draw.Offset, 1, draw.Mirrored, camera.Position, viewProjection);
        }
        catch (Exception exception) { Warn($"Transparent scene rendering failed: {exception.Message}"); }
        finally { ResetBindings(); }
    }

    private void BindPipeline()
    {
        _context.InputAssembler.InputLayout = _layout;
        _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        _context.VertexShader.Set(_vertex); _context.VertexShader.SetConstantBuffer(0, _constants);
        _context.PixelShader.Set(_pixel); _context.PixelShader.SetConstantBuffer(0, _constants);
        _context.PixelShader.SetSampler(0, _sampler);
    }

    private void Draw(MeshResources mesh, int start, int count, bool mirrored, Vector3 camera, Matrix4x4 viewProjection)
    {
        var material = mesh.Material;
        var diffuse = Texture(material.Diffuse, _white, out _);
        var normal = Texture(material.Normal, _flatNormal, out bool normalReady);
        var constants = new Constants
        {
            ViewProjection = viewProjection, Color = material.Color,
            CameraPosition = new Vector4(camera, 0),
            Parameters = new Vector4(material.AlphaTest ? 0.4f : 0,
                material.Transparent ? 1 : 0, normalReady ? 1 : 0, material.Specular)
        };
        _context.UpdateSubresource(ref constants, _constants);
        _context.Rasterizer.State = material.DoubleSided ? (mirrored ? _mirroredTwoSided : _twoSided)
            : mirrored ? _mirroredBack : _back;
        _context.InputAssembler.SetVertexBuffers(0,
            new VertexBufferBinding(mesh.Vertices, 48, 0), new VertexBufferBinding(_instances, 64, 0));
        _context.InputAssembler.SetIndexBuffer(mesh.Indices, Format.R32_UInt, 0);
        _context.PixelShader.SetShaderResource(0, diffuse.View);
        _context.PixelShader.SetShaderResource(1, normal.View);
        _context.DrawIndexedInstanced(mesh.IndexCount, count, 0, 0, start);
    }

    private TerrainTexture Texture(string path, TerrainTexture fallback, out bool ready)
    {
        ready = false;
        if (string.IsNullOrWhiteSpace(path)) return fallback;
        if (_textures.TryGetValue(path, out var texture))
        { texture.LastUsed = _frame; ready = true; return texture.Texture; }
        return fallback;
    }

    private void PrepareTextures(DrawGroup[] groups)
    {
        _textureDemands.Clear();
        foreach (var group in groups)
        {
            if (group.Resources == null || group.Count == 0) continue;
            foreach (var mesh in group.Resources.Meshes)
            {
                Demand(mesh.Material.Diffuse, group.DistanceSquared, false);
                Demand(mesh.Material.Normal, group.DistanceSquared, true);
            }
        }
        foreach (var pair in _pendingTextures)
            if (pair.Value.IsCompletedSuccessfully && pair.Value.Result.Image is { } image)
                _textureInfo[pair.Key] = MipInfo(image);

        // Reserve a base mip chain for every visible texture first. Spending the whole
        // budget on the first few models would permanently starve the rest of the view.
        int baseline = 256;
        long bytes;
        do
        {
            bytes = _textureDemands.Sum(p => TextureBytes(p.Key, baseline));
            if (bytes <= TextureCacheBytes || baseline == 4) break;
            baseline /= 2;
        } while (true);
        foreach (var demand in _textureDemands.Values) demand.Limit = baseline;
        foreach (var pair in _textureDemands.OrderBy(p => p.Value.DistanceSquared).ThenBy(p => p.Value.Normal))
        {
            var demand = pair.Value;
            for (int limit = baseline * 2; limit <= demand.DesiredLimit; limit *= 2)
            {
                long extra = TextureBytes(pair.Key, limit) - TextureBytes(pair.Key, demand.Limit);
                if (bytes + extra > TextureCacheBytes) break;
                bytes += extra; demand.Limit = limit;
            }
        }

        void Demand(string path, float distanceSquared, bool normal)
        {
            if (string.IsNullOrWhiteSpace(path) || _failedTextures.Contains(path)) return;
            int desired = distanceSquared < 64 * 64 ? 1024 : distanceSquared < 256 * 256 ? 512 : 256;
            if (_textureDemands.TryGetValue(path, out var demand))
            {
                demand.DistanceSquared = Math.Min(demand.DistanceSquared, distanceSquared);
                demand.DesiredLimit = Math.Max(demand.DesiredLimit, desired);
                demand.Normal &= normal;
            }
            else _textureDemands.Add(path, new(distanceSquared, desired, normal));
            if (_textures.TryGetValue(path, out var resident)) resident.LastUsed = _frame;
        }
    }

    private long TextureBytes(string path, int limit)
    {
        if (_textureInfo.TryGetValue(path, out var info))
        {
            int first = 0;
            while (first + 1 < info.Length && info[first].Dimension > limit) first++;
            return info[first].Bytes;
        }
        // BC3 is the conservative compressed estimate until the background reader
        // supplies exact mip sizes (including the few uncompressed source textures).
        return (long)limit * limit * 4 / 3 + 64;
    }

    private static TextureMipInfo[] MipInfo(DdsImage image)
    {
        int selectable = 1;
        while (selectable < image.MipLevels.Count &&
            (image.Format == DdsPixelFormat.Rgba8 || BlockAligned(image.MipLevels[selectable]))) selectable++;
        var info = new TextureMipInfo[selectable];
        long bytes = 0;
        for (int i = image.MipLevels.Count - 1; i >= 0; i--)
        {
            var mip = image.MipLevels[i];
            bytes += mip.Data.LongLength;
            if (i < info.Length) info[i] = new(Math.Max(mip.Width, mip.Height), bytes);
        }
        return info;
    }

    private static DdsImage SelectMips(DdsImage image, int limit)
    {
        int first = 0;
        while (first + 1 < image.MipLevels.Count &&
            Math.Max(image.MipLevels[first].Width, image.MipLevels[first].Height) > limit &&
            (image.Format == DdsPixelFormat.Rgba8 || BlockAligned(image.MipLevels[first + 1]))) first++;
        if (first == 0) return image;
        var mips = image.MipLevels.Skip(first).ToArray();
        return new(mips[0].Width, mips[0].Height, image.Format, mips);
    }

    // BC mip tails may reach 1x1, but a new BC resource's top level must be block aligned.
    private static bool BlockAligned(DdsMipLevel mip) => mip.Width % 4 == 0 && mip.Height % 4 == 0;

    private void RequestTextures()
    {
        // Fill missing diffuse/normal maps before refining existing ones. Draw order
        // must not decide which materials are allowed to enter the request queue.
        foreach (var pair in _textureDemands.OrderBy(p => _textures.ContainsKey(p.Key))
            .ThenBy(p => p.Value.Normal).ThenBy(p => p.Value.DistanceSquared))
        {
            if (_pendingTextures.Count >= 2) break;
            if (_pendingTextures.ContainsKey(pair.Key)) continue;
            if (_textures.TryGetValue(pair.Key, out var resident) &&
                Math.Max(resident.Image.Width, resident.Image.Height) >= RequiredDimension(pair.Key, pair.Value.Limit)) continue;
            _pendingTextures.Add(pair.Key, ReadTextureAsync(pair.Key, _stop.Token));
        }
    }

    private int RequiredDimension(string path, int limit)
    {
        if (!_textureInfo.TryGetValue(path, out var info)) return limit;
        int first = 0;
        while (first + 1 < info.Length && info[first].Dimension > limit) first++;
        return info[first].Dimension;
    }

    private static Task<TextureResult> ReadTextureAsync(string path, CancellationToken token) => Task.Run(() =>
    {
        bool entered = false;
        try
        {
            TextureDecoder.Wait(token); entered = true;
            var image = SceneTextureMips.Complete(DdsReader.Read(path), token);
            token.ThrowIfCancellationRequested();
            return new TextureResult(SelectMips(image, 1024), null);
        }
        catch (OperationCanceledException) { return new TextureResult(null, null); }
        catch (Exception exception) { return new TextureResult(null, exception.Message); }
        finally { if (entered) TextureDecoder.Release(); }
    });

    private void UploadReadyTextures()
    {
        long uploaded = 0;
        int count = 0;
        // Keep the resident mip tails in CPU memory so pressure can reduce detail
        // without dropping the texture or waiting for another disk read.
        foreach (var pair in _textures.ToArray())
        {
            if (!_textureDemands.TryGetValue(pair.Key, out var demand)) continue;
            var old = pair.Value;
            var image = SelectMips(old.Image, demand.Limit);
            if (ReferenceEquals(image, old.Image)) continue;
            long bytes = image.MipLevels.Sum(m => m.Data.LongLength);
            if (count >= 8 || (uploaded > 0 && uploaded + bytes > TextureFrameBytes)) continue;
            try
            {
                var texture = new TerrainTexture(_device, image);
                _textures[pair.Key] = new(texture, image, bytes, _frame);
                old.Texture.Dispose(); _textureBytes += bytes - old.Bytes;
                uploaded += bytes; count++;
            }
            catch (Exception exception) { Warn($"Scene DDS {pair.Key}: mip reduction failed: {exception.Message}"); }
        }
        foreach (var pair in _pendingTextures.ToArray())
        {
            if (!pair.Value.IsCompletedSuccessfully) continue;
            var result = pair.Value.Result;
            if (result.Image == null)
            {
                _pendingTextures.Remove(pair.Key);
                _failedTextures.Add(pair.Key);
                if (result.Error != null) Warn($"Scene DDS {pair.Key}: {result.Error}");
                continue;
            }
            // A reader can finish after PrepareTextures scanned the pending queue.
            // Always publish its sizes before consuming the result, so subsequent
            // frames plan with exact costs and small textures are not reread forever.
            _textureInfo[pair.Key] = MipInfo(result.Image);
            if (!_textureDemands.TryGetValue(pair.Key, out var demand))
            { _pendingTextures.Remove(pair.Key); continue; }
            var image = SelectMips(result.Image, demand.Limit);
            long bytes = image.MipLevels.Sum(m => m.Data.LongLength);
            _textures.TryGetValue(pair.Key, out var old);
            if (count >= 8 || (uploaded > 0 && uploaded + bytes > TextureFrameBytes)) continue;
            if (!MakeTextureRoom(bytes - (old?.Bytes ?? 0))) continue;
            _pendingTextures.Remove(pair.Key);
            try
            {
                var texture = new TerrainTexture(_device, image);
                _textures[pair.Key] = new(texture, image, bytes, _frame);
                old?.Texture.Dispose();
                _textureBytes += bytes - (old?.Bytes ?? 0); uploaded += bytes; count++;
            }
            catch (Exception exception)
            {
                if (old == null) _failedTextures.Add(pair.Key);
                Warn($"Scene DDS {pair.Key}: GPU upload failed: {exception.Message}");
            }
        }
    }

    private bool MakeModelRoom(long bytes)
    {
        // Earlier groups already reference their buffers for this draw. Keep them alive and
        // allow one unusually large model alongside those groups until the next eviction.
        if (bytes > ModelCacheBytes && _models.Values.Any(m => m.Bytes > ModelCacheBytes && m.LastUsed == _frame))
            return false;
        long budget = bytes > ModelCacheBytes
            ? bytes + _models.Values.Where(m => m.LastUsed == _frame).Sum(m => m.Bytes)
            : ModelCacheBytes;
        while (_modelBytes + bytes > budget || _models.Count >= 1024)
        {
            var oldest = _models.Where(p => p.Value.LastUsed != _frame)
                .OrderBy(p => p.Value.LastUsed).FirstOrDefault();
            if (oldest.Value == null) return false;
            oldest.Value.Dispose(); _modelBytes -= oldest.Value.Bytes; _models.Remove(oldest.Key);
        }
        return true;
    }

    private static long GeometryBytes(SceneModel model)
    {
        var vertices = new HashSet<SceneVertex[]>(ReferenceEqualityComparer.Instance);
        long bytes = 0;
        foreach (var mesh in model.Meshes)
        {
            if (mesh.Vertices.Length == 0 || mesh.Indices.Length == 0) continue;
            if (vertices.Add(mesh.Vertices)) bytes += (long)mesh.Vertices.Length * 48;
            bytes += (long)mesh.Indices.Length * 4;
        }
        return bytes;
    }

    private bool MakeTextureRoom(long bytes)
    {
        long budget = Math.Max(TextureCacheBytes, bytes);
        while (_textureBytes + bytes > budget)
        {
            var oldest = _textures.Where(p => p.Value.LastUsed != _frame)
                .OrderBy(p => p.Value.LastUsed).FirstOrDefault();
            if (oldest.Value == null) return false;
            oldest.Value.Texture.Dispose(); _textureBytes -= oldest.Value.Bytes; _textures.Remove(oldest.Key);
        }
        return true;
    }

    private static Vector4[] FrustumPlanes(Matrix4x4 m) =>
        [new(m.M14 + m.M11, m.M24 + m.M21, m.M34 + m.M31, m.M44 + m.M41),
         new(m.M14 - m.M11, m.M24 - m.M21, m.M34 - m.M31, m.M44 - m.M41),
         new(m.M14 + m.M12, m.M24 + m.M22, m.M34 + m.M32, m.M44 + m.M42),
         new(m.M14 - m.M12, m.M24 - m.M22, m.M34 - m.M32, m.M44 - m.M42),
         new(m.M13, m.M23, m.M33, m.M43),
         new(m.M14 - m.M13, m.M24 - m.M23, m.M34 - m.M33, m.M44 - m.M43)];

    private static bool Visible(SceneBounds bounds, Vector4[] planes)
    {
        // Positive AABB vertex against each clip plane: works without normalizing planes.
        foreach (var p in planes)
            if (p.X * (p.X >= 0 ? bounds.Maximum.X : bounds.Minimum.X)
                + p.Y * (p.Y >= 0 ? bounds.Maximum.Y : bounds.Minimum.Y)
                + p.Z * (p.Z >= 0 ? bounds.Maximum.Z : bounds.Minimum.Z) + p.W < 0) return false;
        return true;
    }

    private void Warn(string message)
    {
        if (_warnings.Add(message)) _scene.Warnings.Enqueue(message);
    }

    private void ResetBindings()
    {
        _context.PixelShader.SetShaderResource(0, null); _context.PixelShader.SetShaderResource(1, null);
        _context.PixelShader.SetSampler(0, null);
        _context.VertexShader.SetConstantBuffer(0, null); _context.PixelShader.SetConstantBuffer(0, null);
        _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(null, 0, 0), new VertexBufferBinding(null, 0, 0));
        _context.InputAssembler.SetIndexBuffer(null, Format.R32_UInt, 0);
        _context.InputAssembler.InputLayout = null;
        _context.VertexShader.Set(null); _context.PixelShader.Set(null);
        _context.OutputMerger.SetBlendState(null); _context.OutputMerger.SetDepthStencilState(null);
        _context.Rasterizer.State = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stop.Cancel(); _stop.Dispose();
        // Workers own CPU-only data and observe cancellation; disposal never waits for disk IO.
        _pendingTextures.Clear();
        foreach (var model in _models.Values) model.Dispose();
        foreach (var texture in _textures.Values) texture.Texture.Dispose();
        _models.Clear(); _textures.Clear(); _textureDemands.Clear(); _textureInfo.Clear();
        _worlds.Clear(); _transparent.Clear();
        _visibleObjects.Clear();
        _flatNormal?.Dispose(); _white?.Dispose(); _alpha?.Dispose(); _depthRead?.Dispose(); _depth?.Dispose();
        _mirroredTwoSided?.Dispose(); _twoSided?.Dispose(); _mirroredBack?.Dispose(); _back?.Dispose(); _sampler?.Dispose();
        _instances?.Dispose(); _constants?.Dispose(); _layout?.Dispose(); _pixel?.Dispose(); _vertex?.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Constants
    {
        public Matrix4x4 ViewProjection;
        public Vector4 Color, CameraPosition, Parameters;
    }

    private sealed class DrawGroup(string model, bool mirrored)
    {
        public string Model { get; } = model;
        public bool Mirrored { get; } = mirrored;
        public List<SceneInstance> Instances { get; } = [];
        public float DistanceSquared { get; set; } = float.MaxValue;
        public ModelResources? Resources { get; set; }
        public int Start { get; set; }
        public int Count { get; set; }
    }

    private sealed class MeshResources : IDisposable
    {
        public Buffer Vertices { get; }
        public Buffer Indices { get; }
        public SceneMaterial Material { get; }
        public int IndexCount { get; }
        public MeshResources(Device device, SceneMesh mesh, Buffer vertices)
        {
            Material = mesh.Material; IndexCount = mesh.Indices.Length;
            Vertices = vertices;
            Indices = Buffer.Create(device, BindFlags.IndexBuffer, mesh.Indices);
        }
        public void Dispose() => Indices.Dispose();
    }

    private sealed class ModelResources : IDisposable
    {
        public List<MeshResources> Meshes { get; } = [];
        private readonly List<Buffer> _vertices = [];
        public long Bytes { get; }
        public long LastUsed { get; set; }
        public ModelResources(Device device, SceneModel model, long bytes)
        {
            Bytes = bytes;
            var shared = new Dictionary<SceneVertex[], Buffer>(ReferenceEqualityComparer.Instance);
            try
            {
                foreach (var mesh in model.Meshes)
                {
                    if (mesh.Vertices.Length == 0 || mesh.Indices.Length == 0) continue;
                    // Source material groups can share a seam-expanded vertex array.
                    if (!shared.TryGetValue(mesh.Vertices, out var vertices))
                    {
                        vertices = Buffer.Create(device, BindFlags.VertexBuffer, mesh.Vertices);
                        _vertices.Add(vertices);
                        shared.Add(mesh.Vertices, vertices);
                    }
                    Meshes.Add(new(device, mesh, vertices));
                }
            }
            catch { Dispose(); throw; }
        }
        public void Dispose()
        {
            foreach (var mesh in Meshes) mesh.Dispose();
            foreach (var buffer in _vertices) buffer.Dispose();
            Meshes.Clear(); _vertices.Clear();
        }
    }

    private sealed class TextureResources(TerrainTexture texture, DdsImage image, long bytes, long lastUsed)
    {
        public TerrainTexture Texture { get; } = texture;
        public DdsImage Image { get; } = image;
        public long Bytes { get; } = bytes;
        public long LastUsed { get; set; } = lastUsed;
    }
    private sealed class TextureDemand(float distanceSquared, int desiredLimit, bool normal)
    {
        public float DistanceSquared { get; set; } = distanceSquared;
        public int DesiredLimit { get; set; } = desiredLimit;
        public bool Normal { get; set; } = normal;
        public int Limit { get; set; }
    }
    private readonly record struct TextureMipInfo(int Dimension, long Bytes);
    private sealed record TextureResult(DdsImage? Image, string? Error);
    private readonly record struct TransparentDraw(MeshResources Mesh, int Offset, bool Mirrored, float DistanceSquared);
}
