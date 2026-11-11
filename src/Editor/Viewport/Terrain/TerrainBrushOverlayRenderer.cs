using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Terrain;

internal sealed class TerrainBrushOverlayRenderer : IDisposable
{
    private const int MaximumSegments = 512;

    private readonly DeviceContext _context;
    private readonly VertexShader _vertexShader;
    private readonly PixelShader _pixelShader;
    private readonly InputLayout _layout;

    private Buffer _vertices = null!;
    private Buffer _constants = null!;
    private DepthStencilState _depth = null!;
    private RasterizerState _raster = null!;

    public TerrainBrushOverlayRenderer(
        Device device,
        DeviceContext context,
        VertexShader vertexShader,
        PixelShader pixelShader,
        InputLayout layout)
    {
        _context = context;
        _vertexShader = vertexShader;
        _pixelShader = pixelShader;
        _layout = layout;

        try
        {
            _vertices = new Buffer(
                device,
                (MaximumSegments * 2 + 8) *
                Utilities.SizeOf<LineVertex>(),
                ResourceUsage.Dynamic,
                BindFlags.VertexBuffer,
                CpuAccessFlags.Write,
                ResourceOptionFlags.None,
                0
            );

            _constants = new Buffer(
                device,
                Utilities.SizeOf<Matrix4x4>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0
            );

            var depth = DepthStencilStateDescription.Default();
            depth.IsDepthEnabled = true;
            depth.DepthWriteMask = DepthWriteMask.Zero;
            depth.DepthComparison = Comparison.LessEqual;

            _depth = new DepthStencilState(device, depth);

            _raster = new RasterizerState(
                device,
                new RasterizerStateDescription
                {
                    FillMode = FillMode.Solid,
                    CullMode = CullMode.None,
                    IsDepthClipEnabled = true,
                    IsAntialiasedLineEnabled = true,
                    DepthBias = -128,
                    SlopeScaledDepthBias = -1f
                }
            );
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Render(
        TerrainRenderer terrain,
        Vector3 center,
        float radius,
        bool material,
        bool includeHoles,
        Matrix4x4 viewProjection)
    {
        if (!float.IsFinite(radius) ||
            radius <= 0 ||
            !float.IsFinite(center.X) ||
            !float.IsFinite(center.Y) ||
            !float.IsFinite(center.Z))
        {
            return;
        }

        Vector4 color = material
            ? new Vector4(1f, 0.65f, 0.12f, 1f)
            : new Vector4(0.15f, 0.85f, 1f, 1f);

        Vector4 centerColor = new(1f, 1f, 1f, 1f);

        float spacing = Math.Max(
            0.25f,
            Math.Min(terrain.Descriptor.CellSize, 1.25f)
        );

        int segments = Math.Clamp(
            (int)MathF.Ceiling(MathF.Tau * radius / spacing),
            64,
            MaximumSegments
        );

        float heightOffset = Math.Clamp(
            terrain.Descriptor.CellSize * 0.05f,
            0.06f,
            0.2f
        );

        var vertices = new List<LineVertex>(segments * 2 + 8);
        Vector3? previous = null;

        for (int index = 0; index <= segments; index++)
        {
            float angle = MathF.Tau * index / segments;

            float x = center.X + MathF.Cos(angle) * radius;
            float z = center.Z + MathF.Sin(angle) * radius;

            if (!terrain.SurfaceHeight(
                    x,
                    z,
                    out float height,
                    includeHoles))
            {
                previous = null;
                continue;
            }

            Vector3 current = new(
                x,
                height + heightOffset,
                z
            );

            if (previous is { } start)
            {
                vertices.Add(new LineVertex(start, color));
                vertices.Add(new LineVertex(current, color));
            }

            previous = current;
        }

        float crossSize = Math.Clamp(
            radius * 0.12f,
            0.2f,
            1.25f
        );

        AddCrossLine(
            center.X - crossSize,
            center.Z,
            center.X + crossSize,
            center.Z
        );

        AddCrossLine(
            center.X,
            center.Z - crossSize,
            center.X,
            center.Z + crossSize
        );

        if (vertices.Count == 0)
        {
            return;
        }

        LineVertex[] data = vertices.ToArray();

        _context.MapSubresource(
            _vertices,
            MapMode.WriteDiscard,
            SharpDX.Direct3D11.MapFlags.None,
            out DataStream stream
        );

        try
        {
            using (stream)
            {
                stream.WriteRange(data);
            }
        }
        finally
        {
            _context.UnmapSubresource(_vertices, 0);
        }

        try
        {
            _context.UpdateSubresource(
                ref viewProjection,
                _constants
            );

            _context.InputAssembler.InputLayout = _layout;

            _context.InputAssembler.PrimitiveTopology =
                PrimitiveTopology.LineList;

            _context.InputAssembler.SetVertexBuffers(
                0,
                new VertexBufferBinding(
                    _vertices,
                    Utilities.SizeOf<LineVertex>(),
                    0
                ),
                new VertexBufferBinding(null, 0, 0)
            );

            _context.InputAssembler.SetIndexBuffer(
                null,
                Format.R32_UInt,
                0
            );

            _context.VertexShader.Set(_vertexShader);
            _context.VertexShader.SetConstantBuffer(
                0,
                _constants
            );

            _context.PixelShader.Set(_pixelShader);

            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(_depth);
            _context.Rasterizer.State = _raster;

            _context.Draw(data.Length, 0);
        }
        finally
        {
            _context.InputAssembler.SetVertexBuffers(
                0,
                new VertexBufferBinding(null, 0, 0),
                new VertexBufferBinding(null, 0, 0)
            );

            _context.InputAssembler.InputLayout = null;

            _context.VertexShader.SetConstantBuffer(0, null);
            _context.VertexShader.Set(null);
            _context.PixelShader.Set(null);

            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(null);
            _context.Rasterizer.State = null;
        }

        void AddCrossLine(
            float x1,
            float z1,
            float x2,
            float z2)
        {
            if (!terrain.SurfaceHeight(
                    x1,
                    z1,
                    out float firstHeight,
                    includeHoles) ||
                !terrain.SurfaceHeight(
                    x2,
                    z2,
                    out float secondHeight,
                    includeHoles))
            {
                return;
            }

            vertices.Add(new LineVertex(
                new Vector3(x1, firstHeight + heightOffset, z1),
                centerColor
            ));

            vertices.Add(new LineVertex(
                new Vector3(x2, secondHeight + heightOffset, z2),
                centerColor
            ));
        }
    }

    public void Dispose()
    {
        _raster?.Dispose();
        _depth?.Dispose();
        _constants?.Dispose();
        _vertices?.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct LineVertex(
        Vector3 Position,
        Vector4 Color
    );
}
