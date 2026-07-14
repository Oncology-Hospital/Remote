using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace RemoteDesktop.Agent;

public sealed class AgentForm : Form
{
    private static readonly ScreenStreamOptions[] AutoQualityProfiles =
    [
        new ScreenStreamOptions { Mode = "480p", MaxWidth = 854, JpegQuality = 50, FrameIntervalMs = 300 },
        new ScreenStreamOptions { Mode = "720p", MaxWidth = 1280, JpegQuality = 60, FrameIntervalMs = 250 },
        new ScreenStreamOptions { Mode = "1080p", MaxWidth = 1920, JpegQuality = 75, FrameIntervalMs = 250 }
    ];

    private readonly TextBox _serverUrl = new();
    private readonly TextBox _chatInput = new();
    private readonly TextBox _chatLog = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly Label _serverLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Label _status = new() { AutoSize = true };
    private readonly Label _machineInfo = new() { AutoSize = true };
    private readonly Label _remoteStatus = new() { AutoSize = true };
    private readonly Label _lockStatus = new() { AutoSize = true };
    private readonly Label _versionLabel = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Color.RoyalBlue,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleCenter
    };
    private readonly Label _supportHint = new()
    {
        AutoSize = true,
        Padding = new Padding(8, 7, 0, 0),
        ForeColor = Color.DimGray
    };
    private readonly Button _connectButton = new();
    private readonly Button _supportButton = new();
    private readonly Button _sendChatButton = new();
    private readonly Button _unlockButton = new() { AutoSize = true, Enabled = false };
    private readonly ToolTip _toolTip = new();
    private readonly System.Windows.Forms.Timer _heartbeatTimer = new() { Interval = 3000 };
    private readonly System.Windows.Forms.Timer _screenTimer = new() { Interval = 250 };
    private readonly System.Windows.Forms.Timer _cursorTimer = new() { Interval = 50 };
    private readonly MouseBlocker _mouseBlocker = new();

    private HubConnection? _connection;
    private CancellationTokenSource? _discoveryCancellation;
    private bool _isConnecting;
    private MachineInfo _machine = MachineIdentity.Create();
    private bool _isStreaming;
    private bool _isSendingFrame;
    private ScreenStreamOptions _streamOptions = AutoQualityProfiles[1];
    private string _requestedQuality = "auto";
    private int _autoQualityIndex = 1;
    private int _fastFrameCount;
    private int _slowFrameCount;
    private Point _lastCursorPosition = new(int.MinValue, int.MinValue);
    private IntPtr _lastCursorHandle = IntPtr.Zero;
    private bool _lastCursorVisible;

    public AgentForm()
    {
        Width = 560;
        Height = 360;
        MinimumSize = new Size(520, 320);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        ApplyLanguage();
        WireEvents();
        UpdateMachineLabel();
        Shown += async (_, _) => await StartAutoDiscoveryAsync();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _discoveryCancellation?.Cancel();
        _screenTimer.Stop();
        _cursorTimer.Stop();
        _heartbeatTimer.Stop();
        _mouseBlocker.SetLocked(false);
        _ = _connection?.DisposeAsync();
        _discoveryCancellation?.Dispose();
        base.OnFormClosing(e);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 2 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        top.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        top.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        top.Controls.Add(_serverLabel, 0, 0);
        _serverUrl.Dock = DockStyle.Fill;
        top.Controls.Add(_serverUrl, 1, 0);
        top.Controls.Add(_connectButton, 2, 0);
        top.Controls.Add(_supportButton, 3, 0);
        top.Controls.Add(_versionLabel, 4, 0);
        top.Controls.Add(_machineInfo, 1, 1);

        var statusPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        statusPanel.Controls.Add(_status);
        statusPanel.Controls.Add(new Label { Text = " | ", AutoSize = true });
        statusPanel.Controls.Add(_remoteStatus);
        statusPanel.Controls.Add(new Label { Text = " | ", AutoSize = true });
        statusPanel.Controls.Add(_lockStatus);
        top.Controls.Add(statusPanel, 0, 1);

        var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        actionPanel.Controls.Add(_unlockButton);
        actionPanel.Controls.Add(_supportHint);

        _chatLog.Dock = DockStyle.Fill;

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        _chatInput.Dock = DockStyle.Fill;
        bottom.Controls.Add(_chatInput, 0, 0);
        bottom.Controls.Add(_sendChatButton, 1, 0);

        root.Controls.Add(top, 0, 0);
        root.Controls.Add(actionPanel, 0, 1);
        root.Controls.Add(_chatLog, 0, 2);
        root.Controls.Add(bottom, 0, 3);
        Controls.Add(root);
    }

    private void ApplyLanguage()
    {
        Text = $"{AgentLanguage.T("Title")} - {ApplicationVersionInfo.Display}";
        _serverLabel.Text = AgentLanguage.T("Server");
        _serverUrl.Text = AgentLanguage.T("AutoDiscovery");
        _status.Text = AgentLanguage.T("Disconnected");
        _remoteStatus.Text = AgentLanguage.T("RemoteOff");
        _lockStatus.Text = AgentLanguage.T("MouseUnlocked");
        _versionLabel.Text = ApplicationVersionInfo.Display;
        _supportButton.Text = AgentLanguage.T("Support");
        _sendChatButton.Text = AgentLanguage.T("Send");
        _unlockButton.Text = AgentLanguage.T("UnlockMouse");
        _toolTip.SetToolTip(_unlockButton, AgentLanguage.T("UnlockMouseHint"));
        _supportHint.Text = AgentLanguage.T("SupportHint");
        UpdateConnectionUi();
    }

    private void WireEvents()
    {
        _connectButton.Click += async (_, _) => await ToggleConnectionAsync();
        _supportButton.Click += async (_, _) => await ShowSupportDialogAsync();
        _sendChatButton.Click += async (_, _) => await SendChatAsync();
        _chatInput.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SendChatAsync();
            }
        };
        _unlockButton.Click += async (_, _) => await UnlockMouseAsync();
        _heartbeatTimer.Tick += async (_, _) => await SafeInvokeAsync(() => _connection!.InvokeAsync("Heartbeat", _machine.MachineId));
        _screenTimer.Tick += async (_, _) => await SendScreenFrameAsync();
        _cursorTimer.Tick += async (_, _) => await SendCursorPositionAsync();
    }

    private async Task ToggleConnectionAsync()
    {
        if (_isConnecting)
        {
            return;
        }

        if (_connection is null)
        {
            await ConnectAsync();
        }
        else
        {
            await DisconnectAsync();
        }
    }

    private async Task ConnectAsync()
    {
        if (_connection is not null || _isConnecting)
        {
            return;
        }

        var url = NormalizeServerUrl(_serverUrl.Text);
        _serverUrl.Text = url;
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            AppendChat(AgentLanguage.T("UrlNotReady"));
            return;
        }

        _isConnecting = true;
        UpdateConnectionUi();
        _machine = MachineIdentity.Create();
        UpdateMachineLabel();

        _connection = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<ChatMessage>("ReceiveChatMessage", chat =>
        {
            BeginInvoke(() => AppendChat($"{chat.From}: {chat.Message}"));
        });

        _connection.On("StartScreenStream", () =>
        {
            BeginInvoke(() => SetStreaming(true));
        });

        _connection.On<ScreenStreamOptions>("ApplyScreenStreamQuality", options =>
        {
            BeginInvoke(() => ApplyScreenStreamQuality(options));
        });

        _connection.On("StopScreenStream", () =>
        {
            BeginInvoke(() => SetStreaming(false));
        });

        _connection.On<RemoteInputEvent>("ReceiveInputEvent", NativeInput.Apply);

        _connection.On<bool>("SetMouseLock", locked =>
        {
            BeginInvoke(() => SetMouseLocked(locked));
        });

        _connection.Reconnecting += _ =>
        {
            BeginInvoke(() => _status.Text = AgentLanguage.T("Reconnecting"));
            return Task.CompletedTask;
        };

        _connection.Reconnected += async _ =>
        {
            BeginInvoke(() => _status.Text = AgentLanguage.T("Connected"));
            await _connection.InvokeAsync("RegisterMachine", _machine);
        };

        _connection.Closed += _ =>
        {
            BeginInvoke(() =>
            {
                _status.Text = AgentLanguage.T("Disconnected");
                SetStreaming(false);
                SetMouseLocked(false);
                _heartbeatTimer.Stop();
                _cursorTimer.Stop();
                _connection = null;
                UpdateConnectionUi();
            });
            return Task.CompletedTask;
        };

        try
        {
            await _connection.StartAsync();
            await _connection.InvokeAsync("RegisterMachine", _machine);
            _status.Text = AgentLanguage.T("Connected");
            _heartbeatTimer.Start();
            _cursorTimer.Start();
            AppendChat(AgentLanguage.T("ConnectedLog"));
        }
        catch (Exception ex)
        {
            AppendChat(AgentLanguage.Format("ConnectFailed", ex.Message));
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }
            _connection = null;
        }
        finally
        {
            _isConnecting = false;
            UpdateConnectionUi();
        }
    }

    private async Task DisconnectAsync()
    {
        _heartbeatTimer.Stop();
        _cursorTimer.Stop();
        SetStreaming(false);
        SetMouseLocked(false);

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _status.Text = AgentLanguage.T("Disconnected");
        UpdateConnectionUi();
    }

    private void UpdateConnectionUi()
    {
        var hasConnection = _connection is not null;

        _connectButton.Text = _isConnecting
            ? AgentLanguage.T("Connecting")
            : AgentLanguage.T(hasConnection ? "Disconnect" : "Connect");
        _connectButton.Enabled = !_isConnecting;
        _serverUrl.Enabled = !hasConnection && !_isConnecting;

        _connectButton.UseVisualStyleBackColor = !hasConnection;
        _connectButton.BackColor = hasConnection ? Color.MistyRose : SystemColors.Control;
        _connectButton.ForeColor = hasConnection ? Color.DarkRed : SystemColors.ControlText;
    }

    private async Task StartAutoDiscoveryAsync()
    {
        if (_connection is not null)
        {
            return;
        }

        _discoveryCancellation?.Cancel();
        _discoveryCancellation?.Dispose();
        _discoveryCancellation = new CancellationTokenSource();
        _status.Text = AgentLanguage.T("SearchingServer");
        AppendChat(AgentLanguage.T("SearchingLan"));

        try
        {
            var url = await LanDiscoveryClient.WaitForServerUrlAsync(_discoveryCancellation.Token);
            if (string.IsNullOrWhiteSpace(url) || _connection is not null || IsDisposed)
            {
                return;
            }

            _serverUrl.Text = url;
            AppendChat(AgentLanguage.Format("FoundServer", url));
            await ConnectAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AppendChat(AgentLanguage.Format("DiscoveryFailedLog", ex.Message));
            _status.Text = AgentLanguage.T("DiscoveryFailed");
        }
    }

    private async Task SendChatAsync()
    {
        var message = _chatInput.Text.Trim();
        if (message.Length == 0 || _connection is null)
        {
            return;
        }

        _chatInput.Clear();
        AppendChat($"{AgentLanguage.T("Me")}: {message}");
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendChatToAdmin", _machine.MachineId, message));
    }

    private async Task ShowSupportDialogAsync()
    {
        if (_connection is null)
        {
            MessageBox.Show(
                this,
                AgentLanguage.T("MachineNotConnected"),
                AgentLanguage.T("SupportDialogTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new SupportRequestForm();
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var description = dialog.Description.Trim();
        if (description.Length == 0)
        {
            return;
        }

        var request = new SupportRequest
        {
            MachineId = _machine.MachineId,
            HostName = _machine.HostName,
            IpAddress = _machine.IpAddress,
            UserName = _machine.UserName,
            Message = description,
            SentAtUtc = DateTime.UtcNow
        };

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendSupportRequestToAdmin", request));
        AppendChat(AgentLanguage.Format("SupportRequestSent", description));
    }

    private void SetStreaming(bool enabled)
    {
        _isStreaming = enabled;
        _remoteStatus.Text = enabled ? AgentLanguage.T("RemoteStreaming") : AgentLanguage.T("RemoteOff");

        if (enabled)
        {
            _screenTimer.Start();
            AppendChat(AgentLanguage.T("AdminStartedRemote"));
        }
        else
        {
            _screenTimer.Stop();
            AppendChat(AgentLanguage.T("AdminStoppedRemote"));
        }
    }

    private void SetMouseLocked(bool locked)
    {
        _mouseBlocker.SetLocked(locked);
        _lockStatus.Text = locked ? AgentLanguage.T("MouseLocked") : AgentLanguage.T("MouseUnlocked");
        _unlockButton.Enabled = locked;
    }

    private async Task UnlockMouseAsync()
    {
        // Always restore local input first, even if the server is no longer reachable.
        SetMouseLocked(false);

        if (_connection is not null)
        {
            await SafeInvokeAsync(() =>
                _connection.InvokeAsync("SetMouseLock", _machine.MachineId, false));
        }
    }

    private async Task SendScreenFrameAsync()
    {
        if (!_isStreaming || _isSendingFrame || _connection is null)
        {
            return;
        }

        _isSendingFrame = true;
        try
        {
            var settings = GetActiveStreamOptions();
            var stopwatch = Stopwatch.StartNew();
            var frame = ScreenCaptureService.Capture(
                _machine.MachineId,
                settings,
                _requestedQuality);
            await _connection.InvokeAsync("SendScreenFrame", frame);
            stopwatch.Stop();
            UpdateAutomaticQuality(stopwatch.ElapsedMilliseconds, frame.EncodedBytes);
        }
        catch (Exception ex)
        {
            AppendChat(AgentLanguage.Format("FrameError", ex.Message));
        }
        finally
        {
            _isSendingFrame = false;
        }
    }

    private void ApplyScreenStreamQuality(ScreenStreamOptions? options)
    {
        var mode = options?.Mode?.Trim().ToLowerInvariant();
        _requestedQuality = mode is "480p" or "720p" or "1080p" ? mode : "auto";
        _autoQualityIndex = 1;
        _fastFrameCount = 0;
        _slowFrameCount = 0;

        _streamOptions = _requestedQuality == "auto"
            ? AutoQualityProfiles[_autoQualityIndex]
            : new ScreenStreamOptions
            {
                Mode = _requestedQuality,
                MaxWidth = Math.Clamp(options?.MaxWidth ?? 1280, 320, 1920),
                JpegQuality = Math.Clamp(options?.JpegQuality ?? 60, 30, 90),
                FrameIntervalMs = Math.Clamp(options?.FrameIntervalMs ?? 250, 100, 1000)
            };

        _screenTimer.Interval = GetActiveStreamOptions().FrameIntervalMs;
    }

    private ScreenStreamOptions GetActiveStreamOptions()
    {
        return _requestedQuality == "auto"
            ? AutoQualityProfiles[_autoQualityIndex]
            : _streamOptions;
    }

    private void UpdateAutomaticQuality(long elapsedMilliseconds, int encodedBytes)
    {
        if (_requestedQuality != "auto")
        {
            return;
        }

        var current = AutoQualityProfiles[_autoQualityIndex];
        var isSlow = elapsedMilliseconds > Math.Max(450, current.FrameIntervalMs * 2L) || encodedBytes > 1_400_000;
        var isFast = elapsedMilliseconds < current.FrameIntervalMs * 0.65 && encodedBytes < 750_000;

        _slowFrameCount = isSlow ? _slowFrameCount + 1 : 0;
        _fastFrameCount = isFast ? _fastFrameCount + 1 : 0;

        if (_slowFrameCount >= 2 && _autoQualityIndex > 0)
        {
            _autoQualityIndex--;
            ResetAutomaticQualityCounters();
        }
        else if (_fastFrameCount >= 16 && _autoQualityIndex < AutoQualityProfiles.Length - 1)
        {
            _autoQualityIndex++;
            ResetAutomaticQualityCounters();
        }

        _screenTimer.Interval = AutoQualityProfiles[_autoQualityIndex].FrameIntervalMs;
    }

    private void ResetAutomaticQualityCounters()
    {
        _fastFrameCount = 0;
        _slowFrameCount = 0;
    }

    private async Task SendCursorPositionAsync()
    {
        if (_connection is null)
        {
            return;
        }

        var initialCursorImage = _lastCursorHandle == IntPtr.Zero;
        var snapshot = CursorCaptureService.Capture(_machine.MachineId, initialCursorImage);
        var cursorChanged =
            snapshot.Handle != _lastCursorHandle ||
            snapshot.IsVisible != _lastCursorVisible;

        if (cursorChanged && !initialCursorImage)
        {
            snapshot = CursorCaptureService.Capture(_machine.MachineId, includeImage: true);
        }

        if (snapshot.ScreenPosition == _lastCursorPosition && !cursorChanged)
        {
            return;
        }

        _lastCursorPosition = snapshot.ScreenPosition;
        _lastCursorHandle = snapshot.Handle;
        _lastCursorVisible = snapshot.IsVisible;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendCursorPosition", snapshot.Cursor));
    }

    private async Task SafeInvokeAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            AppendChat(AgentLanguage.Format("SignalRError", ex.Message));
        }
    }

    private void UpdateMachineLabel()
    {
        _machineInfo.Text = string.Join(" · ", _machine.HostName, _machine.IpAddress, _machine.UserName, $"{_machine.ScreenWidth}x{_machine.ScreenHeight}");
        _machineInfo.Text = string.Join(" - ", _machine.HostName, _machine.IpAddress, _machine.UserName, $"{_machine.ScreenWidth}x{_machine.ScreenHeight}");
        _machineInfo.Text = $"{_machine.HostName} · {_machine.IpAddress} · {_machine.UserName} · {_machine.ScreenWidth}x{_machine.ScreenHeight}";
        _machineInfo.Text = string.Join(" - ", _machine.HostName, _machine.IpAddress, _machine.UserName, $"{_machine.ScreenWidth}x{_machine.ScreenHeight}");
    }

    private static string NormalizeServerUrl(string input)
    {
        var value = input.Trim();
        if (value.Length == 0)
        {
            return value;
        }

        if (!value.Contains("://", StringComparison.Ordinal))
        {
            value = $"http://{value}";
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return input.Trim();
        }

        var builder = new UriBuilder(uri);
        if (builder.Port is -1 or 80)
        {
            builder.Port = 5000;
        }

        if (string.IsNullOrWhiteSpace(builder.Path) || builder.Path == "/")
        {
            builder.Path = "remoteHub";
        }

        return builder.Uri.ToString().TrimEnd('/');
    }

    private void AppendChat(string text)
    {
        _chatLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }
}

internal static class MachineIdentity
{
    public static MachineInfo Create()
    {
        var bounds = SystemInformation.VirtualScreen;
        return new MachineInfo
        {
            MachineId = $"{Environment.MachineName}-{Environment.UserName}",
            HostName = Environment.MachineName,
            UserName = Environment.UserName,
            IpAddress = GetLocalIpAddress(),
            ScreenWidth = bounds.Width,
            ScreenHeight = bounds.Height
        };
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            return Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(address =>
                    address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address))
                ?.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }
}
