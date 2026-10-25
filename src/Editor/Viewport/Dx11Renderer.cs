using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Editor.Viewport
{
    public sealed class Dx11Renderer : IDisposable
    {
        private const string ShaderSource = """
            cbuffer CameraBuffer : register(b0)
            {
                row_major float4x4 ViewProjection;
            };

            struct VertexInput
            {
                float3 Position : POSITION;
                float4 Color : COLOR;
            };

            struct PixelInput
            {
                float4 Position : SV_POSITION;
                float4 Color : COLOR;
            };

            PixelInput VSMain(VertexInput input)
            {
                PixelInput output;
                output.Position = mul(
                    float4(input.Position, 1.0f),
                    ViewProjection);
                output.Color = input.Color;
                return output;
            }

            float4 PSMain(PixelInput input) : SV_TARGET
            {
                return input.Color;
            }
            """;

        private Device _device = null!;
        private DeviceContext _context = null!;
        private SwapChain _swapChain = null!;
        private RenderTargetView? _target;

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

            var vertexCode = ShaderBytecode.Compile(
                ShaderSource,
                "VSMain",
                "vs_4_0");

            var pixelCode = ShaderBytecode.Compile(
                ShaderSource,
                "PSMain",
                "ps_4_0");

            using (vertexCode.Bytecode)
            using (pixelCode.Bytecode)
            {
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
            }

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
            bool showGrid)
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

            _context.OutputMerger.SetRenderTargets(_target);

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

            if (showGrid)
            {
                var view = CreateViewMatrix(camera);

                var projection = CreateProjectionMatrix(
                    1.05f,
                    (float)_width / _height,
                    0.1f,
                    1000f);

                var viewProjection = view * projection;

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
