using System.Numerics;
using ZoneReborn.World.Scene;

namespace Editor.Viewport;

public sealed partial class DxViewportHost
{
    private string? _placementModel;

    public bool IsObjectPlacing => _placementModel != null;
    public string? ObjectPlacementModel => _placementModel;

    public event Action? ObjectPlacementChanged;
    public event Action<SceneInstance>? ObjectPlaced;

    public void SetObjectPlacement(string model)
    {
        if (!ObjectEditingEnabled ||
            IsPlaying ||
            IsStarting ||
            _level?.Objects is not { CanSave: true } scene)
        {
            return;
        }

        model = model.Replace('\\', '/');

        if (!scene.Models.TryGet(model, out _))
        {
            RendererError?.Invoke(
                "Модель ещё не загружена. Дождитесь завершения загрузки."
            );
            return;
        }

        if (_placementModel == model)
        {
            return;
        }

        CancelObjectDrag();

        _placementModel = model;
        _hoveredObjectAxis = ObjectGizmoAxis.None;

        ObjectPlacementChanged?.Invoke();
    }

    public void CancelObjectPlacement()
    {
        if (_placementModel == null)
        {
            return;
        }

        _placementModel = null;
        ObjectPlacementChanged?.Invoke();
    }

    private void PlaceObject(int x, int y)
    {
        if (!ObjectEditingEnabled ||
            IsPlaying ||
            IsStarting ||
            _level?.Objects is not { CanSave: true } scene ||
            _renderer == null)
        {
            return;
        }

        string? model = _placementModel;

        if (model == null && _selectedObjectId != null)
        {
            model = scene.Find(_selectedObjectId)?.Model;
        }

        if (model == null)
        {
            RendererError?.Invoke(
                "Выберите модель в каталоге или объект на сцене."
            );
            return;
        }

        if (_rotating || !TryObjectRay(x, y, out var direction))
        {
            return;
        }

        try
        {
            var geometry = scene.Models.Get(model);

            if (geometry == null)
            {
                throw new InvalidOperationException(
                    "Не удалось загрузить геометрию выбранной модели."
                );
            }

            scene.Physics.Get(model);

            if (!_renderer.PickObjectSurface(
                    _camera.Position,
                    direction,
                    out var point))
            {
                RendererError?.Invoke(
                    "Поверхность не найдена. Наведите курсор на Terrain или объект."
                );
                return;
            }

            point.Y -= geometry.Bounds.Minimum.Y;

            var world = Matrix4x4.CreateTranslation(point);
            var instance = scene.AddObject(model, world);

            SelectObject(instance.Id);
            ObjectPlaced?.Invoke(instance);
        }
        catch (Exception exception)
        {
            RendererError?.Invoke(
                $"Ошибка размещения объекта: {exception.Message}"
            );
        }
    }

    private bool TryObjectRay(int x, int y, out Vector3 direction)
    {
        direction = default;

        if (!GetClientRect(_windowHandle, out NativeRect rect))
        {
            return false;
        }

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        if (width <= 0 || height <= 0)
        {
            return false;
        }

        var forward = Vector3.Normalize(_camera.LookDirection);
        var right = Vector3.Normalize(
            Vector3.Cross(Vector3.UnitY, forward)
        );
        var up = Vector3.Cross(forward, right);

        float scale = MathF.Tan(1.05f * 0.5f);

        direction = Vector3.Normalize(
            forward +
            right * (
                (2f * x / width - 1) *
                width / height *
                scale
            ) +
            up * (
                (1 - 2f * y / height) *
                scale
            )
        );

        return true;
    }
}
