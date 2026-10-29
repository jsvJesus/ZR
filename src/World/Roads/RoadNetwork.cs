using System.Numerics;
using System.Runtime.InteropServices;

namespace ZoneReborn.World.Roads
{
    public sealed class RoadNetwork
    {
        public List<SourceRoad> Roads { get; } = [];
        public List<RoadMaterial> Materials { get; } = [];
        public List<string> Warnings { get; } = [];
        public bool IsDirty { get; set; }
        public bool CanSave { get; set; } = true;
    }

    public sealed class RoadMaterial
    {
        public string Name { get; init; } = "";
        public string Diffuse { get; init; } = "";
        public string Normal { get; init; } = "";
        public string Specular { get; init; } = "";
        public string SpecularPowerMap { get; init; } = "";
        public float SpecularStrength { get; init; }
        public float Gloss { get; init; } = 0.5f;
        public byte Red { get; init; } = 140;
        public byte Green { get; init; } = 140;
        public byte Blue { get; init; } = 140;
        public override string ToString() => Name;
    }

    // Positions and normals are already in world space in the source DAT.
    [StructLayout(LayoutKind.Sequential)]
    public struct RoadVertex(Vector3 position, Vector3 normal, Vector2 uv)
    {
        public Vector3 Position = position;
        public Vector3 Normal = normal;
        public Vector2 Uv = uv;
    }

    public sealed class RoadNode
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float TangentX { get; set; }
        public float TangentZ { get; set; }
        public float Width { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Vector3 Position => new(X, Y, Z);
        public RoadNode Copy() => (RoadNode)MemberwiseClone();
    }

    public sealed class RoadSegment(int index)
    {
        public int Index { get; } = index;
        public string Material { get; set; } = "";
        public string DiffuseOverride { get; set; } = "";
        public string NormalOverride { get; set; } = "";
        public string SpecularOverride { get; set; } = "";
        public string SpecularPowerOverride { get; set; } = "";
        public float UvScale { get; set; } = 1;
        public bool FollowTerrain { get; set; } = true;
        public override string ToString() => $"Сегмент {Index + 1}";
    }

    public sealed class SourceRoad
    {
        public const int SamplesPerSegment = 50;
        public required string Id { get; init; }
        public required string Fingerprint { get; init; }
        public required string Material { get; init; }
        public required RoadVertex[] Vertices { get; init; }
        public float[] TerrainOffsets { get; set; } = [];
        public required int[] Indices { get; init; }
        public required RoadNode[] OriginalNodes { get; init; }
        public required RoadNode[] Nodes { get; init; }
        public required RoadSegment[] Segments { get; init; }
        public required Vector3 XmlPosition { get; init; }
        public int DrawPriority { get; init; }
        public int Revision { get; private set; }
        public bool GeometryEdited { get; private set; }
        public void Changed(bool geometry = false)
        {
            Revision++;
            GeometryEdited |= geometry;
        }
        public override string ToString() => $"{Id} · {Segments.Length}";
    }
}
