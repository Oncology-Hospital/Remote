using Velopack;

namespace RemoteDesktop.AdminApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Velopack install/update hooks must run before WinForms is initialized.
        VelopackApp.Build().Run();

        ApplicationConfiguration.Initialize();

        using (var startupForm = new StartupUpdateForm())
        {
            startupForm.ShowDialog();
        }

        Application.Run(new LoginForm());
    }
}
