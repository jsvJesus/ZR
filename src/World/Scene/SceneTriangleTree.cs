using System.Numerics;

namespace ZoneReborn.World.Scene;

// Geometry stays in model space and is shared by every instance of a model.
internal sealed class SceneTriangleTree
{
    private readonly SceneTriangle[] _triangles;
    private readonly List<Node> _nodes = [];

    public SceneTriangleTree(ScenePhysicsMesh model)
    {
        var triangles = new List<SceneTriangle>();
        for (int i = 0; i + 2 < model.Indices.Length; i += 3)
        {
            int a = model.Indices[i], b = model.Indices[i + 1], c = model.Indices[i + 2];
            if ((uint)a >= (uint)model.Vertices.Length || (uint)b >= (uint)model.Vertices.Length ||
                (uint)c >= (uint)model.Vertices.Length) continue;
            var triangle = new SceneTriangle(model.Vertices[a], model.Vertices[b], model.Vertices[c]);
            if (SceneTriangle.IsFinite(triangle.A) && SceneTriangle.IsFinite(triangle.B) &&
                SceneTriangle.IsFinite(triangle.C) && triangle.Normal.LengthSquared() > 1e-16f)
                triangles.Add(triangle);
        }
        _triangles = triangles.ToArray();
        if (_triangles.Length != 0) Build(0, _triangles.Length);
    }

    public void Append(SceneBounds localBounds, SceneBounds worldBounds, Matrix4x4 world,
        List<SceneTriangle> output)
    {
        if (_nodes.Count == 0) return;
        // A reflection reverses cross-product winding. Keep the authored outward side.
        Append(0, localBounds, worldBounds, world, world.GetDeterminant() < 0, output);
    }

    private int Build(int start, int count)
    {
        var bounds = _triangles[start].Bounds;
        for (int i = start + 1; i < start + count; i++) bounds = Union(bounds, _triangles[i].Bounds);
        int node = _nodes.Count;
        _nodes.Add(new(bounds, start, count, -1, -1));
        if (count <= 8) return node;
        Vector3 size = bounds.Maximum - bounds.Minimum;
        int axis = size.X >= size.Y && size.X >= size.Z ? 0 : size.Y >= size.Z ? 1 : 2;
        Array.Sort(_triangles, start, count, AxisComparer.All[axis]);
        int half = count / 2;
        int left = Build(start, half), right = Build(start + half, count - half);
        _nodes[node] = new(bounds, 0, 0, left, right);
        return node;
    }

    private void Append(int index, SceneBounds localBounds, SceneBounds worldBounds, Matrix4x4 world,
        bool mirrored, List<SceneTriangle> output)
    {
        var node = _nodes[index];
        if (!node.Bounds.Intersects(localBounds)) return;
        if (node.Left >= 0)
        {
            Append(node.Left, localBounds, worldBounds, world, mirrored, output);
            Append(node.Right, localBounds, worldBounds, world, mirrored, output);
            return;
        }
        for (int i = node.Start; i < node.Start + node.Count; i++)
        {
            var triangle = _triangles[i];
            if (!triangle.Bounds.Intersects(localBounds)) continue;
            var transformed = new SceneTriangle(Vector3.Transform(triangle.A, world),
                Vector3.Transform(mirrored ? triangle.C : triangle.B, world),
                Vector3.Transform(mirrored ? triangle.B : triangle.C, world));
            if (transformed.Bounds.Intersects(worldBounds) && SceneTriangle.IsFinite(transformed.Normal) &&
                transformed.Normal.LengthSquared() > 1e-16f) output.Add(transformed);
        }
    }

    internal static SceneBounds Union(SceneBounds first, SceneBounds second) =>
        new(Vector3.Min(first.Minimum, second.Minimum), Vector3.Max(first.Maximum, second.Maximum));

    private readonly record struct Node(SceneBounds Bounds, int Start, int Count, int Left, int Right);

    private sealed class AxisComparer(int axis) : IComparer<SceneTriangle>
    {
        public static readonly AxisComparer[] All = [new(0), new(1), new(2)];
        public int Compare(SceneTriangle first, SceneTriangle second)
        {
            Vector3 a = first.A + first.B + first.C, b = second.A + second.B + second.C;
            return (axis == 0 ? a.X : axis == 1 ? a.Y : a.Z)
                .CompareTo(axis == 0 ? b.X : axis == 1 ? b.Y : b.Z);
        }
    }
}

internal readonly record struct SceneTriangle(Vector3 A, Vector3 B, Vector3 C)
{
    public Vector3 Normal => Vector3.Cross(B - A, C - A);
    public SceneBounds Bounds => new(Vector3.Min(A, Vector3.Min(B, C)), Vector3.Max(A, Vector3.Max(B, C)));
    public static bool IsFinite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
