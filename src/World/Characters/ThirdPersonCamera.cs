using System.Numerics;
using ZoneReborn.World.Characters.Locomotion;
using ZoneReborn.World.Terrain;

namespace ZoneReborn.World.Characters
{
    public sealed class ThirdPersonCamera
    {
        private const float MouseSensitivity = 0.003f;

        private const float DefaultDistance = 3.1f;
        private const float AimDistance = 1.8f;
        private const float FirstPersonDistance = 0.04f;

        private const float DefaultShoulderOffset = 0.38f;
        private const float AimShoulderOffset = 0.52f;

        private const float ThirdPersonPivotLift = 0.4f;
        private const float FirstPersonEyeLift = 0.43f;

        private const float FollowSpeed = 14f;
        private const float AimBlendSpeed = 12f;
        private const float ViewBlendSpeed = 12f;
        private const float DistanceBlendSpeed = 15f;
        private const float ShoulderBlendSpeed = 15f;
        private const float CollisionReturnSpeed = 5f;

        private const float CameraRadius = 0.16f;
        private const float TerrainClearance = 0.18f;
        private const float TerrainProbeStep = 0.1f;

        private Vector3 _smoothedPivot;
        private float _smoothedDistance = DefaultDistance;
        private float _smoothedShoulder = DefaultShoulderOffset;
        private float _clearRayDistance;
        private float _aimBlend;
        private float _firstPersonBlend;
        private float _sprintBlend;
        private float _torsoRoll;
        private bool _initialized;

        public float Yaw { get; private set; }

        public float Pitch { get; private set; } = -0.16f;
        public float Roll => _torsoRoll * LocomotionSettings.FirstPersonRollMultiplier * _firstPersonBlend;

        public float Distance => DefaultDistance;

        public bool IsFirstPerson { get; private set; }

        public float FirstPersonBlend => _firstPersonBlend;

        public float AimBlend => _aimBlend;

        public float FieldOfView
        {
            get
            {
                const float thirdPersonFov = 0.78f;
                const float thirdPersonAimFov = 0.74f;
                // Restore the actual FPS projection used before the renderer's
                // 1.1-radian clamp was removed. Keep this explicit at the source.
                const float firstPersonFov = 1.1f;
                const float firstPersonAimFov = 0.75f;
                const float firstPersonSprintFov = firstPersonFov;

                float thirdPerson =
                    thirdPersonFov +
                    (thirdPersonAimFov - thirdPersonFov) * _aimBlend;

                float firstPerson =
                    firstPersonFov +
                    (firstPersonAimFov - firstPersonFov) * _aimBlend;
                firstPerson += (firstPersonSprintFov - firstPersonFov) * _sprintBlend * (1 - _aimBlend);

                return thirdPerson +
                       (firstPerson - thirdPerson) * _firstPersonBlend;
            }
        }

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

            // Mouse motion is expressed in the rolled screen basis. At zero
            // roll this is the original yaw += dx, pitch -= dy behaviour.
            var motion = Vector2.Transform(new Vector2(dx, -dy), Matrix3x2.CreateRotation(Roll));
            Yaw = MathF.IEEERemainder(
                Yaw + motion.X * MouseSensitivity,
                MathF.Tau
            );

            Pitch = Math.Clamp(
                Pitch + motion.Y * MouseSensitivity,
                -1.15f,
                0.65f
            );
        }

        public void Zoom(int delta)
        {
            // The TPS camera has a fixed distance.
        }

        public void ToggleView()
        {
            SetFirstPerson(!IsFirstPerson);
        }

        public void SetFirstPerson(bool enabled)
        {
            IsFirstPerson = enabled;
        }

        public void SetTorsoRoll(float angle)
        {
            _torsoRoll = float.IsFinite(angle)
                ? Math.Clamp(angle, -LocomotionSettings.MaximumTorsoRoll, LocomotionSettings.MaximumTorsoRoll)
                : 0;
        }

        public void Update(
            float deltaTime,
            Vector3 playerAnchor,
            TerrainSurface surface,
            float cameraHeight,
            bool aiming,
            Vector3? firstPersonEye = null,
            Vector3? firstPersonAimEye = null,
            bool sprinting = false)
        {
            if (!float.IsFinite(deltaTime))
            {
                return;
            }

            deltaTime = Math.Clamp(deltaTime, 0f, 0.08f);

            bool firstUpdate = !_initialized;

            float targetFirstPersonBlend = IsFirstPerson
                ? 1f
                : 0f;

            float targetAimBlend = aiming
                ? 1f
                : 0f;

            _sprintBlend = Smooth(_sprintBlend, sprinting ? 1f : 0f, AimBlendSpeed, deltaTime);

            if (firstUpdate)
            {
                _firstPersonBlend = targetFirstPersonBlend;
                _aimBlend = targetAimBlend;
            }
            else
            {
                _firstPersonBlend = Smooth(
                    _firstPersonBlend,
                    targetFirstPersonBlend,
                    ViewBlendSpeed,
                    deltaTime
                );

                _aimBlend = Smooth(
                    _aimBlend,
                    targetAimBlend,
                    AimBlendSpeed,
                    deltaTime
                );
            }

            float pivotLift = ThirdPersonPivotLift +
                (FirstPersonEyeLift - ThirdPersonPivotLift) *
                _firstPersonBlend;

            Vector3 targetPivot = playerAnchor + new Vector3(
                0f,
                MathF.Max(0.3f, cameraHeight) + pivotLift,
                0f
            );

            if (firstPersonEye is { } eye)
            {
                var fpsPivot = Vector3.Lerp(eye, firstPersonAimEye ?? eye, _aimBlend);
                targetPivot = Vector3.Lerp(targetPivot, fpsPivot, _firstPersonBlend);
            }

            if (firstUpdate)
            {
                _smoothedPivot = targetPivot;
            }
            else
            {
                _smoothedPivot = Vector3.Lerp(
                    _smoothedPivot,
                    targetPivot,
                    Response(FollowSpeed, deltaTime)
                );
            }

            // FPS follows the actual player position.
            // TPS keeps smooth camera following.
            Vector3 pivot = Vector3.Lerp(
                _smoothedPivot,
                targetPivot,
                _firstPersonBlend
            );

            float thirdPersonDistance =
                DefaultDistance +
                (AimDistance - DefaultDistance) * _aimBlend;

            float thirdPersonShoulder =
                DefaultShoulderOffset +
                (AimShoulderOffset - DefaultShoulderOffset) *
                _aimBlend;

            float targetDistance =
                thirdPersonDistance +
                ((firstPersonEye.HasValue ? 0f : FirstPersonDistance) - thirdPersonDistance) *
                _firstPersonBlend;

            float targetShoulder =
                thirdPersonShoulder *
                (1f - _firstPersonBlend);

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
                    DistanceBlendSpeed,
                    deltaTime
                );

                _smoothedShoulder = Smooth(
                    _smoothedShoulder,
                    targetShoulder,
                    ShoulderBlendSpeed,
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

            var orientation = Matrix4x4.CreateRotationZ(Roll) *
                Matrix4x4.CreateRotationX(-Pitch) * Matrix4x4.CreateRotationY(Yaw);
            Vector3 right = Vector3.TransformNormal(Vector3.UnitX, orientation);

            Vector3 up = Vector3.Normalize(
                Vector3.Cross(forward, right)
            );

            Vector3 desiredPosition =
                pivot -
                forward * _smoothedDistance +
                right * _smoothedShoulder;

            if (_firstPersonBlend >= 0.999f && firstPersonEye.HasValue)
            {
                CameraPosition = targetPivot;
                _clearRayDistance = 0;
                _initialized = true;
                return;
            }

            Vector3 offset = desiredPosition - pivot;
            float maximumDistance = offset.Length();

            if (maximumDistance < 0.0001f)
            {
                CameraPosition = pivot;
                _clearRayDistance = 0f;
                _initialized = true;
                return;
            }

            Vector3 direction = offset / maximumDistance;

            float availableDistance = FindAvailableDistance(
                surface,
                pivot,
                direction,
                right,
                up,
                maximumDistance
            );

            if (firstUpdate ||
                availableDistance < _clearRayDistance)
            {
                _clearRayDistance = availableDistance;
            }
            else
            {
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
                pivot + direction * _clearRayDistance;

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

            available = MathF.Min(
                available,
                surface.CameraDistance(
                    origin,
                    direction,
                    maximum
                )
            );

            available = MathF.Min(
                available,
                surface.CameraDistance(
                    origin + right * CameraRadius,
                    direction,
                    maximum
                )
            );

            available = MathF.Min(
                available,
                surface.CameraDistance(
                    origin - right * CameraRadius,
                    direction,
                    maximum
                )
            );

            available = MathF.Min(
                available,
                surface.CameraDistance(
                    origin + up * CameraRadius,
                    direction,
                    maximum
                )
            );

            available = MathF.Min(
                available,
                surface.CameraDistance(
                    origin - up * CameraRadius,
                    direction,
                    maximum
                )
            );

            available = MathF.Min(
                available,
                FindTerrainDistance(
                    surface,
                    origin,
                    direction,
                    available
                )
            );

            available = MathF.Min(
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
                float distance = maximum * i / steps;
                Vector3 point = origin + direction * distance;

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

                return MathF.Max(
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
            return 1f - MathF.Exp(-speed * deltaTime);
        }
    }
}
