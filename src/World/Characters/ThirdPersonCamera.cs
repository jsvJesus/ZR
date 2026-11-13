using System.Numerics;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Characters
{
    public sealed class ThirdPersonCamera
    {
        private const float MouseSensitivity = 0.003f;

        private const float DefaultDistance = 3.1f;
        private const float MinimumDistance = 1.65f;
        private const float MaximumDistance = 7.0f;
        private const float AimDistance = 1.8f;

        private const float DefaultShoulderOffset = 0.38f;
        private const float AimShoulderOffset = 0.52f;

        private const float FollowSpeed = 14f;
        private const float AimBlendSpeed = 12f;
        private const float ZoomSpeed = 9f;
        private const float ShoulderSpeed = 12f;
        private const float CollisionReturnSpeed = 5f;

        private const float CameraRadius = 0.16f;
        private const float TerrainClearance = 0.18f;
        private const float TerrainProbeStep = 0.1f;

        private Vector3 _smoothedPivot;
        private float _smoothedDistance = DefaultDistance;
        private float _smoothedShoulder = DefaultShoulderOffset;
        private float _clearRayDistance;
        private float _aimBlend;
        private bool _initialized;

        public float Yaw { get; private set; }

        public float Pitch { get; private set; } = -0.12f;

        public float Distance { get; private set; } = DefaultDistance;

        public Vector3 CameraPosition { get; private set; }

        public ThirdPersonCamera(float yaw)
        {
            Yaw = yaw;
        }

        public void Rotate(float dx, float dy)
        {
            if (!float.IsFinite(dx) || !float.IsFinite(dy))
            {
                return;
            }

            Yaw = MathF.IEEERemainder(
                Yaw + dx * MouseSensitivity,
                MathF.Tau
            );

            Pitch = Math.Clamp(
                Pitch - dy * MouseSensitivity,
                -1.15f,
                0.65f
            );
        }

        public void Zoom(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            Distance = Math.Clamp(
                Distance * MathF.Pow(1.12f, -delta / 120f),
                MinimumDistance,
                MaximumDistance
            );
        }

        public void Update(
            float deltaTime,
            Vector3 playerAnchor,
            TerrainSurface surface,
            float cameraHeight,
            bool aiming)
        {
            if (!float.IsFinite(deltaTime))
            {
                return;
            }

            deltaTime = Math.Clamp(deltaTime, 0f, 0.08f);

            Vector3 targetPivot = playerAnchor + new Vector3(
                0f,
                Math.Max(0.3f, cameraHeight),
                0f
            );

            float targetAimBlend = aiming ? 1f : 0f;
            bool firstUpdate = !_initialized;

            if (firstUpdate)
            {
                _smoothedPivot = targetPivot;
                _aimBlend = targetAimBlend;
            }
            else
            {
                float followFactor = Response(
                    FollowSpeed,
                    deltaTime
                );

                _smoothedPivot = Vector3.Lerp(
                    _smoothedPivot,
                    targetPivot,
                    followFactor
                );

                _aimBlend = Smooth(
                    _aimBlend,
                    targetAimBlend,
                    AimBlendSpeed,
                    deltaTime
                );
            }

            float targetDistance =
                Distance + (AimDistance - Distance) * _aimBlend;

            float targetShoulder =
                DefaultShoulderOffset +
                (AimShoulderOffset - DefaultShoulderOffset) *
                _aimBlend;

            if (firstUpdate)
            {
                _smoothedDistance = targetDistance;
                _smoothedShoulder = targetShoulder;
            }
            else
            {
                _smoothedDistance = Smooth(
                    _smoothedDistance,
                    targetDistance,
                    ZoomSpeed,
                    deltaTime
                );

                _smoothedShoulder = Smooth(
                    _smoothedShoulder,
                    targetShoulder,
                    ShoulderSpeed,
                    deltaTime
                );
            }

            float sinYaw = MathF.Sin(Yaw);
            float cosYaw = MathF.Cos(Yaw);
            float sinPitch = MathF.Sin(Pitch);
            float cosPitch = MathF.Cos(Pitch);

            Vector3 forward = new Vector3(
                sinYaw * cosPitch,
                sinPitch,
                cosYaw * cosPitch
            );

            Vector3 right = new Vector3(
                cosYaw,
                0f,
                -sinYaw
            );

            Vector3 up = Vector3.Normalize(
                Vector3.Cross(forward, right)
            );

            Vector3 desiredPosition =
                _smoothedPivot -
                forward * _smoothedDistance +
                right * _smoothedShoulder;

            Vector3 cameraOffset =
                desiredPosition - _smoothedPivot;

            float requestedRayDistance =
                cameraOffset.Length();

            if (requestedRayDistance <= 0.0001f)
            {
                CameraPosition = _smoothedPivot;
                _clearRayDistance = 0f;
                _initialized = true;
                return;
            }

            Vector3 rayDirection =
                cameraOffset / requestedRayDistance;

            float availableDistance = FindAvailableDistance(
                surface,
                _smoothedPivot,
                rayDirection,
                right,
                up,
                requestedRayDistance
            );

            if (firstUpdate)
            {
                _clearRayDistance = availableDistance;
            }
            else if (availableDistance < _clearRayDistance)
            {
                // A wall must never be crossed while easing inward.
                _clearRayDistance = availableDistance;
            }
            else
            {
                // Restore the normal distance gradually.
                _clearRayDistance = Smooth(
                    _clearRayDistance,
                    availableDistance,
                    CollisionReturnSpeed,
                    deltaTime
                );
            }

            _clearRayDistance = Math.Clamp(
                _clearRayDistance,
                0f,
                availableDistance
            );

            CameraPosition =
                _smoothedPivot +
                rayDirection * _clearRayDistance;

            _initialized = true;
        }

        private static float FindAvailableDistance(
            TerrainSurface surface,
            Vector3 origin,
            Vector3 direction,
            Vector3 right,
            Vector3 up,
            float maximum)
        {
            float available = maximum;

            // Center ray.
            available = Math.Min(
                available,
                surface.CameraDistance(
                    origin,
                    direction,
                    available
                )
            );

            // Approximate camera volume with four additional rays.
            available = Math.Min(
                available,
                surface.CameraDistance(
                    origin + right * CameraRadius,
                    direction,
                    available
                )
            );

            available = Math.Min(
                available,
                surface.CameraDistance(
                    origin - right * CameraRadius,
                    direction,
                    available
                )
            );

            available = Math.Min(
                available,
                surface.CameraDistance(
                    origin + up * CameraRadius,
                    direction,
                    available
                )
            );

            available = Math.Min(
                available,
                surface.CameraDistance(
                    origin - up * CameraRadius,
                    direction,
                    available
                )
            );

            // The terrain heightfield is not part of the scene raycast.
            available = Math.Min(
                available,
                FindTerrainDistance(
                    surface,
                    origin,
                    direction,
                    available
                )
            );

            available = Math.Min(
                available,
                FindTerrainDistance(
                    surface,
                    origin - up * CameraRadius,
                    direction,
                    available
                )
            );

            return Math.Clamp(available, 0f, maximum);
        }

        private static float FindTerrainDistance(
            TerrainSurface surface,
            Vector3 origin,
            Vector3 direction,
            float maximum)
        {
            if (maximum <= 0f)
            {
                return 0f;
            }

            int steps = Math.Max(
                1,
                (int)MathF.Ceiling(
                    maximum / TerrainProbeStep
                )
            );

            for (int i = 1; i <= steps; i++)
            {
                float distance =
                    maximum * i / steps;

                Vector3 point =
                    origin + direction * distance;

                if (!surface.TryGetHeight(
                        point.X,
                        point.Z,
                        out float terrainHeight))
                {
                    continue;
                }

                if (point.Y >= terrainHeight + TerrainClearance)
                {
                    continue;
                }

                return Math.Max(
                    0f,
                    distance - TerrainProbeStep
                );
            }

            return maximum;
        }

        private static float Smooth(
            float current,
            float target,
            float speed,
            float deltaTime)
        {
            return current +
                (target - current) *
                Response(speed, deltaTime);
        }

        private static float Response(
            float speed,
            float deltaTime)
        {
            return 1f - MathF.Exp(
                -speed * deltaTime
            );
        }
    }
}
