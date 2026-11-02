using SharpDX;
using SharpDX.Direct3D11;
using ZoneReborn.World.Terrain.Models;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Editor.Viewport.Terrain;

internal sealed class TerrainTileResources : IDisposable
{
    private readonly TerrainDescriptor _descriptor;
    private readonly int _tileX;
    private readonly int _tileZ;
    private readonly Func<int, int, float> _overviewHeight;
    private TerrainVertex[] _sourceVertices = [];
    public Buffer Vertices { get; private set; } = null!;
    public Buffer Indices { get; private set; } = null!;
    public TerrainTexture Normals { get; private set; } = null!;
    public TerrainTexture WeightSum { get; private set; } = null!;
    public Dictionary<int, TerrainTexture> Masks { get; } = [];
    public byte[] ActiveLayers { get; }
    public int IndexCount { get; }
    public int EdgeIndexCount { get; }
    public int SkirtEdges { get; private set; } = -1;
    public TerrainSurfaceGrid Surface { get; private set; } = null!;

    public TerrainTileResources(Device device, TerrainDescriptor descriptor, TerrainTileData tile,
        TerrainTileData? right, TerrainTileData? bottom, TerrainTileData? diagonal, int layerCount,
        Func<int, int, float> overviewHeight)
    {
        _descriptor = descriptor; _tileX = tile.X; _tileZ = tile.Z; _overviewHeight = overviewHeight;
        ActiveLayers = tile.ActiveLayers.Distinct().Order().ToArray();
        try
        {
            var mesh = TerrainMeshBuilder.Build(descriptor, tile, right, bottom, diagonal, overviewHeight);
            _sourceVertices = mesh.Vertices;
            Surface = new TerrainSurfaceGrid(mesh.Vertices.Take(TerrainMeshBuilder.Side * TerrainMeshBuilder.Side)
                .Select(v => v.Position).ToArray(), TerrainMeshBuilder.Side);
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

    public void UpdateLodEdges(DeviceContext context, int edges)
    {
        if (SkirtEdges == edges) return;
        int side = TerrainMeshBuilder.Side, mainCount = side * side;
        var vertices = (TerrainVertex[])_sourceVertices.Clone();
        for (int z = 0; z < side; z++)
        for (int x = 0; x < side; x++)
        {
            bool boundary = (z == 0 && (edges & 1) != 0) || (z == side - 1 && (edges & 2) != 0)
                || (x == 0 && (edges & 4) != 0) || (x == side - 1 && (edges & 8) != 0);
            if (!boundary) continue;
            int index = z * side + x;
            var vertex = vertices[index]; var position = vertex.Position;
            position.Y = _overviewHeight(_tileX * 256 + x * TerrainMeshBuilder.Step,
                _tileZ * 256 + z * TerrainMeshBuilder.Step);
            vertices[index] = vertex with { Position = position };
        }
        // Once the top borders agree, skirts extend down from the common border instead of
        // protruding to the old fine-grid height and occluding projected road edges.
        for (int edge = 0; edge < 4; edge++)
        {
            if ((edges & (1 << edge)) == 0) continue;
            for (int index = 0; index < side; index++)
            {
                int topIndex = edge switch
                {
                    0 => index, 1 => (side - 1) * side + index,
                    2 => index * side, _ => index * side + side - 1
                };
                int skirtIndex = mainCount + edge * side * 2 + index * 2;
                var position = vertices[topIndex].Position;
                vertices[skirtIndex] = vertices[skirtIndex] with { Position = position };
                position.Y -= 2 * _descriptor.CellSize;
                vertices[skirtIndex + 1] = vertices[skirtIndex + 1] with { Position = position };
            }
        }
        context.UpdateSubresource(vertices, Vertices);
        Surface = new TerrainSurfaceGrid(vertices.Take(mainCount).Select(v => v.Position).ToArray(), side);
        SkirtEdges = edges;
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
