using System.Numerics;
using System.Runtime.InteropServices;

namespace ZoneReborn.World.Scene;

public readonly record struct SceneBounds(Vector3 Minimum, Vector3 Maximum)
{
    public Vector3 Center => (Minimum + Maximum) * 0.5f;
    public float Radius => (Maximum - Minimum).Length() * 0.5f;
    public bool Intersects(SceneBounds other) => Minimum.X <= other.Maximum.X && Maximum.X >= other.Minimum.X
        && Minimum.Y <= other.Maximum.Y && Maximum.Y >= other.Minimum.Y
        && Minimum.Z <= other.Maximum.Z && Maximum.Z >= other.Minimum.Z;

    public SceneBounds Transform(Matrix4x4 world)
    {
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        for (int index = 0; index < 8; index++)
        {
            var point = Vector3.Transform(new Vector3((index & 1) == 0 ? Minimum.X : Maximum.X,
                (index & 2) == 0 ? Minimum.Y : Maximum.Y, (index & 4) == 0 ? Minimum.Z : Maximum.Z), world);
            minimum = Vector3.Min(minimum, point); maximum = Vector3.Max(maximum, point);
        }
        return new(minimum, maximum);
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct SceneVertex(Vector3 Position, Vector3 Normal, Vector2 Uv, Vector4 Tangent);

public sealed record SceneMaterial(string Name, string Diffuse, string Normal, Vector4 Color,
    bool DoubleSided, bool AlphaTest, bool Transparent, float Specular);

public sealed record SceneMesh(SceneVertex[] Vertices, int[] Indices, SceneMaterial Material);
public sealed record SceneModel(string File, SceneBounds Bounds, SceneMesh[] Meshes);
public sealed record ScenePhysicsMesh(string File, Vector3[] Vertices, int[] Indices, SceneBounds Bounds);
public sealed record SceneInstance(string Model, Matrix4x4 World, Matrix4x4 InverseWorld,
    SceneBounds Bounds, bool CollisionEnabled)
{
    public bool PlayerCollisionOnly { get; init; }
    public string Id { get; init; } = "";
    public string SourceStamp { get; init; } = "";
    public string? PrefabName { get; init; }
    public SceneBounds LocalBounds { get; init; }
    public SceneBounds VisualLocalBounds { get; init; }
}
