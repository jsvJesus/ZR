using System.Text.Json;
using ZoneReborn.World.Levels.Storage;
using ZoneReborn.World.Terrain.Models;

namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainDocument
{
    private List<TerrainWaterBody>? _waterBodies;

    public long WaterRevision { get; private set; }

    public IReadOnlyList<TerrainWaterBody> WaterBodies =>
        _waterBodies ??= LoadWaterBodies();

    public void SetWaterBody(TerrainWaterBody body)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (string.IsNullOrWhiteSpace(body.Id) ||
            string.IsNullOrWhiteSpace(body.Name))
        {
            throw new InvalidOperationException(
                "Укажите название водоёма.");
        }

        if (!float.IsFinite(body.CenterX) ||
            !float.IsFinite(body.CenterZ) ||
            !float.IsFinite(body.SurfaceHeight) ||
            !float.IsFinite(body.RadiusX) ||
            !float.IsFinite(body.RadiusZ) ||
            !float.IsFinite(body.ShoreWidth) ||
            !float.IsFinite(body.ShoreStrength) ||
            !float.IsFinite(body.MaxShoreRise) ||
            !float.IsFinite(body.MaxShoreSlope))
        {
            throw new InvalidOperationException(
                "Параметры водоёма должны быть конечными числами.");
        }

        if (!Descriptor.Contains(body.CenterX, body.CenterZ))
        {
            throw new InvalidOperationException(
                "Центр водоёма находится за пределами Terrain.");
        }

        if (body.RadiusX < 1 || body.RadiusX > 500 ||
            body.RadiusZ < 1 || body.RadiusZ > 500 ||
            body.ShoreWidth < 0.5f || body.ShoreWidth > 100 ||
            body.ShoreStrength < 0 || body.ShoreStrength > 1 ||
            body.MaxShoreRise < 0.1f || body.MaxShoreRise > 50 ||
            body.MaxShoreSlope < 0.05f || body.MaxShoreSlope > 5)
        {
            throw new InvalidOperationException(
                "Проверьте размеры и параметры берега.");
        }

        if (body.SandLayerIndex < -1 ||
            body.SandLayerIndex >= layers.Count)
        {
            throw new InvalidOperationException(
                "Неверный материал песка.");
        }

        var items = _waterBodies ??= LoadWaterBodies();

        int index = items.FindIndex(
            item => item.Id == body.Id);

        if (index >= 0)
        {
            items[index] = body;
        }
        else
        {
            items.Add(body);
        }

        Revision++;
        WaterRevision++;
    }

    public bool RemoveWaterBody(string id)
    {
        var items = _waterBodies ??= LoadWaterBodies();

        int removed = items.RemoveAll(item => item.Id == id);

        if (removed == 0)
        {
            return false;
        }

        Revision++;
        WaterRevision++;

        return true;
    }

    public float SampleWaterTerrainHeight(float x, float z)
    {
        var d = Descriptor;

        x = Math.Clamp(x, d.OriginX, d.MaxX);
        z = Math.Clamp(z, d.OriginZ, d.MaxZ);

        float gx = (x - d.OriginX) / d.CellSize;
        float gz = (z - d.OriginZ) / d.CellSize;

        int x0 = Math.Clamp(
            (int)MathF.Floor(gx), 0, d.SamplesX - 1);

        int z0 = Math.Clamp(
            (int)MathF.Floor(gz), 0, d.SamplesZ - 1);

        int x1 = Math.Min(x0 + 1, d.SamplesX - 1);
        int z1 = Math.Min(z0 + 1, d.SamplesZ - 1);

        float fx = gx - x0;
        float fz = gz - z0;

        float a = Height(x0, z0);
        float b = Height(x1, z0);
        float c = Height(x0, z1);
        float e = Height(x1, z1);

        float top = a + (b - a) * fx;
        float bottom = c + (e - c) * fx;

        return top + (bottom - top) * fz;
    }

    private List<TerrainWaterBody> LoadWaterBodies()
    {
        string path = Path.Combine(
            directory,
            "terrain",
            "water.json");

        if (!File.Exists(path))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<TerrainWaterBody>>(
            File.ReadAllText(path),
            LevelJson.Options) ?? [];
    }

    private void SaveWaterBodies()
    {
        if (_waterBodies == null)
        {
            return;
        }

        Atomic(
            "terrain/water.json",
            path => File.WriteAllText(
                path,
                JsonSerializer.Serialize(
                    _waterBodies,
                    LevelJson.Options)));
    }
}
