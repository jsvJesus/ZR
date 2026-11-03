using System.Numerics;
using Avalonia.Controls;
using Avalonia.Media;
using Editor.Viewport;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Scene;

namespace Editor.Objects;

/// <summary>A read-only, independent viewport; never binds a level or edits source data.</summary>
internal sealed class ObjectPreviewWindow : Window
{
    private readonly DxViewportHost _viewport = new() { ShowGrid = true };
    private readonly TextBlock _info = new() { Margin = new(12), TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _warning = new() { Margin = new(12), TextWrapping = TextWrapping.Wrap };
    private readonly Avalonia.Threading.DispatcherTimer _warningTimer = new();
    private SceneData? _scene;
    private SceneModel? _model;
    public event Action<string>? PreviewFailed;

    public ObjectPreviewWindow()
    {
        Title = "Предпросмотр модели";
        Width = 850; Height = 640; MinWidth = 520; MinHeight = 380;
        Background = new SolidColorBrush(Color.Parse("#17191E"));
        Foreground = Brushes.White;
        var root = new Grid { RowDefinitions = new("Auto,*,Auto") };
        root.Children.Add(_info);
        Grid.SetRow(_viewport, 1); root.Children.Add(_viewport);
        Grid.SetRow(_warning, 2); root.Children.Add(_warning);
        Content = root;
        _viewport.RendererError += error => { _warning.Text = error; PreviewFailed?.Invoke(error); };
        _warningTimer.Interval = TimeSpan.FromMilliseconds(500);
        _warningTimer.Tick += (_, _) =>
        {
            if (_scene != null && !_scene.Warnings.IsEmpty)
                _warning.Text = string.Join("\n", _scene.Warnings.TakeLast(4));
        };
        Opened += (_, _) => _warningTimer.Start();
        Closed += (_, _) => { _warningTimer.Stop(); _scene = null; _model = null; };
    }

    public void ClearModel(string message)
    {
        _model = null; _scene = null;
        _info.Text = message; _warning.Text = "";
        _viewport.ClearObjectPreview();
    }

    public void SetModel(AssetPaths assets, SceneModel model)
    {
        if (ReferenceEquals(_model, model)) return;
        var instance = new SceneInstance(model.File, Matrix4x4.Identity, Matrix4x4.Identity, model.Bounds, false)
        { Id = "preview", LocalBounds = model.Bounds, VisualLocalBounds = model.Bounds };
        var scene = new SceneData(assets, [instance]);
        scene.Models.Store(model);
        _viewport.SetObjectPreview(assets, scene);
        _scene = scene; _model = model;
        _info.Text = $"{model.File}\nПКМ + мышь — обзор · WASD / Space / Shift — камера · колесо — скорость";
        _warning.Text = "";
        float radius = Math.Max(model.Bounds.Radius, 0.05f);
        // Fit against the narrower viewport axis as well as the vertical FOV.
        float aspect = (float)Math.Max(0.1, Width / Math.Max(1, Height - 100));
        float halfAngle = Math.Min(0.525f, MathF.Atan(MathF.Tan(0.525f) * aspect));
        float distance = radius / MathF.Sin(halfAngle) * 1.25f;
        const float yaw = -0.55f, pitch = -0.23f;
        var look = new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch));
        var position = model.Bounds.Center - look * distance;
        _viewport.SetCamera(new(position.X, position.Y, position.Z, yaw, pitch, Math.Clamp(radius, 1, 5000)));
    }
}
