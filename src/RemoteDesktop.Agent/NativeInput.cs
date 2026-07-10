using System.Runtime.InteropServices;

namespace RemoteDesktop.Agent;

internal static class NativeInput
{
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const uint MouseEventRightDown = 0x0008;
    private const uint MouseEventRightUp = 0x0010;
    private const uint MouseEventMiddleDown = 0x0020;
    private const uint MouseEventMiddleUp = 0x0040;
    private const uint MouseEventWheel = 0x0800;
    private const uint KeyEventKeyUp = 0x0002;

    public static void Apply(RemoteInputEvent input)
    {
        switch (input.Type)
        {
            case "mouseMove":
                MoveMouse(input.X, input.Y);
                break;
            case "mouseDown":
                MoveMouse(input.X, input.Y);
                MouseButton(input.Button, true);
                break;
            case "mouseUp":
                MoveMouse(input.X, input.Y);
                MouseButton(input.Button, false);
                break;
            case "mouseWheel":
                MoveMouse(input.X, input.Y);
                mouse_event(MouseEventWheel, 0, 0, unchecked((uint)-input.Delta), UIntPtr.Zero);
                break;
            case "keyDown":
                if (input.KeyCode > 0)
                {
                    keybd_event((byte)input.KeyCode, 0, 0, UIntPtr.Zero);
                }
                break;
            case "keyUp":
                if (input.KeyCode > 0)
                {
                    keybd_event((byte)input.KeyCode, 0, KeyEventKeyUp, UIntPtr.Zero);
                }
                break;
        }
    }

    private static void MoveMouse(int x, int y)
    {
        var bounds = SystemInformation.VirtualScreen;
        Cursor.Position = new Point(bounds.Left + x, bounds.Top + y);
    }

    private static void MouseButton(string? button, bool down)
    {
        var flag = (button, down) switch
        {
            ("right", true) => MouseEventRightDown,
            ("right", false) => MouseEventRightUp,
            ("middle", true) => MouseEventMiddleDown,
            ("middle", false) => MouseEventMiddleUp,
            (_, true) => MouseEventLeftDown,
            _ => MouseEventLeftUp
        };

        mouse_event(flag, 0, 0, 0, UIntPtr.Zero);
    }

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}

internal sealed class MouseBlocker : IDisposable
{
    private const int WhMouseLl = 14;
    private const int LlmhfInjected = 0x00000001;

    private readonly LowLevelMouseProc _proc;
    private IntPtr _hook;
    private bool _locked;

    public MouseBlocker()
    {
        _proc = HookCallback;
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;

        if (locked && _hook == IntPtr.Zero)
        {
            _hook = SetWindowsHookEx(WhMouseLl, _proc, GetModuleHandle(null), 0);
        }
        else if (!locked && _hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        SetLocked(false);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _locked)
        {
            var hook = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            if ((hook.flags & LlmhfInjected) == 0)
            {
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
