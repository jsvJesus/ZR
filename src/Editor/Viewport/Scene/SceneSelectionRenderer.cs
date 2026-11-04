using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Scene;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Scene;

/// <summary>Draws the selected leaf's transformed visual bounds with the shared grid shaders.</summary>
internal sealed class SceneSelectionRenderer : IDisposable
{
    private readonly DeviceContext _context;
    // Shader resources belong to Dx11Renderer and outlive this helper.
    private readonly VertexShader _vertex;
    private readonly PixelShader _pixel;
    private readonly InputLayout _layout;
    private Buffer _vertices = null!, _constants = null!;
    private DepthStencilState _depth = null!;
    private RasterizerState _raster = null!;

    public SceneSelectionRenderer(Device device, DeviceContext context, VertexShader vertex,
        PixelShader pixel, InputLayout layout)
    {
        _context = context; _vertex = vertex; _pixel = pixel; _layout = layout;
        try
        {
            _vertices = new(device, 24 * Utilities.SizeOf<LineVertex>(), ResourceUsage.Dynamic,
                BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            _constants = new(device, Utilities.SizeOf<Matrix4x4>(), ResourceUsage.Default,
                BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            var depth = DepthStencilStateDescription.Default();
            depth.DepthComparison = Comparison.LessEqual;
            depth.DepthWriteMask = DepthWriteMask.Zero;
            _depth = new(device, depth);
            _raster = new(device, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid, CullMode = CullMode.None,
                IsDepthClipEnabled = true, IsAntialiasedLineEnabled = true, DepthBias = -1
            });
        }
        catch { Dispose(); throw; }
    }

    public void Render(SceneInstance instance, Matrix4x4 viewProjection)
    {
        var bounds = instance.VisualLocalBounds;
        var corners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            corners[i] = Vector3.Transform(new Vector3(
                (i & 1) == 0 ? bounds.Minimum.X : bounds.Maximum.X,
                (i & 2) == 0 ? bounds.Minimum.Y : bounds.Maximum.Y,
                (i & 4) == 0 ? bounds.Minimum.Z : bounds.Maximum.Z), instance.World);
            if (!float.IsFinite(corners[i].X) || !float.IsFinite(corners[i].Y) ||
                !float.IsFinite(corners[i].Z)) return;
        }
        var vertices = new LineVertex[24];
        var color = new Vector4(1, 0.75f, 0.12f, 1);
        int index = 0;
        for (int i = 0; i < 8; i++)
        for (int axis = 1; axis <= 4; axis *= 2)
        {
            if ((i & axis) != 0) continue;
            vertices[index++] = new(corners[i], color);
            vertices[index++] = new(corners[i | axis], color);
        }
        _context.MapSubresource(_vertices, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None,
            out DataStream stream);
        try { using (stream) stream.WriteRange(vertices); }
        finally { _context.UnmapSubresource(_vertices, 0); }
        try
        {
            _context.UpdateSubresource(ref viewProjection, _constants);
            _context.InputAssembler.InputLayout = _layout;
            _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertices,
                Utilities.SizeOf<LineVertex>(), 0), new VertexBufferBinding(null, 0, 0));
            _context.InputAssembler.SetIndexBuffer(null, Format.R32_UInt, 0);
            _context.VertexShader.Set(_vertex);
            _context.VertexShader.SetConstantBuffer(0, _constants);
            _context.PixelShader.Set(_pixel);
            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(_depth);
            _context.Rasterizer.State = _raster;
            _context.Draw(vertices.Length, 0);
        }
        finally
        {
            _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(null, 0, 0));
            _context.InputAssembler.InputLayout = null;
            _context.VertexShader.SetConstantBuffer(0, null);
            _context.VertexShader.Set(null); _context.PixelShader.Set(null);
            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(null);
            _context.Rasterizer.State = null;
        }
    }

    public void Dispose()
    {
        _raster?.Dispose(); _depth?.Dispose(); _constants?.Dispose(); _vertices?.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct LineVertex(Vector3 Position, Vector4 Color);
}
