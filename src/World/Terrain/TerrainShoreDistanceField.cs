using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain;

internal sealed class TerrainShoreDistanceField
{
    private readonly float[] _distances;
    private readonly int _width;
    private readonly int _height;
    private readonly float _minX;
    private readonly float _minZ;
    private readonly float _step;

    private TerrainShoreDistanceField(
        float[] distances,
        int width,
        int height,
        float minX,
        float minZ,
        float step)
    {
        _distances = distances;
        _width = width;
        _height = height;
        _minX = minX;
        _minZ = minZ;
        _step = step;
    }

    public float Distance(float worldX, float worldZ)
    {
        int x = (int)MathF.Round(
            (worldX - _minX) / _step);

        int z = (int)MathF.Round(
            (worldZ - _minZ) / _step);

        if (x < 0 || z < 0 ||
            x >= _width || z >= _height)
        {
            return float.PositiveInfinity;
        }

        return _distances[z * _width + x];
    }

    public static TerrainShoreDistanceField Build(
        TerrainDocument document,
        TerrainWaterBody water)
    {
        var descriptor = document.Descriptor;

        float step = Math.Clamp(
            descriptor.CellSize,
            1f,
            3f);

        float margin = water.ShoreWidth + step * 2f;

        float minX = Math.Max(
            descriptor.OriginX,
            water.CenterX - water.RadiusX - margin);

        float maxX = Math.Min(
            descriptor.MaxX,
            water.CenterX + water.RadiusX + margin);

        float minZ = Math.Max(
            descriptor.OriginZ,
            water.CenterZ - water.RadiusZ - margin);

        float maxZ = Math.Min(
            descriptor.MaxZ,
            water.CenterZ + water.RadiusZ + margin);

        int width = (int)MathF.Ceiling(
            (maxX - minX) / step) + 1;

        int height = (int)MathF.Ceiling(
            (maxZ - minZ) / step) + 1;

        if ((long)width * height > 1_000_000)
        {
            throw new InvalidOperationException(
                "Водоём слишком большой для текущего " +
                "генератора. Разделите его на несколько участков.");
        }

        var distances = new float[width * height];

        Array.Fill(
            distances,
            float.PositiveInfinity);

        var queue = new PriorityQueue<int, float>();

        int flooded = 0;

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float worldX = Math.Min(
                    minX + x * step,
                    descriptor.MaxX);

                float worldZ = Math.Min(
                    minZ + z * step,
                    descriptor.MaxZ);

                if (!water.Contains(worldX, worldZ) ||
                    !document.HasSurface(worldX, worldZ))
                {
                    continue;
                }

                float terrainHeight =
                    document.SampleWaterTerrainHeight(
                        worldX,
                        worldZ);

                if (terrainHeight >= water.SurfaceHeight - 0.03f)
                {
                    continue;
                }

                int index = z * width + x;

                distances[index] = 0f;
                queue.Enqueue(index, 0f);
                flooded++;
            }
        }

        if (flooded == 0)
        {
            throw new InvalidOperationException(
                "Водоём не затапливает Terrain. " +
                "Поднимите уровень воды или опустите рельеф.");
        }

        float maximumDistance = water.ShoreWidth + step * 2f;

        while (queue.TryDequeue(
            out int current,
            out float currentDistance))
        {
            if (currentDistance > distances[current])
            {
                continue;
            }

            if (currentDistance > maximumDistance)
            {
                continue;
            }

            int x = current % width;
            int z = current / width;

            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    int nextX = x + dx;
                    int nextZ = z + dz;

                    if (nextX < 0 || nextZ < 0 ||
                        nextX >= width || nextZ >= height)
                    {
                        continue;
                    }

                    float cost = dx == 0 || dz == 0
                        ? step
                        : step * 1.41421356f;

                    float distance = currentDistance + cost;

                    if (distance > maximumDistance)
                    {
                        continue;
                    }

                    int next = nextZ * width + nextX;

                    if (distance >= distances[next])
                    {
                        continue;
                    }

                    distances[next] = distance;
                    queue.Enqueue(next, distance);
                }
            }
        }

        return new TerrainShoreDistanceField(
            distances,
            width,
            height,
            minX,
            minZ,
            step);
    }
}
