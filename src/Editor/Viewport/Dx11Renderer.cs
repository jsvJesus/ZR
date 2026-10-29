using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using Editor.Rendering.Shaders;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Editor.Viewport.Terrain;
using Editor.Viewport.Characters;
using ZoneReborn.World.Characters;
using ZoneReborn.World.Characters.Locomotion;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Levels.Models;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Editor.Viewport
{
    public sealed class Dx11Renderer : IDisposable
    {
        private Device _device = null!;
        private DeviceContext _context = null!;
        private SwapChain _swapChain = null!;
        private RenderTargetView? _target;
        private Texture2D? _depthTexture;
        private DepthStencilView? _depthTarget;
        private TerrainRenderer? _terrain;
        private CharacterRenderer? _character;

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

            CreateRenderTarget();
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
            using var backBuffer =
                Texture2D.FromSwapChain<Texture2D>(
                    _swapChain,
                    0);

            _target = new RenderTargetView(
                _device,
                backBuffer);

            _depthTexture = new Texture2D(_device, new Texture2DDescription
            {
                Width = _width,
                Height = _height,
                ArraySize = 1,
                MipLevels = 1,
                Format = Format.D32_Float,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil
            });
            _depthTarget = new DepthStencilView(_device, _depthTexture);
        }

        public void SetLevel(LoadedLevel level, AssetPaths assets)
        {
            var terrain = new TerrainRenderer(_device, _context, level, assets);
            _context.ClearState();
            _terrain?.Dispose();
            _terrain = terrain;
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

            _target?.Dispose();
            _target = null;
            _depthTarget?.Dispose();
            _depthTarget = null;
            _depthTexture?.Dispose();
            _depthTexture = null;

            _swapChain.ResizeBuffers(
                2,
                width,
                height,
                Format.R8G8B8A8_UNorm,
                SwapChainFlags.None);

            _width = width;
            _height = height;

            CreateRenderTarget();
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

            if (_target == null)
            {
                return;
            }

            _context.OutputMerger.SetRenderTargets(_depthTarget, _target);
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

            var projection = CreateProjectionMatrix(
                playing ? 0.78f : 1.05f,
                (float)_width / _height,
                playing ? 0.08f : 0.5f,
                _terrain?.FarPlane ?? 1000f);

            var viewProjection = view * projection;
            _terrain?.Render(camera, viewProjection);

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

            _character?.Render(viewProjection);
            _swapChain.Present(1, PresentFlags.None);
        }

        private static Matrix4x4 CreateViewMatrix(FreeCamera camera)
        {
            var forward = Vector3.Normalize(camera.LookDirection);

            var right = Vector3.Normalize(
                Vector3.Cross(Vector3.UnitY, forward));

            var up = Vector3.Cross(forward, right);
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

            _terrain?.Dispose();
            _character?.Dispose();
            _depthTarget?.Dispose();
            _depthTexture?.Dispose();
            _target?.Dispose();
            _cameraBuffer?.Dispose();
            _gridBuffer?.Dispose();
            _inputLayout?.Dispose();
            _pixelShader?.Dispose();
            _vertexShader?.Dispose();

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
