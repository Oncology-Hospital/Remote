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
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public int EncodedBytes { get; set; }
    public string RequestedQuality { get; set; } = "auto";
    public string QualityLevel { get; set; } = "720p";
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ScreenStreamOptions
{
    public string Mode { get; set; } = "auto";
    public int MaxWidth { get; set; } = 1280;
    public long JpegQuality { get; set; } = 60;
    public int FrameIntervalMs { get; set; } = 250;

    public static ScreenStreamOptions FromMode(string? mode)
    {
        return mode?.Trim().ToLowerInvariant() switch
        {
            "480p" => new ScreenStreamOptions
            {
                Mode = "480p",
                MaxWidth = 854,
                JpegQuality = 50,
                FrameIntervalMs = 300
            },
            "1080p" => new ScreenStreamOptions
            {
                Mode = "1080p",
                MaxWidth = 1920,
                JpegQuality = 75,
                FrameIntervalMs = 250
            },
            "720p" => new ScreenStreamOptions
            {
                Mode = "720p",
                MaxWidth = 1280,
                JpegQuality = 60,
                FrameIntervalMs = 250
            },
            _ => new ScreenStreamOptions()
        };
    }
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
