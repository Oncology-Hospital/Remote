namespace RemoteDesktop.Server;

public sealed class MachineInfo
{
    public string MachineId { get; set; } = "";
    public string HostName { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string UserName { get; set; } = "";
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
}

public sealed class MachineView
{
    public string MachineId { get; set; } = "";
    public string HostName { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string UserName { get; set; } = "";
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public string Status { get; set; } = "offline";
    public bool IsStreaming { get; set; }
    public bool MouseLocked { get; set; }
    public DateTime LastSeenUtc { get; set; }
}

public sealed class ChatMessage
{
    public string MachineId { get; set; } = "";
    public string From { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ScreenFrame
{
    public string MachineId { get; set; } = "";
    public string Base64Jpeg { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class RemoteInputEvent
{
    public string Type { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public string? Button { get; set; }
    public int Delta { get; set; }
    public int KeyCode { get; set; }
}

public sealed class CursorPosition
{
    public string MachineId { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? ImageBase64Png { get; set; }
    public int HotspotX { get; set; }
    public int HotspotY { get; set; }
    public int CursorWidth { get; set; }
    public int CursorHeight { get; set; }
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SupportRequest
{
    public string MachineId { get; set; } = "";
    public string HostName { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
