using System.Numerics;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Characters
{
    public sealed class ThirdPersonCamera
    {
        public float Yaw { get; private set; }
        public float Pitch { get; private set; } = -0.12f;
        public float Distance { get; private set; } = 3.5f;
        public ThirdPersonCamera(float yaw) => Yaw = yaw;

        public void Rotate(float dx, float dy)
        {
            Yaw += dx * 0.003f;
            Pitch = Math.Clamp(Pitch - dy * 0.003f, -1.15f, 0.65f);
        }
        public void Zoom(int delta) => Distance = Math.Clamp(Distance * MathF.Pow(1.12f, -delta / 120f), 0.8f, 8f);

        public Vector3 Position(Vector3 player, TerrainSurface surface, float height = 1.15f)
        {
            var pivot = player + new Vector3(0, height, 0);
            var back = -new Vector3(MathF.Sin(Yaw) * MathF.Cos(Pitch), MathF.Sin(Pitch), MathF.Cos(Yaw) * MathF.Cos(Pitch));
            float clearDistance = 0;
            int steps = (int)MathF.Ceiling(Distance / 0.08f);
            for (int i = 1; i <= steps; i++)
            {
                float t = Distance * i / steps;
                var probe = pivot + back * t;
                if (surface.TryGetHeight(probe.X, probe.Z, out float terrainHeight) && probe.Y < terrainHeight + 0.15f)
                    break;
                clearDistance = t;
            }
            return pivot + back * clearDistance;
        }
    }
}
