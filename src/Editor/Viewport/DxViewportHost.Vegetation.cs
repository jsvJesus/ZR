using System.Numerics;
using ZoneReborn.World.Vegetation;
using ZoneReborn.World.Scene;

namespace Editor.Viewport
{
    public sealed partial class DxViewportHost
    {
        private TreeAsset? _placementTree;
        private float _treeScale = 1, _treeYaw, _treePitch, _treeRoll, _treeHeightOffset;
        private bool _treeFollowTerrain = true, _treeEditingEnabled;
        private TreeInstance? _selectedTree;
        public bool TreeEditingEnabled
        {
            get => _treeEditingEnabled;
            set { if (!value) CancelTreeDrag(); _treeEditingEnabled = value; }
        }
        public event Action<TreeInstance>? TreePlaced;
        public event Action<TreeInstance?>? TreePicked;
        public event Action<TreeInstance>? TreeTransformed;
        public event Action? TreeDeleteRequested;

        public void SetTreePlacement(TreeAsset? tree, float scale = 1, float yaw = 0,
            float pitch = 0, float roll = 0, bool followTerrain = true, float heightOffset = 0)
        {
            _placementTree = tree; _treeScale = scale; _treeYaw = yaw; _treePitch = pitch; _treeRoll = roll;
            _treeFollowTerrain = followTerrain; _treeHeightOffset = heightOffset;
            if (tree != null && _level != null) _level.Vegetation.Assets[tree.File] = tree;
        }

        public void SelectTree(TreeInstance? tree)
        {
            if (!ReferenceEquals(tree, _selectedTree)) CancelTreeDrag();
            _selectedTree = tree;
            _renderer?.SelectTree(TreeEditingEnabled && !IsPlaying && !IsStarting ? tree?.Id : null);
        }

        private SceneInstance? TreeSelection()
        {
            if (!TreeEditingEnabled || IsPlaying || IsStarting || _selectedTree is not { } tree ||
                _level?.Vegetation is not { CanSave: true } scene || !scene.Instances.Contains(tree) ||
                !scene.Assets.TryGetValue(tree.Model, out var asset) || _renderer == null) return null;
            var world = _renderer.TreeWorld(tree);
            if (!Matrix4x4.Invert(world, out var inverse)) return null;
            var bounds = new SceneBounds(asset.Minimum, asset.Maximum);
            return new(tree.Model, world, inverse, bounds.Transform(world), false)
            {
                Id = tree.Id.ToString(), LocalBounds = bounds, VisualLocalBounds = bounds
            };
        }

        public void FocusTree(TreeInstance tree)
        {
            if (IsPlaying || IsStarting || IsTreeDragging || _level == null || !_level.Vegetation.Assets.TryGetValue(tree.Model, out var asset)) return;
            float height = Math.Max(2, asset.Maximum.Y - asset.Minimum.Y) * tree.Scale;
            float distance = Math.Clamp(height * 1.7f, 5, 500);
            float y = _renderer?.TreeWorld(tree).M42 ?? tree.Y;
            SetCamera(new(tree.X, y + height * 0.6f + distance * 0.35f, tree.Z - distance,
                0, -MathF.Atan(0.35f), Math.Clamp(distance, 8, 250)));
        }

        private void HandleTreePointer(int x, int y)
        {
            if (!TreeEditingEnabled || IsPlaying || IsStarting || _rotating || _renderer == null ||
                _level == null || !_level.Vegetation.CanSave || !TryObjectRay(x, y, out var direction)) return;
            bool placing = (GetKeyState(KeyControl) & 0x8000) != 0;
            try
            {
                if (!placing && BeginTreeDrag(x, y)) return;
                bool hitTerrain = _renderer.PickTerrain(_camera.Position, direction, out var ground);
                if (placing)
                {
                    var asset = _placementTree;
                    if (asset == null) { RendererError?.Invoke("Выберите модель растительности в каталоге или растение на сцене."); return; }
                    if (!hitTerrain) { RendererError?.Invoke("Курсор не попал в Terrain."); return; }
                    if (_level.Vegetation.Instances.Count >= 100_000) throw new InvalidOperationException("Достигнут лимит деревьев уровня.");
                    var tree = new TreeInstance { Model = asset.File, X = ground.X, Y = ground.Y + _treeHeightOffset, Z = ground.Z,
                        Scale = _treeScale, Yaw = _treeYaw, Pitch = _treePitch, Roll = _treeRoll,
                        FollowTerrain = _treeFollowTerrain, HeightOffset = _treeHeightOffset };
                    VegetationStorage.Validate(tree);
                    _level.Vegetation.Instances.Add(tree); _level.Vegetation.IsDirty = true;
                    SelectTree(tree);
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
