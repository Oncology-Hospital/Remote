using RemoteDesktop.Agent;

namespace RemoteDesktop.AdminApp;

public sealed class LoginForm : Form
{
    private readonly Panel _content = new() { Dock = DockStyle.Fill };
    private readonly TextBox _username = new();
    private readonly TextBox _password = new() { UseSystemPasswordChar = true };
    private readonly Label _message = new() { AutoSize = true, ForeColor = Color.Firebrick };
    private readonly Button _adminButton = new();
    private readonly Button _agentButton = new();
    private readonly Button _backButton = new();
    private readonly Button _loginButton = new();
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _fileMenu = new();
    private readonly ToolStripMenuItem _languageMenu = new();
    private readonly ToolStripMenuItem _vietnameseMenu = new();
    private readonly ToolStripMenuItem _englishMenu = new();
    private readonly ToolStripMenuItem _exitMenu = new();

    private string _currentLanguage = "vi";
    private bool _showingAdminLogin;

    private string CurrentLanguage => _currentLanguage;
    private bool IsVietnamese => CurrentLanguage == "vi";

    public LoginForm()
    {
        Text = "Remote Desktop";
        Width = 460;
        Height = 300;
        MinimumSize = new Size(460, 300);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        BuildMenu();

        _adminButton.Click += (_, _) => ShowAdminLogin();
        _agentButton.Click += (_, _) => OpenAgent();
        _loginButton.Click += (_, _) => LoginAdmin();
        _backButton.Click += (_, _) => ShowRoleSelection();

        MainMenuStrip = _menu;
        Controls.Add(_content);
        Controls.Add(_menu);
        ShowRoleSelection();
    }

    private void BuildMenu()
    {
        _vietnameseMenu.Click += (_, _) => SetLanguage("vi");
        _englishMenu.Click += (_, _) => SetLanguage("en");
        _exitMenu.Click += (_, _) => Close();

        _languageMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            _vietnameseMenu,
            _englishMenu
        });
        _fileMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            _languageMenu,
            new ToolStripSeparator(),
            _exitMenu
        });
        _menu.Items.Add(_fileMenu);
        ApplyMenuText();
    }

    private void ShowRoleSelection()
    {
        _showingAdminLogin = false;
        AcceptButton = null;
        _content.Controls.Clear();
        Text = "Remote Desktop";
        ApplyButtonText();
        ApplyMenuText();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(24, 34, 24, 24)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(new Label
        {
            Text = IsVietnamese ? "Ch\u1ECDn ch\u1EBF \u0111\u1ED9" : "Choose mode",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        root.Controls.Add(new Label
        {
            Text = IsVietnamese
                ? "Qu\u1EA3n tr\u1ECB vi\u00EAn \u0111i\u1EC1u khi\u1EC3n c\u00E1c m\u00E1y. M\u00E1y ng\u01B0\u1EDDi d\u00F9ng s\u1EBD k\u1EBFt n\u1ED1i v\u1EC1 qu\u1EA3n tr\u1ECB vi\u00EAn."
                : "Admin controls machines. Agent connects this machine to an admin.",
            Dock = DockStyle.Fill,
            ForeColor = Color.DimGray,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _adminButton.Dock = DockStyle.Fill;
        _agentButton.Dock = DockStyle.Fill;
        actions.Controls.Add(_adminButton, 0, 0);
        actions.Controls.Add(_agentButton, 1, 0);

        root.Controls.Add(actions, 0, 2);
        _content.Controls.Add(root);
    }

    private void ShowAdminLogin()
    {
        _showingAdminLogin = true;
        _content.Controls.Clear();
        _message.Text = "";
        _password.Text = "";
        Text = IsVietnamese ? "\u0110\u0103ng nh\u1EADp qu\u1EA3n tr\u1ECB Remote Desktop" : "Remote Desktop Admin Login";
        ApplyButtonText();
        ApplyMenuText();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(24, 34, 24, 24)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(new Label
        {
            Text = IsVietnamese ? "\u0110\u0103ng nh\u1EADp qu\u1EA3n tr\u1ECB" : "Admin sign in",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        root.Controls.Add(CreateField(IsVietnamese ? "T\u00E0i kho\u1EA3n" : "Username", _username), 0, 1);
        root.Controls.Add(CreateField(IsVietnamese ? "M\u1EADt kh\u1EA9u" : "Password", _password), 0, 2);
        root.Controls.Add(_message, 0, 3);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        _loginButton.Width = 96;
        _backButton.Width = 86;
        actions.Controls.Add(_loginButton);
        actions.Controls.Add(_backButton);
        root.Controls.Add(actions, 0, 4);

        _content.Controls.Add(root);
        AcceptButton = _loginButton;
        _password.Focus();
    }

    private static Control CreateField(string label, TextBox input)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        panel.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        input.Dock = DockStyle.Fill;
        panel.Controls.Add(input, 1, 0);
        return panel;
    }

    private void LoginAdmin()
    {
        _message.Text = "";

        if (!AccountStore.HasAccounts())
        {
            MessageBox.Show(
                this,
                IsVietnamese
                    ? $"Chưa có tài khoản quản trị. Hãy cấu hình file:\n{AccountStore.ConfigurationPath}"
                    : $"No admin account is configured. Configure this file:\n{AccountStore.ConfigurationPath}",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var account = AccountStore.Validate(_username.Text.Trim(), _password.Text);
        if (account is null || !account.Role.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            _message.Text = IsVietnamese
                ? "Sai t\u00E0i kho\u1EA3n ho\u1EB7c m\u1EADt kh\u1EA9u qu\u1EA3n tr\u1ECB."
                : "Invalid admin username or password.";
            return;
        }

        OpenNext(new AdminForm(account.Username, CurrentLanguage));
    }

    private void OpenAgent()
    {
        AgentLanguage.Current = CurrentLanguage;
        OpenNext(new AgentForm());
    }

    private void OpenNext(Form nextForm)
    {
        Hide();
        nextForm.FormClosed += (_, _) => Close();
        nextForm.Show();
    }

    private void ApplyButtonText()
    {
        _adminButton.Text = IsVietnamese ? "Qu\u1EA3n tr\u1ECB" : "Admin";
        _agentButton.Text = IsVietnamese ? "M\u00E1y ng\u01B0\u1EDDi d\u00F9ng" : "Agent";
        _backButton.Text = IsVietnamese ? "Quay l\u1EA1i" : "Back";
        _loginButton.Text = IsVietnamese ? "\u0110\u0103ng nh\u1EADp" : "Login";
    }

    private void ApplyMenuText()
    {
        _fileMenu.Text = IsVietnamese ? "T\u1EC7p" : "File";
        _languageMenu.Text = IsVietnamese ? "Ng\u00F4n ng\u1EEF" : "Language";
        _vietnameseMenu.Text = "Ti\u1EBFng Vi\u1EC7t";
        _englishMenu.Text = "English";
        _exitMenu.Text = IsVietnamese ? "Tho\u00E1t" : "Exit";
        _vietnameseMenu.Checked = IsVietnamese;
        _englishMenu.Checked = !IsVietnamese;
    }

    private void SetLanguage(string language)
    {
        _currentLanguage = language == "en" ? "en" : "vi";
        ApplyButtonText();
        ApplyMenuText();

        if (_showingAdminLogin)
        {
            ShowAdminLogin();
            return;
        }

        ShowRoleSelection();
    }
}
