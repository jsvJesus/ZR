using System.Numerics;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters.Formats;
using ZoneReborn.World.Textures.Dds;

namespace ZoneReborn.World.Characters;

public static class CharacterLoader
{
    private static readonly string[] Parts =
    [
        "characters2/clothing/body/body_adidas/body_adidas",
        "characters2/clothing/legs/legs_adidas/legs_adidas",
        "characters2/clothing/boots/boots_adidas/boots_adidas",
        "characters2/clothing/ManNude/ManHead",
        "characters2/clothing/ManNude/ManHands",
        "characters2/clothing/ManNude/ManTeeth"
    ];

    public static CharacterAsset Load(AssetPaths assets) => Load(assets, CancellationToken.None);

    public static CharacterAsset Load(AssetPaths assets, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string Resolve(string path) => AssetPathResolver.Resolve(assets.Root, path);
        var model = PackedSection.Read(Resolve("characters2/basemodel/StalkerBase2.model"));
        var idle = model.All("animation").Single(a => a.String("name") == "idle_unarmed");
        var animation = AnimationReader.Read(Resolve(idle.String("nodes") + ".animation"));
        float frameRate = idle.Find("frameRate")!.Floats.Single();
        if (frameRate <= 0) throw new InvalidDataException("Invalid animation frame rate.");
        var meshes = new List<CharacterMesh>();
        foreach (string part in Parts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var partModel = PackedSection.Read(Resolve(part + ".model"));
            string visualPath = partModel.String("nodefullVisual");
            if (string.IsNullOrEmpty(visualPath)) throw new InvalidDataException($"Missing visual: {part}");
            var visual = PackedSection.Read(Resolve(visualPath + ".visual"));
            var nodes = new List<SkeletonNode>();
            AddNode(visual.Find("node") ?? throw new InvalidDataException("Missing skeleton."), -1);
            var rig = new CharacterRig(nodes.ToArray());
            var nodeIndices = nodes.Select((n, i) => (n.Name, i)).ToDictionary(n => n.Name, n => n.i, StringComparer.Ordinal);
            var primitives = PrimitiveReader.Read(Resolve(visualPath + ".primitives"));
            foreach (var renderSet in visual.All("renderSet"))
            {
                var palette = renderSet.All("node").Select(n => nodeIndices.TryGetValue(n.Text, out int i)
                    ? i : throw new InvalidDataException($"Unknown palette node {n.Text}")).ToArray();
                if (palette.Length == 0) throw new InvalidDataException("Empty skin palette.");
                foreach (var geometry in renderSet.All("geometry"))
                {
                    var vertices = PrimitiveReader.Vertices(primitives[geometry.String("vertices")]);
                    foreach (var v in vertices)
                        if (v.Bone0 >= palette.Length || v.Bone1 >= palette.Length || v.Bone2 >= palette.Length)
                            throw new InvalidDataException("Bone index exceeds renderSet palette.");
                    var (indices, groups) = PrimitiveReader.Indices(primitives[geometry.String("primitive")]);
                    if (indices.Any(i => i < 0 || i >= vertices.Length)) throw new InvalidDataException("Vertex index out of range.");
                    foreach (var groupSection in geometry.All("primitiveGroup"))
                    {
                        int groupIndex = Convert.ToInt32(groupSection.Value);
                        if ((uint)groupIndex >= groups.Length) throw new InvalidDataException("Invalid material group.");
                        var material = groupSection.Find("material") ?? throw new InvalidDataException("Missing material.");
                        var diffuse = material.All("property").FirstOrDefault(p => p.Text == "diffuseMap")?.String("Texture");
                        if (string.IsNullOrEmpty(diffuse)) throw new InvalidDataException($"Missing diffuse map in {part}");
                        string diffusePath = Resolve(diffuse);
                        // Validate source DDS during the CPU load, before changing the viewport.
                        cancellationToken.ThrowIfCancellationRequested();
                        DdsReader.Read(diffusePath);
                        var group = groups[groupIndex];
                        bool alpha = material.String("fx").Contains("alpha", StringComparison.OrdinalIgnoreCase);
                        meshes.Add(new(vertices, indices.AsSpan(group.StartIndex, group.TriangleCount * 3).ToArray(), diffusePath, rig, palette, alpha));
                    }
                }
            }
            void AddNode(PackedSection node, int parent)
            {
                var t = node.Find("transform")?.Floats ?? throw new InvalidDataException("Missing node transform.");
                if (t.Length != 12) throw new InvalidDataException("Invalid node matrix.");
                int index = nodes.Count;
                nodes.Add(new(node.String("identifier"), parent,
                    new Matrix4x4(t[0], t[1], t[2], 0, t[3], t[4], t[5], 0, t[6], t[7], t[8], 0, t[9], t[10], t[11], 1)));
                foreach (var child in node.All("node")) AddNode(child, index);
            }
        }
        return new(meshes.ToArray(), animation, frameRate);
    }
}
