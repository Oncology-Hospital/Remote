using RemoteDesktop.Agent;
using Velopack;

namespace RemoteDesktop.AdminApp;

internal sealed class UpdateDialog : Form
{
    private readonly UpdateManager _manager;
    private readonly UpdateInfo _update;
    private readonly bool _isVietnamese;
    private readonly string _targetVersion;

    private readonly Label _titleLabel = new();
    private readonly Label _statusLabel = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Button _updateButton = new();
    private readonly Button _laterButton = new();

    private bool _isUpdating;
    private bool _allowClose;

    public UpdateDialog(UpdateManager manager, UpdateInfo update, bool isVietnamese)
    {
        _manager = manager;
        _update = update;
        _isVietnamese = isVietnamese;
        _targetVersion = update.TargetFullRelease.Version.ToString();

        Text = isVietnamese ? "C\u1EADp nh\u1EADt ph\u1EA7n m\u1EC1m" : "Software update";
        Width = 520;
        Height = 260;
        MinimumSize = new Size(520, 260);
        MaximumSize = new Size(520, 260);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        BuildLayout();
        ApplyText();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isUpdating && !_allowClose)
        {
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(24, 20, 24, 18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.Font = new Font(Font.FontFamily, 15, FontStyle.Bold);
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = Color.DimGray;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Minimum = 0;
        _progressBar.Maximum = 100;
        _progressBar.Style = ProgressBarStyle.Continuous;

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        _updateButton.AutoSize = true;
        _updateButton.MinimumSize = new Size(120, 32);
        _laterButton.AutoSize = true;
        _laterButton.MinimumSize = new Size(92, 32);
        _updateButton.Click += async (_, _) => await DownloadAndApplyAsync();
        _laterButton.Click += (_, _) => Close();

        actions.Controls.Add(_updateButton);
        actions.Controls.Add(_laterButton);

        root.Controls.Add(_titleLabel, 0, 0);
        root.Controls.Add(_statusLabel, 0, 1);
        root.Controls.Add(_progressBar, 0, 2);
        root.Controls.Add(new Label
        {
            Text = $"{ApplicationVersionInfo.Display}  \u2192  v{_targetVersion}",
            Dock = DockStyle.Fill,
            ForeColor = Color.DimGray,
            TextAlign = ContentAlignment.MiddleRight
        }, 0, 3);
        root.Controls.Add(actions, 0, 4);

        Controls.Add(root);
        AcceptButton = _updateButton;
        CancelButton = _laterButton;
    }

    private void ApplyText()
    {
        _titleLabel.Text = _isVietnamese
            ? $"\u0110\u00E3 ph\u00E1t hi\u1EC7n phi\u00EAn b\u1EA3n m\u1EDBi v{_targetVersion}"
            : $"A new version v{_targetVersion} is available";
        _statusLabel.Text = _isVietnamese
            ? "Vui l\u00F2ng c\u1EADp nh\u1EADt \u0111\u1EC3 s\u1EED d\u1EE5ng c\u00E1c thay \u0111\u1ED5i m\u1EDBi. B\u1EA1n c\u00F3 th\u1EC3 ti\u1EBFp t\u1EE5c sau n\u1EBFu ch\u01B0a thu\u1EADn ti\u1EC7n."
            : "Please update to use the latest changes. You can postpone the update if needed.";
        _updateButton.Text = _isVietnamese ? "C\u1EADp nh\u1EADt ngay" : "Update now";
        _laterButton.Text = _isVietnamese ? "\u0110\u1EC3 sau" : "Later";
    }

    private async Task DownloadAndApplyAsync()
    {
        if (_isUpdating)
        {
            return;
        }

        _isUpdating = true;
        ControlBox = false;
        _updateButton.Enabled = false;
        _laterButton.Enabled = false;
        SetProgress(0);

        try
        {
            await _manager.DownloadUpdatesAsync(_update, ReportProgress);
            SetProgress(100);
            _statusLabel.Text = _isVietnamese
                ? "\u0110\u00E3 t\u1EA3i v\u00E0 chu\u1EA9n b\u1ECB xong b\u1EA3n c\u1EADp nh\u1EADt."
                : "The update has been downloaded and prepared.";

            AutoUpdateService.MarkUpdateReady(_targetVersion);

            MessageBox.Show(
                this,
                _isVietnamese
                    ? "B\u1EA3n c\u1EADp nh\u1EADt \u0111\u00E3 s\u1EB5n s\u00E0ng. \u1EE8ng d\u1EE5ng s\u1EBD kh\u1EDFi \u0111\u1ED9ng l\u1EA1i \u0111\u1EC3 ho\u00E0n t\u1EA5t c\u00E0i \u0111\u1EB7t."
                    : "The update is ready. The application will restart to complete the installation.",
                _isVietnamese ? "S\u1EB5n s\u00E0ng c\u1EADp nh\u1EADt" : "Update ready",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            _allowClose = true;
            _manager.ApplyUpdatesAndRestart(_update);
        }
        catch (Exception exception)
        {
            AutoUpdateService.LogError(exception);
            MessageBox.Show(
                this,
                _isVietnamese
                    ? $"Kh\u00F4ng th\u1EC3 c\u1EADp nh\u1EADt \u1EE9ng d\u1EE5ng. Vui l\u00F2ng th\u1EED l\u1EA1i sau.{Environment.NewLine}{exception.Message}"
                    : $"The application could not be updated. Please try again later.{Environment.NewLine}{exception.Message}",
                _isVietnamese ? "C\u1EADp nh\u1EADt th\u1EA5t b\u1EA1i" : "Update failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            _isUpdating = false;
            ControlBox = true;
            _updateButton.Enabled = true;
            _laterButton.Enabled = true;
            ApplyText();
        }
    }

    private void ReportProgress(int value)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => SetProgress(value));
            return;
        }

        SetProgress(value);
    }

    private void SetProgress(int value)
    {
        var progress = Math.Clamp(value, 0, 100);
        _progressBar.Value = progress;
        _statusLabel.Text = _isVietnamese
            ? $"\u0110ang t\u1EA3i v\u00E0 chu\u1EA9n b\u1ECB b\u1EA3n c\u1EADp nh\u1EADt... {progress}%"
            : $"Downloading and preparing the update... {progress}%";
    }
}

