namespace ZoneReborn.World.Terrain;

/// <summary>
/// Preview of a height edit before terrain samples change.
/// </summary>
public sealed record TerrainHeightChange(
    float MinimumX,
    float MinimumZ,
    float MaximumX,
    float MaximumZ,
    Func<int, int, float> SampleHeight);

public sealed partial class TerrainDocument
{
    public event Action<TerrainHeightChange>? HeightsChanging;
    public event Action? HeightsChanged;

    private void WriteHeightChanges(
        List<(int X, int Z, ushort Value)> values,
        Change? undo)
    {
        if (values.Count == 0)
        {
            return;
        }

        var descriptor = Descriptor;

        if (HeightsChanging != null)
        {
            var samples = values.ToDictionary(
                value => (value.X, value.Z),
                value => value.Value);

            float padding =
                descriptor.MeshStep * descriptor.CellSize;

            var change = new TerrainHeightChange(
                descriptor.OriginX +
                    values.Min(value => value.X) *
                    descriptor.CellSize - padding,
                descriptor.OriginZ +
                    values.Min(value => value.Z) *
                    descriptor.CellSize - padding,
                descriptor.OriginX +
                    values.Max(value => value.X) *
                    descriptor.CellSize + padding,
                descriptor.OriginZ +
                    values.Max(value => value.Z) *
                    descriptor.CellSize + padding,
                (x, z) =>
                    samples.TryGetValue(
                        (x, z),
                        out ushort encoded)
                        ? descriptor.DecodeHeight(encoded)
                        : BaseHeight(x, z));

            HeightsChanging.Invoke(change);
        }

        var affectedTiles = new HashSet<(int X, int Z)>();

        foreach (var value in values)
        {
            affectedTiles.Add((
                value.X / 256,
                value.Z / 256));
        }

        foreach (var tile in affectedTiles)
        {
            InvalidateEffectiveTile(tile.X, tile.Z);

            int minX = Math.Max(0, tile.X - 1);
            int maxX = Math.Min(
                Descriptor.TilesX - 1,
                tile.X + 1
            );

            int minZ = Math.Max(0, tile.Z - 1);
            int maxZ = Math.Min(
                Descriptor.TilesZ - 1,
                tile.Z + 1
            );

            for (int tz = minZ; tz <= maxZ; tz++)
            {
                for (int tx = minX; tx <= maxX; tx++)
                {
                    _normalDirty.Add((tx, tz));
                }
            }
        }

        foreach (var value in values)
        {
            if (undo != null &&
                (_sculptStroke == null ||
                 _sculptHeightsTouched.Add(
                     (value.X, value.Z))))
            {
                ushort previous = Raw(
                    value.X / 256,
                    value.Z / 256).Heights[
                        (value.Z % 256) * 256 +
                        value.X % 256];

                undo.Heights.Add((
                    value.X,
                    value.Z,
                    previous));
            }

            SetHeight(
                value.X,
                value.Z,
                value.Value);
        }
        HeightsChanged?.Invoke();
    }
}
