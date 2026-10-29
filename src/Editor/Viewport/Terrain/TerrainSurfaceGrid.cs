using System.Numerics;

namespace Editor.Viewport.Terrain
{
    // Immutable CPU copy of the exact grid currently submitted to DX11, including shortened map edges.
    internal sealed class TerrainSurfaceGrid(Vector3[] positions, int side)
    {
        public Vector3[] Positions { get; } = positions;
        public int Side { get; } = side;
        public float MinX => Positions[0].X;
        public float MinZ => Positions[0].Z;
        public float StepX => Positions[1].X - MinX;
        public float StepZ => Positions[Side].Z - MinZ;
    }
}
