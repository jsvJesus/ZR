using SharpDX;
using SharpDX.Direct3D11;
using ZoneReborn.World.Terrain.Models;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Editor.Viewport.Terrain;

internal sealed class TerrainTileResources : IDisposable
{
    public Buffer Vertices { get; private set; } = null!;
    public Buffer Indices { get; private set; } = null!;
    public TerrainTexture Normals { get; private set; } = null!;
    public TerrainTexture WeightSum { get; private set; } = null!;
    public Dictionary<int, TerrainTexture> Masks { get; } = [];
    public byte[] ActiveLayers { get; }
    public int IndexCount { get; }
    public int EdgeIndexCount { get; }
    public int SkirtEdges { get; set; }

    public TerrainTileResources(Device device, TerrainDescriptor descriptor, TerrainTileData tile,
        TerrainTileData? right, TerrainTileData? bottom, TerrainTileData? diagonal, int layerCount,
        Func<int, int, float> overviewHeight)
    {
        ActiveLayers = tile.ActiveLayers.Distinct().Order().ToArray();
        try
        {
            var mesh = TerrainMeshBuilder.Build(descriptor, tile, right, bottom, diagonal, overviewHeight);
            Vertices = Buffer.Create(device, BindFlags.VertexBuffer, mesh.Vertices);
            Indices = Buffer.Create(device, BindFlags.IndexBuffer, mesh.Indices);
            IndexCount = mesh.MainIndexCount;
            EdgeIndexCount = mesh.EdgeIndexCount;
            Normals = TerrainTexture.Bc1(device, tile.Normals);
            WeightSum = TerrainTexture.WeightSum(device, TerrainMeshBuilder.BuildWeightSum(tile, layerCount));
            foreach (var mask in tile.Masks) Masks.Add(mask.Group, TerrainTexture.Bc1(device, mask.Data));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        foreach (var mask in Masks.Values) mask.Dispose();
        Masks.Clear();
        WeightSum?.Dispose();
        Normals?.Dispose();
        Indices?.Dispose();
        Vertices?.Dispose();
    }
}
