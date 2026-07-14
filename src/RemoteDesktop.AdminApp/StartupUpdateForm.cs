using RemoteDesktop.Agent;

namespace RemoteDesktop.AdminApp;

internal sealed class StartupUpdateForm : Form
{
    private bool _allowClose;

    public StartupUpdateForm()
    {
        Text = $"Remote Desktop - {ApplicationVersionInfo.Display}";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(440, 150);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ControlBox = false;
        ShowInTaskbar = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(24, 20, 24, 20)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        root.Controls.Add(new Label
        {
            Text = "Đang kiểm tra phiên bản mới...",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        root.Controls.Add(new Label
        {
            Text = "Vui lòng chờ trước khi đăng nhập.",
            Dock = DockStyle.Fill,
            ForeColor = Color.DimGray,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        root.Controls.Add(new ProgressBar
        {
            Dock = DockStyle.Fill,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 25
        }, 0, 2);

        Controls.Add(root);
        Shown += async (_, _) => await CheckForUpdatesAsync();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
    }

    private async Task CheckForUpdatesAsync()
    {
        AutoUpdateService.ShowCompletedUpdateIfPending(this, isVietnamese: true);
        await AutoUpdateService.CheckAndApplyAsync(this, isVietnamese: true);

        _allowClose = true;
        DialogResult = DialogResult.OK;
        Close();
    }
}
