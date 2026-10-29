using System.Numerics;

namespace Editor.Viewport.Terrain
{
    internal sealed partial class TerrainRenderer
    {
        public bool SurfaceHeight(float x, float z, out float height)
        {
            height = 0;
            var d = Descriptor;
            if (x < 0 || z < 0 || x > d.WorldSizeX || z > d.WorldSizeZ) return false;
            int tx = Math.Clamp((int)(x / (d.CellSize * 256)), 0, d.TilesX - 1);
            int tz = Math.Clamp((int)(z / (d.CellSize * 256)), 0, d.TilesZ - 1);
            var grid = GetSurfaceGrid(tx, tz);
            int ix = Math.Clamp((int)((x - grid.MinX) / grid.StepX), 0, grid.Side - 2);
            int iz = Math.Clamp((int)((z - grid.MinZ) / grid.StepZ), 0, grid.Side - 2);
            var a = grid.Positions[iz * grid.Side + ix]; var b = grid.Positions[iz * grid.Side + ix + 1];
            var c = grid.Positions[(iz + 1) * grid.Side + ix]; var e = grid.Positions[(iz + 1) * grid.Side + ix + 1];
            height = ZoneReborn.World.Terrain.TerrainSurface.Interpolate(a.Y, b.Y, c.Y, e.Y,
                (x - a.X) / (b.X - a.X), (z - a.Z) / (c.Z - a.Z));
            return true;
        }

        public bool PickSurface(Vector3 origin, Vector3 direction, out Vector3 position)
        {
            position = default;
            var d = Descriptor;
            if (!ViewportPicking.Box(origin, direction, new Vector3(0, -1e8f, 0),
                new Vector3(d.WorldSizeX, 1e8f, d.WorldSizeZ), out float near, out float far)) return false;
            far = Math.Min(far, FarPlane);
            float distance = near;
            for (int iteration = 0; iteration < 32768 && distance <= far; iteration++)
            {
                var point = origin + direction * distance;
                float x = Math.Clamp(point.X, 0, d.WorldSizeX), z = Math.Clamp(point.Z, 0, d.WorldSizeZ);
                int tx = Math.Clamp((int)(x / (d.CellSize * 256)), 0, d.TilesX - 1);
                int tz = Math.Clamp((int)(z / (d.CellSize * 256)), 0, d.TilesZ - 1);
                var grid = GetSurfaceGrid(tx, tz);
                int ix = Math.Clamp((int)((x - grid.MinX) / grid.StepX), 0, grid.Side - 2);
                int iz = Math.Clamp((int)((z - grid.MinZ) / grid.StepZ), 0, grid.Side - 2);
                var a = grid.Positions[iz * grid.Side + ix]; var b = grid.Positions[iz * grid.Side + ix + 1];
                var c = grid.Positions[(iz + 1) * grid.Side + ix]; var e = grid.Positions[(iz + 1) * grid.Side + ix + 1];
                float closest = float.MaxValue;
                if (ViewportPicking.Triangle(origin, direction, a, b, c, out float first)) closest = first;
                if (ViewportPicking.Triangle(origin, direction, e, c, b, out float second)) closest = Math.Min(closest, second);
                if (closest >= near && closest <= far && closest >= distance - 0.01f)
                {
                    position = origin + direction * closest; return true;
                }
                float exitX = MathF.Abs(direction.X) < 1e-9f ? float.MaxValue
                    : ((direction.X > 0 ? b.X : a.X) - origin.X) / direction.X;
                float exitZ = MathF.Abs(direction.Z) < 1e-9f ? float.MaxValue
                    : ((direction.Z > 0 ? c.Z : a.Z) - origin.Z) / direction.Z;
                float exit = Math.Min(exitX, exitZ);
                if (!float.IsFinite(exit) || exit == float.MaxValue) return false;
                float advance = Math.Max(0.001f, 8 * MathF.Abs(MathF.BitIncrement(exit) - exit));
                distance = Math.Max(distance + advance, exit + advance);
            }
            return false;
        }
    }
}
