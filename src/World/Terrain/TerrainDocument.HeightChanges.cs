namespace ZoneReborn.World.Terrain;

/// <summary>Preview of a height edit, before any samples or undo records are changed.</summary>
public sealed record TerrainHeightChange(
    float MinimumX, float MinimumZ, float MaximumX, float MaximumZ,
    Func<int, int, float> SampleHeight);

public sealed partial class TerrainDocument
{
    public event Action<TerrainHeightChange>? HeightsChanging;
    public event Action? HeightsChanged;

    private void WriteHeightChanges(List<(int X, int Z, ushort Value)> values, Change? undo)
    {
        if (values.Count == 0) return;

        if (HeightsChanging != null)
        {
            var samples = values.ToDictionary(v => (v.X, v.Z), v => v.Value);
            var d = Descriptor;
            // A rendered triangle can use a changed vertex one mesh step away.
            float padding = d.MeshStep * d.CellSize;
            var change = new TerrainHeightChange(
                d.OriginX + values.Min(v => v.X) * d.CellSize - padding,
                d.OriginZ + values.Min(v => v.Z) * d.CellSize - padding,
                d.OriginX + values.Max(v => v.X) * d.CellSize + padding,
                d.OriginZ + values.Max(v => v.Z) * d.CellSize + padding,
                (x, z) => samples.TryGetValue((x, z), out ushort value) ? d.DecodeHeight(value) : Height(x, z));
            // Subscribers validate and prepare their changes here. A rejection leaves heights intact.
            HeightsChanging.Invoke(change);
        }

        foreach (var value in values)
        {
            if (undo != null && (_sculptStroke == null || _sculptHeightsTouched.Add((value.X, value.Z))))
            {
                ushort previous = Raw(value.X / 256, value.Z / 256).Heights[(value.Z % 256) * 256 + value.X % 256];
                undo.Heights.Add((value.X, value.Z, previous));
            }
            SetHeight(value.X, value.Z, value.Value);
        }

        HeightsChanged?.Invoke();
    }
}
