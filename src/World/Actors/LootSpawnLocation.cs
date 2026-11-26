using System.Numerics;
using System.Text.Json.Serialization;

namespace ZoneReborn.World.Actors
{
    public sealed class LootSpawnLocation
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        [JsonIgnore]
        public Vector3 LocalPosition
        {
            get => new(X, Y, Z);

            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        public Vector3 GetWorldPosition(LevelActor actor)
        {
            Matrix4x4 rotation =
                Matrix4x4.CreateRotationY(actor.Yaw);

            return actor.Position +
                   Vector3.Transform(LocalPosition, rotation);
        }

        public void SetWorldPosition(
            LevelActor actor,
            Vector3 position)
        {
            Matrix4x4 inverseRotation =
                Matrix4x4.CreateRotationY(-actor.Yaw);

            LocalPosition = Vector3.Transform(
                position - actor.Position,
                inverseRotation);
        }
    }
}
