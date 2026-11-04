using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ZoneReborn.World.Scene;

/// <summary>Shared model triangles queried in world units. Capsule positions are at the feet.</summary>
public sealed class SceneCollision
{
    private const float Skin = 0.001f;
    private const float Tolerance = 0.0001f;
    private const float GroundProbeLift = 0.01f;
    private static readonly float WalkableY = MathF.Cos(40f * MathF.PI / 180f);
    private static readonly ConditionalWeakTable<ScenePhysicsMesh, Lazy<SceneTriangleTree>> Trees = new();
    private readonly ConcurrentBag<List<SceneTriangle>> _buffers = [];
    private readonly SceneData _scene;
    private readonly SceneBounds _bounds;
    private readonly bool _hasGeometry;

    public SceneCollision(SceneData scene)
    {
        _scene = scene;
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        foreach (var instance in scene.Instances)
        {
            if (!instance.CollisionEnabled || !SceneTriangle.IsFinite(instance.Bounds.Minimum) ||
                !SceneTriangle.IsFinite(instance.Bounds.Maximum)) continue;
            minimum = Vector3.Min(minimum, instance.Bounds.Minimum);
            maximum = Vector3.Max(maximum, instance.Bounds.Maximum);
            _hasGeometry = true;
        }
        _bounds = new(minimum, maximum);
    }

    /// <summary>Highest walkable feet height in the supplied vertical window; radius zero queries the raw surface.</summary>
    public bool TryGround(float x, float z, float maximumY, float minimumY, out float height, float radius = 0)
    {
        height = 0;
        if (!_hasGeometry || !float.IsFinite(x) || !float.IsFinite(z) ||
            !float.IsFinite(radius) || radius < 0 || float.IsNaN(maximumY) ||
            float.IsNaN(minimumY) || maximumY < minimumY) return false;
        // A sphere touching a ramp needs feet above the plane by r * (sec(slope) - 1).
        // Apply the caller's storey window to that contact height, not the raw triangle Y.
        // The 40-degree slope cap makes radius a conservative bound on this extra height.
        float rawMinimum = Math.Max(minimumY - radius, _bounds.Minimum.Y);
        float rawMaximum = Math.Min(maximumY + radius, _bounds.Maximum.Y);
        if (rawMaximum < rawMinimum) return false;
        var triangles = Rent();
        try
        {
            Collect(new(new(x - radius - Tolerance, rawMinimum - Tolerance, z - radius - Tolerance),
                new(x + radius + Tolerance, rawMaximum + Tolerance, z + radius + Tolerance)), triangles);
            bool found = false;
            foreach (var triangle in triangles)
            {
                Vector3 normal = triangle.Normal;
                float length = normal.Length();
                if (normal.Y < WalkableY * length) continue;
                float rawY = triangle.A.Y - (normal.X * (x - triangle.A.X) + normal.Z * (z - triangle.A.Z)) / normal.Y;
                float y = rawY + radius * (length / normal.Y - 1);
                if (y < minimumY || y > maximumY || (found && y <= height)) continue;
                // The sphere contacts an inclined face away from the vertical foot ray.
                // Check that actual contact, or finite ramp crests become an invisible plane.
                var point = new Vector3(x, y + radius, z) - normal * (radius / length);
                if (!PointInTriangle(point, triangle)) continue;
                height = y;
                found = true;
            }
            return found;
        }
        finally { Return(triangles); }
    }

    /// <summary>Moves a vertical capsule with continuous triangle sweeps, sliding and limited depenetration.</summary>
    public Vector3 MoveCapsule(Vector3 from, Vector3 target, float radius, float height)
    {
        if (!ValidCapsule(from, radius, height) || !SceneTriangle.IsFinite(target)) return from;
        if (!_hasGeometry) return target;
        Vector3 displacement = target - from;
        float length = displacement.Length();
        if (!float.IsFinite(length)) return from;
        double count = Math.Max(1, Math.Ceiling(length / (radius * 0.5f)));
        if (count > int.MaxValue) return from;
        int steps = (int)count;
        Vector3 step = displacement / steps;
        var triangles = Rent();
        try
        {
            Vector3 position = from;
            for (int i = 0; i < steps; i++)
            {
                // Refresh around the actual slide path: a long diagonal slide can leave
                // the bounds of the original from/target segment. Reuse the same buffer.
                triangles.Clear();
                var padding = new Vector3(radius * 2 + Skin);
                Collect(new(Vector3.Min(position, position + step) - padding,
                    Vector3.Max(position, position + step) + new Vector3(0, height, 0) + padding), triangles);
                if (triangles.Count == 0) { position += step; continue; }
                position = Depenetrate(position, radius, height, triangles);
                position = Slide(position, step, radius, height, triangles);
            }
            return position;
        }
        finally { Return(triangles); }
    }

    /// <summary>Allows surface contact but rejects penetration, including crouch-to-stand ceiling obstruction.</summary>
    public bool CanOccupy(Vector3 position, float radius, float height)
    {
        if (!ValidCapsule(position, radius, height)) return false;
        if (!_hasGeometry) return true;
        var triangles = Rent();
        try
        {
            Collect(CapsuleBounds(position, radius, height), triangles);
            CapsuleSegment(Vector3.Zero, radius, height, out Vector3 a, out Vector3 b);
            float allowed = Math.Max(0, radius - Tolerance);
            foreach (var triangle in triangles)
            {
                ClosestPoints(a, b, RelativeTo(triangle, position), out Vector3 onCapsule, out Vector3 onTriangle);
                if (Vector3.DistanceSquared(onCapsule, onTriangle) < allowed * allowed) return false;
            }
            return true;
        }
        finally { Return(triangles); }
    }

    /// <summary>Finds the first walkable capsule support below the feet, including a stair tread's rounded-cap edge contact.</summary>
    public bool TrySnapDown(Vector3 position, float radius, float height, float maxDistance, out Vector3 supported,
        float maximumSurfaceY = float.PositiveInfinity) =>
        TrySnapDown(position, radius, height, maxDistance, out supported, out _, maximumSurfaceY);

    public bool TrySnapDown(Vector3 position, float radius, float height, float maxDistance, out Vector3 supported,
        out float surfaceY, float maximumSurfaceY = float.PositiveInfinity)
    {
        supported = position;
        surfaceY = position.Y;
        if (!ValidCapsule(position, radius, height) || !float.IsFinite(maxDistance) ||
            maxDistance <= 0 || float.IsNaN(maximumSurfaceY) || !_hasGeometry) return false;
        // Recover small contact errors at seams instead of returning an already penetrating
        // position that CanOccupy rejects. A low ceiling can prevent this optional lift.
        Vector3 lifted = position + Vector3.UnitY * GroundProbeLift;
        if (CanOccupy(lifted, radius, height))
        {
            position = lifted;
            maxDistance += GroundProbeLift;
        }
        Vector3 down = new(0, -maxDistance, 0);
        var triangles = Rent();
        try
        {
            var bounds = SceneTriangleTree.Union(CapsuleBounds(position, radius, height),
                CapsuleBounds(position + down, radius, height));
            var padding = new Vector3(Skin);
            Collect(new(bounds.Minimum - padding, bounds.Maximum + padding), triangles);
            bool found = false;
            float time = 1;
            foreach (var triangle in triangles)
            {
                // Classify the tread's authored face, rather than the contact normal:
                // near a step edge the lower sphere touches on a diagonal normal.
                Vector3 faceNormal = triangle.Normal;
                if (faceNormal.Y < WalkableY * faceNormal.Length()) continue;
                if (!Sweep(position, down, radius, height, triangle, time, out float contact, out var contactNormal)
                    || contactNormal.Y <= Tolerance) continue;
                Vector3 contactFeet = position + down * contact;
                CapsuleSegment(Vector3.Zero, radius, height, out var a, out var b);
                ClosestPoints(a, b, RelativeTo(triangle, contactFeet), out _, out var onTriangle);
                // A capsule may touch a tall tread while its feet are still much lower.
                // Limit the actual surface so repeated edge contacts cannot climb a tall wall.
                if (onTriangle.Y > maximumSurfaceY - contactFeet.Y + Skin) continue;
                time = contact;
                surfaceY = contactFeet.Y + onTriangle.Y;
                found = true;
            }
            if (found) supported = position + down * time;
            return found;
        }
        finally { Return(triangles); }
    }

    /// <summary>Two-sided ray query; returned distance is in world units for any nonzero direction.</summary>
    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out float distance)
    {
        distance = 0;
        if (!_hasGeometry || !SceneTriangle.IsFinite(origin) || !SceneTriangle.IsFinite(direction) ||
            float.IsNaN(maxDistance) || maxDistance < 0) return false;
        float directionLength = direction.Length();
        if (!float.IsFinite(directionLength) || directionLength < 1e-12f) return false;
        direction /= directionLength;
        // Clip before using the scene grid, so an infinite/very long ray cannot enumerate unbounded cells.
        if (!RayBounds(origin, direction, _bounds, maxDistance, out float near, out float far)) return false;
        Vector3 first = origin + direction * near, last = origin + direction * far;
        var padding = new Vector3(Skin);
        var triangles = Rent();
        try
        {
            Collect(new(Vector3.Min(first, last) - padding, Vector3.Max(first, last) + padding), triangles, includePlayerOnly: false);
            bool found = false;
            float nearest = far;
            foreach (var triangle in triangles)
            {
                if (!RayTriangle(origin, direction, triangle, out float hit) || hit < 0 || hit > nearest) continue;
                nearest = hit;
                found = true;
            }
            if (found) distance = nearest;
            return found;
        }
        finally { Return(triangles); }
    }

    private List<SceneTriangle> Rent() => _buffers.TryTake(out var buffer) ? buffer : new(128);
    private void Return(List<SceneTriangle> buffer)
    {
        buffer.Clear();
        // A one-off long ray should not retain a whole level's transformed triangles forever.
        if (buffer.Capacity <= 32768) _buffers.Add(buffer);
    }

    private void Collect(SceneBounds bounds, List<SceneTriangle> triangles, bool includePlayerOnly = true)
    {
        if (!bounds.Intersects(_bounds)) return;
        bounds = new(Vector3.Max(bounds.Minimum, _bounds.Minimum), Vector3.Min(bounds.Maximum, _bounds.Maximum));
        foreach (var instance in _scene.Query(bounds))
        {
            if (!instance.CollisionEnabled || (!includePlayerOnly && instance.PlayerCollisionOnly)) continue;
            // Read the authored physical mesh; visual vertices and materials are irrelevant here.
            ScenePhysicsMesh? model = _scene.Physics.Get(instance.Model);
            if (model == null) continue;
            SceneBounds local = bounds.Transform(instance.InverseWorld);
            if (!SceneTriangle.IsFinite(local.Minimum) || !SceneTriangle.IsFinite(local.Maximum)) continue;
            var tree = Trees.GetValue(model, static value => new Lazy<SceneTriangleTree>(
                () => new SceneTriangleTree(value), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
            tree.Append(local, bounds, instance.World, triangles);
        }
    }

    private static bool ValidCapsule(Vector3 position, float radius, float height) =>
        SceneTriangle.IsFinite(position) && float.IsFinite(radius) && float.IsFinite(height) &&
        radius > Tolerance && height >= 2 * radius;

    private static SceneBounds CapsuleBounds(Vector3 position, float radius, float height) =>
        new(position - new Vector3(radius, 0, radius), position + new Vector3(radius, height, radius));

    private static void CapsuleSegment(Vector3 position, float radius, float height, out Vector3 a, out Vector3 b)
    {
        a = position + new Vector3(0, radius, 0);
        b = position + new Vector3(0, height - radius, 0);
    }

    // Keep closest points and their difference near the capsule, rather than adding
    // kilometre-scale world coordinates back before computing a contact normal.
    // At Aspen coordinates one world-space float ULP can exceed Tolerance.
    private static SceneTriangle RelativeTo(SceneTriangle triangle, Vector3 origin) =>
        new(triangle.A - origin, triangle.B - origin, triangle.C - origin);

    private static Vector3 Depenetrate(Vector3 position, float radius, float height, List<SceneTriangle> triangles)
    {
        for (int pass = 0; pass < 6; pass++)
        {
            bool changed = false;
            foreach (var triangle in triangles)
            {
                if (!CapsuleBounds(position, radius, height).Intersects(triangle.Bounds)) continue;
                CapsuleSegment(Vector3.Zero, radius, height, out Vector3 a, out Vector3 b);
                var local = RelativeTo(triangle, position);
                ClosestPoints(a, b, local, out Vector3 onCapsule, out Vector3 onTriangle);
                Vector3 separation = onCapsule - onTriangle;
                float length = separation.Length();
                if (length >= radius - Tolerance) continue;
                float depth = radius - length;
                Vector3 normal;
                if (length > 1e-6f) normal = separation / length;
                else
                {
                    normal = SurfaceNormal(a, b, local, Vector3.Zero);
                    depth = radius - Math.Min(Vector3.Dot(a - local.A, normal), Vector3.Dot(b - local.A, normal));
                }
                position += normal * (depth + Skin);
                changed = true;
            }
            if (!changed) break;
        }
        return position;
    }

    private static Vector3 Slide(Vector3 position, Vector3 remaining, float radius, float height, List<SceneTriangle> triangles)
    {
        bool horizontalMovement = Math.Abs(remaining.Y) <= Tolerance;
        for (int pass = 0; pass < 6 && remaining.LengthSquared() > 1e-12f; pass++)
        {
            float time = 1;
            Vector3 normal = default;
            bool hit = false;
            bool walkableFace = false;
            var sweepBounds = SceneTriangleTree.Union(CapsuleBounds(position, radius + Skin, height),
                CapsuleBounds(position + remaining, radius + Skin, height));
            foreach (var triangle in triangles)
            {
                if (!triangle.Bounds.Intersects(sweepBounds) ||
                    !Sweep(position, remaining, radius, height, triangle, time, out float contact, out Vector3 contactNormal)) continue;
                time = contact;
                normal = contactNormal;
                Vector3 faceNormal = triangle.Normal;
                walkableFace = faceNormal.Y >= WalkableY * faceNormal.Length();
                hit = true;
            }
            position += remaining * time;
            if (!hit) break;
            remaining *= 1 - time;
            // A horizontal push must not become an ascent of a wall or an unwalkable slope.
            // Keep the intent of the original sweep: projecting onto an earlier ramp
            // must not disable this constraint at the next wall or steep edge.
            if (horizontalMovement && normal.Y > 0 && (!walkableFace || normal.Y < WalkableY))
            {
                normal.Y = 0;
                if (normal.LengthSquared() <= 1e-12f) break;
                normal = Vector3.Normalize(normal);
                remaining.Y = Math.Min(remaining.Y, 0);
            }
            float into = Vector3.Dot(remaining, normal);
            if (into >= 0) break;
            remaining -= normal * into;
        }
        return position;
    }

    private static bool Sweep(Vector3 position, Vector3 movement, float radius, float height,
        SceneTriangle triangle, float limit, out float time, out Vector3 normal)
    {
        time = 0;
        normal = default;
        triangle = RelativeTo(triangle, position);
        CapsuleSegment(Vector3.Zero, radius, height, out Vector3 a, out Vector3 b);
        // Distance to a convex triangle is convex along this translation. Its separating
        // plane gives a conservative advancement, including triangle edges and vertices.
        for (int iteration = 0; iteration < 24; iteration++)
        {
            Vector3 offset = movement * time;
            ClosestPoints(a + offset, b + offset, triangle, out Vector3 onCapsule, out Vector3 onTriangle);
            Vector3 separation = onCapsule - onTriangle;
            float length = separation.Length();
            normal = length > 1e-7f ? separation / length : SurfaceNormal(a + offset, b + offset, triangle, movement);
            float closing = -Vector3.Dot(movement, normal);
            if (closing <= 1e-8f) return false;
            float gap = length - radius - Skin;
            if (gap <= Tolerance) return true;
            float advance = gap / closing;
            if (time + advance > limit) return false;
            time += advance;
        }
        // Stop conservatively if edge grazing converges too slowly to resolve this substep.
        return true;
    }

    private static Vector3 SurfaceNormal(Vector3 a, Vector3 b, SceneTriangle triangle, Vector3 movement)
    {
        Vector3 normal = Vector3.Normalize(triangle.Normal);
        float side = Vector3.Dot((a + b) * 0.5f - triangle.A, normal);
        if (side < 0 || (Math.Abs(side) < 1e-7f && Vector3.Dot(movement, normal) > 0)) normal = -normal;
        return normal;
    }

    private static bool RayBounds(Vector3 origin, Vector3 direction, SceneBounds bounds, float limit,
        out float near, out float far)
    {
        near = 0;
        far = limit;
        for (int axis = 0; axis < 3; axis++)
        {
            float o = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
            float d = axis == 0 ? direction.X : axis == 1 ? direction.Y : direction.Z;
            float low = axis == 0 ? bounds.Minimum.X : axis == 1 ? bounds.Minimum.Y : bounds.Minimum.Z;
            float high = axis == 0 ? bounds.Maximum.X : axis == 1 ? bounds.Maximum.Y : bounds.Maximum.Z;
            if (Math.Abs(d) < 1e-12f)
            {
                if (o < low || o > high) return false;
                continue;
            }
            float first = (low - o) / d, last = (high - o) / d;
            if (first > last) (first, last) = (last, first);
            near = Math.Max(near, first);
            far = Math.Min(far, last);
            if (near > far) return false;
        }
        return float.IsFinite(near) && float.IsFinite(far);
    }

    private static bool RayTriangle(Vector3 origin, Vector3 direction, SceneTriangle triangle, out float time)
    {
        time = 0;
        Vector3 edge1 = triangle.B - triangle.A, edge2 = triangle.C - triangle.A;
        Vector3 cross = Vector3.Cross(direction, edge2);
        float determinant = Vector3.Dot(edge1, cross);
        if (Math.Abs(determinant) < 1e-10f) return false;
        float inverse = 1 / determinant;
        Vector3 offset = origin - triangle.A;
        float u = Vector3.Dot(offset, cross) * inverse;
        if (u < -1e-6f || u > 1 + 1e-6f) return false;
        Vector3 q = Vector3.Cross(offset, edge1);
        float v = Vector3.Dot(direction, q) * inverse;
        if (v < -1e-6f || u + v > 1 + 1e-6f) return false;
        time = Vector3.Dot(edge2, q) * inverse;
        return true;
    }

    private static bool PointInTriangle(Vector3 point, SceneTriangle triangle)
    {
        Vector3 normal = triangle.Normal;
        float tolerance = -1e-6f * normal.LengthSquared();
        return Vector3.Dot(Vector3.Cross(triangle.B - triangle.A, point - triangle.A), normal) >= tolerance &&
            Vector3.Dot(Vector3.Cross(triangle.C - triangle.B, point - triangle.B), normal) >= tolerance &&
            Vector3.Dot(Vector3.Cross(triangle.A - triangle.C, point - triangle.C), normal) >= tolerance;
    }

    private static void ClosestPoints(Vector3 a, Vector3 b, SceneTriangle triangle, out Vector3 onSegment, out Vector3 onTriangle)
    {
        Vector3 direction = b - a;
        if (RayTriangle(a, direction, triangle, out float t) && t >= 0 && t <= 1)
        {
            onSegment = onTriangle = a + direction * t;
            return;
        }
        onSegment = a;
        onTriangle = ClosestPoint(a, triangle);
        float best = Vector3.DistanceSquared(onSegment, onTriangle);
        Vector3 end = ClosestPoint(b, triangle);
        float endDistance = Vector3.DistanceSquared(b, end);
        if (endDistance < best) { best = endDistance; onSegment = b; onTriangle = end; }
        CompareEdge(a, b, triangle.A, triangle.B, ref best, ref onSegment, ref onTriangle);
        CompareEdge(a, b, triangle.B, triangle.C, ref best, ref onSegment, ref onTriangle);
        CompareEdge(a, b, triangle.C, triangle.A, ref best, ref onSegment, ref onTriangle);
    }

    private static void CompareEdge(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
        ref float best, ref Vector3 onSegment, ref Vector3 onTriangle)
    {
        SegmentPoints(a, b, c, d, out Vector3 first, out Vector3 second);
        float distance = Vector3.DistanceSquared(first, second);
        if (distance >= best) return;
        best = distance;
        onSegment = first;
        onTriangle = second;
    }

    private static void SegmentPoints(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out Vector3 first, out Vector3 second)
    {
        Vector3 u = b - a, v = d - c, r = a - c;
        float uu = Vector3.Dot(u, u), vv = Vector3.Dot(v, v), vr = Vector3.Dot(v, r);
        float s, t;
        if (uu <= 1e-14f && vv <= 1e-14f) { first = a; second = c; return; }
        if (uu <= 1e-14f) { s = 0; t = Math.Clamp(vr / vv, 0, 1); }
        else
        {
            float ur = Vector3.Dot(u, r);
            if (vv <= 1e-14f) { t = 0; s = Math.Clamp(-ur / uu, 0, 1); }
            else
            {
                float uv = Vector3.Dot(u, v), denominator = uu * vv - uv * uv;
                s = denominator > 1e-14f ? Math.Clamp((uv * vr - ur * vv) / denominator, 0, 1) : 0;
                t = (uv * s + vr) / vv;
                if (t < 0) { t = 0; s = Math.Clamp(-ur / uu, 0, 1); }
                else if (t > 1) { t = 1; s = Math.Clamp((uv - ur) / uu, 0, 1); }
            }
        }
        first = a + u * s;
        second = c + v * t;
    }

    private static Vector3 ClosestPoint(Vector3 point, SceneTriangle triangle)
    {
        Vector3 ab = triangle.B - triangle.A, ac = triangle.C - triangle.A, ap = point - triangle.A;
        float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0 && d2 <= 0) return triangle.A;
        Vector3 bp = point - triangle.B;
        float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0 && d4 <= d3) return triangle.B;
        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0 && d1 >= 0 && d3 <= 0) return triangle.A + ab * (d1 / (d1 - d3));
        Vector3 cp = point - triangle.C;
        float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0 && d5 <= d6) return triangle.C;
        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0 && d2 >= 0 && d6 <= 0) return triangle.A + ac * (d2 / (d2 - d6));
        float va = d3 * d6 - d5 * d4;
        if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0)
            return triangle.B + (triangle.C - triangle.B) * ((d4 - d3) / (d4 - d3 + d5 - d6));
        float inverse = 1 / (va + vb + vc);
        return triangle.A + ab * (vb * inverse) + ac * (vc * inverse);
    }
}
