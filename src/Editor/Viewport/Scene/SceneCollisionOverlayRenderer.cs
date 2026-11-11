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

internal sealed class SceneCollisionOverlayRenderer : IDisposable
{
    private const int MaximumPreviewTriangles = 300_000;

    private static readonly Vector4 LineColor = new(
        0.1f,
        1f,
        0.35f,
        0.6f
    );

    private readonly Device _device;
    private readonly DeviceContext _context;
    private readonly VertexShader _vertex;
    private readonly PixelShader _pixel;
    private readonly InputLayout _layout;
    private readonly Action<string> _warning;

    private Buffer _constants = null!;
    private Buffer? _lines;
    private DepthStencilState _depth = null!;
    private RasterizerState _raster = null!;
    private BlendState _blend = null!;

    private ScenePhysicsMesh? _cachedMesh;
    private Task<WireframeData>? _pending;
    private int _lineCount;
    private bool _disposed;

    public SceneCollisionOverlayRenderer(
        Device device,
        DeviceContext context,
        VertexShader vertex,
        PixelShader pixel,
        InputLayout layout,
        Action<string> warning)
    {
        _device = device;
        _context = context;
        _vertex = vertex;
        _pixel = pixel;
        _layout = layout;
        _warning = warning;

        try
        {
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
            depth.IsDepthEnabled = false;
            depth.DepthWriteMask = DepthWriteMask.Zero;

            _depth = new DepthStencilState(device, depth);

            _raster = new RasterizerState(
                device,
                new RasterizerStateDescription
                {
                    FillMode = FillMode.Solid,
                    CullMode = CullMode.None,
                    IsDepthClipEnabled = true,
                    IsAntialiasedLineEnabled = true
                }
            );

            var blend = BlendStateDescription.Default();
            blend.RenderTarget[0].IsBlendEnabled = true;
            blend.RenderTarget[0].SourceBlend =
                BlendOption.SourceAlpha;
            blend.RenderTarget[0].DestinationBlend =
                BlendOption.InverseSourceAlpha;
            blend.RenderTarget[0].BlendOperation =
                BlendOperation.Add;
            blend.RenderTarget[0].SourceAlphaBlend =
                BlendOption.One;
            blend.RenderTarget[0].DestinationAlphaBlend =
                BlendOption.InverseSourceAlpha;
            blend.RenderTarget[0].AlphaBlendOperation =
                BlendOperation.Add;
            blend.RenderTarget[0].RenderTargetWriteMask =
                ColorWriteMaskFlags.All;

            _blend = new BlendState(device, blend);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Render(
        ScenePhysicsMesh mesh,
        Matrix4x4 world,
        Matrix4x4 viewProjection)
    {
        if (_disposed)
        {
            return;
        }

        if (!ReferenceEquals(_cachedMesh, mesh))
        {
            _cachedMesh = mesh;

            _lines?.Dispose();
            _lines = null;
            _lineCount = 0;

            _pending = Task.Run(
                () => BuildWireframe(mesh)
            );
        }

        if (_lines == null)
        {
            if (_pending == null ||
                !_pending.IsCompleted)
            {
                return;
            }

            if (!_pending.IsCompletedSuccessfully)
            {
                _warning(
                    "Не удалось подготовить сетку коллизии: " +
                    (_pending.Exception?
                        .GetBaseException()
                        .Message ?? "Задача отменена.")
                );

                _pending = null;
                return;
            }

            WireframeData result = _pending.Result;
            _pending = null;

            if (result.Vertices.Length == 0)
            {
                _warning(
                    "У выбранного объекта нет треугольников коллизии."
                );

                return;
            }

            try
            {
                _lines = Buffer.Create(
                    _device,
                    BindFlags.VertexBuffer,
                    result.Vertices
                );

                _lineCount = result.Vertices.Length;
            }
            catch (Exception exception)
            {
                _warning(
                    "Ошибка создания буфера коллизии: " +
                    exception.Message
                );

                return;
            }

            if (result.Sampled)
            {
                _warning(
                    "Сетка коллизии слишком большая. " +
                    "Для отображения использована выборка треугольников."
                );
            }
        }

        if (_lineCount == 0)
        {
            return;
        }

        Matrix4x4 transform = world * viewProjection;

        try
        {
            _context.UpdateSubresource(
                ref transform,
                _constants
            );

            _context.InputAssembler.InputLayout = _layout;

            _context.InputAssembler.PrimitiveTopology =
                PrimitiveTopology.LineList;

            _context.InputAssembler.SetVertexBuffers(
                0,
                new VertexBufferBinding(
                    _lines,
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

            _context.VertexShader.Set(_vertex);
            _context.VertexShader.SetConstantBuffer(
                0,
                _constants
            );

            _context.PixelShader.Set(_pixel);

            _context.OutputMerger.SetBlendState(_blend);
            _context.OutputMerger.SetDepthStencilState(_depth);
            _context.Rasterizer.State = _raster;

            _context.Draw(_lineCount, 0);
        }
        finally
        {
            _context.InputAssembler.SetVertexBuffers(
                0,
                new VertexBufferBinding(null, 0, 0),
                new VertexBufferBinding(null, 0, 0)
            );

            _context.InputAssembler.InputLayout = null;

            _context.VertexShader.SetConstantBuffer(
                0,
                null
            );

            _context.VertexShader.Set(null);
            _context.PixelShader.Set(null);

            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(null);
            _context.Rasterizer.State = null;
        }
    }

    private static WireframeData BuildWireframe(
        ScenePhysicsMesh mesh)
    {
        int triangleCount = mesh.Indices.Length / 3;

        int step = Math.Max(
            1,
            (int)Math.Ceiling(
                (double)triangleCount / MaximumPreviewTriangles
            )
        );

        var edges = new HashSet<ulong>();
        var vertices = new List<LineVertex>();

        for (int triangle = 0;
             triangle < triangleCount;
             triangle += step)
        {
            int offset = triangle * 3;

            int a = mesh.Indices[offset];
            int b = mesh.Indices[offset + 1];
            int c = mesh.Indices[offset + 2];

            if ((uint)a >= (uint)mesh.Vertices.Length ||
                (uint)b >= (uint)mesh.Vertices.Length ||
                (uint)c >= (uint)mesh.Vertices.Length)
            {
                continue;
            }

            AddEdge(a, b);
            AddEdge(b, c);
            AddEdge(c, a);
        }

        return new WireframeData(
            vertices.ToArray(),
            step > 1
        );

        void AddEdge(int first, int second)
        {
            if (first == second)
            {
                return;
            }

            ulong minimum = (uint)Math.Min(first, second);
            ulong maximum = (uint)Math.Max(first, second);
            ulong key = (minimum << 32) | maximum;

            if (!edges.Add(key))
            {
                return;
            }

            Vector3 a = mesh.Vertices[first];
            Vector3 b = mesh.Vertices[second];

            if (!float.IsFinite(a.X) ||
                !float.IsFinite(a.Y) ||
                !float.IsFinite(a.Z) ||
                !float.IsFinite(b.X) ||
                !float.IsFinite(b.Y) ||
                !float.IsFinite(b.Z))
            {
                return;
            }

            vertices.Add(new LineVertex(a, LineColor));
            vertices.Add(new LineVertex(b, LineColor));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _lines?.Dispose();
        _blend?.Dispose();
        _raster?.Dispose();
        _depth?.Dispose();
        _constants?.Dispose();

        _lines = null;
        _pending = null;
        _cachedMesh = null;
    }

    private readonly record struct WireframeData(
        LineVertex[] Vertices,
        bool Sampled
    );

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct LineVertex(
        Vector3 Position,
        Vector4 Color
    );
}
