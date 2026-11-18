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
    private Func<int, int, float> _overviewHeight;
    private TerrainVertex[] _sourceVertices = [];
    public Buffer Vertices { get; private set; } = null!;
    public Buffer Indices { get; private set; } = null!;
    public TerrainTexture Normals { get; private set; } = null!;
    public TerrainTexture WeightSum { get; private set; } = null!;
    public Dictionary<int, TerrainTexture> Masks { get; } = [];
    public byte[] ActiveLayers { get; private set; }
    public long MaterialVersion { get; set; }
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
            int side = TerrainMeshBuilder.Side(descriptor);
            Surface = new TerrainSurfaceGrid(mesh.Vertices.Take(side * side)
                .Select(v => v.Position).ToArray(), side);
            Vertices = Buffer.Create(device, BindFlags.VertexBuffer, mesh.Vertices);
            Indices = Buffer.Create(device, BindFlags.IndexBuffer, mesh.Indices);
            IndexCount = mesh.MainIndexCount;
            EdgeIndexCount = mesh.EdgeIndexCount;
            Normals = TerrainTexture.Bc1(device, tile.Normals);
            WeightSum = TerrainTexture.WeightSum(device, TerrainMeshBuilder.BuildWeightSum(tile, layerCount));
            foreach (var mask in tile.Masks) Masks.Add(mask.Group, TerrainTexture.Mask(device, mask.Data));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void SetOverviewHeight(
        Func<int, int, float> overviewHeight)
    {
        ArgumentNullException.ThrowIfNull(overviewHeight);

        _overviewHeight = overviewHeight;

        // Force the LOD transition geometry to use
        // the current overview on the next render.
        SkirtEdges = -1;
    }

    public void UpdateLodEdges(DeviceContext context, int edges)
    {
        if (SkirtEdges == edges)
        {
            return;
        }

        int side = TerrainMeshBuilder.Side(_descriptor);
        int mainCount = side * side;

        var vertices = (TerrainVertex[])_sourceVertices.Clone();

        for (int edge = 0; edge < 4; edge++)
        {
            if ((edges & (1 << edge)) == 0)
            {
                continue;
            }

            for (int index = 0; index < side; index++)
            {
                int topIndex = edge switch
                {
                    0 => index,
                    1 => (side - 1) * side + index,
                    2 => index * side,
                    _ => index * side + side - 1
                };

                int skirtIndex =
                    mainCount +
                    edge * side * 2 +
                    index * 2;

                TerrainVertex top = vertices[topIndex];

                int globalX =
                    _tileX * 256 +
                    (int)MathF.Round(top.TileUv.X * 256f);

                int globalZ =
                    _tileZ * 256 +
                    (int)MathF.Round(top.TileUv.Y * 256f);

                float overviewHeight = _overviewHeight(globalX, globalZ);

                float clearance = MathF.Max(
                    2f * _descriptor.CellSize,
                    0.01f
                );

                float lowerHeight = MathF.Min(
                    top.Position.Y,
                    overviewHeight
                ) - clearance;

                vertices[skirtIndex] = top;

                var lowerPosition = top.Position;
                lowerPosition.Y = lowerHeight;

                vertices[skirtIndex + 1] = top with
                {
                    Position = lowerPosition
                };
            }
        }

        context.UpdateSubresource(vertices, Vertices);

        Surface = new TerrainSurfaceGrid(
            vertices
                .Take(mainCount)
                .Select(vertex => vertex.Position)
                .ToArray(),
            side
        );

        SkirtEdges = edges;
    }

    public void RefreshMaterials(Device device, TerrainTileData tile, int layerCount)
    {
        var masks = new Dictionary<int, TerrainTexture>();
        TerrainTexture? weightSum = null;
        try
        {
            weightSum = TerrainTexture.WeightSum(device, TerrainMeshBuilder.BuildWeightSum(tile, layerCount));
            foreach (var mask in tile.Masks) masks.Add(mask.Group, TerrainTexture.Mask(device, mask.Data));
        }
        catch
        {
            weightSum?.Dispose(); foreach (var texture in masks.Values) texture.Dispose(); throw;
        }
        foreach (var texture in Masks.Values) texture.Dispose();
        Masks.Clear(); foreach (var pair in masks) Masks.Add(pair.Key, pair.Value);
        WeightSum.Dispose(); WeightSum = weightSum;
        ActiveLayers = tile.ActiveLayers.Distinct().Order().ToArray();
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
