using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Terrain;
using ZoneReborn.World.Actors;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Runtime.Viewport.Roads;

internal sealed partial class RoadOverlayRenderer : IDisposable
{
    private const int MaximumVertices = 120000;

    private readonly Device _device;
    private readonly DeviceContext _context;
    private readonly VertexShader _vertex;
    private readonly PixelShader _pixel;
    private readonly InputLayout _layout;

    private readonly List<RoadVertex> _lines = [];

    private Buffer? _vertices;
    private Buffer _constants = null!;
    private DepthStencilState _depth = null!;
    private RasterizerState _raster = null!;

    private int _capacity;

    public RoadOverlayRenderer(
        Device device,
        DeviceContext context,
        VertexShader vertex,
        PixelShader pixel,
        InputLayout layout)
    {
        _device = device;
        _context = context;
        _vertex = vertex;
        _pixel = pixel;
        _layout = layout;

        try
        {
            _constants = new Buffer(
                device,
                Utilities.SizeOf<Matrix4x4>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

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
                });
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Render(
        RoadNetwork network,
        RoadCatalog? catalog,
        TerrainSurface surface,
        Guid? selectedRoad,
        int selectedPoint,
        Matrix4x4 viewProjection)
    {
        _lines.Clear();

        foreach (var road in network.Roads)
        {
            bool selected = selectedRoad == road.Id;

            var color = selected
                ? new Vector4(1f, 0.72f, 0.12f, 1f)
                : new Vector4(0.15f, 0.8f, 1f, 1f);

            float curveWidth = road.Width;

            if (road.Category == RoadCategory.Railway &&
                catalog != null &&
                catalog.TryGet(road.Model, out RoadModelInfo model))
            {
                curveWidth = model.Width;
            }

            RoadCurve curve = RoadCurve.Create(
                road.Points,
                curveWidth);

            for (int index = 1; index < curve.Samples.Count; index++)
            {
                Vector2 a = curve.Samples[index - 1].Position;
                Vector2 b = curve.Samples[index].Position;

                DrawSegment(
                    surface,
                    new RoadPoint(a.X, a.Y),
                    new RoadPoint(b.X, b.Y),
                    color);
            }

            for (int index = 0; index < road.Points.Count; index++)
            {
                if (!TryWorld(
                        surface,
                        road.Points[index],
                        out Vector3 center))
                {
                    continue;
                }

                var pointColor = selected && index == selectedPoint
                    ? new Vector4(1f, 0.25f, 0.1f, 1f)
                    : color;

                DrawPoint(center, pointColor);
            }
        }

        Flush(viewProjection);
    }

    private void Flush(Matrix4x4 viewProjection)
    {
        if (_lines.Count == 0)
        {
            return;
        }

        EnsureCapacity(_lines.Count);

        _context.MapSubresource(
            _vertices!,
            MapMode.WriteDiscard,
            SharpDX.Direct3D11.MapFlags.None,
            out DataStream stream);

        try
        {
            using (stream)
            {
                stream.WriteRange(_lines.ToArray());
            }
        }
        finally
        {
            _context.UnmapSubresource(_vertices!, 0);
        }

        try
        {
            _context.UpdateSubresource(
                ref viewProjection,
                _constants);

            _context.InputAssembler.InputLayout = _layout;
            _context.InputAssembler.PrimitiveTopology =
                PrimitiveTopology.LineList;

            _context.InputAssembler.SetIndexBuffer(
                null,
                Format.R32_UInt,
                0);

            _context.InputAssembler.SetVertexBuffers(
                0,
                new VertexBufferBinding(
                    _vertices,
                    Utilities.SizeOf<RoadVertex>(),
                    0),
                new VertexBufferBinding(null, 0, 0));

            _context.VertexShader.Set(_vertex);
            _context.VertexShader.SetConstantBuffer(
                0,
                _constants);

            _context.PixelShader.Set(_pixel);
            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(_depth);
            _context.Rasterizer.State = _raster;

            _context.Draw(_lines.Count, 0);
        }
        finally
        {
            _context.InputAssembler.SetVertexBuffers(
                0,
                new VertexBufferBinding(null, 0, 0),
                new VertexBufferBinding(null, 0, 0));

            _context.InputAssembler.InputLayout = null;
            _context.VertexShader.SetConstantBuffer(0, null);
            _context.VertexShader.Set(null);
            _context.PixelShader.Set(null);
            _context.OutputMerger.SetBlendState(null);
            _context.OutputMerger.SetDepthStencilState(null);
            _context.Rasterizer.State = null;
        }
    }

    public void RenderActors(
        ActorDocument document,
        Guid? selectedActor,
        Matrix4x4 viewProjection,
        Func<Guid, bool>? hasNpcModel = null,
        Guid? selectedLocation = null)
    {
        _lines.Clear();

        if (selectedActor.HasValue &&
            document.Find(selectedActor.Value) is { } selected)
        {
            bool modelVisible =
                selected.Kind == ActorKind.NpcSpawn &&
                hasNpcModel?.Invoke(selected.Id) == true;

            if (modelVisible)
            {
                Vector3 center = selected.Position +
                                 Vector3.UnitY * 0.08f;

                DrawActorCircle(
                    center,
                    0.85f,
                    new Vector4(
                        0.15f,
                        0.55f,
                        1f,
                        1f));

                DrawActorCircle(
                    center + Vector3.UnitY * 0.03f,
                    0.95f,
                    Vector4.One);
            }
            else
            {
                DrawActor(selected, true);
            }
        }

        foreach (LevelActor actor in document.Actors)
        {
            if (actor.Id == selectedActor)
            {
                continue;
            }

            if (actor.Kind == ActorKind.NpcSpawn &&
                hasNpcModel?.Invoke(actor.Id) == true)
            {
                continue;
            }

            if (_lines.Count + 160 > MaximumVertices)
            {
                break;
            }

            DrawActor(actor, false);
        }

        foreach (LevelActor actor in document.Actors)
        {
            if (actor.Kind != ActorKind.LootSpawn)
            {
                continue;
            }

            DrawLootLocations(
                actor,
                actor.Id == selectedActor
                    ? selectedLocation
                    : null);
        }

        Flush(viewProjection);
    }

    private void DrawActor(
        LevelActor actor,
        bool selected)
    {
        Vector4 color = actor.Kind switch
        {
            ActorKind.PlayerSpawn =>
                new Vector4(0.15f, 1f, 0.25f, 1f),

            ActorKind.NpcSpawn =>
                new Vector4(0.15f, 0.55f, 1f, 1f),

            ActorKind.MobSpawn =>
                new Vector4(1f, 0.15f, 0.15f, 1f),

            ActorKind.LootSpawn =>
                new Vector4(1f, 0.85f, 0.1f, 1f),

            _ => Vector4.One
        };

        if (!actor.Enabled)
        {
            color = new Vector4(
                0.4f, 0.4f, 0.4f, 1f);
        }

        Vector3 center = actor.Position +
            Vector3.UnitY * 0.08f;

        float height = actor.Kind == ActorKind.LootSpawn
            ? 0.8f
            : 1.8f;

        Vector3 top = center +
            Vector3.UnitY * height;

        DrawActorCircle(
            center,
            0.65f,
            color);

        DrawActorCircle(
            top,
            0.2f,
            color);

        AddLine(
            center,
            top,
            color);

        Vector3 forward = new(
            MathF.Sin(actor.Yaw),
            0f,
            MathF.Cos(actor.Yaw));

        Vector3 right = new(
            forward.Z,
            0f,
            -forward.X);

        Vector3 arrow = center + forward * 1.4f;

        AddLine(center, arrow, color);

        AddLine(
            arrow,
            arrow - forward * 0.35f + right * 0.2f,
            color);

        AddLine(
            arrow,
            arrow - forward * 0.35f - right * 0.2f,
            color);

        if (actor.Kind == ActorKind.MobSpawn)
        {
            DrawActorCircle(
                center,
                actor.Radius,
                color);
        }

        if (selected)
        {
            DrawActorCircle(
                center + Vector3.UnitY * 0.05f,
                0.85f,
                Vector4.One);
        }
    }

    private void DrawActorCircle(
        Vector3 center,
        float radius,
        Vector4 color)
    {
        const int segments = 16;

        for (int index = 0; index < segments; index++)
        {
            float angleA =
                index * MathF.Tau / segments;

            float angleB =
                (index + 1) * MathF.Tau / segments;

            Vector3 a = center + new Vector3(
                MathF.Cos(angleA) * radius,
                0f,
                MathF.Sin(angleA) * radius);

            Vector3 b = center + new Vector3(
                MathF.Cos(angleB) * radius,
                0f,
                MathF.Sin(angleB) * radius);

            AddLine(a, b, color);
        }
    }

    private void DrawSegment(
        TerrainSurface surface,
        RoadPoint start,
        RoadPoint end,
        Vector4 color)
    {
        float distance = Vector2.Distance(
            new Vector2(start.X, start.Z),
            new Vector2(end.X, end.Z));

        int steps = Math.Clamp(
            (int)MathF.Ceiling(distance / 3f),
            1,
            256);

        bool hasPrevious = TryWorld(
            surface,
            start,
            out Vector3 previous);

        for (int index = 1; index <= steps; index++)
        {
            float t = index / (float)steps;

            var point = new RoadPoint(
                start.X + (end.X - start.X) * t,
                start.Z + (end.Z - start.Z) * t);

            bool hasCurrent = TryWorld(
                surface,
                point,
                out Vector3 current);

            if (hasPrevious && hasCurrent)
            {
                AddLine(previous, current, color);
            }

            previous = current;
            hasPrevious = hasCurrent;
        }
    }

    private static bool TryWorld(
        TerrainSurface surface,
        RoadPoint point,
        out Vector3 world)
    {
        world = default;

        if (!surface.TryGetHeight(
                point.X,
                point.Z,
                out float height))
        {
            return false;
        }

        world = new Vector3(
            point.X,
            height + 0.35f,
            point.Z);

        return true;
    }

    private void DrawPoint(Vector3 center, Vector4 color)
    {
        const float radius = 0.65f;
        const int segments = 12;

        for (int index = 0; index < segments; index++)
        {
            float angleA =
                index * MathF.Tau / segments;

            float angleB =
                (index + 1) * MathF.Tau / segments;

            var a = center + new Vector3(
                MathF.Cos(angleA) * radius,
                0f,
                MathF.Sin(angleA) * radius);

            var b = center + new Vector3(
                MathF.Cos(angleB) * radius,
                0f,
                MathF.Sin(angleB) * radius);

            AddLine(a, b, color);
        }

        AddLine(
            center + new Vector3(0f, 0.1f, 0f),
            center + new Vector3(0f, 1.5f, 0f),
            color);
    }

    private void AddLine(
        Vector3 start,
        Vector3 end,
        Vector4 color)
    {
        if (_lines.Count + 2 > MaximumVertices)
        {
            return;
        }

        _lines.Add(new RoadVertex(start, color));
        _lines.Add(new RoadVertex(end, color));
    }

    private void EnsureCapacity(int required)
    {
        if (_vertices != null && required <= _capacity)
        {
            return;
        }

        _capacity = Math.Max(
            required,
            Math.Max(4096, _capacity * 2));

        _vertices?.Dispose();

        _vertices = new Buffer(
            _device,
            _capacity * Utilities.SizeOf<RoadVertex>(),
            ResourceUsage.Dynamic,
            BindFlags.VertexBuffer,
            CpuAccessFlags.Write,
            ResourceOptionFlags.None,
            0);
    }

    public void Dispose()
    {
        _vertices?.Dispose();
        _constants?.Dispose();
        _depth?.Dispose();
        _raster?.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct RoadVertex(
        Vector3 Position,
        Vector4 Color);
}
