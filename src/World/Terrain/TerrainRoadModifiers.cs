using System.Numerics;
using ZoneReborn.World.Roads;
using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Terrain.Storage;

namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainDocument
{
    private const float ModifierBucketSize = 16;
    private const int MaximumModifierSegments = 262144;
    private const int MaximumModifierLinks = 1_000_000;
    private const int MaximumModifierTiles = 16384;
    private RoadNetwork? _roadNetwork;
    private TerrainSurface? _roadSource;
    private bool _roadModifiersPending;
    private readonly HashSet<RoadDefinition> _roadProfilesInvalid = [];
    private Dictionary<RoadDefinition, RoadModifierProfile> _roadProfiles = [];
    private Dictionary<(int X, int Z), List<RoadModifierSegment>> _roadBuckets = [];
    private readonly Dictionary<(int X, int Z), long> _terrainTileVersions = [];
    private readonly Dictionary<(int X, int Z), TerrainTileData> _effectiveTiles = [];
    private readonly HashSet<(int X, int Z)> _roadAffectedTiles = [];
    private readonly List<string> _roadModifierWarnings = [];
    private HashSet<RoadDefinition> _activeRoadModifiers = [];
    private TerrainOverviewData? _effectiveOverview;
    private long _terrainTileVersion;

    public IReadOnlyList<string> RoadModifierWarnings
    {
        get { EnsureRoadModifiers(); return _roadModifierWarnings; }
    }

    /// <summary>Attach after the level's roads and TerrainDocument have both been loaded.</summary>
    public void AttachRoadNetwork(RoadNetwork network, TerrainSurface surface)
    {
        ArgumentNullException.ThrowIfNull(network);
        ArgumentNullException.ThrowIfNull(surface);
        if (_roadNetwork != null) _roadNetwork.Modified -= OnRoadNetworkModified;
        _roadNetwork = network;
        _roadSource = surface;
        network.Modified += OnRoadNetworkModified;
        foreach (var profile in _roadProfiles.Values)
            foreach (var tile in profile.Tiles) InvalidateEffectiveTile(tile.X, tile.Z);
        _roadProfiles.Clear();
        InvalidateRoadModifiers();
        EnsureRoadModifiers();
    }

    private void OnRoadNetworkModified() => InvalidateRoadModifiers();

    /// <summary>Call for previews too: a RoadDefinition revision alone does not emit network.Modified.</summary>
    public void InvalidateRoadModifiers(RoadDefinition? road = null)
    {
        if (road != null) _roadProfilesInvalid.Add(road);
        _roadModifiersPending = true;
        _effectiveOverview = null;
        // Derived revisions refresh rendering/collision without making BaseTerrain dirty for saving.
        HeightRevision++;
    }

    public long TerrainTileRevision(int x, int z)
    {
        EnsureRoadModifiers();
        return _terrainTileVersions.GetValueOrDefault((x, z));
    }

    /// <summary>Changes anywhere in this road's source/support corridor invalidate its mesh.</summary>
    public long RoadTerrainRevision(RoadDefinition road)
    {
        EnsureRoadModifiers();
        if (!_roadProfiles.TryGetValue(road, out var profile) || profile.Tiles.Count == 0) return HeightRevision;
        long revision = 0;
        foreach (var tile in profile.Tiles) revision = Math.Max(revision, _terrainTileVersions.GetValueOrDefault(tile));
        return revision;
    }

    public bool IsRoadTerrainSupported(RoadDefinition road, int sectionIndex)
    {
        EnsureRoadModifiers();
        if (!road.FollowTerrain || !road.TerrainModifierEnabled || road.PreserveSourceElevation) return true;
        return _activeRoadModifiers.Contains(road) && _roadProfiles.TryGetValue(road, out var profile)
            && !profile.UnsupportedSections.Contains(sectionIndex)
            && profile.Segments.Any(segment => segment.SectionIndex == sectionIndex);
    }

    private void InvalidateEffectiveTile(int x, int z)
    {
        long version = ++_terrainTileVersion;
        // Normal derivatives and shared mesh-border vertices are owned by neighboring tiles.
        for (int tz = Math.Max(0, z - 1); tz <= Math.Min(Descriptor.TilesZ - 1, z + 1); tz++)
        for (int tx = Math.Max(0, x - 1); tx <= Math.Min(Descriptor.TilesX - 1, x + 1); tx++)
        {
            _terrainTileVersions[(tx, tz)] = version;
            _effectiveTiles.Remove((tx, tz));
            _roadAffectedTiles.Add((tx, tz));
        }
        _effectiveOverview = null;
    }

    private void InvalidateRoadModifiersForTerrain(List<(int X, int Z, ushort Value)> values)
    {
        if (_roadNetwork == null || values.Count == 0) return;
        var d = Descriptor;
        float padding = Math.Max(1, d.MeshStep) * d.CellSize;
        var bounds = new Vector4(
            d.OriginX + values.Min(v => v.X) * d.CellSize - padding,
            d.OriginZ + values.Min(v => v.Z) * d.CellSize - padding,
            d.OriginX + values.Max(v => v.X) * d.CellSize + padding,
            d.OriginZ + values.Max(v => v.Z) * d.CellSize + padding);
        foreach (var pair in _roadProfiles)
            if (Overlaps(pair.Value.Bounds, bounds)) _roadProfilesInvalid.Add(pair.Key);
        // A road rejected for excessive cut/fill must be reconsidered after terrain sculpting as well.
        _roadModifiersPending = true;
    }

    private void EnsureRoadModifiers(CancellationToken cancellationToken = default)
    {
        if (!_roadModifiersPending || _roadNetwork == null || _roadSource == null) return;
        cancellationToken.ThrowIfCancellationRequested();
        var current = _roadNetwork.Roads.ToHashSet();
        var replacements = new Dictionary<RoadDefinition, RoadModifierProfile>();
        var changedTiles = new HashSet<(int X, int Z)>();
        int retainedSegments = 0;
        var retainedTiles = new HashSet<(int X, int Z)>();
        foreach (var pair in _roadProfiles)
            if (!current.Contains(pair.Key)) changedTiles.UnionWith(pair.Value.Tiles);
        foreach (var road in _roadNetwork.Roads)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RoadModifierProfile profile;
            if (_roadProfiles.TryGetValue(road, out var old) && old.Revision == road.Revision && !_roadProfilesInvalid.Contains(road)
                && retainedSegments + old.Segments.Length <= MaximumModifierSegments)
            {
                profile = old;
            }
            else
            {
                if (old != null) changedTiles.UnionWith(old.Tiles);
                profile = BuildRoadModifier(road, cancellationToken, MaximumModifierSegments - retainedSegments);
                changedTiles.UnionWith(profile.Tiles);
            }
            if (retainedTiles.Count + profile.Tiles.Count(t => !retainedTiles.Contains(t)) > MaximumModifierTiles)
            {
                changedTiles.UnionWith(profile.Tiles);
                profile = new(-1, [], [], profile.Bounds, $"{road.Name}: превышен бюджет тайлов дорожных модификаторов.");
            }
            replacements.Add(road, profile);
            retainedSegments += profile.Segments.Length;
            retainedTiles.UnionWith(profile.Tiles);
        }
        var buckets = new Dictionary<(int X, int Z), List<RoadModifierSegment>>();
        var warnings = new List<string>();
        int segments = 0, links = 0;
        var active = new HashSet<RoadDefinition>();
        // Deterministic priority is resolved at sampling time, independently of list insertion order.
        foreach (var pair in replacements.OrderBy(p => p.Key.Id, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var profile = pair.Value;
            if (profile.Warning != null) warnings.Add(profile.Warning);
            long requiredLinks = 0;
            foreach (var segment in profile.Segments)
            {
                var range = BucketRange(segment.Bounds);
                requiredLinks += (long)(range.X1 - range.X0 + 1) * (range.Z1 - range.Z0 + 1);
            }
            if (segments + profile.Segments.Length > MaximumModifierSegments || links + requiredLinks > MaximumModifierLinks)
            {
                warnings.Add($"{pair.Key.Name}: превышен бюджет модификаторов Terrain; разделите дорожную сеть.");
                // Retain bounds for invalidation, but never apply a partial, order-dependent corridor.
                continue;
            }
            segments += profile.Segments.Length;
            links += (int)requiredLinks;
            if (profile.Segments.Length > 0) active.Add(pair.Key);
            foreach (var segment in profile.Segments)
            {
                var range = BucketRange(segment.Bounds);
                for (int z = range.Z0; z <= range.Z1; z++)
                for (int x = range.X0; x <= range.X1; x++)
                {
                    if (!buckets.TryGetValue((x, z), out var list)) buckets.Add((x, z), list = []);
                    list.Add(segment);
                }
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var road in active.Except(_activeRoadModifiers).Concat(_activeRoadModifiers.Except(active)))
        {
            if (_roadProfiles.TryGetValue(road, out var old)) changedTiles.UnionWith(old.Tiles);
            if (replacements.TryGetValue(road, out var next)) changedTiles.UnionWith(next.Tiles);
        }
        _activeRoadModifiers = active;
        _roadProfiles = replacements;
        _roadBuckets = buckets;
        _roadModifierWarnings.Clear();
        _roadModifierWarnings.AddRange(warnings);
        _roadProfilesInvalid.Clear();
        _roadModifiersPending = false;
        foreach (var tile in changedTiles) InvalidateEffectiveTile(tile.X, tile.Z);
    }

    private RoadModifierProfile BuildRoadModifier(RoadDefinition road, CancellationToken token, int remainingSegments)
    {
        var tiles = new HashSet<(int X, int Z)>();
        // Store source bounds even for rejected/draft roads so a later terrain edit can retry them.
        Vector4 bounds = road.Nodes.Count > 0
            ? new(road.Nodes.Min(n => n.X), road.Nodes.Min(n => n.Z), road.Nodes.Max(n => n.X), road.Nodes.Max(n => n.Z))
            : Vector4.Zero;
        if (road.Nodes.Count < 2)
            return new(road.Revision, [], tiles, bounds, null);
        try
        {
            RoadStorage.Validate(road);
            bool applyModifier = road.FollowTerrain && road.TerrainModifierEnabled && !road.PreserveSourceElevation;
            if (remainingSegments <= 0 && applyModifier)
                return new(-1, [], [], bounds, $"{road.Name}: превышен бюджет дорожных модификаторов.");
            float edge = road.Width * 0.5f + road.ShoulderWidth;
            float guard = Descriptor.MeshStep * Descriptor.CellSize * 1.5f;
            float inner = edge + guard, outer = inner + road.TerrainTransitionWidth;
            bounds += new Vector4(-outer, -outer, outer, outer);
            var samples = RoadSpline.Sample(road, _roadSource, token);
            var segments = new List<RoadModifierSegment>();
            var unsupported = new HashSet<int>();
            string? warning = null;
            for (int i = 1; i < samples.Count; i++)
            {
                if ((i & 63) == 0) token.ThrowIfCancellationRequested();
                var a = samples[i - 1].Position; var b = samples[i].Position;
                if (Vector2.DistanceSquared(new(a.X, a.Z), new(b.X, b.Z)) < 1e-8f) continue;
                var segmentBounds = new Vector4(MathF.Min(a.X, b.X) - outer, MathF.Min(a.Z, b.Z) - outer,
                    MathF.Max(a.X, b.X) + outer, MathF.Max(a.Z, b.Z) + outer);
                bounds = new(MathF.Min(bounds.X, segmentBounds.X), MathF.Min(bounds.Y, segmentBounds.Y),
                    MathF.Max(bounds.Z, segmentBounds.Z), MathF.Max(bounds.W, segmentBounds.W));
                if (!ClipToTerrain(ref segmentBounds)) continue;
                AddTiles(segmentBounds, tiles);
                // Retain a source/support footprint for manual profiles and disabled modifiers too.
                if (!applyModifier) continue;
                bool supported = true;
                Vector3 direction = Vector3.Normalize(new Vector3(b.X - a.X, 0, b.Z - a.Z));
                Vector3 right = new(direction.Z, 0, -direction.X);
                int probes = Math.Max(2, (int)MathF.Ceiling(edge * 2 / MathF.Max(0.5f, Descriptor.CellSize)));
                // Validate the complete width before activating this interval. Huge fills/cuts are never clamped into existence.
                for (int end = 0; end <= 2 && supported; end++)
                for (int p = 0; p <= probes; p++)
                {
                    float lateral = -edge + edge * 2 * p / probes;
                    Vector3 center = Vector3.Lerp(a, b, end * 0.5f);
                    Vector3 probe = center + right * lateral;
                    if (!_roadSource!.TryGetBaseHeight(probe.X, probe.Z, out float ground))
                    {
                        supported = false;
                        warning = $"{road.Name}: под полотном отсутствует Terrain; требуется мост или изменение маршрута.";
                        break;
                    }
                    float change = center.Y + RoadSpline.CrossSectionHeight(road, lateral) - road.TerrainBaseDepth - ground;
                    if (change > road.TerrainFillLimit || -change > road.TerrainCutLimit)
                    {
                        supported = false;
                        warning = $"{road.Name}: превышены допустимые выемка/насыпь ({road.TerrainCutLimit:F1}/{road.TerrainFillLimit:F1} м); требуется конструкция или изменение профиля.";
                        break;
                    }
                }
                if (supported)
                {
                    if (segments.Count >= remainingSegments)
                        return new(-1, [], [], bounds, $"{road.Name}: превышен бюджет дорожных модификаторов.");
                    segments.Add(new(road, samples[i].SegmentIndex, a, b, edge, inner, outer, segmentBounds));
                }
                else unsupported.Add(samples[i].SegmentIndex);
            }
            // A rejected spline section must not retain isolated patches of an otherwise missing corridor.
            segments.RemoveAll(segment => unsupported.Contains(segment.SectionIndex));
            return new(road.Revision, segments.ToArray(), tiles, bounds, warning) { UnsupportedSections = unsupported };
        }
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or IOException)
        {
            return new(road.Revision, [], tiles, bounds, $"{road.Name}: {exception.Message}");
        }
    }

    private float EffectiveHeight(int gx, int gz, float baseHeight)
    {
        var d = Descriptor;
        float x = d.OriginX + gx * d.CellSize, z = d.OriginZ + gz * d.CellSize;
        if (!_roadBuckets.TryGetValue(Bucket(x, z), out var segments)) return baseHeight;
        RoadModifierSegment? best = null;
        float bestDistance = float.PositiveInfinity, bestT = 0, bestWeight = 0;
        foreach (var segment in segments)
        {
            if (x < segment.Bounds.X || x > segment.Bounds.Z || z < segment.Bounds.Y || z > segment.Bounds.W) continue;
            var a = new Vector2(segment.A.X, segment.A.Z); var delta = new Vector2(segment.B.X - segment.A.X, segment.B.Z - segment.A.Z);
            float t = Math.Clamp(Vector2.Dot(new Vector2(x, z) - a, delta) / delta.LengthSquared(), 0, 1);
            float distance = Vector2.Distance(new Vector2(x, z), a + delta * t);
            if (distance >= segment.Outer) continue;
            float blend = Math.Clamp((segment.Outer - distance) / MathF.Max(0.01f, segment.Outer - segment.Inner), 0, 1);
            float weight = blend * blend * (3 - 2 * blend);
            if (best != null)
            {
                int priority = Priority(segment.Road.Type).CompareTo(Priority(best.Road.Type));
                if (priority < 0 || priority == 0 && (distance > bestDistance || distance == bestDistance &&
                    StringComparer.Ordinal.Compare(segment.Road.Id, best.Road.Id) >= 0)) continue;
            }
            float desired = segment.A.Y + (segment.B.Y - segment.A.Y) * t
                + RoadSpline.CrossSectionHeight(segment.Road, MathF.Min(distance, segment.Edge)) - segment.Road.TerrainBaseDepth;
            float deltaHeight = (desired - baseHeight) * weight;
            if (deltaHeight > segment.Road.TerrainFillLimit || -deltaHeight > segment.Road.TerrainCutLimit) continue;
            best = segment; bestDistance = distance; bestT = t; bestWeight = weight;
        }
        if (best == null) return baseHeight;
        float target = best.A.Y + (best.B.Y - best.A.Y) * bestT
            + RoadSpline.CrossSectionHeight(best.Road, MathF.Min(bestDistance, best.Edge)) - best.Road.TerrainBaseDepth;
        return d.DecodeHeight(EncodeEffective(baseHeight + (target - baseHeight) * bestWeight));
    }

    private TerrainTileData EffectiveTile(TerrainTileData source)
    {
        var key = (source.X, source.Z);
        if (!_roadAffectedTiles.Contains(key)) return source;
        if (_effectiveTiles.TryGetValue(key, out var cached))
            return cached with { Masks = source.Masks, ActiveLayers = source.ActiveLayers };
        var heights = new ushort[source.Heights.Length];
        for (int z = 0; z < 256; z++)
        for (int x = 0; x < 256; x++)
        {
            int gx = Math.Min(source.X * 256 + x, Descriptor.SamplesX - 1);
            int gz = Math.Min(source.Z * 256 + z, Descriptor.SamplesZ - 1);
            heights[z * 256 + x] = EncodeEffective(EffectiveHeight(gx, gz, BaseHeight(gx, gz)));
        }
        var result = source with { Heights = heights, Normals = TerrainNormals.Build(Descriptor, source.X, source.Z, Height) };
        if (_effectiveTiles.Count >= 64) _effectiveTiles.Remove(_effectiveTiles.Keys.First());
        _effectiveTiles[key] = result;
        return result;
    }

    private TerrainOverviewData EffectiveOverview(TerrainOverviewData source)
    {
        if (_effectiveOverview != null) return _effectiveOverview;
        if (_roadBuckets.Count == 0) return source;
        var heights = (ushort[])source.Heights.Clone();
        foreach (var key in _roadProfiles.Values.SelectMany(p => p.Tiles).Distinct())
        {
            int x0 = key.X * 256 / source.Step, z0 = key.Z * 256 / source.Step;
            int x1 = Math.Min(source.Width - 1, (key.X + 1) * 256 / source.Step + 1);
            int z1 = Math.Min(source.Height - 1, (key.Z + 1) * 256 / source.Step + 1);
            for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
                heights[z * source.Width + x] = EncodeEffective(Height(Math.Min(x * source.Step, Descriptor.SamplesX - 1),
                    Math.Min(z * source.Step, Descriptor.SamplesZ - 1)));
        }
        return _effectiveOverview = source with { Heights = heights };
    }

    private ushort EncodeEffective(float height)
    {
        float raw = (height - Descriptor.HeightOffset) / Descriptor.HeightScale * 65535f;
        if (!float.IsFinite(raw)) throw new InvalidDataException("Некорректная высота модификатора Terrain.");
        return (ushort)Math.Clamp(MathF.Round(raw), 0, 65535);
    }

    private bool ClipToTerrain(ref Vector4 bounds)
    {
        var d = Descriptor;
        bounds = new(MathF.Max(bounds.X, d.OriginX), MathF.Max(bounds.Y, d.OriginZ),
            MathF.Min(bounds.Z, d.OriginX + (d.SamplesX - 1) * d.CellSize), MathF.Min(bounds.W, d.OriginZ + (d.SamplesZ - 1) * d.CellSize));
        return bounds.X <= bounds.Z && bounds.Y <= bounds.W;
    }

    private void AddTiles(Vector4 bounds, HashSet<(int X, int Z)> tiles)
    {
        var d = Descriptor;
        float size = d.CellSize * 256;
        int x0 = Math.Clamp((int)((bounds.X - d.OriginX) / size), 0, d.TilesX - 1);
        int z0 = Math.Clamp((int)((bounds.Y - d.OriginZ) / size), 0, d.TilesZ - 1);
        int x1 = Math.Clamp((int)((bounds.Z - d.OriginX) / size), 0, d.TilesX - 1);
        int z1 = Math.Clamp((int)((bounds.W - d.OriginZ) / size), 0, d.TilesZ - 1);
        for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
        {
            if (!tiles.Contains((x, z)) && tiles.Count >= MaximumModifierTiles)
                throw new InvalidDataException("Превышен бюджет тайлов дорожного коридора.");
            tiles.Add((x, z));
        }
    }

    private static int Priority(RoadType type) => type == RoadType.Automobile ? 3 : type == RoadType.Railway ? 2 : 1;
    private static (int X, int Z) Bucket(float x, float z) => ((int)MathF.Floor(x / ModifierBucketSize), (int)MathF.Floor(z / ModifierBucketSize));
    private static (int X0, int Z0, int X1, int Z1) BucketRange(Vector4 bounds)
    {
        var a = Bucket(bounds.X, bounds.Y); var b = Bucket(bounds.Z, bounds.W);
        return (a.X, a.Z, b.X, b.Z);
    }
    private static bool Overlaps(Vector4 a, Vector4 b) => a.X <= b.Z && a.Z >= b.X && a.Y <= b.W && a.W >= b.Y;
    private sealed record RoadModifierProfile(int Revision, RoadModifierSegment[] Segments, HashSet<(int X, int Z)> Tiles, Vector4 Bounds, string? Warning)
    {
        public HashSet<int> UnsupportedSections { get; init; } = [];
    }
    private sealed record RoadModifierSegment(RoadDefinition Road, int SectionIndex, Vector3 A, Vector3 B, float Edge, float Inner, float Outer, Vector4 Bounds)
    {
        public string ModifierId => $"{Road.Id}:{SectionIndex}";
    }
}
