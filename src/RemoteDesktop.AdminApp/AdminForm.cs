using Microsoft.AspNetCore.Builder;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using RemoteDesktop.Agent;
using RemoteDesktop.Server;

namespace RemoteDesktop.AdminApp;

public sealed class AdminForm : Form
{
    private const string LocalAdminUrl = "http://localhost:5000";

    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private readonly Label _statusLabel = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter
    };
    private readonly string _language;

    private WebApplication? _server;
    private CancellationTokenSource? _serverCancellation;

    public AdminForm(string adminName = "admin", string language = "vi")
    {
        _language = language == "en" ? "en" : "vi";
        Text = $"Remote Desktop Admin - {adminName} - {ApplicationVersionInfo.Display}";
        _statusLabel.Text = _language == "vi" ? "Đang khởi động máy chủ nội bộ..." : "Starting local server...";
        Width = 1280;
        Height = 820;
        MinimumSize = new Size(980, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(_statusLabel);
        Shown += async (_, _) => await StartAsync();
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        if (_server is not null)
        {
            _serverCancellation?.Cancel();
            using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await _server.StopAsync(shutdown.Token);
            await _server.DisposeAsync();
        }

        _serverCancellation?.Dispose();
        base.OnFormClosing(e);
    }

    private async Task StartAsync()
    {
        try
        {
            var contentRoot = AppContext.BaseDirectory;
            _serverCancellation = new CancellationTokenSource();
            _server = ServerHost.Build(
                new[] { "--urls", "http://0.0.0.0:5000" },
                contentRoot);

            await _server.StartAsync(_serverCancellation.Token);
            await StartWebViewAsync();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = _language == "vi"
                ? $"Không thể khởi động ứng dụng quản trị.{Environment.NewLine}{ex.Message}"
                : $"Cannot start admin app.{Environment.NewLine}{ex.Message}";
        }
    }

    private async Task StartWebViewAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RemoteDesktopAdmin",
            "WebView2");

        var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await _webView.EnsureCoreWebView2Async(environment);
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        var version = Uri.EscapeDataString(ApplicationVersionInfo.Current);
        _webView.Source = new Uri($"{LocalAdminUrl}?lang={_language}&version={version}");

        Controls.Remove(_statusLabel);
        Controls.Add(_webView);
        _webView.BringToFront();
    }
}
