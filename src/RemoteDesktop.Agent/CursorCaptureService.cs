using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RemoteDesktop.Agent;

internal static class CursorCaptureService
{
    private const int CursorShowing = 0x00000001;

    public static CursorSnapshot Capture(string machineId, bool includeImage)
    {
        var cursorInfo = new CURSORINFO
        {
            cbSize = Marshal.SizeOf<CURSORINFO>()
        };

        if (!GetCursorInfo(ref cursorInfo))
        {
            return CursorSnapshot.Hidden(machineId, Cursor.Position);
        }

        var isVisible = (cursorInfo.flags & CursorShowing) == CursorShowing && cursorInfo.hCursor != IntPtr.Zero;
        var position = Cursor.Position;
        var bounds = SystemInformation.VirtualScreen;
        var cursor = new CursorPosition
        {
            MachineId = machineId,
            X = Math.Clamp(position.X - bounds.Left, 0, Math.Max(0, bounds.Width)),
            Y = Math.Clamp(position.Y - bounds.Top, 0, Math.Max(0, bounds.Height)),
            IsVisible = isVisible,
            SentAtUtc = DateTime.UtcNow
        };

        if (isVisible && includeImage)
        {
            AddCursorImage(cursor, cursorInfo.hCursor);
        }

        return new CursorSnapshot(cursor, cursorInfo.hCursor, isVisible, position);
    }

    private static void AddCursorImage(CursorPosition cursor, IntPtr cursorHandle)
    {
        var iconHandle = CopyIcon(cursorHandle);
        if (iconHandle == IntPtr.Zero)
        {
            return;
        }

        ICONINFO iconInfo = default;
        try
        {
            if (GetIconInfo(iconHandle, out iconInfo))
            {
                cursor.HotspotX = iconInfo.xHotspot;
                cursor.HotspotY = iconInfo.yHotspot;
            }

            using var icon = Icon.FromHandle(iconHandle);
            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);

            cursor.CursorWidth = bitmap.Width;
            cursor.CursorHeight = bitmap.Height;
            cursor.ImageBase64Png = Convert.ToBase64String(stream.ToArray());
        }
        finally
        {
            if (iconInfo.hbmMask != IntPtr.Zero)
            {
                DeleteObject(iconInfo.hbmMask);
            }

            if (iconInfo.hbmColor != IntPtr.Zero)
            {
                DeleteObject(iconInfo.hbmColor);
            }

            DestroyIcon(iconHandle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorInfo(ref CURSORINFO pci);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CopyIcon(IntPtr hIcon);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CURSORINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }
}

internal sealed class CursorSnapshot
{
    public CursorSnapshot(CursorPosition cursor, IntPtr handle, bool isVisible, Point screenPosition)
    {
        Cursor = cursor;
        Handle = handle;
        IsVisible = isVisible;
        ScreenPosition = screenPosition;
    }

    public CursorPosition Cursor { get; }
    public IntPtr Handle { get; }
    public bool IsVisible { get; }
    public Point ScreenPosition { get; }

    public static CursorSnapshot Hidden(string machineId, Point screenPosition)
    {
        return new CursorSnapshot(
            new CursorPosition
            {
                MachineId = machineId,
                X = screenPosition.X,
                Y = screenPosition.Y,
                IsVisible = false,
                SentAtUtc = DateTime.UtcNow
            },
            IntPtr.Zero,
            false,
            screenPosition);
    }
}
