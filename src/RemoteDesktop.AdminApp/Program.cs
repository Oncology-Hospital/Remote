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

        var loginForm = new LoginForm();
        loginForm.Shown += async (_, _) => await AutoUpdateService.CheckAndApplyAsync();

        Application.Run(loginForm);
    }
}
