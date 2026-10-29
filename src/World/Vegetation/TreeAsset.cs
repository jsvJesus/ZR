using System.Numerics;
using System.Runtime.InteropServices;

namespace ZoneReborn.World.Vegetation
{
    public enum TreePartKind { Branches, Fronds, Leaves, Billboard }

    [StructLayout(LayoutKind.Sequential)]
    public struct TreeVertex(Vector3 position, Vector3 normal, Vector2 uv, Vector4 card, Vector2 leaf)
    {
        public Vector3 Position = position;
        public Vector3 Normal = normal;
        public Vector2 Uv = uv;
        public Vector4 Card = card;
        public Vector2 Leaf = leaf;
    }

    public sealed record TreePart(TreePartKind Kind, TreeVertex[] Vertices, int[][] Lods, string Diffuse, string Normal);
    public sealed record TreeAsset(string File, Vector3 Minimum, Vector3 Maximum, TreePart[] Parts);
    public sealed record TreeCatalogEntry(string File, string Name)
    {
        public override string ToString() => Name;
    }

    public sealed class TreeInstance
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Model { get; init; } = "";
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
        public float Scale { get; set; } = 1;
        public bool FollowTerrain { get; set; } = true;
        public float HeightOffset { get; set; }
        public override string ToString() => $"{Path.GetFileNameWithoutExtension(Model)} · {X:F1}, {Z:F1}";
    }

    public sealed class VegetationScene
    {
        public List<TreeInstance> Instances { get; } = [];
        public Dictionary<string, TreeAsset> Assets { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Warnings { get; } = [];
        public bool IsDirty { get; set; }
        public bool CanSave { get; set; } = true;
    }
}
