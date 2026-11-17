using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Editor.Rendering.Shaders;
using Editor.Viewport.Terrain;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Scene;
using ZoneReborn.World.Terrain;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Editor.Viewport.Roads;

internal sealed partial class RoadRenderer : IDisposable
{
    private const int VertexStride = 36;
    private const long GeometryCacheBytes = 128L * 1024 * 1024;
    private long _usage;
    private readonly Device _device;
    private readonly DeviceContext _context;
    private readonly LoadedLevel _level;
    private readonly TerrainSurface _surface;
    private readonly Dictionary<RoadDefinition, RoadResources> _roads = [];
    private VertexShader _vertex = null!;
    private PixelShader _pixel = null!;
    private InputLayout _layout = null!;
    private Buffer _constants = null!;
    private Buffer _markerVertices = null!;
    private Buffer _markerIndices = null!;
    private RasterizerState _rasterizer = null!;
    private DepthStencilState _depth = null!;
    private DepthStencilState _markerDepth = null!;
    private BlendState _blend = null!;
    private float _farPlane = 3500;
    private bool _disposed;
    public RoadDefinition? SelectedRoad { get; set; }
    public int SelectedNodeIndex { get; set; } = -1;

    public RoadRenderer(Device device, DeviceContext context, AssetPaths assets, LoadedLevel level, TerrainRenderer terrain)
    {
        _device = device; _context = context; _level = level;
        // The terrain query is deliberately shared with the world mesh/collision builder;
        // render-tile LOD and upload timing must never alter road geometry.
        _surface = new TerrainSurface(level);
        try
        {
            var compiler = new ShaderCompiler(assets);
            using var vs = compiler.Compile("Roads/Road.hlsl", "VSMain", "vs_4_0");
            using var ps = compiler.Compile("Roads/Road.hlsl", "PSMain", "ps_4_0");
            _vertex = new(device, vs.Bytecode); _pixel = new(device, ps.Bytecode);
            _layout = new(device, vs.Bytecode,
                [new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                 new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                 new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0),
                 new InputElement("TEXCOORD", 1, Format.R32_Float, 32, 0)]);
            _constants = new(device, Utilities.SizeOf<Constants>(), ResourceUsage.Default,
                BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _rasterizer = new(device, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid, CullMode = CullMode.None, IsDepthClipEnabled = true
            });
            var depth = DepthStencilStateDescription.Default();
            depth.IsDepthEnabled = true; depth.DepthComparison = Comparison.LessEqual;
            depth.DepthWriteMask = DepthWriteMask.All;
            _depth = new(device, depth);
            // Control points remain visible when terrain clearance lifts the deck over them.
            depth.IsDepthEnabled = false;
            depth.DepthWriteMask = DepthWriteMask.Zero;
            _markerDepth = new(device, depth);
            _blend = new(device, BlendStateDescription.Default());
            Vector3[] positions = [Vector3.UnitY, -Vector3.UnitY, Vector3.UnitX,
                Vector3.UnitZ, -Vector3.UnitX, -Vector3.UnitZ];
            _markerVertices = Buffer.Create(device, BindFlags.VertexBuffer,
                positions.Select(p => new RoadVertex(p, p, Vector2.Zero, 0)).ToArray());
            _markerIndices = Buffer.Create(device, BindFlags.IndexBuffer,
                new[] { 0, 2, 3, 0, 3, 4, 0, 4, 5, 0, 5, 2, 1, 3, 2, 1, 4, 3, 1, 5, 4, 1, 2, 5 });
        }
        catch { Dispose(); throw; }
    }

    public void Render(Vector3 camera, Matrix4x4 viewProjection, float farPlane, bool playing)
    {
        if (_disposed) return;
        _farPlane = float.IsFinite(farPlane) ? Math.Clamp(farPlane, 1, 3500) : 3500;
        Synchronize();
        var planes = FrustumPlanes(viewProjection);
        _context.InputAssembler.InputLayout = _layout;
        _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        _context.VertexShader.Set(_vertex); _context.PixelShader.Set(_pixel);
        _context.VertexShader.SetConstantBuffer(0, _constants);
        _context.PixelShader.SetConstantBuffer(0, _constants);
        _context.Rasterizer.State = _rasterizer;
        _context.OutputMerger.SetDepthStencilState(_depth);
        _context.OutputMerger.SetBlendState(_blend);
        try
        {
            foreach (var (road, resources) in _roads)
            {
                if (!NearRoad(road, camera, _farPlane)) { resources.ReleaseGeometry(); continue; }
                EnsureGeometry(road, resources);
                if (resources.Sections == null) continue;
                var constants = MakeConstants(road, camera, viewProjection, !playing && road == SelectedRoad);
                _context.UpdateSubresource(ref constants, _constants);
                foreach (var section in resources.Sections)
                {
                    if (Vector3.Distance(camera, section.Mesh.Bounds.Center) > _farPlane + section.Mesh.Bounds.Radius ||
                        !Visible(section.Mesh.Bounds, planes)) { section.ReleaseGpu(); continue; }
                    if (section.Mesh.Indices.Length == 0 || section.UploadFailed) continue;
                    try { section.Upload(_device); }
                    catch (Exception ex) when (Recoverable(ex))
                    {
                        section.UploadFailed = true; Warn(road, resources, ex); continue;
                    }
                    Draw(section.VertexBuffer!, section.IndexBuffer!, section.Mesh.Indices.Length);
                }
            }
            if (!playing && SelectedRoad is { } selected && _roads.TryGetValue(selected, out var selectedResources))
                DrawMarkers(selected, selectedResources, camera, viewProjection);
        }
        finally
        {
            _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(null, 0, 0));
            _context.InputAssembler.SetIndexBuffer(null, Format.R32_UInt, 0);
            _context.InputAssembler.InputLayout = null;
            _context.VertexShader.SetConstantBuffer(0, null);
            _context.PixelShader.SetConstantBuffer(0, null);
            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(null);
            _context.Rasterizer.State = null;
        }
    }

    private void Synchronize()
    {
        var members = _level.Roads.Roads.Take(1024).ToHashSet();
        foreach (var road in _roads.Keys.Where(road => !members.Contains(road)).ToArray())
        {
            _roads[road].ReleaseGeometry(); _roads.Remove(road);
        }
        long heights = _level.TerrainDocument?.HeightRevision ?? 0;
        long holes = _level.TerrainDocument?.HoleRevision ?? 0;
        foreach (var road in members)
        {
            if (!_roads.TryGetValue(road, out var resources)) _roads.Add(road, resources = new());
            if (resources.Revision == road.Revision && resources.HeightRevision == heights && resources.HoleRevision == holes) continue;
            resources.ReleaseGeometry(); resources.Failed = false; resources.Warning = null;
            resources.Revision = road.Revision; resources.HeightRevision = heights; resources.HoleRevision = holes;
        }
        if (SelectedRoad != null && !members.Contains(SelectedRoad)) { SelectedRoad = null; SelectedNodeIndex = -1; }
        if (SelectedRoad == null || SelectedNodeIndex >= (SelectedRoad.Nodes?.Count ?? 0)) SelectedNodeIndex = -1;
    }

    private void EnsureGeometry(RoadDefinition road, RoadResources resources)
    {
        resources.LastUse = ++_usage;
        if (resources.Sections != null || resources.Failed) return;
        try
        {
            var meshes = RoadMesh.Build(road, _surface);
            long bytes = meshes.Sum(mesh => (long)mesh.Vertices.Length * VertexStride + (long)mesh.Indices.Length * sizeof(int));
            if (bytes > GeometryCacheBytes) throw new InvalidDataException("Road exceeds the renderer geometry budget.");
            long cached = _roads.Values.Sum(item => item.Bytes);
            foreach (var old in _roads.Values.Where(item => item != resources && item.Sections != null).OrderBy(item => item.LastUse))
            {
                if (cached + bytes <= GeometryCacheBytes) break;
                cached -= old.Bytes; old.ReleaseGeometry();
            }
            resources.Sections = meshes.Select(mesh => new SectionResources(mesh)).ToArray();
            resources.Bytes = bytes;
        }
        catch (Exception ex) when (Recoverable(ex)) { resources.Failed = true; Warn(road, resources, ex); }
    }

    private static bool Recoverable(Exception ex) => ex is IOException or InvalidDataException or ArgumentException or
        InvalidOperationException or OverflowException or SharpDXException;

    private void Warn(RoadDefinition road, RoadResources resources, Exception ex)
    {
        string warning = $"Road '{road.Name}' ({road.Id}): {ex.Message}";
        if (resources.Warning == warning) return;
        resources.Warning = warning;
        // A broken draft must not flood diagnostics on every frame or drag update.
        if (_level.Roads.Warnings.Count < 256) _level.Roads.Warnings.Add(warning);
        System.Diagnostics.Trace.TraceWarning(warning);
    }

    private Constants MakeConstants(RoadDefinition road, Vector3 camera, Matrix4x4 viewProjection, bool selected) => new()
    {
        ViewProjection = viewProjection,
        Parameters = new Vector4(selected ? 1 : 0, road.Width, road.LaneCount, 0),
        CameraPosition = new Vector4(camera, 0),
        WeatherParameters = new Vector4(_roadWetness, _uniformRoadWetness ? 1 : 0, 12, 0.9f)
    };

    private void Draw(Buffer vertices, Buffer indices, int count)
    {
        _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, VertexStride, 0));
        _context.InputAssembler.SetIndexBuffer(indices, Format.R32_UInt, 0);
        _context.DrawIndexed(count, 0, 0);
    }

    private void DrawMarkers(RoadDefinition road, RoadResources resources, Vector3 camera, Matrix4x4 viewProjection)
    {
        _context.OutputMerger.SetDepthStencilState(_markerDepth);
        if (road.Nodes == null) return;
        for (int index = 0; index < Math.Min(road.Nodes.Count, 256); index++)
        {
            var node = road.Nodes[index];
            if (node == null) continue;
            Vector3 position = node.Position;
            try
            {
                if (road.FollowTerrain)
                {
                    if (_surface.TryGetHeight(node.X, node.Z, out float height)) position.Y = height;
                    position.Y += node.HeightOffset;
                }
            }
            catch (Exception ex) when (Recoverable(ex)) { Warn(road, resources, ex); continue; }
            position.Y += road.SurfaceOffset;
            if (!Finite(position) || Vector3.Distance(camera, position) > _farPlane) continue;
            float size = Math.Clamp(Vector3.Distance(camera, position) * 0.008f, 0.16f, 1.5f);
            position.Y += size;
            var constants = MakeConstants(road, camera, viewProjection, false);
            constants.Parameters.W = index == SelectedNodeIndex ? 2 : 1;
            constants.Marker = new Vector4(position, size);
            _context.UpdateSubresource(ref constants, _constants);
            Draw(_markerVertices, _markerIndices, 24);
        }
    }

    public (RoadDefinition Road, int SegmentIndex)? Pick(Vector3 origin, Vector3 direction)
    {
        if (_disposed || !Finite(origin) || !Finite(direction) || direction.LengthSquared() < 1e-12f) return null;
        float length = direction.Length();
        if (!float.IsFinite(length)) return null;
        direction /= length;
        Synchronize(); // Picking may happen between an edit and the next rendered frame.
        float nearest = _farPlane;
        (RoadDefinition Road, int SegmentIndex)? result = null;
        foreach (var (road, resources) in _roads)
        {
            if (!NearRoad(road, origin, nearest)) continue;
            EnsureGeometry(road, resources);
            if (resources.Sections == null) continue;
            foreach (var section in resources.Sections)
            {
                var mesh = section.Mesh;
                if (!RayBounds(origin, direction, mesh.Bounds, nearest)) continue;
                for (int i = 0; i < mesh.Indices.Length; i += 3)
                {
                    var a = mesh.Vertices[mesh.Indices[i]].Position;
                    var e1 = mesh.Vertices[mesh.Indices[i + 1]].Position - a;
                    var e2 = mesh.Vertices[mesh.Indices[i + 2]].Position - a;
                    var p = Vector3.Cross(direction, e2);
                    float determinant = Vector3.Dot(e1, p);
                    if (MathF.Abs(determinant) < 1e-8f) continue;
                    float inverse = 1 / determinant;
                    var t = origin - a;
                    float u = Vector3.Dot(t, p) * inverse;
                    if (u < 0 || u > 1) continue;
                    var q = Vector3.Cross(t, e1);
                    float v = Vector3.Dot(direction, q) * inverse;
                    if (v < 0 || u + v > 1) continue;
                    float distance = Vector3.Dot(e2, q) * inverse;
                    if (!float.IsFinite(distance) || distance < 0 || distance >= nearest) continue;
                    nearest = distance; result = (road, mesh.SegmentIndex);
                }
            }
        }
        return result;
    }

    private static bool NearRoad(RoadDefinition road, Vector3 point, float range)
    {
        if (road.Nodes == null || road.Nodes.Count > 256) return true;
        if (road.Nodes.Count == 0) return false;
        var minimum = new Vector2(float.PositiveInfinity); var maximum = new Vector2(float.NegativeInfinity);
        float longest = 0;
        for (int i = 0; i < road.Nodes.Count; i++)
        {
            if (road.Nodes[i] == null) return true;
            var p = new Vector2(road.Nodes[i].X, road.Nodes[i].Z);
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y)) return true; // Let validation diagnose this draft.
            minimum = Vector2.Min(minimum, p); maximum = Vector2.Max(maximum, p);
            if (i > 0) longest = Math.Max(longest, Vector2.Distance(p, new(road.Nodes[i - 1].X, road.Nodes[i - 1].Z)));
        }
        // Conservative padding includes spline overshoot as well as shoulders and rails.
        float padding = longest + Math.Max(road.Width, 0) + Math.Max(road.ShoulderWidth, 0) + 4;
        var xz = new Vector2(point.X, point.Z);
        var closest = Vector2.Clamp(xz, minimum - new Vector2(padding), maximum + new Vector2(padding));
        return Vector2.DistanceSquared(xz, closest) <= range * range;
    }

    private static bool RayBounds(Vector3 origin, Vector3 direction, SceneBounds bounds, float maximum)
    {
        float near = 0, far = maximum;
        return Slab(origin.X, direction.X, bounds.Minimum.X, bounds.Maximum.X, ref near, ref far) &&
            Slab(origin.Y, direction.Y, bounds.Minimum.Y, bounds.Maximum.Y, ref near, ref far) &&
            Slab(origin.Z, direction.Z, bounds.Minimum.Z, bounds.Maximum.Z, ref near, ref far);
    }

    private static bool Slab(float origin, float direction, float minimum, float maximum, ref float near, ref float far)
    {
        if (MathF.Abs(direction) < 1e-10f) return origin >= minimum && origin <= maximum;
        float a = (minimum - origin) / direction, b = (maximum - origin) / direction;
        near = Math.Max(near, Math.Min(a, b)); far = Math.Min(far, Math.Max(a, b));
        return near <= far;
    }

    private static bool Finite(Vector3 p) => float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z);
    private static Vector4[] FrustumPlanes(Matrix4x4 m) =>
        [new(m.M14 + m.M11, m.M24 + m.M21, m.M34 + m.M31, m.M44 + m.M41),
         new(m.M14 - m.M11, m.M24 - m.M21, m.M34 - m.M31, m.M44 - m.M41),
         new(m.M14 + m.M12, m.M24 + m.M22, m.M34 + m.M32, m.M44 + m.M42),
         new(m.M14 - m.M12, m.M24 - m.M22, m.M34 - m.M32, m.M44 - m.M42),
         new(m.M13, m.M23, m.M33, m.M43),
         new(m.M14 - m.M13, m.M24 - m.M23, m.M34 - m.M33, m.M44 - m.M43)];

    private static bool Visible(SceneBounds bounds, Vector4[] planes)
    {
        foreach (var p in planes)
            if (p.X * (p.X >= 0 ? bounds.Maximum.X : bounds.Minimum.X) +
                p.Y * (p.Y >= 0 ? bounds.Maximum.Y : bounds.Minimum.Y) +
                p.Z * (p.Z >= 0 ? bounds.Maximum.Z : bounds.Minimum.Z) + p.W < 0) return false;
        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var resources in _roads.Values) resources.ReleaseGeometry();
        _roads.Clear();
        _markerIndices?.Dispose(); _markerVertices?.Dispose();
        _blend?.Dispose(); _markerDepth?.Dispose(); _depth?.Dispose(); _rasterizer?.Dispose();
        _constants?.Dispose(); _layout?.Dispose(); _pixel?.Dispose(); _vertex?.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Constants
    {
        public Matrix4x4 ViewProjection;
        public Vector4 Parameters;
        public Vector4 CameraPosition;
        public Vector4 WeatherParameters;
        public Vector4 Marker;
    }

    private sealed class RoadResources
    {
        public int Revision = -1;
        public long HeightRevision = -1, HoleRevision = -1;
        public long Bytes, LastUse;
        public bool Failed;
        public string? Warning;
        public SectionResources[]? Sections;
        public void ReleaseGeometry()
        {
            if (Sections != null) foreach (var section in Sections) section.ReleaseGpu();
            Sections = null; Bytes = 0;
        }
    }

    private sealed class SectionResources(RoadSectionMesh mesh)
    {
        public RoadSectionMesh Mesh { get; } = mesh;
        public Buffer? VertexBuffer;
        public Buffer? IndexBuffer;
        public bool UploadFailed;
        public void Upload(Device device)
        {
            if (VertexBuffer != null) return;
            try
            {
                VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, Mesh.Vertices);
                IndexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, Mesh.Indices);
            }
            catch { ReleaseGpu(); throw; }
        }
        public void ReleaseGpu()
        {
            VertexBuffer?.Dispose(); IndexBuffer?.Dispose(); VertexBuffer = null; IndexBuffer = null;
        }
    }
}
