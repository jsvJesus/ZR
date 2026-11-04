using System.Numerics;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Editor.Viewport.Scene;

/// <summary>Depth-free transform handles; shaders and input layout belong to Dx11Renderer.</summary>
internal sealed class SceneGizmoRenderer : IDisposable
{
    private const int LineCapacity = 3 * ObjectGizmoGeometry.RingSegments * 2;
    private const int TriangleCapacity = 3 * ObjectGizmoGeometry.ArrowSegments * 12;
    private readonly DeviceContext _context;
    private readonly VertexShader _vertex;
    private readonly PixelShader _pixel;
    private readonly InputLayout _layout;
    private readonly List<ObjectGizmoVertex> _lines = new(LineCapacity);
    private readonly List<ObjectGizmoVertex> _triangles = new(TriangleCapacity);
    private readonly ObjectGizmoVertex[] _lineData = new ObjectGizmoVertex[LineCapacity];
    private readonly ObjectGizmoVertex[] _triangleData = new ObjectGizmoVertex[TriangleCapacity];
    private Buffer _lineBuffer = null!, _triangleBuffer = null!, _constants = null!;
    private DepthStencilState _depth = null!;
    private RasterizerState _raster = null!;

    public SceneGizmoRenderer(Device device, DeviceContext context, VertexShader vertex,
        PixelShader pixel, InputLayout layout)
    {
        _context = context; _vertex = vertex; _pixel = pixel; _layout = layout;
        try
        {
            _lineBuffer = new(device, LineCapacity * Utilities.SizeOf<ObjectGizmoVertex>(), ResourceUsage.Dynamic,
                BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            _triangleBuffer = new(device, TriangleCapacity * Utilities.SizeOf<ObjectGizmoVertex>(), ResourceUsage.Dynamic,
                BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            _constants = new(device, Utilities.SizeOf<Matrix4x4>(), ResourceUsage.Default,
                BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            var depth = DepthStencilStateDescription.Default();
            depth.IsDepthEnabled = false;
            depth.DepthWriteMask = DepthWriteMask.Zero;
            _depth = new(device, depth);
            _raster = new(device, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid, CullMode = CullMode.None,
                IsDepthClipEnabled = true, IsAntialiasedLineEnabled = true
            });
        }
        catch { Dispose(); throw; }
    }

    public void Render(ObjectGizmoFrame frame, ObjectGizmoMode mode, ObjectGizmoAxis highlighted,
        Matrix4x4 viewProjection)
    {
        ObjectGizmoGeometry.Build(frame, mode, highlighted, _lines, _triangles);
        Upload(_lineBuffer, _lines, _lineData);
        Upload(_triangleBuffer, _triangles, _triangleData);
        try
        {
            _context.UpdateSubresource(ref viewProjection, _constants);
            _context.InputAssembler.InputLayout = _layout;
            _context.InputAssembler.SetIndexBuffer(null, Format.R32_UInt, 0);
            _context.VertexShader.Set(_vertex);
            _context.VertexShader.SetConstantBuffer(0, _constants);
            _context.PixelShader.Set(_pixel);
            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(_depth);
            _context.Rasterizer.State = _raster;
            Draw(_lineBuffer, _lines.Count, PrimitiveTopology.LineList);
            Draw(_triangleBuffer, _triangles.Count, PrimitiveTopology.TriangleList);
        }
        finally
        {
            _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(null, 0, 0),
                new VertexBufferBinding(null, 0, 0));
            _context.InputAssembler.InputLayout = null;
            _context.VertexShader.SetConstantBuffer(0, null);
            _context.VertexShader.Set(null); _context.PixelShader.Set(null);
            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(null);
            _context.Rasterizer.State = null;
        }
    }

    private void Upload(Buffer buffer, List<ObjectGizmoVertex> vertices, ObjectGizmoVertex[] data)
    {
        if (vertices.Count == 0) return;
        if (vertices.Count > data.Length) throw new InvalidOperationException("Object gizmo vertex capacity exceeded.");
        vertices.CopyTo(data);
        _context.MapSubresource(buffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out DataStream stream);
        try
        {
            using (stream) stream.WriteRange(data, 0, vertices.Count);
        }
        finally { _context.UnmapSubresource(buffer, 0); }
    }

    private void Draw(Buffer buffer, int count, PrimitiveTopology topology)
    {
        if (count == 0) return;
        _context.InputAssembler.PrimitiveTopology = topology;
        _context.InputAssembler.SetVertexBuffers(0,
            new VertexBufferBinding(buffer, Utilities.SizeOf<ObjectGizmoVertex>(), 0),
            new VertexBufferBinding(null, 0, 0));
        _context.Draw(count, 0);
    }

    public void Dispose()
    {
        _raster?.Dispose(); _depth?.Dispose(); _constants?.Dispose();
        _triangleBuffer?.Dispose(); _lineBuffer?.Dispose();
    }
}
