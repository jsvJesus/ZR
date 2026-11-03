using System.Numerics;
using System.Runtime.InteropServices;
using ZoneReborn.World.Scene;

namespace Editor.Viewport;

public enum ObjectGizmoMode { Move, Rotate }
public enum ObjectGizmoAxis { None, X, Y, Z }

/// <summary>The edit camera basis and world scale shared by drawing, picking and dragging.</summary>
public readonly record struct ObjectGizmoFrame(Vector3 Pivot, float Size, Vector3 CameraPosition,
    Vector3 CameraForward, Vector3 CameraRight, Vector3 CameraUp, int Width, int Height)
{
    public float FarPlane { get; init; } = float.MaxValue;

    public bool TryProject(Vector3 point, out Vector2 pixel)
    {
        pixel = default;
        var offset = point - CameraPosition;
        float depth = Vector3.Dot(offset, CameraForward);
        if (Width < 1 || Height < 1 || !float.IsFinite(depth) || depth < ObjectGizmoGeometry.NearPlane ||
            depth > FarPlane)
            return false;
        float focal = Height / (2 * MathF.Tan(ObjectGizmoGeometry.FieldOfView * 0.5f));
        pixel = new(Width * 0.5f + Vector3.Dot(offset, CameraRight) * focal / depth,
            Height * 0.5f - Vector3.Dot(offset, CameraUp) * focal / depth);
        return float.IsFinite(pixel.X) && float.IsFinite(pixel.Y);
    }

    public Vector3 RayDirection(Vector2 pixel)
    {
        float focal = Height / (2 * MathF.Tan(ObjectGizmoGeometry.FieldOfView * 0.5f));
        return Vector3.Normalize(CameraForward + CameraRight * ((pixel.X - Width * 0.5f) / focal)
            - CameraUp * ((pixel.Y - Height * 0.5f) / focal));
    }
}

public static class ObjectGizmoGeometry
{
    internal const float FieldOfView = 1.05f;
    internal const float NearPlane = 0.5f;
    internal const int RingSegments = 96;
    internal const int ArrowSegments = 12;
    private const float HitTolerance = 8;

    public static bool TryCreateFrame(SceneInstance instance, FreeCamera camera, int width, int height,
        out ObjectGizmoFrame frame, float farPlane = float.MaxValue)
    {
        frame = default;
        if (width < 1 || height < 1 || !float.IsFinite(farPlane) || farPlane <= NearPlane) return false;
        var pivot = instance.World.Translation;
        var forward = Vector3.Normalize(camera.LookDirection);
        var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
        var up = Vector3.Cross(forward, right);
        float depth = Vector3.Dot(pivot - camera.Position, forward);
        float size = 96 * depth * (2 * MathF.Tan(FieldOfView * 0.5f)) / height;
        if (!Finite(pivot) || !Finite(forward) || !Finite(right) || !Finite(up) ||
            !float.IsFinite(size) || depth < NearPlane || depth > farPlane || size <= 0) return false;
        frame = new(pivot, size, camera.Position, forward, right, up, width, height) { FarPlane = farPlane };
        return frame.TryProject(pivot, out _);
    }

    public static Vector3 AxisDirection(ObjectGizmoAxis axis) => axis switch
    {
        ObjectGizmoAxis.X => Vector3.UnitX,
        ObjectGizmoAxis.Y => Vector3.UnitY,
        ObjectGizmoAxis.Z => Vector3.UnitZ,
        _ => Vector3.Zero
    };

    public static ObjectGizmoAxis HitTest(ObjectGizmoFrame frame, ObjectGizmoMode mode, Vector2 pixel)
    {
        if (!float.IsFinite(pixel.X) || !float.IsFinite(pixel.Y) || pixel.X < 0 || pixel.Y < 0 ||
            pixel.X >= frame.Width || pixel.Y >= frame.Height) return ObjectGizmoAxis.None;
        float best = HitTolerance * HitTolerance;
        var result = ObjectGizmoAxis.None;
        for (var axis = ObjectGizmoAxis.X; axis <= ObjectGizmoAxis.Z; axis++)
        {
            if (mode == ObjectGizmoMode.Move)
            {
                if (!MoveVisible(frame, axis)) continue;
                Check(frame.Pivot, frame.Pivot + AxisDirection(axis) * frame.Size);
                // Include the solid cone itself, including its clipped silhouette.
                var tip = frame.Pivot + AxisDirection(axis) * frame.Size;
                var headBase = frame.Pivot + AxisDirection(axis) * (frame.Size * 0.77f);
                for (int i = 0; i < ArrowSegments; i++)
                {
                    var a = ArrowBasePoint(frame, axis, i);
                    var b = ArrowBasePoint(frame, axis, i + 1);
                    Consider(TriangleDistanceSquared(frame, pixel, a, b, tip));
                    Consider(TriangleDistanceSquared(frame, pixel, b, a, headBase));
                }
            }
            else
            {
                for (int i = 0; i < RingSegments; i++)
                    Check(RingPoint(frame, axis, i), RingPoint(frame, axis, i + 1));
            }

            void Check(Vector3 start, Vector3 end)
            {
                if (!TryProjectSegment(frame, start, end, out var a, out var b)) return;
                Consider(SegmentDistanceSquared(pixel, a, b));
            }

            void Consider(float distance)
            {
                if (!float.IsFinite(distance) || distance > best) return;
                best = distance;
                result = axis;
            }
        }
        return result;
    }

    internal static void Build(ObjectGizmoFrame frame, ObjectGizmoMode mode, ObjectGizmoAxis highlighted,
        List<ObjectGizmoVertex> lines, List<ObjectGizmoVertex> triangles)
    {
        lines.Clear(); triangles.Clear();
        for (var axis = ObjectGizmoAxis.X; axis <= ObjectGizmoAxis.Z; axis++)
        {
            var color = axis == highlighted ? new Vector4(1, 0.9f, 0.12f, 1) : axis switch
            {
                ObjectGizmoAxis.X => new Vector4(1, 0.2f, 0.18f, 1),
                ObjectGizmoAxis.Y => new Vector4(0.2f, 1, 0.32f, 1),
                _ => new Vector4(0.25f, 0.5f, 1, 1)
            };
            if (mode == ObjectGizmoMode.Rotate)
            {
                for (int i = 0; i < RingSegments; i++)
                {
                    lines.Add(new(RingPoint(frame, axis, i), color));
                    lines.Add(new(RingPoint(frame, axis, i + 1), color));
                }
                continue;
            }
            if (!MoveVisible(frame, axis)) continue;
            var direction = AxisDirection(axis);
            Basis(axis, out var u, out var v);
            var headBase = frame.Pivot + direction * (frame.Size * 0.77f);
            var tip = frame.Pivot + direction * frame.Size;
            lines.Add(new(frame.Pivot, color)); lines.Add(new(headBase, color));
            // A solid thin shaft and closed cone remain readable against bright surfaces.
            for (int i = 0; i < ArrowSegments; i++)
            {
                float a = i * MathF.Tau / ArrowSegments, b = (i + 1) * MathF.Tau / ArrowSegments;
                var radialA = u * MathF.Cos(a) + v * MathF.Sin(a);
                var radialB = u * MathF.Cos(b) + v * MathF.Sin(b);
                var baseA = ArrowBasePoint(frame, axis, i);
                var baseB = ArrowBasePoint(frame, axis, i + 1);
                Triangle(baseA, baseB, tip); Triangle(baseB, baseA, headBase);
                var startA = frame.Pivot + radialA * (frame.Size * 0.012f);
                var startB = frame.Pivot + radialB * (frame.Size * 0.012f);
                var endA = headBase + radialA * (frame.Size * 0.012f);
                var endB = headBase + radialB * (frame.Size * 0.012f);
                Triangle(startA, startB, endA); Triangle(startB, endB, endA);
            }

            void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                triangles.Add(new(a, color)); triangles.Add(new(b, color)); triangles.Add(new(c, color));
            }
        }
    }

    private static bool MoveVisible(ObjectGizmoFrame frame, ObjectGizmoAxis axis) =>
        TryProjectSegment(frame, frame.Pivot, frame.Pivot + AxisDirection(axis) * (frame.Size * 0.77f),
            out var start, out var end) && Vector2.DistanceSquared(start, end) >= 8 * 8;

    private static Vector3 RingPoint(ObjectGizmoFrame frame, ObjectGizmoAxis axis, int index)
    {
        Basis(axis, out var u, out var v);
        float angle = index * MathF.Tau / RingSegments;
        return frame.Pivot + (u * MathF.Cos(angle) + v * MathF.Sin(angle)) * (frame.Size * 0.85f);
    }

    private static Vector3 ArrowBasePoint(ObjectGizmoFrame frame, ObjectGizmoAxis axis, int index)
    {
        Basis(axis, out var u, out var v);
        float angle = index * MathF.Tau / ArrowSegments;
        return frame.Pivot + AxisDirection(axis) * (frame.Size * 0.77f) +
            (u * MathF.Cos(angle) + v * MathF.Sin(angle)) * (frame.Size * 0.065f);
    }

    private static void Basis(ObjectGizmoAxis axis, out Vector3 u, out Vector3 v)
    {
        u = axis == ObjectGizmoAxis.X ? Vector3.UnitY : Vector3.UnitX;
        v = Vector3.Cross(AxisDirection(axis), u);
    }

    private static bool TryProjectSegment(ObjectGizmoFrame frame, Vector3 start, Vector3 end,
        out Vector2 a, out Vector2 b)
    {
        a = b = default;
        float startDepth = Vector3.Dot(start - frame.CameraPosition, frame.CameraForward);
        float endDepth = Vector3.Dot(end - frame.CameraPosition, frame.CameraForward);
        if (!float.IsFinite(startDepth) || !float.IsFinite(endDepth) ||
            (startDepth < NearPlane && endDepth < NearPlane) ||
            (startDepth > frame.FarPlane && endDepth > frame.FarPlane)) return false;
        // Clip with a tiny inward offset to avoid rounding back behind the near plane.
        float clip = Math.Min(NearPlane + 0.0001f, Math.Max(startDepth, endDepth));
        if (startDepth < NearPlane) start = Vector3.Lerp(start, end, (clip - startDepth) / (endDepth - startDepth));
        else if (endDepth < NearPlane) end = Vector3.Lerp(start, end, (startDepth - clip) / (startDepth - endDepth));
        startDepth = Vector3.Dot(start - frame.CameraPosition, frame.CameraForward);
        endDepth = Vector3.Dot(end - frame.CameraPosition, frame.CameraForward);
        // Use the preceding representable float so interpolation remains inside the far plane.
        clip = Math.Max(MathF.BitDecrement(frame.FarPlane), Math.Min(startDepth, endDepth));
        if (startDepth > frame.FarPlane) start = Vector3.Lerp(start, end, (startDepth - clip) / (startDepth - endDepth));
        else if (endDepth > frame.FarPlane) end = Vector3.Lerp(start, end, (clip - startDepth) / (endDepth - startDepth));
        return frame.TryProject(start, out a) && frame.TryProject(end, out b);
    }

    private static float SegmentDistanceSquared(Vector2 point, Vector2 start, Vector2 end)
    {
        var delta = end - start;
        float length = delta.LengthSquared();
        float amount = length > 1e-8f ? Math.Clamp(Vector2.Dot(point - start, delta) / length, 0, 1) : 0;
        return Vector2.DistanceSquared(point, start + delta * amount);
    }

    private static float TriangleDistanceSquared(ObjectGizmoFrame frame, Vector2 point,
        Vector3 a, Vector3 b, Vector3 c)
    {
        Span<Vector3> input = stackalloc Vector3[3] { a, b, c };
        Span<Vector3> nearClipped = stackalloc Vector3[4];
        // Clipping a triangle by two parallel planes yields at most five vertices.
        Span<Vector3> clipped = stackalloc Vector3[5];
        int count = ClipPolygon(frame, input, nearClipped, NearPlane, true);
        if (count < 3) return float.PositiveInfinity;
        count = ClipPolygon(frame, nearClipped[..count], clipped, frame.FarPlane, false);
        if (count < 3) return float.PositiveInfinity;
        Span<Vector2> pixels = stackalloc Vector2[5];
        for (int i = 0; i < count; i++)
            if (!frame.TryProject(clipped[i], out pixels[i])) return float.PositiveInfinity;
        float distance = float.PositiveInfinity;
        bool positive = false, negative = false;
        for (int i = 0; i < count; i++)
        {
            var start = pixels[i]; var end = pixels[(i + 1) % count];
            distance = Math.Min(distance, SegmentDistanceSquared(point, start, end));
            var edge = end - start; var offset = point - start;
            float cross = edge.X * offset.Y - edge.Y * offset.X;
            positive |= cross > 0; negative |= cross < 0;
        }
        // Degenerate projected triangles must still be picked as lines rather than an infinite plane.
        float area = 0;
        for (int i = 1; i < count - 1; i++)
        {
            var edgeA = pixels[i] - pixels[0]; var edgeB = pixels[i + 1] - pixels[0];
            area += edgeA.X * edgeB.Y - edgeA.Y * edgeB.X;
        }
        return MathF.Abs(area) > 0.001f && !(positive && negative) ? 0 : distance;
    }

    private static int ClipPolygon(ObjectGizmoFrame frame, ReadOnlySpan<Vector3> input,
        Span<Vector3> output, float plane, bool keepGreater)
    {
        var previous = input[^1];
        float previousDepth = Vector3.Dot(previous - frame.CameraPosition, frame.CameraForward);
        int count = 0;
        foreach (var current in input)
        {
            float depth = Vector3.Dot(current - frame.CameraPosition, frame.CameraForward);
            if (!float.IsFinite(depth) || !float.IsFinite(previousDepth)) return 0;
            bool inside = keepGreater ? depth >= plane : depth <= plane;
            bool previousInside = keepGreater ? previousDepth >= plane : previousDepth <= plane;
            if (inside != previousInside)
            {
                float clip = keepGreater
                    ? Math.Min(plane + 0.0001f, Math.Max(previousDepth, depth))
                    : Math.Max(MathF.BitDecrement(plane), Math.Min(previousDepth, depth));
                output[count++] = Vector3.Lerp(previous, current, (clip - previousDepth) / (depth - previousDepth));
            }
            if (inside) output[count++] = current;
            previous = current; previousDepth = depth;
        }
        return count;
    }

    private static bool Finite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct ObjectGizmoVertex(Vector3 Position, Vector4 Color);
