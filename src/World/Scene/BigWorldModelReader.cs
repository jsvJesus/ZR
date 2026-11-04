using System.Numerics;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Scene;

/// <summary>Reads packed BigWorld model/visual metadata, primitive meshes and authored BSP collision.</summary>
public static class BigWorldModelReader
{
    public static SceneModel Read(AssetPaths assets, string file, CancellationToken cancellationToken = default,
        Action<string>? warning = null)
    {
        var context = new Context(assets, cancellationToken, warning);
        var source = context.Open(file);
        var meshes = new List<SceneMesh>();
        var nodes = ReadNodes(source.Visual);
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        int totalVertices = 0, totalIndices = 0;
        using var primitives = new BigWorldPrimitives(Path.ChangeExtension(source.VisualPath, ".primitives"));
        foreach (var renderSet in source.Visual.All("renderSet"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            Matrix4x4[] palette = renderSet.All("node").Select(node => nodes.TryGetValue(node.Text, out var transform)
                ? transform : throw new InvalidDataException($"Unknown BigWorld render-set node '{node.Text}'.")).ToArray();
            if (palette.Length == 0) palette = [nodes.Values.FirstOrDefault(Matrix4x4.Identity)];
            bool skinned = renderSet.Child("treatAsWorldSpaceObject")?.Flag ?? false;
            foreach (var geometry in renderSet.All("geometry"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string vertexName = Required(geometry, "vertices").Text;
                var vertices = BigWorldPrimitives.ReadVertices(context.ReadPrimitive(primitives, vertexName), palette, skinned,
                    vertexName.EndsWith("mvertices", StringComparison.Ordinal), cancellationToken, context.Warn);
                totalVertices = checked(totalVertices + vertices.Length);
                if (totalVertices > 2_000_000) throw new InvalidDataException("BigWorld model exceeds the vertex limit.");
                var indices = BigWorldPrimitives.ReadIndices(context.ReadPrimitive(primitives, Required(geometry, "primitive").Text),
                    vertices.Length, cancellationToken);
                foreach (var group in geometry.All("primitiveGroup"))
                {
                    int number = group.Integer;
                    if ((uint)number >= (uint)indices.Groups.Length) throw new InvalidDataException("Invalid BigWorld material group index.");
                    var range = indices.Groups[number];
                    if (range.TriangleCount == 0) continue;
                    int count = checked(range.TriangleCount * 3);
                    totalIndices = checked(totalIndices + count);
                    if (totalIndices > 12_000_000 || meshes.Count >= 16384) throw new InvalidDataException("BigWorld model exceeds the mesh limit.");
                    var material = context.Material(Required(group, "material"), source.DefaultMaterials);
                    meshes.Add(new(vertices, indices.Indices.AsSpan(range.StartIndex, count).ToArray(), material));
                }
                foreach (var vertex in vertices)
                {
                    minimum = Vector3.Min(minimum, vertex.Position);
                    maximum = Vector3.Max(maximum, vertex.Position);
                }
                if (geometry.All("stream").Any())
                    context.Warn("BigWorld secondary UV/vertex-colour streams are not represented by the editor's standard material.");
            }
        }
        if (meshes.Count == 0) throw new InvalidDataException("BigWorld visual has no supported triangle meshes.");
        if (source.Model.Child("nodefullVisual") != null || source.Model.All("animation").Any())
            context.Warn("BigWorld animated model is displayed in its authored static pose; animation playback is unavailable.");
        return new(SourceModelReader.Normalize(file), new(minimum, maximum), meshes.ToArray());
    }

    public static SceneBounds ReadBounds(AssetPaths assets, string file, CancellationToken cancellationToken = default)
    {
        var source = new Context(assets, cancellationToken, null).Open(file);
        var bounds = source.Visual.Child("boundingBox");
        if (bounds == null) return Read(assets, file, cancellationToken).Bounds;
        Vector3 minimum = Vector(Required(bounds, "min")), maximum = Vector(Required(bounds, "max"));
        if (minimum.X > maximum.X || minimum.Y > maximum.Y || minimum.Z > maximum.Z)
            throw new InvalidDataException("Invalid BigWorld visual bounds.");
        return new(minimum, maximum);
    }

    public static ScenePhysicsMesh? ReadPhysics(AssetPaths assets, string file,
        CancellationToken cancellationToken = default, Action<string>? warning = null)
    {
        var context = new Context(assets, cancellationToken, warning);
        var source = context.Open(file);
        string customVisual = Path.ChangeExtension(source.VisualPath, null) + "_bsp.visual";
        if (File.Exists(customVisual))
        {
            context.Warn("The authored _bsp.visual collision proxy is unsupported; collision is disabled for this model.");
            return null;
        }
        using var primitives = new BigWorldPrimitives(Path.ChangeExtension(source.VisualPath, ".primitives"));
        bool remap = primitives.Contains("bsp2");
        byte[] bytes;
        if (remap || primitives.Contains("bsp")) bytes = primitives.Read(remap ? "bsp2" : "bsp", cancellationToken);
        else
        {
            string bsp = Path.ChangeExtension(source.VisualPath, ".bsp");
            if (!File.Exists(bsp))
            {
                context.Warn("No authored BSP collision proxy exists; collision is disabled for this model.");
                return null;
            }
            using var stream = File.OpenRead(bsp);
            if (stream.Length > 512 * 1024 * 1024) throw new InvalidDataException("BigWorld BSP exceeds the size limit.");
            bytes = new byte[checked((int)stream.Length)];
            stream.ReadExactly(bytes);
        }
        var materialFlags = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var set in source.Visual.All("renderSet"))
            foreach (var geometry in set.All("geometry"))
                foreach (var group in geometry.All("primitiveGroup"))
                {
                    var material = Required(group, "material");
                    string name = material.Child("identifier")?.Text ?? "";
                    var fields = context.MaterialFields(material, source.DefaultMaterials);
                    materialFlags[name] = fields.GetValueOrDefault("collisionFlags")?.Integer ?? 0;
                }
        int[] flags = [0];
        if (remap)
        {
            if (!primitives.Contains("bsp2_materials"))
            {
                context.Warn("BSP2 material mapping is missing; collision is disabled because collision flags cannot be resolved.");
                return null;
            }
            var mapping = BigWorldSection.Read(primitives.Read("bsp2_materials", cancellationToken), cancellationToken);
            flags = mapping.All("id").Select(id => materialFlags.GetValueOrDefault(id.Text)).ToArray();
            if (flags.Length == 0) flags = [0];
        }
        var collision = BigWorldBsp.Read(SourceModelReader.Normalize(file), bytes, remap ? flags : null, cancellationToken);
        if (collision == null) context.Warn("The authored BSP contains no player-collidable triangles.");
        return collision;
    }

    private static Dictionary<string, Matrix4x4> ReadNodes(BigWorldSection visual)
    {
        var nodes = new Dictionary<string, Matrix4x4>(StringComparer.Ordinal);
        foreach (var root in visual.All("node")) Visit(root, Matrix4x4.Identity, 0);
        return nodes;
        void Visit(BigWorldSection node, Matrix4x4 parent, int depth)
        {
            if (depth > 128) throw new InvalidDataException("BigWorld node hierarchy is too deep.");
            var section = node.Child("transform");
            Matrix4x4 local = Matrix4x4.Identity;
            if (section != null)
            {
                float[] values = section.Children.Length == 0 ? section.Floats()
                    : Enumerable.Range(0, 4).SelectMany(index => Required(section, "row" + index).Floats()).ToArray();
                if (values.Length != 12) throw new InvalidDataException("Invalid BigWorld matrix.");
                local = new(values[0], values[1], values[2], 0, values[3], values[4], values[5], 0,
                    values[6], values[7], values[8], 0, values[9], values[10], values[11], 1);
            }
            Matrix4x4 world = local * parent;
            if (!float.IsFinite(world.GetDeterminant()) || !Matrix4x4.Invert(world, out _))
                throw new InvalidDataException("Invalid BigWorld node transform.");
            string name = Required(node, "identifier").Text;
            if (!nodes.TryAdd(name, world)) throw new InvalidDataException($"Duplicate BigWorld node '{name}'.");
            foreach (var child in node.All("node")) Visit(child, world, depth + 1);
        }
    }

    private static BigWorldSection Required(BigWorldSection parent, string name) => parent.Child(name)
        ?? throw new InvalidDataException($"Missing BigWorld {parent.Name}/{name}.");
    private static Vector3 Vector(BigWorldSection section)
    {
        float[] values = section.Floats();
        return values.Length == 3 ? new(values[0], values[1], values[2]) : throw new InvalidDataException("Invalid BigWorld Vector3.");
    }

    private sealed record Source(BigWorldSection Model, BigWorldSection Visual, string VisualPath,
        Dictionary<string, BigWorldSection> DefaultMaterials);

    private sealed class Context(AssetPaths assets, CancellationToken cancellationToken, Action<string>? warning)
    {
        private readonly HashSet<string> _warnings = new(StringComparer.Ordinal);
        private string _file = "";
        public void Warn(string message)
        {
            if (_warnings.Count < 32 && _warnings.Add(message)) warning?.Invoke($"{_file}: {message}");
        }
        public Source Open(string file)
        {
            _file = file;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var defaults = new Dictionary<string, BigWorldSection>(StringComparer.Ordinal);
            string path = SourceModelReader.Resolve(assets, file);
            for (int depth = 0; depth < 32; depth++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!visited.Add(path)) throw new InvalidDataException("Cyclic BigWorld model parent references.");
                var model = BigWorldSection.Read(path, cancellationToken);
                foreach (var dye in model.All("dye"))
                {
                    string replaces = dye.Child("replaces")?.Text ?? "";
                    var tint = dye.All("tint").FirstOrDefault(tint => tint.Child("name")?.Text == "Default");
                    if (tint?.Child("material") is { } material && replaces.Length > 0) defaults.TryAdd(replaces, material);
                }
                string visual = model.Child("nodefullVisual")?.Text ?? model.Child("nodelessVisual")?.Text ?? "";
                if (visual.Length > 0)
                {
                    string visualPath = Resolve(WithExtension(visual, ".visual"));
                    return new(model, BigWorldSection.Read(visualPath, cancellationToken), visualPath, defaults);
                }
                string parent = model.Child("parent")?.Text ?? "";
                if (parent.Length == 0) throw new InvalidDataException("BigWorld model has no visual or parent reference.");
                path = Resolve(WithExtension(parent, ".model"));
            }
            throw new InvalidDataException("BigWorld model parent chain is too long.");
        }

        public byte[] ReadPrimitive(BigWorldPrimitives local, string name)
        {
            cancellationToken.ThrowIfCancellationRequested();
            name = name.Replace('\\', '/');
            if (!name.Contains('/')) return local.Read(name, cancellationToken);
            int split = name.IndexOf(".primitives/", StringComparison.OrdinalIgnoreCase);
            if (split < 0) throw new InvalidDataException($"Unsupported external BigWorld primitive reference '{name}'.");
            string path = Resolve(name[..(split + 11)]);
            using var external = new BigWorldPrimitives(path);
            return external.Read(name[(split + 12)..], cancellationToken);
        }

        public Dictionary<string, BigWorldSection> MaterialFields(BigWorldSection material,
            Dictionary<string, BigWorldSection> overrides)
        {
            var fields = new Dictionary<string, BigWorldSection>(StringComparer.OrdinalIgnoreCase);
            string name = material.Child("identifier")?.Text ?? "";
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Merge(material, 0);
            if (overrides.TryGetValue(name, out var replacement)) Merge(replacement, 0);
            return fields;
            void Merge(BigWorldSection value, int depth)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (depth > 16) throw new InvalidDataException("BigWorld material inheritance is too deep.");
                string reference = value.Child("mfm")?.Text ?? "";
                if (reference.Length > 0)
                {
                    string path = Resolve(reference);
                    if (!visited.Add(path)) throw new InvalidDataException("Cyclic BigWorld material references.");
                    if (File.Exists(path)) Merge(BigWorldSection.Read(path, cancellationToken), depth + 1);
                    else Warn($"Missing material dependency '{reference}'.");
                    visited.Remove(path);
                }
                foreach (var field in value.Children)
                    if (field.Name == "property")
                    {
                        if (field.Children.Length > 0) fields[field.Text] = field.Children[0];
                    }
                    else fields[field.Name] = field;
            }
        }

        public SceneMaterial Material(BigWorldSection material, Dictionary<string, BigWorldSection> overrides)
        {
            string name = material.Child("identifier")?.Text ?? "BigWorld material";
            var fields = MaterialFields(material, overrides);
            string shader = fields.GetValueOrDefault("fx")?.Text ?? "";
            string[] shaderParts = Path.GetFileNameWithoutExtension(shader).Split('_');
            bool transparent = Bool("alphaBlendEnable") || Bool("blendEnable")
                || shader.Contains("_add", StringComparison.OrdinalIgnoreCase) || shader.Contains("_blend", StringComparison.OrdinalIgnoreCase)
                || shader.Contains("transparent", StringComparison.OrdinalIgnoreCase)
                || shaderParts.Any(part => part.Equals("alpha", StringComparison.OrdinalIgnoreCase))
                || shader.EndsWith("water_surface.fx", StringComparison.OrdinalIgnoreCase);
            bool alphaTest = Bool("alphaTestEnable") || shader.Contains("alphatest", StringComparison.OrdinalIgnoreCase);
            var color = Vector4.One;
            var colorField = fields.GetValueOrDefault("diffuseColour") ?? fields.GetValueOrDefault("diffuseColor");
            if (colorField != null)
            {
                float[] values = colorField.Floats();
                if (values.Length is not (3 or 4)) throw new InvalidDataException("Invalid BigWorld material colour.");
                color = new(values[0], values[1], values[2], values.Length == 4 ? values[3] : 1);
                color = Vector4.Clamp(color, Vector4.Zero, Vector4.One);
            }
            if (fields.TryGetValue("opacity", out var opacity))
            {
                float[] values = opacity.Floats();
                if (values.Length != 1) throw new InvalidDataException("Invalid BigWorld opacity.");
                color.W *= Math.Clamp(values[0], 0, 1);
                transparent |= color.W < 1;
            }
            string diffuse = Texture("diffuseMap"), normal = Texture("normalMap");
            if (shader.Contains("uvtransform", StringComparison.OrdinalIgnoreCase) || shader.Contains("chrome", StringComparison.OrdinalIgnoreCase)
                || shader.Contains("glow", StringComparison.OrdinalIgnoreCase) || shader.Contains("wave", StringComparison.OrdinalIgnoreCase)
                || shader.Contains("pixelup", StringComparison.OrdinalIgnoreCase) || shader.Contains("colourise", StringComparison.OrdinalIgnoreCase)
                || shader.Contains("distort", StringComparison.OrdinalIgnoreCase) || shader.Contains("water", StringComparison.OrdinalIgnoreCase)
                || shader.Contains("particle", StringComparison.OrdinalIgnoreCase) || shader.Contains("spin", StringComparison.OrdinalIgnoreCase)
                || fields.ContainsKey("reflectionMap") || fields.ContainsKey("DiffMap1") || fields.ContainsKey("Lightmap")
                || fields.ContainsKey("glowMap") || fields.ContainsKey("selfIllumination"))
                Warn("Source animation, reflection, lightmap, glow and layered shader effects use the editor's standard material approximation.");
            float specular = fields.ContainsKey("specularMap") || shader.Contains("spec", StringComparison.OrdinalIgnoreCase) ? 0.5f : 0;
            if (fields.TryGetValue("materialSpecular", out var specularField))
            {
                float[] values = specularField.Floats();
                // BigWorld stores this property as an RGB specular colour (usually Vector4),
                // while the editor's standard material has a single strength. Ignore alpha.
                float strength = values.Length switch
                {
                    1 => values[0],
                    3 or 4 => Vector3.Dot(
                        Vector3.Clamp(new(values[0], values[1], values[2]), Vector3.Zero, Vector3.One),
                        new(0.2126f, 0.7152f, 0.0722f)),
                    _ => throw new InvalidDataException("Invalid BigWorld material specular colour or strength.")
                };
                specular = Math.Clamp(strength, 0, 1);
            }
            return new(name, diffuse, normal, color, Bool("doubleSided"), alphaTest, transparent, specular);
            bool Bool(string key) => fields.TryGetValue(key, out var value) && value.Flag;
            string Texture(string key)
            {
                if (!fields.TryGetValue(key, out var field) || string.IsNullOrWhiteSpace(field.Text)) return "";
                string reference = field.Text.Trim();
                string path = Resolve(reference);
                // Exported authoring references may retain .tga/.bmp although the shipped asset is DDS.
                string dds = Path.ChangeExtension(path, ".dds");
                if (File.Exists(dds)) return dds;
                if (File.Exists(path)) return path;
                Warn($"Missing texture '{reference}'.");
                return "";
            }
        }

        private string Resolve(string reference)
        {
            reference = reference.Trim().Replace('\\', '/');
            if (reference.StartsWith("packs/", StringComparison.OrdinalIgnoreCase)) reference = reference[6..];
            // This alias belongs only to BigWorld resources. Existing models/*.sco stay untouched.
            if (reference.StartsWith("models/", StringComparison.OrdinalIgnoreCase)) reference = "objects/" + reference[7..];
            if (reference.Split('/').Any(part => part is ".." or "." || part.Length == 0) || reference.Contains(':'))
                throw new InvalidDataException("Invalid BigWorld dependency path.");
            return AssetPathResolver.Resolve(assets.Root, reference);
        }
        private static string WithExtension(string reference, string extension) => reference.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
            ? reference : reference + extension;
    }
}
