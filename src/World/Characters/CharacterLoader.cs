using System.Numerics;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters.Formats;
using ZoneReborn.World.Characters.Locomotion;
using ZoneReborn.World.Textures.Dds;

namespace ZoneReborn.World.Characters;

public static class CharacterLoader
{
    private const string BaseModel = "characters2/basemodel/StalkerBase2";
    private static readonly IReadOnlyDictionary<string, string> DefaultTints = new Dictionary<string, string>();
    private static readonly CharacterPartSelection[] DefaultParts =
    [
        new("characters2/clothing/body/body_adidas/body_adidas", DefaultTints),
        new("characters2/clothing/legs/legs_adidas/legs_adidas", DefaultTints),
        new("characters2/clothing/boots/boots_adidas/boots_adidas", DefaultTints),
        new("characters2/clothing/ManNude/ManHead", DefaultTints),
        new("characters2/clothing/ManNude/ManHands", DefaultTints),
        new("characters2/clothing/ManNude/ManTeeth", DefaultTints)
    ];

    public static CharacterAsset Load(AssetPaths assets) => Load(assets, CancellationToken.None);

    public static CharacterAsset Load(AssetPaths assets, CancellationToken cancellationToken) =>
        Load(assets, DefaultParts, cancellationToken);

    public static CharacterAsset Load(AssetPaths assets, IReadOnlyList<CharacterPartSelection> selections, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assets);
        cancellationToken.ThrowIfCancellationRequested();
        var model = PackedSection.Read(AssetPathResolver.Resolve(assets.Root, BaseModel + ".model"));
        var idle = model.All("animation").Single(a => a.String("name") == "idle_unarmed");
        var animation = AnimationReader.Read(AssetPathResolver.Resolve(assets.Root, WithExtension(idle.String("nodes"), ".animation")));
        float frameRate = idle.Find("frameRate")?.Floats.Single() ?? 0;
        if (frameRate <= 0) throw new InvalidDataException("Invalid animation frame rate.");
        var asset = Build(assets, selections, animation, frameRate, cancellationToken);
        var locomotion = LocomotionLibrary.Read(assets, model, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return asset with { Locomotion = locomotion };
    }

    public static CharacterAsset Rebuild(AssetPaths assets, IReadOnlyList<CharacterPartSelection> selections, CharacterAsset template, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(template);
        return Build(assets, selections, template.Animation, template.FrameRate, cancellationToken)
            with { Locomotion = template.Locomotion };
    }

    private static CharacterAsset Build(AssetPaths assets, IReadOnlyList<CharacterPartSelection> selections, SourceAnimation animation, float frameRate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(selections);
        cancellationToken.ThrowIfCancellationRequested();
        string Resolve(string path) => AssetPathResolver.Resolve(assets.Root, path);
        // Anchors belong to the avatar, even if no garment exports their skeleton branch.
        var avatarRig = ReadRig(PackedSection.Read(Resolve(BaseModel + ".visual")), true, cancellationToken);
        var avatarNodes = NodeIndices(avatarRig);
        var meshes = new List<CharacterMesh>();
        var validatedTextures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var selection in selections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(selection);
            ArgumentNullException.ThrowIfNull(selection.Tints);
            string modelPath = WithExtension(selection.Model, ".model");
            var model = PackedSection.Read(Resolve(modelPath));
            string visualPath = model.String("nodefullVisual");
            if (string.IsNullOrWhiteSpace(visualPath)) visualPath = model.String("nodelessVisual");
            if (string.IsNullOrWhiteSpace(visualPath)) throw new InvalidDataException($"Missing visual in character model: {modelPath}");
            var visual = PackedSection.Read(Resolve(WithExtension(visualPath, ".visual")));
            bool attached = !string.IsNullOrWhiteSpace(selection.AttachmentNode);
            if (!attached && selection.OriginNode != null) throw new InvalidDataException($"Attachment node is missing for {modelPath}.");
            var rig = ReadRig(visual, !attached, cancellationToken);
            var nodeIndices = NodeIndices(rig);
            CharacterAttachment? attachment = null;
            if (attached)
            {
                if (!avatarNodes.TryGetValue(selection.AttachmentNode!, out int anchor))
                    throw new InvalidDataException($"Unknown avatar attachment node '{selection.AttachmentNode}' for {modelPath}.");
                string originName = selection.OriginNode ?? rig.Nodes[0].Name;
                if (!nodeIndices.TryGetValue(originName, out int origin))
                    throw new InvalidDataException($"Unknown model origin node '{originName}' in {modelPath}.");
                if (!Matrix4x4.Invert(BindGlobals(rig)[origin], out var inverseOrigin))
                    throw new InvalidDataException($"Non-invertible attachment origin '{originName}' in {modelPath}.");
                attachment = new(avatarRig, anchor, inverseOrigin);
                if (selection.Weapon != null)
                {
                    var globals = BindGlobals(rig);
                    bool legacy = !nodeIndices.ContainsKey("HP_Rhand");
                    string rightName = legacy ? "HP_effectorright" : "HP_Rhand";
                    if (!nodeIndices.TryGetValue(rightName, out int right))
                        throw new InvalidDataException($"Missing weapon hand target in {modelPath}.");
                    var rightHand = globals[right];
                    var gunOrigin = nodeIndices.TryGetValue("HP_GUNR", out int gun) ? globals[gun] : rightHand;
                    var pivot = nodeIndices.TryGetValue("HP_GUNRW", out int gunPivot) ? globals[gunPivot] : gunOrigin;
                    string leftName = legacy ? "HP_effectorleft" : "HP_Lhand";
                    Matrix4x4? leftHand = nodeIndices.TryGetValue(leftName, out int left) ? globals[left] : null;
                    if (!Matrix4x4.Invert(rightHand, out var inverseHand) || !Matrix4x4.Invert(gunOrigin, out var inverseGun))
                        throw new InvalidDataException($"Non-invertible weapon hand target in {modelPath}.");
                    attachment = attachment with { Weapon = new(avatarNodes["HP_Rhand"], avatarNodes["HP_GUNR"],
                        inverseHand, inverseGun, pivot, rightHand, leftHand, legacy) };
                }
            }
            // Parent normally names the next LOD; never append its geometry.
            string primitivesPath = Path.ChangeExtension(WithExtension(visualPath, ".visual"), ".primitives");
            var primitives = PrimitiveReader.Read(Resolve(primitivesPath));
            foreach (var renderSet in visual.All("renderSet"))
            {
                int[]? palette = null;
                foreach (var geometry in renderSet.All("geometry"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var materials = geometry.All("primitiveGroup")
                        .Select(group => (Group: group, Material: SelectMaterial(model, group, selection.Tints)))
                        .Where(pair => IsRendered(pair.Material)).ToArray();
                    // Hidden groups can also have empty palettes.
                    if (materials.Length == 0) continue;
                    palette ??= renderSet.All("node").Select(n => nodeIndices.TryGetValue(n.Text, out int i)
                        ? i : throw new InvalidDataException($"Unknown palette node '{n.Text}' in {modelPath}.")).ToArray();
                    if (palette.Length == 0) throw new InvalidDataException($"Empty skin palette in {modelPath}.");
                    var vertices = PrimitiveReader.Vertices(Section(geometry.String("vertices")), out bool rigid);
                    var uv2 = geometry.All("stream").FirstOrDefault(stream => stream.Text.EndsWith(".uv2", StringComparison.OrdinalIgnoreCase));
                    if (uv2 != null) PrimitiveReader.ApplySecondaryUv(vertices, Section(uv2.Text));
                    if (rigid && palette.Length != 1) throw new InvalidDataException($"Rigid geometry requires one palette node in {modelPath}.");
                    foreach (var vertex in vertices)
                        if ((uint)vertex.Bone0 >= palette.Length || (uint)vertex.Bone1 >= palette.Length || (uint)vertex.Bone2 >= palette.Length)
                            throw new InvalidDataException($"Bone index exceeds renderSet palette in {modelPath}.");
                    var (indices, groups) = PrimitiveReader.Indices(Section(geometry.String("primitive")));
                    if (indices.Any(i => (uint)i >= vertices.Length)) throw new InvalidDataException($"Vertex index out of range in {modelPath}.");
                    foreach (var (groupSection, material) in materials)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        int groupIndex = Convert.ToInt32(groupSection.Value);
                        if ((uint)groupIndex >= groups.Length) throw new InvalidDataException($"Invalid material group in {modelPath}.");
                        string diffuse = Diffuse(material);
                        if (string.IsNullOrWhiteSpace(diffuse))
                            throw new InvalidDataException($"Missing diffuse map for '{material.String("identifier")}' in {modelPath}.");
                        string diffusePath = Resolve(diffuse);
                        if (validatedTextures.Add(diffusePath)) DdsReader.Read(diffusePath);
                        var group = groups[groupIndex];
                        if ((long)group.StartVertex + group.VertexCount > vertices.Length)
                            throw new InvalidDataException($"Primitive vertex range exceeds geometry in {modelPath}.");
                        var surface = ReadSurface(material);
                        foreach (string texture in new[] { surface.ColourMaskPath, surface.DiffuseMaskPath, surface.OpacityMaskPath }.OfType<string>())
                            if (validatedTextures.Add(texture)) DdsReader.Read(texture);
                        bool alpha = surface.UsesAlpha && !Boolean(material, "alphaTestEnable", false);
                        meshes.Add(new(vertices, indices.AsSpan(group.StartIndex, checked(group.TriangleCount * 3)).ToArray(), diffusePath, rig, palette, alpha)
                        {
                            Attachment = attachment,
                            DiffuseColor = BaseColour(material),
                            Surface = surface
                        });
                    }
                }
            }
            byte[] Section(string name) => primitives.TryGetValue(name, out var data) ? data
                : throw new InvalidDataException($"Missing primitive section '{name}' in {primitivesPath}.");
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (meshes.Count == 0) throw new InvalidDataException("The character selection has no visible geometry.");
        return new(meshes.ToArray(), animation, frameRate) { AvatarRig = avatarRig,
            Weapon = selections.Select(part => part.Weapon).FirstOrDefault(weapon => weapon != null) };

        CharacterSurfaceMaterial ReadSurface(PackedSection material)
        {
            string fx = material.String("fx");
            bool colourise = fx.Contains("colourise", StringComparison.OrdinalIgnoreCase);
            bool hair = fx.Contains("/hair_", StringComparison.OrdinalIgnoreCase);
            bool alpha = fx.Contains("alpha", StringComparison.OrdinalIgnoreCase);
            bool alphaTest = Boolean(material, "alphaTestEnable", false);
            float scale = Scalar(material, "gScale", 1);
            float rotation = Scalar(material, "gRotateMask", 0) * MathF.PI / 180f;
            string? Texture(string name)
            {
                string path = Property(material, name)?.String("Texture") ?? "";
                return string.IsNullOrWhiteSpace(path) ? null : Resolve(path);
            }
            return new()
            {
                ColourMaskPath = colourise ? Texture("MaskForColourise") : null,
                DiffuseMaskPath = colourise ? Texture("MaskForDiffuse") : null,
                OpacityMaskPath = hair ? Texture("hairMask") : null,
                ColourR = Colour(material, "colouriseR"), ColourG = Colour(material, "colouriseG"),
                ColourB = Colour(material, "colouriseB"), ColourA = Colour(material, "colouriseA"),
                PatternScaleShift = new(scale * Scalar(material, "gScaleU", 1), scale * Scalar(material, "gScaleV", 1),
                    Scalar(material, "gShiftMaskU", 0), Scalar(material, "gShiftMaskV", 0)),
                PatternRotation = new(MathF.Cos(rotation), MathF.Sin(rotation)),
                ColourByUv2 = fx.Contains("colourise_by_uv2", StringComparison.OrdinalIgnoreCase),
                OpacityScaleShift = new(Scalar(material, "hairScaleU", 1), 1, 0, Scalar(material, "hairShiftV", 0)),
                DoubleSided = Boolean(material, "doubleSided", false),
                UsesAlpha = alpha || alphaTest,
                AlphaCutoff = alphaTest ? Math.Clamp(Scalar(material, "alphaReference", 128) / 255f, 0, 1) : alpha ? 0.001f : 0
            };
        }
    }

    private static string WithExtension(string path, string extension)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new InvalidDataException($"Missing resource path ({extension}).");
        return path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? path : path + extension;
    }

    private static Dictionary<string, int> NodeIndices(CharacterRig rig)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < rig.Nodes.Length; i++)
            if (!result.TryAdd(rig.Nodes[i].Name, i)) throw new InvalidDataException($"Duplicate skeleton node '{rig.Nodes[i].Name}'.");
        return result;
    }

    private static CharacterRig ReadRig(PackedSection visual, bool animate, CancellationToken cancellationToken)
    {
        var nodes = new List<SkeletonNode>();
        foreach (var root in visual.All("node")) AddNode(root, -1);
        if (nodes.Count == 0) throw new InvalidDataException("Missing character skeleton.");
        return new(nodes.ToArray()) { Animate = animate };
        void AddNode(PackedSection node, int parent)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var t = node.Find("transform")?.Floats ?? throw new InvalidDataException("Missing node transform.");
            if (t.Length != 12) throw new InvalidDataException("Invalid node matrix.");
            string name = node.String("identifier");
            if (string.IsNullOrEmpty(name)) throw new InvalidDataException("Missing skeleton node name.");
            int index = nodes.Count;
            nodes.Add(new(name, parent,
                new Matrix4x4(t[0], t[1], t[2], 0, t[3], t[4], t[5], 0, t[6], t[7], t[8], 0, t[9], t[10], t[11], 1)));
            foreach (var child in node.All("node")) AddNode(child, index);
        }
    }

    private static Matrix4x4[] BindGlobals(CharacterRig rig)
    {
        var matrices = new Matrix4x4[rig.Nodes.Length];
        for (int i = 0; i < matrices.Length; i++)
        {
            var node = rig.Nodes[i];
            matrices[i] = node.Parent < 0 ? node.Local : node.Local * matrices[node.Parent];
        }
        return matrices;
    }

    private static PackedSection SelectMaterial(PackedSection model, PackedSection group, IReadOnlyDictionary<string, string> tints)
    {
        var material = group.Find("material") ?? throw new InvalidDataException("Missing character material.");
        string identifier = material.String("identifier");
        foreach (var dye in model.All("dye"))
        {
            if (!dye.All("replaces").Any(replaces => replaces.Text == identifier)) continue;
            string matter = dye.String("matter");
            bool explicitTint = tints.TryGetValue(matter, out string? selected);
            string tintName = explicitTint ? selected! : "stock";
            var tint = dye.All("tint").FirstOrDefault(candidate => candidate.String("name") == tintName);
            if (explicitTint && tint == null)
                throw new InvalidDataException($"Unknown tint '{tintName}' for matter '{matter}'.");
            var replacement = tint?.Find("material");
            if (replacement == null) continue;
            // Merge named properties with the visual's defaults.
            var children = material.Children.ToList();
            foreach (var child in replacement.Children)
            {
                children.RemoveAll(old => old.Name == child.Name && (child.Name != "property" || old.Text == child.Text));
                children.Add(child);
            }
            material = material with { Children = children.ToArray() };
        }
        return material;
    }

    private static string Diffuse(PackedSection material) =>
        material.All("property").FirstOrDefault(property => property.Text == "diffuseMap")?.String("Texture") ?? "";

    private static PackedSection? Property(PackedSection material, string name) =>
        material.All("property").FirstOrDefault(property => property.Text == name);

    private static bool Boolean(PackedSection material, string name, bool fallback) =>
        Property(material, name)?.Find("Bool")?.Value is bool value ? value : fallback;

    private static float Scalar(PackedSection material, string name, float fallback)
    {
        var property = Property(material, name);
        if (property?.Find("Float")?.Value is float[] values) return values.Single();
        return property?.Find("Int") is { } integer ? Convert.ToSingle(integer.Value) : fallback;
    }

    private static Vector4 Colour(PackedSection material, string name)
    {
        var color = Property(material, name)?.Find("Vector4")?.Floats;
        if (color == null) return Vector4.One;
        if (color.Length != 4) throw new InvalidDataException($"Invalid material {name} vector.");
        return new(color[0], color[1], color[2], color[3]);
    }

    private static Vector4 BaseColour(PackedSection material) =>
        material.String("fx").Contains("/hair_", StringComparison.OrdinalIgnoreCase) ? Colour(material, "hairColour")
        : material.String("fx").Contains("/head_", StringComparison.OrdinalIgnoreCase) ? Colour(material, "skinColour") : Vector4.One;

    private static bool IsRendered(PackedSection material) =>
        !material.String("identifier").Contains("NoRender", StringComparison.OrdinalIgnoreCase)
        && !material.String("fx").Contains("NoRender", StringComparison.OrdinalIgnoreCase)
        && !(string.IsNullOrWhiteSpace(material.String("fx")) && string.IsNullOrWhiteSpace(Diffuse(material)));
}
