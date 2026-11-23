using System;
using System.Numerics;

namespace Editor.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private int _taaSequenceIndex;

        private Vector2 _taaJitterPixels;
        private Vector3 _taaPreviousCameraPosition;
        private Vector3 _taaPreviousLookDirection;

        private float _taaPreviousFieldOfView;

        private bool _taaCameraRecorded;
        private bool _taaPreviousEnabled;
        private bool _taaPreviousFirstPerson;

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

            Vector3 direction = Vector3.Normalize(
                camera.LookDirection);

            bool cameraCut = false;

            if (_taaCameraRecorded)
            {
                float distanceSquared =
                    Vector3.DistanceSquared(
                        camera.Position,
                        _taaPreviousCameraPosition);

                float rotation =
                    Vector3.Dot(
                        direction,
                        _taaPreviousLookDirection);

                cameraCut =
                    distanceSquared > 9f ||
                    rotation < 0.94f ||
                    MathF.Abs(
                        fieldOfView -
                        _taaPreviousFieldOfView) > 0.04f;
            }

            if (cameraCut ||
                enabled != _taaPreviousEnabled ||
                firstPerson != _taaPreviousFirstPerson)
            {
                ResetPostProcessHistory();
                _taaSequenceIndex = 0;
            }

            _taaPreviousCameraPosition =
                camera.Position;

            _taaPreviousLookDirection =
                direction;

            _taaPreviousFieldOfView =
                fieldOfView;

            _taaPreviousEnabled = enabled;
            _taaPreviousFirstPerson = firstPerson;
            _taaCameraRecorded = true;

            if (!enabled)
            {
                _taaJitterPixels = Vector2.Zero;
                _taaSequenceIndex = 0;
                return;
            }

            int sampleIndex =
                _taaSequenceIndex % 8 + 1;

            _taaJitterPixels = new Vector2(
                Halton(sampleIndex, 2) - 0.5f,
                Halton(sampleIndex, 3) - 0.5f);

            _taaSequenceIndex++;
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
