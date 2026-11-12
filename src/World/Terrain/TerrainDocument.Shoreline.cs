using ZoneReborn.World.Roads;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainDocument
{
    public int GenerateShoreline(
        string waterId,
        RoadNetwork? roads = null)
    {
        var water = WaterBodies.FirstOrDefault(
            item => item.Id == waterId)
            ?? throw new InvalidOperationException(
                "Водоём не найден.");

        int sandIndex = water.SandLayerIndex;

        if (sandIndex <= 0 || sandIndex >= layers.Count)
        {
            throw new InvalidOperationException(
                "Назначьте материал песка.");
        }

        var sand = layers[sandIndex];

        if (sand.IsArchived ||
            sand.NeedsTexture ||
            sand.Settings.SurfaceType != TerrainSurfaceType.Sand)
        {
            throw new InvalidOperationException(
                "Выбранный слой должен быть активным " +
                "материалом типа «Песок» с текстурой.");
        }

        EndPaintStroke();

        var field = TerrainShoreDistanceField.Build(
            this,
            water);

        var roadSegments = BuildShoreRoadSegments(
            water,
            roads);

        var d = Descriptor;
        float pixelSize = 256f / 248f * d.CellSize;
        float border = 5f * pixelSize;

        float extentX = water.RadiusX +
            water.ShoreWidth + border;

        float extentZ = water.RadiusZ +
            water.ShoreWidth + border;

        float tileSize = 256f * d.CellSize;

        int tx0 = Math.Clamp(
            (int)MathF.Floor(
                (water.CenterX - extentX) / tileSize),
            0,
            d.TilesX - 1);

        int tx1 = Math.Clamp(
            (int)MathF.Floor(
                (water.CenterX + extentX) / tileSize),
            0,
            d.TilesX - 1);

        int tz0 = Math.Clamp(
            (int)MathF.Floor(
                (water.CenterZ - extentZ) / tileSize),
            0,
            d.TilesZ - 1);

        int tz1 = Math.Clamp(
            (int)MathF.Floor(
                (water.CenterZ + extentZ) / tileSize),
            0,
            d.TilesZ - 1);

        int sandOrder = GetPaintOrder(sand);

        var change = new Change();
        int changedTiles = 0;

        float slopeStep = Math.Max(d.CellSize, 1f);

        for (int tz = tz0; tz <= tz1; tz++)
        {
            for (int tx = tx0; tx <= tx1; tx++)
            {
                var tile = Raw(tx, tz);

                var masks = tile.Masks.ToDictionary(
                    mask => mask.Group,
                    mask => TerrainMaskCodec.Editable(mask.Data));

                int sandGroup = (sandIndex - 1) / 3;
                int sandChannel = (sandIndex - 1) % 3;

                masks.TryAdd(
                    sandGroup,
                    new byte[256 * 256 * 4]);

                if (!tile.ActiveLayers.Contains((byte)sandIndex))
                {
                    byte[] data = masks[sandGroup];

                    for (int offset = sandChannel;
                         offset < data.Length;
                         offset += 4)
                    {
                        data[offset] = 0;
                    }
                }

                int[] active = tile.ActiveLayers
                    .Where(index =>
                        index > 0 &&
                        index < layers.Count)
                    .Select(index => (int)index)
                    .Append(sandIndex)
                    .Distinct()
                    .ToArray();

                int[] protectedIndices = active
                    .Where(index =>
                        index != sandIndex &&
                        (
                            layers[index].IsPaintLocked ||
                            GetPaintOrder(layers[index]) < sandOrder
                        ))
                    .ToArray();

                bool tileChanged = false;

                for (int pz = 0; pz < 256; pz++)
                {
                    for (int px = 0; px < 256; px++)
                    {
                        float worldX = Math.Clamp(
                            tx * tileSize +
                            (px - 3.5f) * pixelSize,
                            0f,
                            d.WorldSizeX);

                        float worldZ = Math.Clamp(
                            tz * tileSize +
                            (pz - 3.5f) * pixelSize,
                            0f,
                            d.WorldSizeZ);

                        if (!HasSurface(worldX, worldZ))
                        {
                            continue;
                        }

                        float distance = field.Distance(
                            worldX,
                            worldZ);

                        if (distance > water.ShoreWidth)
                        {
                            continue;
                        }

                        if (IsProtectedShoreRoad(
                            worldX,
                            worldZ,
                            roadSegments))
                        {
                            continue;
                        }

                        float terrainHeight =
                            SampleWaterTerrainHeight(
                                worldX,
                                worldZ);

                        if (water.Contains(worldX, worldZ) &&
                            terrainHeight < water.SurfaceHeight)
                        {
                            continue;
                        }

                        float rise = terrainHeight -
                            water.SurfaceHeight;

                        if (rise < -0.02f ||
                            rise > water.MaxShoreRise)
                        {
                            continue;
                        }

                        float heightX =
                            SampleWaterTerrainHeight(
                                worldX + slopeStep,
                                worldZ) -
                            SampleWaterTerrainHeight(
                                worldX - slopeStep,
                                worldZ);

                        float heightZ =
                            SampleWaterTerrainHeight(
                                worldX,
                                worldZ + slopeStep) -
                            SampleWaterTerrainHeight(
                                worldX,
                                worldZ - slopeStep);

                        float slope = MathF.Sqrt(
                            heightX * heightX +
                            heightZ * heightZ) /
                            (2f * slopeStep);

                        if (slope > water.MaxShoreSlope)
                        {
                            continue;
                        }

                        int offset = (pz * 256 + px) * 4;

                        bool protectedPixel = false;

                        foreach (int index in protectedIndices)
                        {
                            int group = (index - 1) / 3;
                            int channel = (index - 1) % 3;

                            if (masks.TryGetValue(
                                    group,
                                    out var data) &&
                                data[offset + channel] > 0)
                            {
                                protectedPixel = true;
                                break;
                            }
                        }

                        if (protectedPixel)
                        {
                            continue;
                        }

                        float edge = Math.Clamp(
                            1f - distance / water.ShoreWidth,
                            0f,
                            1f);

                        float falloff = edge * edge *
                            (3f - 2f * edge);

                        float heightFactor = Math.Clamp(
                            1f - rise / water.MaxShoreRise,
                            0f,
                            1f);

                        float slopeFactor = Math.Clamp(
                            1f - slope / water.MaxShoreSlope,
                            0f,
                            1f);

                        float alpha = Math.Clamp(
                            water.ShoreStrength *
                            falloff *
                            heightFactor *
                            slopeFactor,
                            0f,
                            1f);

                        if (alpha <= 0.001f)
                        {
                            continue;
                        }

                        float sum = 0f;

                        foreach (int index in active)
                        {
                            int group = (index - 1) / 3;
                            int channel = (index - 1) % 3;

                            if (masks.TryGetValue(
                                    group,
                                    out var data))
                            {
                                sum += data[offset + channel] / 255f;
                            }
                        }

                        foreach (int index in active)
                        {
                            int group = (index - 1) / 3;
                            int channel = (index - 1) % 3;

                            if (!masks.TryGetValue(
                                    group,
                                    out var data))
                            {
                                continue;
                            }

                            int position = offset + channel;

                            float current = data[position] / 255f;

                            float weight =
                                current / Math.Max(1f, sum) *
                                (1f - alpha);

                            if (index == sandIndex)
                            {
                                weight += alpha;
                            }

                            byte value = (byte)Math.Clamp(
                                (int)MathF.Round(weight * 255f),
                                0,
                                255);

                            if (data[position] == value)
                            {
                                continue;
                            }

                            data[position] = value;
                            tileChanged = true;
                        }
                    }
                }

                if (!tileChanged)
                {
                    continue;
                }

                change.Materials.TryAdd((tx, tz), tile);

                _tiles[(tx, tz)] = tile with
                {
                    Masks = masks
                        .OrderBy(pair => pair.Key)
                        .Select(pair => new TerrainMask(
                            pair.Key,
                            pair.Value))
                        .ToArray(),

                    ActiveLayers = active
                        .Order()
                        .Select(index => (byte)index)
                        .ToArray()
                };

                _dirty.Add((tx, tz));

                _materialVersions[(tx, tz)] =
                    MaterialRevision + 1;

                _paintOverviewDirty.Add((tx, tz));

                changedTiles++;
            }
        }

        if (changedTiles == 0)
        {
            return 0;
        }

        PushPaintUndo(change);

        Revision++;
        MaterialRevision++;

        return changedTiles;
    }

    private static List<ShoreRoadSegment> BuildShoreRoadSegments(
        TerrainWaterBody water,
        RoadNetwork? roads)
    {
        var result = new List<ShoreRoadSegment>();

        if (roads == null)
        {
            return result;
        }

        float extentX = water.RadiusX + water.ShoreWidth;
        float extentZ = water.RadiusZ + water.ShoreWidth;

        foreach (var road in roads.Roads)
        {
            var nodes = road.Nodes;

            for (int i = 1; i < nodes.Length; i++)
            {
                var a = nodes[i - 1];
                var b = nodes[i];

                float radius = Math.Clamp(
                    Math.Max(a.Width, b.Width) * 0.5f,
                    2f,
                    30f) + 1.5f;

                if (Math.Max(a.X, b.X) + radius <
                        water.CenterX - extentX ||
                    Math.Min(a.X, b.X) - radius >
                        water.CenterX + extentX ||
                    Math.Max(a.Z, b.Z) + radius <
                        water.CenterZ - extentZ ||
                    Math.Min(a.Z, b.Z) - radius >
                        water.CenterZ + extentZ)
                {
                    continue;
                }

                result.Add(new ShoreRoadSegment(
                    a.X,
                    a.Z,
                    b.X,
                    b.Z,
                    radius));
            }
        }

        return result;
    }

    private static bool IsProtectedShoreRoad(
        float x,
        float z,
        IReadOnlyList<ShoreRoadSegment> segments)
    {
        foreach (var road in segments)
        {
            float dx = road.Bx - road.Ax;
            float dz = road.Bz - road.Az;

            float lengthSquared = dx * dx + dz * dz;

            float t = lengthSquared > 0.00001f
                ? Math.Clamp(
                    ((x - road.Ax) * dx +
                     (z - road.Az) * dz) / lengthSquared,
                    0f,
                    1f)
                : 0f;

            float closestX = road.Ax + dx * t;
            float closestZ = road.Az + dz * t;

            float offsetX = x - closestX;
            float offsetZ = z - closestZ;

            if (offsetX * offsetX + offsetZ * offsetZ <=
                road.Radius * road.Radius)
            {
                return true;
            }
        }

        return false;
    }

    private readonly record struct ShoreRoadSegment(
        float Ax,
        float Az,
        float Bx,
        float Bz,
        float Radius);
}
