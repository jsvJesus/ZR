namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainDocument
{
    private Change? _sculptStroke;

    private readonly HashSet<(int X, int Z)>
        _sculptHeightsTouched = [];

    private readonly HashSet<(int X, int Z)>
        _sculptHolesTouched = [];

    public void BeginSculptStroke()
    {
        EndSculptStroke();

        _sculptStroke = new Change();

        _sculptHeightsTouched.Clear();
        _sculptHolesTouched.Clear();
    }

    public void EndSculptStroke()
    {
        if (_sculptStroke == null)
        {
            return;
        }

        var change = _sculptStroke;
        _sculptStroke = null;

        if (change.Heights.Count > 0 || change.Holes.Count > 0)
        {
            if (_undo.Count >= 64)
            {
                _undo.RemoveAt(0);
            }

            _undo.Add(change);
        }

        _sculptHeightsTouched.Clear();
        _sculptHolesTouched.Clear();
    }

    private bool ApplyStrongSmooth(
        float x,
        float z,
        float radius,
        float strength)
    {
        var d = Descriptor;

        float effectiveStrength = Math.Min(5f, strength);
        int passes = (int)MathF.Ceiling(effectiveStrength);

        int x0 = Math.Clamp(
            (int)MathF.Floor((x - d.OriginX - radius) / d.CellSize),
            0,
            d.SamplesX - 1
        );

        int x1 = Math.Clamp(
            (int)MathF.Ceiling((x - d.OriginX + radius) / d.CellSize),
            0,
            d.SamplesX - 1
        );

        int z0 = Math.Clamp(
            (int)MathF.Floor((z - d.OriginZ - radius) / d.CellSize),
            0,
            d.SamplesZ - 1
        );

        int z1 = Math.Clamp(
            (int)MathF.Ceiling((z - d.OriginZ + radius) / d.CellSize),
            0,
            d.SamplesZ - 1
        );

        int startX = x0 - 1;
        int startZ = z0 - 1;

        int width = x1 - x0 + 3;
        int height = z1 - z0 + 3;

        float[] current = new float[checked(width * height)];
        float[] next = new float[current.Length];

        for (int gz = startZ; gz <= z1 + 1; gz++)
        {
            for (int gx = startX; gx <= x1 + 1; gx++)
            {
                int index =
                    (gz - startZ) * width +
                    (gx - startX);

                current[index] = Height(gx, gz);
            }
        }

        for (int pass = 0; pass < passes; pass++)
        {
            float passStrength = Math.Clamp(
                effectiveStrength - pass,
                0f,
                1f
            );

            Array.Copy(current, next, current.Length);

            for (int gz = z0; gz <= z1; gz++)
            {
                float wz = d.OriginZ + gz * d.CellSize;
                float dz = wz - z;

                for (int gx = x0; gx <= x1; gx++)
                {
                    float wx = d.OriginX + gx * d.CellSize;
                    float dx = wx - x;

                    float distance = MathF.Sqrt(
                        dx * dx + dz * dz
                    );

                    if (distance > radius)
                    {
                        continue;
                    }

                    float weight = 1f - distance / radius;
                    float blend = passStrength * weight;

                    if (blend <= 0f)
                    {
                        continue;
                    }

                    int index =
                        (gz - startZ) * width +
                        (gx - startX);

                    float center = current[index];

                    float cardinal =
                        current[index - 1] +
                        current[index + 1] +
                        current[index - width] +
                        current[index + width];

                    float average;

                    if (pass == 0)
                    {
                        average = cardinal * 0.25f;
                    }
                    else
                    {
                        float diagonal =
                            current[index - width - 1] +
                            current[index - width + 1] +
                            current[index + width - 1] +
                            current[index + width + 1];

                        average =
                            (center * 4f +
                             cardinal * 2f +
                             diagonal) / 16f;
                    }

                    next[index] = center +
                        (average - center) * blend;
                }
            }

            (current, next) = (next, current);
        }

        var change = _sculptStroke ?? new Change();
        var pending = new List<(int X, int Z, ushort Value)>();

        for (int gz = z0; gz <= z1; gz++)
        {
            float wz = d.OriginZ + gz * d.CellSize;

            for (int gx = x0; gx <= x1; gx++)
            {
                float wx = d.OriginX + gx * d.CellSize;

                float dx = wx - x;
                float dz = wz - z;

                if (dx * dx + dz * dz > radius * radius)
                {
                    continue;
                }

                int index =
                    (gz - startZ) * width +
                    (gx - startX);

                ushort old = Raw(
                    gx / 256,
                    gz / 256
                ).Heights[
                    (gz % 256) * 256 + gx % 256
                ];

                float newHeight = current[index];

                ushort encoded = (ushort)Math.Clamp(
                    (int)MathF.Round(
                        (newHeight - d.HeightOffset) /
                        d.HeightScale * 65535f
                    ),
                    0,
                    65535
                );

                if (encoded == old)
                {
                    continue;
                }

                if (_sculptStroke == null ||
                    _sculptHeightsTouched.Add((gx, gz)))
                {
                    change.Heights.Add((gx, gz, old));
                }

                pending.Add((gx, gz, encoded));
            }
        }

        if (pending.Count == 0)
        {
            return false;
        }

        foreach (var value in pending)
        {
            SetHeight(value.X, value.Z, value.Value);
        }

        if (_sculptStroke == null)
        {
            if (_undo.Count >= 64)
            {
                _undo.RemoveAt(0);
            }

            _undo.Add(change);
        }

        Revision++;
        HeightRevision++;

        return true;
    }
}
