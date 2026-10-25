using Microsoft.Xna.Framework;
using SharpDX;

namespace Editor.Viewport
{
    public readonly record struct CameraPose(
        float X,
        float Y,
        float Z,
        float Yaw,
        float Pitch,
        float Speed);

    public sealed class FreeCamera
    {
        public Vector3 Position { get; private set; } =
            new Vector3(0f, 4f, -12f);

        public float Yaw { get; private set; }

        public float Pitch { get; private set; } = -0.18f;

        public float Speed { get; set; } = 8f;

        public CameraPose Pose => new CameraPose(
            Position.X,
            Position.Y,
            Position.Z,
            Yaw,
            Pitch,
            Speed);

        public Vector3 LookDirection
        {
            get
            {
                return new Vector3(
                    MathF.Sin(Yaw) * MathF.Cos(Pitch),
                    MathF.Sin(Pitch),
                    MathF.Cos(Yaw) * MathF.Cos(Pitch));
            }
        }

        public void Rotate(float deltaX, float deltaY)
        {
            const float sensitivity = 0.003f;

            Yaw += deltaX * sensitivity;

            Pitch = Math.Clamp(
                Pitch - deltaY * sensitivity,
                -1.50f,
                1.50f);
        }

        public void ChangeSpeed(int wheelDelta)
        {
            if (wheelDelta == 0)
            {
                return;
            }

            float steps = wheelDelta / 120f;

            Speed = Math.Clamp(
                Speed * MathF.Pow(1.2f, steps),
                1f,
                100f);
        }

        public void Move(
            bool forward,
            bool backward,
            bool left,
            bool right,
            bool up,
            bool down,
            float deltaTime)
        {
            var front = new Vector3(
                MathF.Sin(Yaw),
                0f,
                MathF.Cos(Yaw));

            var side = new Vector3(
                front.Z,
                0f,
                -front.X);

            var direction = Vector3.Zero;

            if (forward)
            {
                direction += front;
            }

            if (backward)
            {
                direction -= front;
            }

            if (right)
            {
                direction += side;
            }

            if (left)
            {
                direction -= side;
            }

            if (up)
            {
                direction += Vector3.UnitY;
            }

            if (down)
            {
                direction -= Vector3.UnitY;
            }

            if (direction.LengthSquared() > 0.0001f)
            {
                direction.Normalize();
                Position += direction * Speed * deltaTime;
            }
        }

        public void Restore(CameraPose pose)
        {
            Position = new Vector3(pose.X, pose.Y, pose.Z);
            Yaw = pose.Yaw;
            Pitch = Math.Clamp(pose.Pitch, -1.50f, 1.50f);
            Speed = Math.Clamp(pose.Speed, 1f, 100f);
        }
    }
}
