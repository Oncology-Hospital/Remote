namespace RemoteDesktop.Agent;

internal sealed class SupportRequestForm : Form
{
    private readonly TextBox _description = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill
    };

    public SupportRequestForm()
    {
        Text = AgentLanguage.T("SupportDialogTitle");
        Width = 420;
        Height = 260;
        MinimumSize = new Size(360, 220);
        StartPosition = FormStartPosition.CenterParent;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        root.Controls.Add(new Label
        {
            Text = AgentLanguage.T("SupportDialogDescription"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        root.Controls.Add(_description, 0, 1);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        var send = new Button { Text = AgentLanguage.T("Send"), DialogResult = DialogResult.OK, Width = 82 };
        var cancel = new Button { Text = AgentLanguage.T("Cancel"), DialogResult = DialogResult.Cancel, Width = 82 };
        actions.Controls.Add(send);
        actions.Controls.Add(cancel);
        root.Controls.Add(actions, 0, 2);

        AcceptButton = send;
        CancelButton = cancel;
        Controls.Add(root);
    }

    public string Description => _description.Text;
}
