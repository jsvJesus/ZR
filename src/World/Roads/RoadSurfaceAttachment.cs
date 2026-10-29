using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Roads
{
    public static class RoadSurfaceAttachment
    {
        public static void ReadSourceOffsets(LoadedLevel level, RoadNetwork network, CancellationToken cancellationToken)
        {
            var surface = new TerrainSurface(level);
            foreach (var road in network.Roads)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var offsets = new float[road.Vertices.Length];
                for (int i = 0; i < offsets.Length; i++)
                {
                    if ((i & 255) == 0) cancellationToken.ThrowIfCancellationRequested();
                    var p = road.Vertices[i].Position;
                    if (surface.TryGetSourceHeight(p.X, p.Z, out float ground))
                    {
                        // Ignore float roundoff, which otherwise creates different layers for coplanar roads.
                        float tolerance = 8 * Math.Max(MathF.Abs(MathF.BitIncrement(p.Y) - p.Y),
                            MathF.Abs(MathF.BitIncrement(ground) - ground));
                        float offset = p.Y - ground;
                        offsets[i] = offset > tolerance ? offset : 0;
                    }
                }
                road.TerrainOffsets = offsets;
            }
        }
    }
}
