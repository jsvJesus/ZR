using System;
using System.Numerics;

namespace Runtime.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private int _taaSequenceIndex;

        private Vector2 _taaJitterPixels;
        private Vector3 _taaPreviousCameraPosition;
        private Vector3 _taaPreviousLookDirection;
        private Vector3 _taaPreviousUpDirection;

        private float _taaPreviousFieldOfView;
        private float _taaPreviousJitterStrength = -1f;

        private bool _taaCameraRecorded;
        private bool _taaPreviousEnabled;
        private bool _taaPreviousFirstPerson;
        private bool _taaPreviousFirstPersonMeshes;
        private bool _taaPreviousPlaying;

        private void PrepareTaaJitter(
            FreeCamera camera,
            float fieldOfView,
            bool playing)
        {
            bool enabled =
                _environmentSettings.TaaEnabled &&
                !_objectPreview;

            bool firstPerson =
                playing &&
                _playFirstPersonBlend > 0.01f;
            bool firstPersonMeshes = playing && _playFirstPersonBlend >= 0.8f;

            float jitterStrength = Math.Clamp(
                _environmentSettings.TaaJitterStrength,
                0f,
                1f);

            Vector3 direction = Vector3.Normalize(
                camera.LookDirection);

            bool cameraCut = false;

            if (_taaCameraRecorded)
            {
                float distanceSquared =
                    Vector3.DistanceSquared(
                        camera.Position,
                        _taaPreviousCameraPosition);

                float directionDot = Vector3.Dot(
                    direction,
                    _taaPreviousLookDirection);

                cameraCut =
                    distanceSquared > 9f ||
                    directionDot < 0.94f ||
                    Vector3.Dot(camera.UpDirection, _taaPreviousUpDirection) < 0.94f ||
                    MathF.Abs(
                        fieldOfView -
                        _taaPreviousFieldOfView) > 0.04f;
            }

            bool settingsChanged =
                enabled != _taaPreviousEnabled ||
                firstPerson != _taaPreviousFirstPerson ||
                firstPersonMeshes != _taaPreviousFirstPersonMeshes ||
                playing != _taaPreviousPlaying ||
                MathF.Abs(
                    jitterStrength -
                    _taaPreviousJitterStrength) > 0.001f;

            if (cameraCut || settingsChanged)
            {
                ResetPostProcessHistory();
                _taaSequenceIndex = 0;
            }

            _taaPreviousCameraPosition =
                camera.Position;

            _taaPreviousLookDirection =
                direction;
            _taaPreviousUpDirection = camera.UpDirection;

            _taaPreviousFieldOfView =
                fieldOfView;

            _taaPreviousEnabled = enabled;
            _taaPreviousFirstPerson = firstPerson;
            _taaPreviousFirstPersonMeshes = firstPersonMeshes;
            _taaPreviousPlaying = playing;
            _taaPreviousJitterStrength = jitterStrength;
            _taaCameraRecorded = true;

            if (!enabled || jitterStrength <= 0f)
            {
                _taaJitterPixels = Vector2.Zero;
                _taaSequenceIndex = 0;
                return;
            }

            int sampleIndex =
                _taaSequenceIndex % 8 + 1;

            _taaJitterPixels = new Vector2(
                // Centre the actual eight-sample sequence, whose base-2 mean
                // is 0.4453125, rather than assuming an infinite Halton mean.
                Halton(sampleIndex, 2) - 0.4453125f,
                Halton(sampleIndex, 3) - 0.5f) *
                jitterStrength;

            _taaSequenceIndex = (_taaSequenceIndex + 1) & 7;
        }

        private void ApplyTaaProjectionJitter(
            ref Matrix4x4 projection)
        {
            if (!_taaPreviousEnabled)
            {
                return;
            }

            projection.M31 +=
                2f * _taaJitterPixels.X / _width;

            projection.M32 -=
                2f * _taaJitterPixels.Y / _height;
        }

        private static float Halton(
            int index,
            int numberBase)
        {
            float result = 0f;
            float fraction = 1f / numberBase;

            while (index > 0)
            {
                result +=
                    fraction * (index % numberBase);

                index /= numberBase;
                fraction /= numberBase;
            }

            return result;
        }
    }
}
