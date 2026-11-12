using ZoneReborn.World.Terrain.Models;
using ZoneReborn.World.Terrain.Import;

namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainDocument
{
    private readonly Dictionary<(int X, int Z), long> _materialVersions = [];
    private Change? _stroke;
    public long TileMaterialVersion(int x, int z) => _materialVersions.GetValueOrDefault((x, z));

    public void ImportMaterials(IEnumerable<TerrainLayer> imported)
    {
        var candidates = imported.ToArray();
        var merged = layers.ToList();
        var identities = candidates.Where(item => !string.IsNullOrEmpty(item.SourceMaterial))
            .GroupBy(RelocationKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        foreach (var item in candidates)
        {
            int index = merged.FindIndex(layer => !string.IsNullOrEmpty(item.SourceMaterial)
                && string.Equals(layer.SourceMaterial, item.SourceMaterial, StringComparison.OrdinalIgnoreCase));
            if (index < 0 && !string.IsNullOrEmpty(item.SourceMaterial) && identities.GetValueOrDefault(RelocationKey(item)) == 1)
            {
                // A moved workspace must keep its existing material indices and chosen images.
                // Only match a unique FBX filename + material name; ambiguous sources stay distinct.
                int[] matches = merged.Where(layer => !string.IsNullOrEmpty(layer.SourceMaterial)
                    && string.Equals(RelocationKey(layer), RelocationKey(item), StringComparison.OrdinalIgnoreCase))
                    .Select(layer => layer.Index).ToArray();
                if (matches.Length == 1) index = matches[0];
            }
            if (index < 0) merged.Add(item with { Index = merged.Count });
            else if (!item.NeedsTexture && merged[index].NeedsTexture)
            {
                var existing = merged[index];

                merged[index] = item with
                {
                    Index = index,
                    Settings = existing.Settings,
                    TextureVariants = existing.TextureVariants,
                    IsArchived = existing.IsArchived
                };
            }
            else merged[index] = merged[index] with { SourceMaterial = item.SourceMaterial };
        }
        if (merged.Count > 255) throw new InvalidOperationException("Terrain поддерживает до 255 слоёв.");
        layers.Clear(); layers.AddRange(merged);
        Revision++; MaterialRevision++;
        InvalidatePaintOverview();

        static string RelocationKey(TerrainLayer layer)
        {
            int separator = layer.SourceMaterial.IndexOf('|');
            return (separator < 0 ? "" : Path.GetFileName(layer.SourceMaterial[..separator])) + "|" + layer.Name;
        }
    }

    public void SetLayer(TerrainLayer layer)
    {
        if (layer.Index < 0 || layer.Index >= layers.Count || !float.IsFinite(layer.ScaleU) || layer.ScaleU <= 0
            || !float.IsFinite(layer.ScaleV) || layer.ScaleV <= 0) throw new ArgumentException("Некорректный слой Terrain.");
        layers[layer.Index] = layer; Revision++; MaterialRevision++;
        InvalidatePaintOverview();
    }

    public void ApplySourceMaterialLayout(IReadOnlyDictionary<(int X, int Z), TerrainMaterialTile> tiles)
    {
        if (tiles.Count != manifest.Tiles.Count || manifest.Tiles.Any(tile => !tiles.ContainsKey((tile.X, tile.Z))))
            throw new InvalidDataException("Неполное распределение материалов Terrain.");
        EndPaintStroke();
        var change = new Change { SourceMaterialLayout = Descriptor.HasSourceMaterialLayout };
        foreach (var reference in manifest.Tiles)
        {
            var key = (reference.X, reference.Z);
            var old = Raw(key.X, key.Z);
            var source = tiles[key];
            change.Materials.Add(key, old);
            _tiles[key] = old with { ActiveLayers = source.ActiveLayers, Masks = source.Masks };
            _dirty.Add(key); _materialVersions[key] = MaterialRevision + 1; _paintOverviewDirty.Add(key);
        }
        Descriptor.HasSourceMaterialLayout = true;
        PushPaintUndo(change); Revision++; MaterialRevision++;
    }

    public void BeginPaintStroke() { EndPaintStroke(); _stroke = new Change(); }
    public void EndPaintStroke()
    {
        if (_stroke is { Materials.Count: > 0 }) PushPaintUndo(_stroke);
        _stroke = null;
    }

    private void PushPaintUndo(Change change)
    {
        // A large brush can touch many masks. Bound retained undo memory as well as stroke count.
        long size = change.Materials.Values.Sum(tile => tile.Masks.Sum(mask => (long)mask.Data.Length));
        while (_undo.Count > 0 && (_undo.Count >= 64 || size + _undo.Sum(entry => entry.Materials.Values
            .Sum(tile => tile.Masks.Sum(mask => (long)mask.Data.Length))) > 128L * 1024 * 1024)) _undo.RemoveAt(0);
        _undo.Add(change);
    }

    public bool Paint(float x, float z, float radius, float strength, int layerIndex)
    {
        if (!float.IsFinite(x) || !float.IsFinite(z) || !float.IsFinite(radius) || radius <= 0 || radius > 100
            || !float.IsFinite(strength) || strength <= 0 || strength > 1 || layerIndex < 0 || layerIndex >= layers.Count) return false;

        if (layers[layerIndex].IsArchived)
        {
            return false;
        }

        if (layers[layerIndex].NeedsTexture) throw new InvalidOperationException("Сначала выберите изображение для этого материала.");
        var change = _stroke ?? new Change();
        var d = Descriptor;
        float pixelSize = 256f / 248 * d.CellSize, border = 5 * pixelSize;
        int tx0 = Math.Clamp((int)MathF.Floor((x - radius - border) / (256 * d.CellSize)), 0, d.TilesX - 1);
        int tx1 = Math.Clamp((int)MathF.Floor((x + radius + border) / (256 * d.CellSize)), 0, d.TilesX - 1);
        int tz0 = Math.Clamp((int)MathF.Floor((z - radius - border) / (256 * d.CellSize)), 0, d.TilesZ - 1);
        int tz1 = Math.Clamp((int)MathF.Floor((z + radius + border) / (256 * d.CellSize)), 0, d.TilesZ - 1);
        bool changed = false;
        for (int tz = tz0; tz <= tz1; tz++)
        for (int tx = tx0; tx <= tx1; tx++)
        {
            var tile = Raw(tx, tz);
            var masks = tile.Masks.ToDictionary(mask => mask.Group, mask => TerrainMaskCodec.Editable(mask.Data));
            if (layerIndex > 0) masks.TryAdd((layerIndex - 1) / 3, new byte[256 * 256 * 4]);
            if (layerIndex > 0 && !tile.ActiveLayers.Contains((byte)layerIndex))
            {
                // BC1 can contain values in channels the renderer has never enabled.
                // Activating a new layer must not reveal that unused data outside the brush.
                byte[] channel = masks[(layerIndex - 1) / 3];
                for (int offset = (layerIndex - 1) % 3; offset < channel.Length; offset += 4) channel[offset] = 0;
            }
            int[] active = tile.ActiveLayers.Where(index => index > 0 && index < layers.Count)
                .Select(index => (int)index).Append(layerIndex).Where(index => index > 0).Distinct().ToArray();
            bool tileChanged = false;
            int px0 = Math.Clamp((int)MathF.Floor((x - radius - tx * 256 * d.CellSize) / pixelSize + 3.5f), 0, 255);
            int px1 = Math.Clamp((int)MathF.Ceiling((x + radius - tx * 256 * d.CellSize) / pixelSize + 3.5f), 0, 255);
            int pz0 = Math.Clamp((int)MathF.Floor((z - radius - tz * 256 * d.CellSize) / pixelSize + 3.5f), 0, 255);
            int pz1 = Math.Clamp((int)MathF.Ceiling((z + radius - tz * 256 * d.CellSize) / pixelSize + 3.5f), 0, 255);
            for (int pz = pz0; pz <= pz1; pz++)
            for (int px = px0; px <= px1; px++)
            {
                float wx = Math.Clamp(tx * 256 * d.CellSize + (px - 3.5f) * pixelSize, 0, d.WorldSizeX);
                float wz = Math.Clamp(tz * 256 * d.CellSize + (pz - 3.5f) * pixelSize, 0, d.WorldSizeZ);
                float distance = MathF.Sqrt((wx - x) * (wx - x) + (wz - z) * (wz - z));
                if (distance >= radius || !HasSurface(wx, wz)) continue;
                float alpha = strength * (1 - distance / radius);
                int offset = (pz * 256 + px) * 4;
                float sum = 0;
                foreach (int index in active)
                    if (masks.TryGetValue((index - 1) / 3, out var data)) sum += data[offset + (index - 1) % 3] / 255f;
                foreach (int index in active)
                {
                    if (!masks.TryGetValue((index - 1) / 3, out var data)) continue;
                    int channel = offset + (index - 1) % 3;
                    float weight = data[channel] / 255f / Math.Max(1, sum) * (1 - alpha) + (index == layerIndex ? alpha : 0);
                    byte value = (byte)Math.Clamp((int)MathF.Round(weight * 255), 0, 255);
                    if (value != data[channel]) { data[channel] = value; tileChanged = true; }
                }
            }
            if (!tileChanged) continue;
            change.Materials.TryAdd((tx, tz), tile);
            _tiles[(tx, tz)] = tile with
            {
                Masks = masks.OrderBy(pair => pair.Key).Select(pair => new TerrainMask(pair.Key, pair.Value)).ToArray(),
                ActiveLayers = active.Order().Select(index => (byte)index).ToArray()
            };
            _dirty.Add((tx, tz)); _materialVersions[(tx, tz)] = MaterialRevision + 1; changed = true;
            _paintOverviewDirty.Add((tx, tz));
        }
        if (!changed) return false;
        if (_stroke == null) PushPaintUndo(change);
        Revision++; MaterialRevision++; return true;
    }
}
