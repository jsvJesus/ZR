using System;
using System.Collections.Generic;
using System.Numerics;
using ZoneReborn.World.Scene;

namespace Editor.Viewport.Environment;

internal sealed class WeatherOcclusion : IDisposable
{
    private const float CellSize = 0.4f;
    private const float SurfaceClearance = 0.05f;
    private const float BoundsPadding = 0.05f;
    private const int MaximumCachedColumns = 16384;
    private const int MaximumRaycastsPerFrame = 192;

    private readonly Dictionary<(int X, int Z), HeightSample> _heights = new();
    private readonly List<SceneBounds> _occluders = new();

    private SceneData? _scene;
    private float _rayBottom;
    private int _raycastsThisFrame;

    public void Prepare(
        SceneData? scene,
        Vector3 cameraPosition,
        float extent,
        float windSpeed)
    {
        SetScene(scene);

        _occluders.Clear();
        _raycastsThisFrame = 0;

        if (scene == null)
        {
            return;
        }

        if (scene.HasTransformPreview)
        {
            _heights.Clear();
        }

        float radius = extent +
                       Math.Clamp(MathF.Abs(windSpeed) * 5f, 0f, 150f) +
                       2f;

        var area = new SceneBounds(
            new Vector3(
                cameraPosition.X - radius,
                cameraPosition.Y - extent - 5f,
                cameraPosition.Z - radius),
            new Vector3(
                cameraPosition.X + radius,
                cameraPosition.Y + extent + 256f,
                cameraPosition.Z + radius));

        _rayBottom = cameraPosition.Y - extent - 5f;

        foreach (SceneInstance instance in scene.Query(area))
        {
            if (!instance.CollisionEnabled ||
                instance.PlayerCollisionOnly ||
                instance.IsEnvironmentCollision)
            {
                continue;
            }

            _occluders.Add(instance.Bounds);
        }

        if (_heights.Count > MaximumCachedColumns)
        {
            _heights.Clear();
        }
    }

    public bool Clip(ref Vector3 start, ref Vector3 end)
    {
        if (_scene == null || _occluders.Count == 0)
        {
            return true;
        }

        float x = (start.X + end.X) * 0.5f;
        float z = (start.Z + end.Z) * 0.5f;
        float lowest = MathF.Min(start.Y, end.Y);

        float candidateTop = float.NegativeInfinity;

        foreach (SceneBounds bounds in _occluders)
        {
            if (x < bounds.Minimum.X - BoundsPadding ||
                x > bounds.Maximum.X + BoundsPadding ||
                z < bounds.Minimum.Z - BoundsPadding ||
                z > bounds.Maximum.Z + BoundsPadding ||
                bounds.Maximum.Y < lowest - SurfaceClearance)
            {
                continue;
            }

            candidateTop = MathF.Max(
                candidateTop,
                bounds.Maximum.Y);
        }

        if (!float.IsFinite(candidateTop))
        {
            return true;
        }

        float surfaceHeight = SampleHeight(
            x,
            z,
            candidateTop);

        if (!float.IsFinite(surfaceHeight))
        {
            return true;
        }

        float upper = MathF.Max(start.Y, end.Y);

        if (surfaceHeight >= upper - SurfaceClearance)
        {
            return false;
        }

        if (surfaceHeight > lowest + SurfaceClearance)
        {
            float clippedHeight =
                surfaceHeight + SurfaceClearance;

            if (start.Y <= end.Y)
            {
                start.Y = clippedHeight;
            }
            else
            {
                end.Y = clippedHeight;
            }
        }

        return true;
    }

    private float SampleHeight(
        float x,
        float z,
        float candidateTop)
    {
        var key = (
            (int)MathF.Floor(x / CellSize),
            (int)MathF.Floor(z / CellSize));

        float rayTop = candidateTop + 0.1f;

        if (_heights.TryGetValue(
                key,
                out HeightSample cached) &&
            cached.Top >= rayTop &&
            cached.Bottom <= _rayBottom)
        {
            return cached.Height;
        }

        if (_raycastsThisFrame >= MaximumRaycastsPerFrame)
        {
            return float.NegativeInfinity;
        }

        _raycastsThisFrame++;

        Vector3 origin = new(
            (key.Item1 + 0.5f) * CellSize,
            rayTop,
            (key.Item2 + 0.5f) * CellSize);

        bool hit = _scene!.Collision.Raycast(
            origin,
            -Vector3.UnitY,
            MathF.Max(0.1f, rayTop - _rayBottom),
            out float distance);

        float height = hit
            ? rayTop - distance
            : float.NegativeInfinity;

        _heights[key] = new HeightSample(
            height,
            rayTop,
            _rayBottom);

        return height;
    }

    private void SetScene(SceneData? scene)
    {
        if (ReferenceEquals(_scene, scene))
        {
            return;
        }

        if (_scene != null)
        {
            _scene.Changed -= ClearCache;
        }

        _scene = scene;
        _heights.Clear();

        if (_scene != null)
        {
            _scene.Changed += ClearCache;
        }
    }

    private void ClearCache()
    {
        _heights.Clear();
    }

    public void Dispose()
    {
        SetScene(null);
        _occluders.Clear();
    }

    private readonly record struct HeightSample(
        float Height,
        float Top,
        float Bottom);
}
