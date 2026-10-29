using System.Numerics;
using Editor.Viewport.Vegetation;
using ZoneReborn.World.Vegetation;

namespace Editor.Viewport
{
    public sealed partial class DxViewportHost
    {
        private TreeAsset? _placementTree;
        private float _treeScale = 1, _treeYaw;
        public bool TreeEditingEnabled { get; set; }
        public event Action<TreeInstance>? TreePlaced;
        public event Action<TreeInstance?>? TreePicked;

        public void SetTreePlacement(TreeAsset? tree, float scale = 1, float yaw = 0)
        {
            _placementTree = tree; _treeScale = scale; _treeYaw = yaw;
            if (tree != null && _level != null) _level.Vegetation.Assets[tree.File] = tree;
        }

        public void SelectTree(TreeInstance? tree) => _renderer?.SelectTree(tree?.Id);

        public void FocusTree(TreeInstance tree)
        {
            if (IsPlaying || IsStarting || _level == null || !_level.Vegetation.Assets.TryGetValue(tree.Model, out var asset)) return;
            float height = Math.Max(2, asset.Maximum.Y - asset.Minimum.Y) * tree.Scale;
            float distance = Math.Clamp(height * 1.7f, 5, 500);
            float y = _renderer?.TreeWorld(tree).M42 ?? tree.Y;
            SetCamera(new(tree.X, y + height * 0.6f + distance * 0.35f, tree.Z - distance,
                0, -MathF.Atan(0.35f), Math.Clamp(distance, 8, 250)));
        }

        private void HandleTreePointer(int x, int y)
        {
            if (!TreeEditingEnabled || IsPlaying || IsStarting || _renderer == null || _level == null || !_level.Vegetation.CanSave ||
                !GetClientRect(_windowHandle, out NativeRect rect)) return;
            int width = rect.Right - rect.Left, height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0) return;
            var forward = Vector3.Normalize(_camera.LookDirection);
            var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
            var up = Vector3.Cross(forward, right);
            float scale = MathF.Tan(1.05f * 0.5f);
            var direction = Vector3.Normalize(forward + right * ((2f * x / width - 1) * width / height * scale)
                + up * ((1 - 2f * y / height) * scale));
            try
            {
                bool hitTerrain = _renderer.PickTerrain(_camera.Position, direction, out var ground);
                if (_placementTree != null)
                {
                    if (!hitTerrain) { RendererError?.Invoke("Курсор не попал в Terrain."); return; }
                    if (_level.Vegetation.Instances.Count >= 100_000) throw new InvalidOperationException("Достигнут лимит деревьев уровня.");
                    var tree = new TreeInstance { Model = _placementTree.File, X = ground.X, Y = ground.Y, Z = ground.Z,
                        Scale = _treeScale, Yaw = _treeYaw };
                    VegetationStorage.Validate(tree);
                    _level.Vegetation.Instances.Add(tree); _level.Vegetation.IsDirty = true;
                    TreePlaced?.Invoke(tree);
                }
                else
                {
                    float nearest = hitTerrain ? Vector3.Distance(_camera.Position, ground) + 0.05f : float.MaxValue;
                    TreeInstance? picked = null;
                    foreach (var tree in _level.Vegetation.Instances)
                    {
                        if (!_level.Vegetation.Assets.TryGetValue(tree.Model, out var asset) ||
                            !Matrix4x4.Invert(_renderer.TreeWorld(tree), out var inverse)) continue;
                        var localOrigin = Vector3.Transform(_camera.Position, inverse);
                        var localDirection = Vector3.TransformNormal(direction, inverse);
                        if (ViewportPicking.Box(localOrigin, localDirection, asset.Minimum, asset.Maximum, out float distance, out _) && distance < nearest)
                        { nearest = distance; picked = tree; }
                    }
                    SelectTree(picked); TreePicked?.Invoke(picked);
                }
            }
            catch (Exception exception) { RendererError?.Invoke(exception.Message); }
        }
    }
}
