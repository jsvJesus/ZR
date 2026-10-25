using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Editor.Viewport
{
    public sealed class DxViewportHost : NativeControlHost
    {
        private const uint WsChild = 0x40000000;
        private const uint WsVisible = 0x10000000;
        private const uint SsNotify = 0x00000100;

        private const uint WmSetFocus = 0x0007;
        private const uint WmKillFocus = 0x0008;
        private const uint WmKeyDown = 0x0100;
        private const uint WmKeyUp = 0x0101;
        private const uint WmMouseMove = 0x0200;
        private const uint WmLeftDown = 0x0201;
        private const uint WmRightDown = 0x0204;
        private const uint WmRightUp = 0x0205;
        private const uint WmMouseWheel = 0x020A;
        private const uint WmCaptureChanged = 0x0215;

        private const int GwlpWndProc = -4;

        private const int KeyW = 0x57;
        private const int KeyA = 0x41;
        private const int KeyS = 0x53;
        private const int KeyD = 0x44;
        private const int KeySpace = 0x20;
        private const int KeyShift = 0x10;

        private readonly FreeCamera _camera = new();
        private readonly HashSet<int> _pressedKeys = new();
        private readonly DispatcherTimer _timer = new();
        private readonly WindowProcedure _windowProcedure;

        private Dx11Renderer? _renderer;

        private nint _windowHandle;
        private nint _previousProcedure;

        private long _lastFrameTime;
        private int _frameCounter;
        private int _mouseX;
        private int _mouseY;

        private bool _rotating;

        public event Action<CameraPose>? CameraChanged;
        public event Action<string>? RendererError;

        public bool ShowGrid { get; set; } = true;

        public CameraPose CameraPose => _camera.Pose;

        public float CameraSpeed
        {
            get => _camera.Speed;
            set => _camera.Speed = Math.Clamp(value, 1f, 100f);
        }

        public DxViewportHost()
        {
            _windowProcedure = ProcessWindowMessage;

            _timer.Interval = TimeSpan.FromMilliseconds(16);
            _timer.Tick += OnFrame;
        }

        protected override IPlatformHandle CreateNativeControlCore(
            IPlatformHandle parent)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException(
                    "DirectX 11 Viewport поддерживает только Windows.");
            }

            _windowHandle = CreateWindowEx(
                0,
                "STATIC",
                "",
                // STATIC returns HTTRANSPARENT without SS_NOTIFY, bypassing our mouse handlers.
                WsChild | WsVisible | SsNotify,
                0,
                0,
                1,
                1,
                parent.Handle,
                nint.Zero,
                nint.Zero,
                nint.Zero);

            if (_windowHandle == nint.Zero)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            try
            {
                nint procedurePointer =
                    Marshal.GetFunctionPointerForDelegate(
                        _windowProcedure);

                _previousProcedure = SetWindowLongPtr(
                    _windowHandle,
                    GwlpWndProc,
                    procedurePointer);

                if (_previousProcedure == nint.Zero)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());
                }

                _renderer = new Dx11Renderer(_windowHandle);

                _lastFrameTime = Stopwatch.GetTimestamp();
                _timer.Start();
            }
            catch (Exception ex)
            {
                RendererError?.Invoke(ex.Message);

                _timer.Stop();
                _renderer?.Dispose();
                _renderer = null;

                if (_previousProcedure != nint.Zero)
                {
                    SetWindowLongPtr(
                        _windowHandle,
                        GwlpWndProc,
                        _previousProcedure);
                }

                DestroyWindow(_windowHandle);

                _windowHandle = nint.Zero;
                _previousProcedure = nint.Zero;

                throw;
            }

            return new PlatformHandle(_windowHandle, "HWND");
        }

        protected override void DestroyNativeControlCore(
            IPlatformHandle control)
        {
            _timer.Stop();
            _pressedKeys.Clear();

            if (_rotating)
            {
                ReleaseCapture();
                _rotating = false;
            }

            _renderer?.Dispose();
            _renderer = null;

            if (_previousProcedure != nint.Zero)
            {
                SetWindowLongPtr(
                    control.Handle,
                    GwlpWndProc,
                    _previousProcedure);

                _previousProcedure = nint.Zero;
            }

            DestroyWindow(control.Handle);
            _windowHandle = nint.Zero;
        }

        private void OnFrame(object? sender, EventArgs e)
        {
            if (_renderer == null ||
                _windowHandle == nint.Zero)
            {
                return;
            }

            long currentTime = Stopwatch.GetTimestamp();

            float deltaTime = (float)Math.Clamp(
                Stopwatch.GetElapsedTime(
                    _lastFrameTime,
                    currentTime).TotalSeconds,
                0.0,
                0.08);

            _lastFrameTime = currentTime;

            try
            {
                _camera.Move(
                    IsPressed(KeyW),
                    IsPressed(KeyS),
                    IsPressed(KeyA),
                    IsPressed(KeyD),
                    IsPressed(KeySpace),
                    IsPressed(KeyShift),
                    deltaTime);

                if (GetClientRect(
                    _windowHandle,
                    out NativeRect rect))
                {
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;

                    if (width > 0 && height > 0)
                    {
                        _renderer.Render(
                            _camera,
                            width,
                            height,
                            ShowGrid);
                    }
                }

                _frameCounter++;

                if (_frameCounter % 4 == 0)
                {
                    CameraChanged?.Invoke(_camera.Pose);
                }
            }
            catch (Exception ex)
            {
                _timer.Stop();
                RendererError?.Invoke(ex.Message);
            }
        }

        private bool IsPressed(int key)
        {
            return _pressedKeys.Contains(key);
        }

        public void SetCamera(CameraPose pose)
        {
            _camera.Restore(pose);
            _pressedKeys.Clear();

            CameraChanged?.Invoke(_camera.Pose);
        }

        private nint ProcessWindowMessage(
            nint window,
            uint message,
            nint wParam,
            nint lParam)
        {
            switch (message)
            {
                case WmLeftDown:
                    SetFocus(window);
                    return nint.Zero;

                case WmRightDown:
                    SetFocus(window);
                    SetCapture(window);

                    _rotating = true;
                    _mouseX = ReadX(lParam);
                    _mouseY = ReadY(lParam);

                    return nint.Zero;

                case WmRightUp:
                    _rotating = false;
                    ReleaseCapture();
                    return nint.Zero;

                case WmCaptureChanged:
                    _rotating = false;
                    break;

                case WmMouseMove:
                    HandleMouseMove(lParam);
                    return nint.Zero;

                case WmMouseWheel:
                    int wheelDelta = unchecked(
                        (short)((wParam.ToInt64() >> 16) & 0xFFFF));

                    _camera.ChangeSpeed(wheelDelta);
                    CameraChanged?.Invoke(_camera.Pose);

                    return nint.Zero;

                case WmKeyDown:
                    if (IsMovementKey((int)wParam))
                    {
                        _pressedKeys.Add((int)wParam);
                        return nint.Zero;
                    }

                    break;

                case WmKeyUp:
                    if (IsMovementKey((int)wParam))
                    {
                        _pressedKeys.Remove((int)wParam);
                        return nint.Zero;
                    }

                    break;

                case WmKillFocus:
                    _pressedKeys.Clear();

                    if (_rotating)
                    {
                        _rotating = false;
                        ReleaseCapture();
                    }

                    break;

                case WmSetFocus:
                    _pressedKeys.Clear();
                    break;
            }

            return CallWindowProc(
                _previousProcedure,
                window,
                message,
                wParam,
                lParam);
        }

        private void HandleMouseMove(nint lParam)
        {
            int x = ReadX(lParam);
            int y = ReadY(lParam);

            if (_rotating)
            {
                _camera.Rotate(
                    x - _mouseX,
                    y - _mouseY);
            }

            _mouseX = x;
            _mouseY = y;
        }

        private static bool IsMovementKey(int key)
        {
            return key == KeyW ||
                   key == KeyA ||
                   key == KeyS ||
                   key == KeyD ||
                   key == KeySpace ||
                   key == KeyShift;
        }

        private static int ReadX(nint lParam)
        {
            return unchecked((short)(
                lParam.ToInt64() & 0xFFFF));
        }

        private static int ReadY(nint lParam)
        {
            return unchecked((short)(
                (lParam.ToInt64() >> 16) & 0xFFFF));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate nint WindowProcedure(
            nint window,
            uint message,
            nint wParam,
            nint lParam);

        [DllImport(
            "user32.dll",
            EntryPoint = "CreateWindowExW",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        private static extern nint CreateWindowEx(
            uint extendedStyle,
            string className,
            string windowName,
            uint style,
            int x,
            int y,
            int width,
            int height,
            nint parent,
            nint menu,
            nint instance,
            nint parameter);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(nint window);

        [DllImport(
            "user32.dll",
            EntryPoint = "SetWindowLongPtrW",
            SetLastError = true)]
        private static extern nint SetWindowLongPtr(
            nint window,
            int index,
            nint value);

        [DllImport(
            "user32.dll",
            EntryPoint = "CallWindowProcW")]
        private static extern nint CallWindowProc(
            nint previousProcedure,
            nint window,
            uint message,
            nint wParam,
            nint lParam);

        [DllImport("user32.dll")]
        private static extern nint SetFocus(nint window);

        [DllImport("user32.dll")]
        private static extern nint SetCapture(nint window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetClientRect(
            nint window,
            out NativeRect rect);
    }
}
