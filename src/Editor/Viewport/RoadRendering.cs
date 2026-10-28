using System.Numerics;
using Editor.Roads;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Editor.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private Buffer? _roadSurfaceBuffer;
        private Buffer? _roadHandlesBuffer;
        private RasterizerState? _roadRasterizer;

        private int _roadSurfaceCount;
        private int _roadHandlesCount;

        private void InitializeRoadGraphics()
        {
            _roadRasterizer = new RasterizerState(
                _device,
                new RasterizerStateDescription
                {
                    FillMode = FillMode.Solid,
                    CullMode = CullMode.None,
                    IsDepthClipEnabled = true
                });
        }

        public void SetRoads(
            RoadDocument? document,
            string? selectedRoadId)
        {
            _roadSurfaceBuffer?.Dispose();
            _roadHandlesBuffer?.Dispose();

            _roadSurfaceBuffer = null;
            _roadHandlesBuffer = null;

            _roadSurfaceCount = 0;
            _roadHandlesCount = 0;

            if (document == null)
            {
                return;
            }

            var surface = new List<GridVertex>();
            var handles = new List<GridVertex>();

            foreach (var road in document.Roads)
            {
                bool selected = road.Id == selectedRoadId;

                var color = road.NeedsReview
                    ? new RawColor4(0.70f, 0.22f, 0.25f, 1f)
                    : selected
                        ? new RawColor4(0.90f, 0.58f, 0.19f, 1f)
                        : new RawColor4(0.40f, 0.42f, 0.43f, 1f);

                List<Vector3[]> rows =
                    RoadMeshBuilder.BuildRows(road);

                for (int row = 0; row < rows.Count - 1; row++)
                {
                    Vector3[] a = rows[row];
                    Vector3[] b = rows[row + 1];

                    for (int column = 0; column < 3; column++)
                    {
                        AddTriangle(
                            surface,
                            a[column],
                            a[column + 1],
                            b[column + 1],
                            color);

                        AddTriangle(
                            surface,
                            b[column + 1],
                            b[column],
                            a[column],
                            color);
                    }
                }

                if (!selected)
                {
                    continue;
                }

                var handleColor = new RawColor4(
                    0.35f, 0.85f, 1f, 1f);

                foreach (RoadControlPoint point in road.Points)
                {
                    var center = new Vector3(
                        point.X,
                        point.Y + 1f,
                        point.Z);

                    AddLine(
                        handles,
                        center + new Vector3(-2f, 0f, 0f),
                        center + new Vector3(2f, 0f, 0f),
                        handleColor);

                    AddLine(
                        handles,
                        center + new Vector3(0f, 0f, -2f),
                        center + new Vector3(0f, 0f, 2f),
                        handleColor);

                    AddLine(
                        handles,
                        center + new Vector3(0f, -2f, 0f),
                        center + new Vector3(0f, 2f, 0f),
                        handleColor);
                }
            }

            if (surface.Count > 0)
            {
                _roadSurfaceBuffer = Buffer.Create(
                    _device,
                    BindFlags.VertexBuffer,
                    surface.ToArray());

                _roadSurfaceCount = surface.Count;
            }

            if (handles.Count > 0)
            {
                _roadHandlesBuffer = Buffer.Create(
                    _device,
                    BindFlags.VertexBuffer,
                    handles.ToArray());

                _roadHandlesCount = handles.Count;
            }
        }

        private static void AddTriangle(
            List<GridVertex> vertices,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            RawColor4 color)
        {
            vertices.Add(new GridVertex(a, color));
            vertices.Add(new GridVertex(b, color));
            vertices.Add(new GridVertex(c, color));
        }

        private static void AddLine(
            List<GridVertex> vertices,
            Vector3 a,
            Vector3 b,
            RawColor4 color)
        {
            vertices.Add(new GridVertex(a, color));
            vertices.Add(new GridVertex(b, color));
        }

        private void DrawRoadGraphics(FreeCamera camera)
        {
            if (_roadSurfaceBuffer == null &&
                _roadHandlesBuffer == null)
            {
                return;
            }

            Matrix4x4 view = CreateViewMatrix(camera);

            Matrix4x4 projection = CreateProjectionMatrix(
                1.05f,
                (float)_width / _height,
                0.1f,
                10000f);

            Matrix4x4 viewProjection = view * projection;

            _context.UpdateSubresource(
                ref viewProjection,
                _cameraBuffer);

            _context.InputAssembler.InputLayout = _inputLayout;

            _context.VertexShader.Set(_vertexShader);
            _context.VertexShader.SetConstantBuffer(
                0,
                _cameraBuffer);

            _context.PixelShader.Set(_pixelShader);
            _context.Rasterizer.State = _roadRasterizer;

            if (_roadSurfaceBuffer != null)
            {
                _context.InputAssembler.PrimitiveTopology =
                    PrimitiveTopology.TriangleList;

                _context.InputAssembler.SetVertexBuffers(
                    0,
                    new VertexBufferBinding(
                        _roadSurfaceBuffer,
                        Utilities.SizeOf<GridVertex>(),
                        0));

                _context.Draw(_roadSurfaceCount, 0);
            }

            if (_roadHandlesBuffer != null)
            {
                _context.InputAssembler.PrimitiveTopology =
                    PrimitiveTopology.LineList;

                _context.InputAssembler.SetVertexBuffers(
                    0,
                    new VertexBufferBinding(
                        _roadHandlesBuffer,
                        Utilities.SizeOf<GridVertex>(),
                        0));

                _context.Draw(_roadHandlesCount, 0);
            }

            _context.Rasterizer.State = null;
        }

        private void DisposeRoadGraphics()
        {
            _roadSurfaceBuffer?.Dispose();
            _roadHandlesBuffer?.Dispose();
            _roadRasterizer?.Dispose();

            _roadSurfaceBuffer = null;
            _roadHandlesBuffer = null;
            _roadRasterizer = null;
        }
    }
}
