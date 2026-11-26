using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using Runtime.Rendering.Shaders;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Runtime.Viewport.Terrain;
using Runtime.Viewport.Characters;
using Runtime.Viewport.Vegetation;
using Runtime.Viewport.Scene;
using ZoneReborn.World.Characters;
using ZoneReborn.World.Characters.Locomotion;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using ZoneReborn.World.Scene;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Runtime.Viewport
{
    public sealed partial class Dx11Renderer : IDisposable
    {
        private Device _device = null!;
        private DeviceContext _context = null!;
        private SwapChain _swapChain = null!;
        private RenderTargetView? _target;
        private Texture2D? _depthTexture;
        private DepthStencilView? _depthTarget;
        private TerrainRenderer? _terrain;
        private CharacterRenderer? _character;
        private TreeRenderer? _trees;
        private SceneRenderer? _objects;
        private SceneSelectionRenderer? _objectSelection;
        private SceneGizmoRenderer? _objectGizmo;
        private SceneData? _scene;
        private bool _objectPreview;
        private string? _selectedObject;
        private ObjectGizmoMode _objectGizmoMode;
        private ObjectGizmoAxis _objectGizmoAxis;
        private bool _objectGizmoEnabled;

        private VertexShader _vertexShader = null!;
        private PixelShader _pixelShader = null!;
        private InputLayout _inputLayout = null!;
        private Buffer _gridBuffer = null!;
        private Buffer _cameraBuffer = null!;

        private int _width;
        private int _height;
        private int _vertexCount;

        public Dx11Renderer(nint windowHandle)
        {
            try
            {
                Initialize(windowHandle);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void Initialize(nint windowHandle)
        {
            _width = 1;
            _height = 1;

            var description = new SwapChainDescription
            {
                BufferCount = 2,
                ModeDescription = new ModeDescription(
                    _width,
                    _height,
                    new Rational(60, 1),
                    Format.R8G8B8A8_UNorm),

                IsWindowed = true,
                OutputHandle = windowHandle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            Device.CreateWithSwapChain(
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                description,
                out _device,
                out _swapChain);

            _context = _device.ImmediateContext;

            var shaders = new ShaderCompiler(AssetPaths.FromApplication());
            using var vertexCode = shaders.Compile("Viewport/Grid.hlsl", "VSMain", "vs_4_0");
            using var pixelCode = shaders.Compile("Viewport/Grid.hlsl", "PSMain", "ps_4_0");

            _vertexShader = new VertexShader(
                _device,
                vertexCode.Bytecode);

            _pixelShader = new PixelShader(
                _device,
                pixelCode.Bytecode);

            _inputLayout = new InputLayout(
                _device,
                vertexCode.Bytecode,
                [
                    new InputElement(
                        "POSITION",
                        0,
                        Format.R32G32B32_Float,
                        0,
                        0),

                    new InputElement(
                        "COLOR",
                        0,
                        Format.R32G32B32A32_Float,
                        12,
                        0)
                ]);

            var vertices = CreateGrid();

            _vertexCount = vertices.Length;

            _gridBuffer = Buffer.Create(
                _device,
                BindFlags.VertexBuffer,
                vertices);

            _cameraBuffer = new Buffer(
                _device,
                Utilities.SizeOf<Matrix4x4>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            _objectSelection = new(_device, _context, _vertexShader, _pixelShader, _inputLayout);
            _objectGizmo = new(_device, _context, _vertexShader, _pixelShader, _inputLayout);

            CreateRenderTarget();
            InitializePostProcessing();
            InitializeEnvironment();
        }

        private static GridVertex[] CreateGrid()
        {
            var vertices = new List<GridVertex>();

            const int extent = 100;

            var regular = new RawColor4(
                0.20f, 0.25f, 0.32f, 1f);

            var xAxis = new RawColor4(
                0.75f, 0.24f, 0.26f, 1f);

            var zAxis = new RawColor4(
                0.20f, 0.45f, 0.80f, 1f);

            for (int index = -extent; index <= extent; index++)
            {
                var verticalColor =
                    index == 0 ? zAxis : regular;

                var horizontalColor =
                    index == 0 ? xAxis : regular;

                vertices.Add(new GridVertex(
                    new Vector3(index, 0f, -extent),
                    verticalColor));

                vertices.Add(new GridVertex(
                    new Vector3(index, 0f, extent),
                    verticalColor));

                vertices.Add(new GridVertex(
                    new Vector3(-extent, 0f, index),
                    horizontalColor));

                vertices.Add(new GridVertex(
                    new Vector3(extent, 0f, index),
                    horizontalColor));
            }

            return vertices.ToArray();
        }

        private void CreateRenderTarget()
        {
            CreateRenderTargetResources();
        }

        public void SetLevel(LoadedLevel level, AssetPaths assets)
        {
            var terrain = new TerrainRenderer(_device, _context, level, assets);
            TreeRenderer trees;
            try
            {
                trees = new TreeRenderer(
                    _device,
                    _context,
                    assets,
                    level.Vegetation,
                    terrain);
            }
            catch
            {
                terrain.Dispose();
                throw;
            }

            SceneRenderer? objects = null;
            try
            {
                if (level.Objects != null) objects = new SceneRenderer(_device, _context, assets, level.Objects);
            }
            catch { trees.Dispose(); terrain.Dispose(); throw; }
            _context.ClearState();
            _terrain?.Dispose();
            _trees?.Dispose();
            _objects?.Dispose();
            _terrain = terrain;
            _trees = trees;
            _objects = objects;
            _objectPreview = false;
            _scene = level.Objects;
            _selectedObject = null;
            SetRoadSurface(level);
            SetGroundCover(level, assets);
            SetNpcLevel(level.Actors, assets);
            SetLootPreviewLevel(assets);
            ResetPostProcessHistory();
        }

        public void SetObjectPreview(AssetPaths assets, SceneData scene)
        {
            var objects = new SceneRenderer(_device, _context, assets, scene, objectPreview: true);
            _context.ClearState();
            _terrain?.Dispose();
            _terrain = null;
            _trees?.Dispose();
            _trees = null;
            _objects?.Dispose();
            _objects = objects;
            _scene = scene;
            _objectPreview = true;
            SetRoadSurface(null);
            SetGroundCover(null, null);
            SetNpcLevel(null, null);
            SetLootPreviewLevel(null);
            _selectedObject = null;
            ResetPostProcessHistory();
        }

        public void ClearObjectPreview()
        {
            if (!_objectPreview) return;
            _context.ClearState();
            _objects?.Dispose(); _objects = null;
            _scene = null; _selectedObject = null;
            ResetPostProcessHistory();
        }

        public bool PickObjectSurface(Vector3 origin, Vector3 direction, out Vector3 position)
        {
            position = default;
            if (!float.IsFinite(direction.LengthSquared()) || direction.LengthSquared() < 1e-12f) return false;
            direction = Vector3.Normalize(direction);
            float maximum = Math.Min(_terrain?.FarPlane ?? 1000, 1800);
            bool found = PickTerrain(origin, direction, out var ground);
            if (found)
            {
                float distance = Vector3.Distance(origin, ground);
                found = distance <= maximum;
                if (found) { maximum = distance; position = ground; }
            }
            if (_objects != null && _objects.Pick(origin, direction, maximum, out float objectDistance) != null)
            {
                position = origin + direction * objectDistance;
                found = true;
            }
            return found;
        }

        public void SelectObject(string? id) => _selectedObject = id;

        public float ObjectGizmoFarPlane => _terrain?.FarPlane ?? 1000f;

        public void SetObjectGizmo(ObjectGizmoMode mode, ObjectGizmoAxis axis, bool enabled)
        {
            _objectGizmoMode = mode;
            _objectGizmoAxis = axis;
            _objectGizmoEnabled = enabled;
        }

        public SceneInstance? PickObject(Vector3 origin, Vector3 direction)
        {
            if (_objects == null || !float.IsFinite(direction.LengthSquared()) ||
                direction.LengthSquared() < 1e-12f) return null;
            direction = Vector3.Normalize(direction);
            float maximum = Math.Min(_terrain?.FarPlane ?? 1000, 1800);
            if (PickTerrain(origin, direction, out var ground))
                maximum = Math.Min(maximum, Vector3.Distance(origin, ground) + 0.05f);
            return _objects.Pick(origin, direction, maximum);
        }

        public void SelectTree(Guid? id) { if (_trees != null) _trees.Selected = id; }
        public Matrix4x4 TreeWorld(ZoneReborn.World.Vegetation.TreeInstance tree) => TreeRenderer.World(tree, _terrain);

        public bool PickTerrain(Vector3 origin, Vector3 direction, out Vector3 position, bool includeHoles = false,
            float maximumDistance = float.PositiveInfinity)
        {
            position = default;
            return _terrain != null && _terrain.PickSurface(origin, direction, out position, includeHoles, maximumDistance);
        }

        public void SetCharacter(AssetPaths assets, CharacterAsset asset, CharacterLocomotion locomotion)
        {
            var character = new CharacterRenderer(_device, _context, assets, asset, locomotion);
            ClearCharacter();
            _character = character;
        }

        public void ClearCharacter()
        {
            _context.ClearState();
            _character?.Dispose();
            _character = null;
        }

        private void Resize(int width, int height)
        {
            if (width < 1 || height < 1)
            {
                return;
            }

            if (_width == width && _height == height)
            {
                return;
            }

            _context.ClearState();
            _firstPersonDepth?.ReleaseTargets();
            _postProcessManager?.ReleaseSizeDependentResources();
            DisposeRenderTargetResources();

            _swapChain.ResizeBuffers(
                2,
                width,
                height,
                Format.R8G8B8A8_UNorm,
                SwapChainFlags.None);

            _width = width;
            _height = height;

            CreateRenderTarget();
            ResetPostProcessHistory();
        }

        public void Render(
            FreeCamera camera,
            int width,
            int height,
            bool showGrid,
            bool playing = false)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            Resize(width, height);

            if (_target == null || _ambientTarget == null)
            {
                return;
            }

            _firstPersonDepthActive = false;
            _context.OutputMerger.SetRenderTargets(_depthTarget, _target, _ambientTarget);
            _context.ClearRenderTargetView(_ambientTarget, new RawColor4(0f, 0f, 0f, 0f));
            _context.ClearDepthStencilView(_depthTarget!, DepthStencilClearFlags.Depth, 1f, 0);

            _context.Rasterizer.SetViewport(
                0,
                0,
                _width,
                _height);

            _context.ClearRenderTargetView(
                _target,
                new RawColor4(
                    0.055f,
                    0.075f,
                    0.105f,
                    1f));

            var view = CreateViewMatrix(camera);

            float farPlane = _terrain?.FarPlane ?? 1000f;

            float nearPlane = playing
                ? 0.15f
                : 0.5f;

            if (_objectPreview && _scene?.Instances.FirstOrDefault() is { } preview)
            {
                float radius = Math.Max(preview.Bounds.Radius, 0.05f);
                farPlane = Math.Max(100, Vector3.Distance(camera.Position, preview.Bounds.Center) + radius * 2 + 50);
                nearPlane = Math.Clamp(radius * 0.001f, 0.001f, 0.5f);
            }
            var projection = CreateProjectionMatrix(
                playing ? _playFieldOfView : 1.05f,
                (float)_width / _height,
                nearPlane,
                farPlane);

            PrepareTaaJitter(
                camera,
                playing ? _playFieldOfView : 1.05f,
                playing);

            Matrix4x4 actorViewProjection =
                view * projection;

            ApplyTaaProjectionJitter(
                ref projection);

            var viewProjection = view * projection;

            AdvanceEnvironmentTime();

            Vector3 sunDirection = UpdateEnvironmentLighting(
                camera.Position,
                out float daylight);

            UpdateNpcPreviews(
                camera.Position,
                farPlane);

            RenderEnvironmentShadows(
                camera,
                sunDirection,
                farPlane,
                playing);

            RenderEnvironmentSky(
                camera,
                sunDirection,
                daylight,
                playing);

            _terrain?.Render(camera, viewProjection);

            RenderRoadModels(
                camera,
                viewProjection,
                farPlane);

            _objects?.Render(
                camera,
                viewProjection,
                farPlane);

            RenderLootPreview(
                camera,
                viewProjection,
                farPlane);

            _trees?.Render(
                camera,
                viewProjection,
                playing);

            RenderGroundCover(camera, viewProjection);

            _terrain?.RenderWater(
                viewProjection,
                camera.Position,
                sunDirection,
                _environmentTime);

            if (showGrid)
            {

                _context.UpdateSubresource(
                    ref viewProjection,
                    _cameraBuffer);

                _context.InputAssembler.InputLayout =
                    _inputLayout;

                _context.InputAssembler.PrimitiveTopology =
                    PrimitiveTopology.LineList;

                _context.InputAssembler.SetVertexBuffers(
                    0,
                    new VertexBufferBinding(
                        _gridBuffer,
                        Utilities.SizeOf<GridVertex>(),
                        0));

                _context.VertexShader.Set(_vertexShader);

                _context.VertexShader.SetConstantBuffer(
                    0,
                    _cameraBuffer);

                _context.PixelShader.Set(_pixelShader);

                _context.Draw(_vertexCount, 0);
            }

            if (!playing)
            {
                RenderTerrainBrushPreview(viewProjection);
            }

            var characterView = view;
            characterView.Translation = Vector3.Zero;
            _character?.Render(characterView * projection, camera.Position,
                playing ? _playFirstPersonBlend : 0);

            RenderNpcPreviews(
                characterView * projection,
                camera.Position);

            _objects?.RenderTransparent(camera, viewProjection);

            RenderLootPreviewTransparency(
                camera,
                viewProjection);

            RenderRoadTransparency(
                camera,
                viewProjection);

            RenderEnvironmentWeather(camera, viewProjection);

            // Finish the world before drawing FPS hands/weapons. Their color,
            // combined depth and reactive coverage must agree about occlusion;
            // late world glass must not be blended over the foreground weapon.
            if (playing)
            {
                RenderFirstPersonScene(
                    camera,
                    characterView,
                    nearPlane,
                    farPlane);
            }

            if (playing &&
                _showPlayCrosshair &&
                _playFirstPersonBlend < 0.8f)
            {
                RenderPlayCrosshair();
            }

            if (!playing && _selectedObject != null && _scene?.Find(_selectedObject) is { } selected)
            {
                if (ShowSelectedCollision)
                {
                    RenderSelectedCollision(selected, viewProjection);
                }

                _objectSelection?.Render(selected, viewProjection);
                if (_objectGizmoEnabled && _scene.CanSave &&
                    ObjectGizmoGeometry.TryCreateFrame(selected, camera, _width, _height, out var frame,
                        ObjectGizmoFarPlane))
                    _objectGizmo?.Render(frame, _objectGizmoMode, _objectGizmoAxis, viewProjection);
            }
            if (!playing) RenderTreeEditing(camera, viewProjection);

            if (!playing)
            {
                RenderRoadOverlay(viewProjection);
            }

            RenderPostProcessing(
                viewProjection,
                camera.Position,
                sunDirection);

            if (!playing &&
                _actorDocument != null &&
                _backBufferTarget != null)
            {
                _context.OutputMerger.SetRenderTargets(
                    null,
                    _backBufferTarget);

                _context.Rasterizer.SetViewport(
                    0,
                    0,
                    _width,
                    _height);

                RenderActorOverlay(
                    camera,
                    actorViewProjection);

                RenderActorGizmo(
                    camera,
                    actorViewProjection);
            }

            if (playing)
            {
                RenderHud();
            }

            _swapChain.Present(1, PresentFlags.None);
        }

        private static Matrix4x4 CreateViewMatrix(FreeCamera camera)
        {
            var forward = Vector3.Normalize(camera.LookDirection);

            var right = camera.RightDirection;
            var up = camera.UpDirection;
            var position = camera.Position;

            return new Matrix4x4
            {
                M11 = right.X,
                M12 = up.X,
                M13 = forward.X,
                M14 = 0f,

                M21 = right.Y,
                M22 = up.Y,
                M23 = forward.Y,
                M24 = 0f,

                M31 = right.Z,
                M32 = up.Z,
                M33 = forward.Z,
                M34 = 0f,

                M41 = -Vector3.Dot(right, position),
                M42 = -Vector3.Dot(up, position),
                M43 = -Vector3.Dot(forward, position),
                M44 = 1f
            };
        }

        private static Matrix4x4 CreateProjectionMatrix(
            float fieldOfView,
            float aspectRatio,
            float nearPlane,
            float farPlane)
        {
            float scaleY = 1f / MathF.Tan(fieldOfView * 0.5f);
            float scaleX = scaleY / aspectRatio;
            float depth = farPlane / (farPlane - nearPlane);

            return new Matrix4x4
            {
                M11 = scaleX,
                M22 = scaleY,
                M33 = depth,
                M34 = 1f,
                M43 = -nearPlane * depth
            };
        }

        public void Dispose()
        {
            _context?.ClearState();
            DisposeHud();
            DisposeEnvironment();

            _terrain?.Dispose();
            _terrainBrushOverlay?.Dispose();
            _trees?.Dispose();
            DisposeRoadModels();
            SetGroundCover(null, null);
            _objects?.Dispose();
            _collisionOverlay?.Dispose();
            _roadOverlay?.Dispose();
            _objectSelection?.Dispose();
            _objectGizmo?.Dispose();
            _character?.Dispose();
            DisposeNpcPreviews();
            DisposeLootPreview();
            DisposePostProcessing();
            DisposeRenderTargetResources();
            _cameraBuffer?.Dispose();
            _gridBuffer?.Dispose();
            _inputLayout?.Dispose();
            _pixelShader?.Dispose();
            _vertexShader?.Dispose();

            DisposeFirstPersonScene();

            _swapChain?.Dispose();
            _context?.Dispose();
            _device?.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct GridVertex
        {
            public readonly Vector3 Position;
            public readonly RawColor4 Color;

            public GridVertex(Vector3 position, RawColor4 color)
            {
                Position = position;
                Color = color;
            }
        }
    }
}
