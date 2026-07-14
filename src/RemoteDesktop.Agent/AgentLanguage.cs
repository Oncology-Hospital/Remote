namespace RemoteDesktop.Agent;

public static class AgentLanguage
{
    public static string Current { get; set; } = "vi";

    public static string T(string key)
    {
        var table = Current == "en" ? English : Vietnamese;
        return table.TryGetValue(key, out var value) ? value : key;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }

    private static readonly Dictionary<string, string> Vietnamese = new()
    {
        ["Title"] = "Remote Desktop - Máy người dùng",
        ["Server"] = "Máy chủ",
        ["AutoDiscovery"] = "Tự động dò tìm...",
        ["Disconnected"] = "Chưa kết nối",
        ["Connected"] = "Đã kết nối",
        ["Connecting"] = "Đang kết nối...",
        ["Reconnecting"] = "Đang kết nối lại",
        ["SearchingServer"] = "Đang tìm máy chủ",
        ["DiscoveryFailed"] = "Dò tìm thất bại",
        ["RemoteOff"] = "Màn hình: tắt",
        ["RemoteStreaming"] = "Màn hình: đang truyền",
        ["MouseUnlocked"] = "Chuột: chưa khóa",
        ["MouseLocked"] = "Chuột: đã khóa",
        ["Connect"] = "Kết nối",
        ["Disconnect"] = "Ngắt kết nối",
        ["Support"] = "Gọi hỗ trợ",
        ["Send"] = "Gửi",
        ["Cancel"] = "Hủy",
        ["UnlockMouse"] = "Mở khóa chuột",
        ["UnlockMouseHint"] = "Mở lại chuột trên máy này nếu quản trị viên đã khóa chuột khi điều khiển từ xa.",
        ["SupportHint"] = "Nhấn Gọi hỗ trợ để gửi mô tả đến quản trị viên.",
        ["UrlNotReady"] = "Đường dẫn máy chủ chưa sẵn sàng.",
        ["ConnectedLog"] = "Đã kết nối đến máy chủ.",
        ["ConnectFailed"] = "Kết nối thất bại: {0}",
        ["SearchingLan"] = "Đang tìm máy chủ trong mạng LAN...",
        ["FoundServer"] = "Đã tìm thấy máy chủ: {0}",
        ["DiscoveryFailedLog"] = "Dò tìm thất bại: {0}",
        ["Me"] = "tôi",
        ["MachineNotConnected"] = "Máy chưa kết nối đến máy chủ quản trị.",
        ["SupportRequestSent"] = "Đã gửi yêu cầu hỗ trợ: {0}",
        ["AdminStartedRemote"] = "Quản trị viên đã bắt đầu xem màn hình.",
        ["AdminStoppedRemote"] = "Quản trị viên đã dừng xem màn hình.",
        ["FrameError"] = "Lỗi gửi hình ảnh: {0}",
        ["SignalRError"] = "Lỗi SignalR: {0}",
        ["ServerReconnectingLog"] = "Mất kết nối với máy chủ, đang thử kết nối lại...",
        ["ServerReconnectedLog"] = "Đã kết nối lại với máy chủ.",
        ["ServerDisconnectedLog"] = "Máy chủ đã đóng hoặc kết nối đã kết thúc.",
        ["LicenseCheckStarted"] = "Quản trị viên đang kiểm tra bản quyền Windows và Microsoft Office.",
        ["LicenseCheckCompleted"] = "Đã gửi kết quả kiểm tra bản quyền cho quản trị viên.",
        ["LicenseCheckFailed"] = "Không thể kiểm tra bản quyền: {0}",
        ["SupportDialogTitle"] = "Gọi hỗ trợ",
        ["SupportDialogDescription"] = "Mô tả vấn đề cần hỗ trợ"
    };

    private static readonly Dictionary<string, string> English = new()
    {
        ["Title"] = "Remote Desktop Agent",
        ["Server"] = "Server",
        ["AutoDiscovery"] = "Auto discovery...",
        ["Disconnected"] = "Disconnected",
        ["Connected"] = "Connected",
        ["Connecting"] = "Connecting...",
        ["Reconnecting"] = "Reconnecting",
        ["SearchingServer"] = "Searching server",
        ["DiscoveryFailed"] = "Discovery failed",
        ["RemoteOff"] = "Remote: off",
        ["RemoteStreaming"] = "Remote: streaming",
        ["MouseUnlocked"] = "Mouse: unlocked",
        ["MouseLocked"] = "Mouse: locked",
        ["Connect"] = "Connect",
        ["Disconnect"] = "Disconnect",
        ["Support"] = "Request support",
        ["Send"] = "Send",
        ["Cancel"] = "Cancel",
        ["UnlockMouse"] = "Unlock mouse",
        ["UnlockMouseHint"] = "Restores mouse input on this computer after the administrator has locked it during remote control.",
        ["SupportHint"] = "Press Request support to send a description to the admin.",
        ["UrlNotReady"] = "Server URL is not ready.",
        ["ConnectedLog"] = "Connected to server.",
        ["ConnectFailed"] = "Connect failed: {0}",
        ["SearchingLan"] = "Searching LAN server...",
        ["FoundServer"] = "Found server: {0}",
        ["DiscoveryFailedLog"] = "Discovery failed: {0}",
        ["Me"] = "me",
        ["MachineNotConnected"] = "This machine is not connected to the admin server.",
        ["SupportRequestSent"] = "Support request sent: {0}",
        ["AdminStartedRemote"] = "Admin started remote view.",
        ["AdminStoppedRemote"] = "Admin stopped remote view.",
        ["FrameError"] = "Frame error: {0}",
        ["SignalRError"] = "SignalR error: {0}",
        ["ServerReconnectingLog"] = "Connection to the server was lost. Reconnecting...",
        ["ServerReconnectedLog"] = "Reconnected to the server.",
        ["ServerDisconnectedLog"] = "The server closed or the connection ended.",
        ["LicenseCheckStarted"] = "The administrator is checking Windows and Microsoft Office licensing.",
        ["LicenseCheckCompleted"] = "License check results were sent to the administrator.",
        ["LicenseCheckFailed"] = "Could not check licensing: {0}",
        ["SupportDialogTitle"] = "Request support",
        ["SupportDialogDescription"] = "Describe the issue that needs support"
    };
}
