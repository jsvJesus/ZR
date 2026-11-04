using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Editor.Viewport
{
    public sealed partial class DxViewportHost
    {
        private const uint WmInput = 0x00FF;
        private const uint WmSetCursor = 0x0020;
        private bool _playMouseCaptured;
        private bool _playRawMouseRegistered;
        private bool _playAbsoluteMouse;
        private NativePoint _playAbsolutePosition;
        private NativePoint _playSavedCursorPosition;
        private bool _playSavedCursorValid;
        private NativeRect _playMouseBounds;
        private nint _playPreviousCursor;
        private NativeRawInputDevice? _previousRawMouse;

        private bool HasPlayMouseFocus => _windowHandle != nint.Zero && GetFocus() == _windowHandle
            && GetForegroundWindow() == GetAncestor(_windowHandle, 2);

        private void UpdatePlayMouseCapture()
        {
            if (!IsPlaying || IsCharacterEditing || !HasPlayMouseFocus || !GetClientRect(_windowHandle, out var bounds)
                || bounds.Right <= bounds.Left || bounds.Bottom <= bounds.Top)
            {
                if (_playMouseCaptured)
                {
                    _pressedKeys.Clear(); ReleasePlayerInput();
                }
                ReleasePlayMouse(false);
                return;
            }
            var start = new NativePoint { X = bounds.Left, Y = bounds.Top };
            var end = new NativePoint { X = bounds.Right, Y = bounds.Bottom };
            if (!ClientToScreen(_windowHandle, ref start) || !ClientToScreen(_windowHandle, ref end)) return;
            var screen = new NativeRect { Left = start.X, Top = start.Y, Right = end.X, Bottom = end.Y };
            if (!_playMouseCaptured)
            {
                RegisterPlayMouse();
                _playSavedCursorValid = GetCursorPos(out _playSavedCursorPosition);
                if (!ClipCursor(ref screen))
                {
                    int error = Marshal.GetLastWin32Error();
                    ReleasePlayMouse(false);
                    throw new Win32Exception(error);
                }
                _playMouseBounds = screen;
                _playAbsoluteMouse = false;
                _playMouseCaptured = true;
                _playPreviousCursor = SetCursor(nint.Zero);
            }
            else if (screen.Left != _playMouseBounds.Left || screen.Top != _playMouseBounds.Top
                || screen.Right != _playMouseBounds.Right || screen.Bottom != _playMouseBounds.Bottom)
            {
                if (ClipCursor(ref screen)) _playMouseBounds = screen;
            }
        }

        private void RegisterPlayMouse()
        {
            if (_playRawMouseRegistered) return;
            uint size = (uint)Marshal.SizeOf<NativeRawInputDevice>(), count = 0;
            if (GetRegisteredRawInputDevices(null, ref count, size) == uint.MaxValue)
            {
                int error = Marshal.GetLastWin32Error();
                // A null-buffer size query may report ERROR_INSUFFICIENT_BUFFER with the required count.
                if (error != 122) throw new Win32Exception(error);
            }
            _previousRawMouse = null;
            if (count > 0)
            {
                var devices = new NativeRawInputDevice[checked((int)count)];
                uint read = GetRegisteredRawInputDevices(devices, ref count, size);
                if (read == uint.MaxValue) throw new Win32Exception(Marshal.GetLastWin32Error());
                foreach (var device in devices.Take((int)read))
                    if (device.UsagePage == 1 && device.Usage == 2) _previousRawMouse = device;
            }
            // Keep legacy button/wheel messages for the editor UI; consume look deltas only from WM_INPUT.
            var mouse = new NativeRawInputDevice { UsagePage = 1, Usage = 2, Target = _windowHandle };
            if (!RegisterRawInputDevices([mouse], 1, size)) throw new Win32Exception(Marshal.GetLastWin32Error());
            _playRawMouseRegistered = true;
        }

        private void ReleasePlayMouse(bool restorePosition)
        {
            bool wasCaptured = _playMouseCaptured;
            bool restore = wasCaptured && restorePosition && HasPlayMouseFocus && _playSavedCursorValid;
            _playMouseCaptured = false; _playAbsoluteMouse = false;
            if (wasCaptured)
            {
                ClipCursor(nint.Zero);
                SetCursor(_playPreviousCursor != nint.Zero ? _playPreviousCursor : LoadCursor(nint.Zero, (nint)32512));
                if (restore) SetCursorPos(_playSavedCursorPosition.X, _playSavedCursorPosition.Y);
            }
            if (_playRawMouseRegistered)
            {
                // Raw mouse registration is per process; restore the previous recipient if there was one.
                var mouse = _previousRawMouse ?? new NativeRawInputDevice { UsagePage = 1, Usage = 2, Flags = 1 };
                RegisterRawInputDevices([mouse], 1, (uint)Marshal.SizeOf<NativeRawInputDevice>());
                _playRawMouseRegistered = false; _previousRawMouse = null;
            }
        }

        private void ReadPlayMouse(nint input)
        {
            if (!_playMouseCaptured || !IsPlaying || !HasPlayMouseFocus) return;
            uint size = (uint)Marshal.SizeOf<NativeRawInput>();
            uint read = GetRawInputData(input, 0x10000003, out var data, ref size, (uint)Marshal.SizeOf<NativeRawInputHeader>());
            if (read == uint.MaxValue || read < Marshal.SizeOf<NativeRawInputHeader>() + 24 || data.Header.Type != 0) return;
            float dx = data.Mouse.X, dy = data.Mouse.Y;
            if ((data.Mouse.Flags & 1) != 0)
            {
                var current = new NativePoint { X = data.Mouse.X, Y = data.Mouse.Y };
                if (!_playAbsoluteMouse) { _playAbsoluteMouse = true; _playAbsolutePosition = current; return; }
                bool virtualDesktop = (data.Mouse.Flags & 2) != 0;
                dx = (current.X - _playAbsolutePosition.X) * ((float)GetSystemMetrics(virtualDesktop ? 78 : 0) - 1) / 65535f;
                dy = (current.Y - _playAbsolutePosition.Y) * ((float)GetSystemMetrics(virtualDesktop ? 79 : 1) - 1) / 65535f;
                _playAbsolutePosition = current;
            }
            else _playAbsoluteMouse = false;
            if (IsPlayInspecting) _inspectionCamera.Rotate(dx, dy);
            else _orbit!.Rotate(dx, dy);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRawInputDevice
        {
            public ushort UsagePage, Usage;
            public uint Flags;
            public nint Target;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRawInputHeader
        {
            public uint Type, Size;
            public nint Device, WParam;
        }
        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct NativeRawMouse
        {
            [FieldOffset(0)] public ushort Flags;
            [FieldOffset(12)] public int X;
            [FieldOffset(16)] public int Y;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRawInput { public NativeRawInputHeader Header; public NativeRawMouse Mouse; }

        [DllImport("user32.dll")] private static extern nint GetFocus();
        [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
        private static extern nint DefWindowProc(nint window, uint message, nint wParam, nint lParam);
        [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
        [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
        [DllImport("user32.dll")] private static extern nint SetCursor(nint cursor);
        [DllImport("user32.dll", EntryPoint = "LoadCursorW")] private static extern nint LoadCursor(nint instance, nint cursor);
        [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ClientToScreen(nint window, ref NativePoint point);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out NativePoint point);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ClipCursor(ref NativeRect rect);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ClipCursor(nint rect);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool RegisterRawInputDevices(
            [In] NativeRawInputDevice[] devices, uint count, uint size);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRegisteredRawInputDevices([In, Out] NativeRawInputDevice[]? devices, ref uint count, uint size);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(nint input, uint command, out NativeRawInput data, ref uint size, uint headerSize);
    }
}
