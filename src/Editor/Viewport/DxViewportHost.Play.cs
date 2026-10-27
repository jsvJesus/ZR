using System.Numerics;
using ZoneReborn.World.Characters;
using ZoneReborn.World.Terrain;

namespace Editor.Viewport;

public sealed partial class DxViewportHost
{
    private readonly FreeCamera _playCamera = new();
    private CancellationTokenSource? _playLoad;
    private ThirdPersonCamera? _orbit;
    private TerrainSurface? _surface;
    private Vector3 _playerPosition;
    private CameraPose _savedCamera;

    public bool IsPlaying => _orbit != null;
    public bool IsStarting => _playLoad != null;
    public event Action? PlayStateChanged;

    public async Task StartPlayAsync()
    {
        if (IsPlaying || IsStarting) return;
        var renderer = _renderer ?? throw new InvalidOperationException("Viewport ещё не готов.");
        var level = _level ?? throw new InvalidOperationException("Сначала откройте уровень.");
        var assets = _assets ?? throw new InvalidOperationException("Не найдены ресурсы проекта.");
        _savedCamera = _camera.Pose;
        _pressedKeys.Clear();
        using var cancellation = new CancellationTokenSource();
        _playLoad = cancellation;
        PlayStateChanged?.Invoke();
        try
        {
            var saved = _savedCamera;
            var loaded = await Task.Run(() =>
            {
                cancellation.Token.ThrowIfCancellationRequested();
                var surface = new TerrainSurface(level);
                if (!surface.TryGetHeight(saved.X, saved.Z, out float height))
                    throw new InvalidOperationException("Камера находится вне Terrain. Подлетите к месту появления на уровне.");
                var asset = CharacterLoader.Load(assets, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                return (Asset: asset, Surface: surface, Position: new Vector3(saved.X, height, saved.Z));
            }, cancellation.Token);
            if (cancellation.IsCancellationRequested || !ReferenceEquals(_playLoad, cancellation)
                || !ReferenceEquals(_renderer, renderer) || !ReferenceEquals(_level, level)) return;
            renderer.SetCharacter(assets, loaded.Asset, loaded.Position, saved.Yaw);
            _playerPosition = loaded.Position;
            _surface = loaded.Surface;
            _orbit = new ThirdPersonCamera(saved.Yaw);
            UpdatePlayCamera();
            if (_windowHandle != nint.Zero) SetFocus(_windowHandle);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch
        {
            if (ReferenceEquals(_playLoad, cancellation)) StopPlay();
            throw;
        }
        finally
        {
            if (ReferenceEquals(_playLoad, cancellation)) _playLoad = null;
            PlayStateChanged?.Invoke();
        }
    }

    public void StopPlay()
    {
        bool active = IsPlaying || IsStarting;
        _playLoad?.Cancel();
        _playLoad = null;
        _renderer?.ClearCharacter();
        _orbit = null;
        _surface = null;
        _pressedKeys.Clear();
        if (_rotating) { _rotating = false; ReleaseCapture(); }
        if (active)
        {
            _camera.Restore(_savedCamera);
            CameraChanged?.Invoke(_camera.Pose);
            PlayStateChanged?.Invoke();
        }
    }

    private void UpdatePlayCamera()
    {
        if (_orbit == null || _surface == null) return;
        var p = _orbit.Position(_playerPosition, _surface);
        _playCamera.Restore(new(p.X, p.Y, p.Z, _orbit.Yaw, _orbit.Pitch, _camera.Speed));
    }
}
