using System.IO;
using Editor.Viewport.Terrain;
using SharpDX.Direct3D11;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Textures.Dds;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Roads;

internal sealed partial class RoadRenderer
{
    private const long TextureCacheBytes = 128L * 1024 * 1024;
    private readonly AssetPaths _assets;
    private readonly Dictionary<string, MaterialResources> _materialResources = new(StringComparer.Ordinal);
    private readonly HashSet<string> _materialWarnings = new(StringComparer.Ordinal);
    private Task<RoadAssetCatalog>? _catalogTask;
    private SamplerState? _materialSampler;
    private long _materialUse;

    private Vector4 BindMaterial(RoadDefinition road)
    {
        UnbindMaterial();
        float repeat = float.IsFinite(road.TextureRepeatLength) ? Math.Clamp(road.TextureRepeatLength, 0.1f, 100) : 4;
        var parameters = new Vector4(0, 0, 0, repeat);
        if (road.Type == RoadType.Railway || string.IsNullOrWhiteSpace(road.MaterialId)) return parameters;
        _catalogTask ??= RoadAssetCatalog.LoadCachedAsync(_assets);
        if (!_catalogTask.IsCompleted) return parameters;
        if (!_catalogTask.IsCompletedSuccessfully)
        {
            WarnMaterial("Road material catalog: " + (_catalogTask.Exception?.GetBaseException().Message ?? "scan canceled"));
            return parameters;
        }
        var option = _catalogTask.Result.Materials.FirstOrDefault(value => value.Id == road.MaterialId);
        if (option == null)
        {
            WarnMaterial($"Road material '{road.MaterialId}' is unavailable; using procedural surface.");
            return parameters;
        }
        if (!_materialResources.TryGetValue(option.Id, out var resources))
        {
            resources = LoadMaterial(option);
            while (_materialResources.Count >= 16 || _materialResources.Values.Sum(value => value.Bytes) + resources.Bytes > TextureCacheBytes)
            {
                if (_materialResources.Count == 0) break;
                var oldest = _materialResources.MinBy(pair => pair.Value.LastUse);
                oldest.Value.Dispose(); _materialResources.Remove(oldest.Key);
            }
            _materialResources.Add(option.Id, resources);
        }
        resources.LastUse = ++_materialUse;
        if (resources.Diffuse == null) return parameters;
        if (_materialSampler == null)
        {
            try
            {
                _materialSampler = new(_device, new SamplerStateDescription
                {
                    Filter = Filter.Anisotropic, MaximumAnisotropy = 8,
                    AddressU = TextureAddressMode.Wrap, AddressV = TextureAddressMode.Wrap, AddressW = TextureAddressMode.Wrap,
                    ComparisonFunction = Comparison.Never, MinimumLod = 0, MaximumLod = float.MaxValue
                });
            }
            catch (Exception error) when (Recoverable(error)) { WarnMaterial(error.Message); return parameters; }
        }
        _context.PixelShader.SetSampler(0, _materialSampler);
        _context.PixelShader.SetShaderResource(0, resources.Diffuse.View);
        _context.PixelShader.SetShaderResource(1, resources.Normal?.View);
        _context.PixelShader.SetShaderResource(2, resources.Specular?.View);
        return new Vector4(1, resources.Normal != null ? 1 : 0, resources.Specular != null ? 1 : 0, repeat);
    }

    private MaterialResources LoadMaterial(RoadMaterialOption option)
    {
        var resources = new MaterialResources();
        resources.Diffuse = LoadTexture(option.Diffuse, resources);
        if (resources.Diffuse == null) return resources;
        resources.Normal = LoadTexture(option.Normal, resources);
        resources.Specular = LoadTexture(option.Specular, resources);
        if (option.RequiresSpecializedShader || option.AlphaBlend || option.AlphaTest)
            WarnMaterial($"Road texture set '{option.Name}' uses base maps; source shader layers/alpha effects are not reproduced.");
        return resources;
    }

    private TerrainTexture? LoadTexture(string reference, MaterialResources owner)
    {
        if (reference.Length == 0) return null;
        try
        {
            if (!Path.GetExtension(reference).Equals(".dds", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Road texture is not a supported DDS: {reference}");
            string path = AssetPathResolver.Resolve(_assets.Root, reference);
            if (new FileInfo(path).Length > 16L * 1024 * 1024)
                throw new InvalidDataException($"Road texture exceeds 16 MiB: {reference}");
            var image = DdsReader.Read(path);
            long bytes = image.MipLevels.Sum(mip => (long)mip.Data.Length);
            if (bytes + owner.Bytes > 64L * 1024 * 1024)
                throw new InvalidDataException($"Road material exceeds 64 MiB: {reference}");
            var texture = new TerrainTexture(_device, image);
            owner.Bytes += bytes;
            return texture;
        }
        catch (Exception error) when (Recoverable(error) || error is UnauthorizedAccessException or NotSupportedException)
        {
            WarnMaterial($"Road texture '{reference}': {error.Message}");
            return null;
        }
    }

    private void UnbindMaterial()
    {
        _context.PixelShader.SetShaderResource(0, null);
        _context.PixelShader.SetShaderResource(1, null);
        _context.PixelShader.SetShaderResource(2, null);
        _context.PixelShader.SetSampler(0, null);
    }

    private void WarnMaterial(string message)
    {
        if (_materialWarnings.Count >= 256 || !_materialWarnings.Add(message)) return;
        if (_level.Roads.Warnings.Count < 256) _level.Roads.Warnings.Add(message);
        System.Diagnostics.Trace.TraceWarning(message);
    }

    private void DisposeMaterials()
    {
        foreach (var resources in _materialResources.Values) resources.Dispose();
        _materialResources.Clear();
        _materialSampler?.Dispose(); _materialSampler = null;
    }

    private sealed class MaterialResources : IDisposable
    {
        public TerrainTexture? Diffuse, Normal, Specular;
        public long Bytes, LastUse;
        public void Dispose() { Diffuse?.Dispose(); Normal?.Dispose(); Specular?.Dispose(); }
    }
}
