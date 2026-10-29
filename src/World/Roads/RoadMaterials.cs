using System.Globalization;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Roads
{
    public static class RoadMaterials
    {
        public static IReadOnlyList<RoadMaterial> Read(string path)
        {
            var result = new List<RoadMaterial>();
            if (!File.Exists(path)) return result;
            Dictionary<string, string>? fields = null;
            foreach (string line in File.ReadLines(path))
            {
                string text = line.Trim();
                if (text == "[MaterialBegin]") fields = new(StringComparer.OrdinalIgnoreCase);
                else if (text == "[MaterialEnd]" && fields != null)
                {
                    string[] rgb = fields.GetValueOrDefault("Color24", "140 140 140").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    byte Color(int i) => i < rgb.Length && byte.TryParse(rgb[i], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out byte v) ? v : (byte)140;
                    float Amount(string key, float fallback) => float.TryParse(fields.GetValueOrDefault(key),
                        NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && float.IsFinite(value)
                        ? Math.Clamp(value, 0, 1) : fallback;
                    result.Add(new RoadMaterial { Name = fields.GetValueOrDefault("Name", "-DEFAULT-"),
                        Diffuse = fields.GetValueOrDefault("Texture", ""), Normal = fields.GetValueOrDefault("NormalMap", ""),
                        Specular = fields.GetValueOrDefault("SpecularMap", ""),
                        SpecularPowerMap = fields.GetValueOrDefault("SpecPowMap", ""),
                        SpecularStrength = Amount("SpecularPower", 0), Gloss = Amount("Specular1Power", 0.5f),
                        Red = Color(0), Green = Color(1), Blue = Color(2) });
                    fields = null;
                }
                else if (fields != null)
                {
                    int split = text.IndexOf('=');
                    if (split > 0) fields[text[..split].Trim()] = text[(split + 1)..].Trim();
                }
            }
            return result;
        }

        public static RoadMaterial? Find(RoadNetwork network, string name) =>
            network.Materials.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public static string? ResolveTexture(AssetPaths assets, string texture)
        {
            if (string.IsNullOrWhiteSpace(texture)) return null;
            if (Path.IsPathRooted(texture)) return File.Exists(texture) ? Path.GetFullPath(texture) : null;
            // The shared roads directory can be populated with the original ObjectsDepot DDS files.
            string[] candidates = [texture, "textures/roads/" + texture,
                "import/objectsdepot/_roads/Textures/" + texture, "objectsdepot/_roads/Textures/" + texture];
            foreach (string candidate in candidates)
            {
                string full = AssetPathResolver.Resolve(assets.Root, candidate);
                if (File.Exists(full)) return full;
            }
            return null;
        }
    }
}
