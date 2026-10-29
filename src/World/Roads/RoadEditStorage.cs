using ZoneReborn.World.Levels.Storage;

namespace ZoneReborn.World.Roads
{
    public static class RoadEditStorage
    {
        public const string FileName = "road-segments.json";

        public static async Task LoadAsync(string directory, RoadNetwork network, CancellationToken cancellationToken = default)
        {
            string path = Path.Combine(directory, FileName);
            if (!File.Exists(path)) return;
            try
            {
                if (new FileInfo(path).Length > 64 * 1024 * 1024) throw new InvalidDataException("Слишком большой файл правок дорог.");
                var edits = await LevelJson.ReadAsync<RoadEdits>(path, cancellationToken);
                if (edits.Format != "ZoneReborn.RoadSegments" || edits.Version != 1 || edits.Roads == null)
                    throw new InvalidDataException("Неподдерживаемый формат правок дорог.");
                var byId = network.Roads.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
                var pending = new List<(SourceRoad Road, RoadEdit Edit)>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var edit in edits.Roads)
                {
                    if (edit == null || string.IsNullOrWhiteSpace(edit.Id) || !seen.Add(edit.Id) ||
                        !byId.TryGetValue(edit.Id, out var road) || edit.Fingerprint != road.Fingerprint ||
                        edit.Nodes == null || edit.Nodes.Length != road.Nodes.Length || edit.Segments == null ||
                        edit.Segments.Length != road.Segments.Length)
                        throw new InvalidDataException("Правки не соответствуют исходным DAT. Файл сохранён без изменений.");
                    foreach (var node in edit.Nodes) ValidateNode(node);
                    foreach (var segment in edit.Segments)
                    {
                        if (segment == null || string.IsNullOrWhiteSpace(segment.Material) ||
                            segment.DiffuseOverride == null || segment.NormalOverride == null ||
                            segment.SpecularOverride == null || segment.SpecularPowerOverride == null ||
                            !float.IsFinite(segment.UvScale) || segment.UvScale is < 0.01f or > 1000)
                            throw new InvalidDataException("Некорректный материал или масштаб UV сегмента.");
                        ValidateTexturePath(directory, segment.DiffuseOverride);
                        ValidateTexturePath(directory, segment.NormalOverride);
                        ValidateTexturePath(directory, segment.SpecularOverride);
                        ValidateTexturePath(directory, segment.SpecularPowerOverride);
                    }
                    pending.Add((road, edit));
                }
                // Validate the complete document before applying any changes.
                foreach (var (road, edit) in pending)
                {
                    bool geometry = false;
                    for (int i = 0; i < road.Nodes.Length; i++)
                    {
                        geometry |= !SameNode(road.OriginalNodes[i], edit.Nodes[i]);
                        road.Nodes[i] = edit.Nodes[i].Copy();
                    }
                    for (int i = 0; i < road.Segments.Length; i++)
                    {
                        var target = road.Segments[i]; var source = edit.Segments[i];
                        target.Material = source.Material; target.DiffuseOverride = source.DiffuseOverride;
                        target.NormalOverride = source.NormalOverride; target.UvScale = source.UvScale;
                        target.SpecularOverride = source.SpecularOverride;
                        target.SpecularPowerOverride = source.SpecularPowerOverride;
                        target.FollowTerrain = source.FollowTerrain;
                    }
                    road.Changed(geometry);
                }
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or System.Text.Json.JsonException)
            {
                network.CanSave = false;
                network.Warnings.Add($"{FileName}: {exception.Message} Сохранение отключено, чтобы не потерять правки.");
            }
        }

        public static async Task SaveAsync(string directory, RoadNetwork network)
        {
            if (!network.CanSave) throw new InvalidOperationException("Не удалось прочитать предыдущие правки дорог.");
            var edits = new RoadEdits { Roads = network.Roads.Where(r => r.Revision > 0).Select(r => new RoadEdit
            {
                Id = r.Id, Fingerprint = r.Fingerprint, Nodes = r.Nodes.Select(n => n.Copy()).ToArray(),
                Segments = r.Segments.Select(s => new SegmentEdit { Material = s.Material,
                    DiffuseOverride = s.DiffuseOverride, NormalOverride = s.NormalOverride,
                    SpecularOverride = s.SpecularOverride, SpecularPowerOverride = s.SpecularPowerOverride,
                    UvScale = s.UvScale, FollowTerrain = s.FollowTerrain }).ToArray()
            }).ToList() };
            string temporary = Path.Combine(directory, $".road-segments-{Guid.NewGuid():N}.tmp");
            try
            {
                await LevelJson.WriteAsync(temporary, edits);
                File.Move(temporary, Path.Combine(directory, FileName), true);
                network.IsDirty = false;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        public static void ValidateNode(RoadNode node)
        {
            if (node == null || !float.IsFinite(node.X) || !float.IsFinite(node.Y) || !float.IsFinite(node.Z) ||
                !float.IsFinite(node.TangentX) || !float.IsFinite(node.TangentZ) || !float.IsFinite(node.Width) ||
                MathF.Abs(node.X) > 1e7f || MathF.Abs(node.Y) > 1e7f || MathF.Abs(node.Z) > 1e7f ||
                MathF.Abs(node.TangentX) > 1e6f || MathF.Abs(node.TangentZ) > 1e6f || node.Width is < 0.05f or > 1000)
                throw new InvalidDataException("Некорректные координаты, касательные или ширина узла.");
        }

        private static bool SameNode(RoadNode a, RoadNode b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z
            && a.TangentX == b.TangentX && a.TangentZ == b.TangentZ && a.Width == b.Width;

        private static void ValidateTexturePath(string directory, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (!Path.GetExtension(path).Equals(".dds", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Правка текстуры должна ссылаться на DDS.");
            if (!Path.IsPathRooted(path)) _ = Assets.AssetPathResolver.Resolve(directory, path);
        }

        public sealed class RoadEdits
        {
            public string Format { get; init; } = "ZoneReborn.RoadSegments";
            public int Version { get; init; } = 1;
            public List<RoadEdit> Roads { get; init; } = [];
        }
        public sealed class RoadEdit
        {
            public string Id { get; init; } = "";
            public string Fingerprint { get; init; } = "";
            public RoadNode[] Nodes { get; init; } = [];
            public SegmentEdit[] Segments { get; init; } = [];
        }
        public sealed class SegmentEdit
        {
            public string Material { get; init; } = "";
            public string DiffuseOverride { get; init; } = "";
            public string NormalOverride { get; init; } = "";
            public string SpecularOverride { get; init; } = "";
            public string SpecularPowerOverride { get; init; } = "";
            public float UvScale { get; init; } = 1;
            public bool FollowTerrain { get; init; } = true;
        }
    }
}
