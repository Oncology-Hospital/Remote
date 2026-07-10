using RemoteDesktop.Agent;
using Velopack;
using Velopack.Sources;

namespace RemoteDesktop.AdminApp;

internal static class AutoUpdateService
{
    private const string RepositoryUrl = "https://github.com/Oncology-Hospital/Remote";

    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Oncology-Hospital",
        "Remote");

    private static readonly string CompletedUpdateMarker = Path.Combine(
        DataDirectory,
        "completed-update.version");

    public static async Task CheckAndApplyAsync(IWin32Window owner, bool isVietnamese)
    {
        try
        {
            var source = new GithubSource(
                RepositoryUrl,
                accessToken: null,
                prerelease: false);

            var manager = new UpdateManager(source);
            var update = await manager.CheckForUpdatesAsync();
            if (update is null)
            {
                return;
            }

            using var dialog = new UpdateDialog(manager, update, isVietnamese);
            dialog.ShowDialog(owner);
        }
        catch (Exception exception)
        {
            // A failed update check must not prevent the application from starting.
            LogError(exception);
        }
    }

    public static void ShowCompletedUpdateIfPending(IWin32Window owner, bool isVietnamese)
    {
        try
        {
            if (!File.Exists(CompletedUpdateMarker))
            {
                return;
            }

            var targetVersion = File.ReadAllText(CompletedUpdateMarker).Trim();
            if (!string.Equals(
                    targetVersion,
                    ApplicationVersionInfo.Current,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            File.Delete(CompletedUpdateMarker);
            MessageBox.Show(
                owner,
                isVietnamese
                    ? $"C\u1EADp nh\u1EADt th\u00E0nh c\u00F4ng l\u00EAn phi\u00EAn b\u1EA3n v{targetVersion}. \u1EE8ng d\u1EE5ng \u0111\u00E3 s\u1EB5n s\u00E0ng."
                    : $"Successfully updated to version v{targetVersion}. The application is ready.",
                isVietnamese ? "C\u1EADp nh\u1EADt ho\u00E0n t\u1EA5t" : "Update completed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            LogError(exception);
        }
    }

    internal static void MarkUpdateReady(string targetVersion)
    {
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllText(CompletedUpdateMarker, targetVersion);
    }

    internal static void LogError(Exception exception)
    {
        try
        {
            var logDirectory = Path.Combine(DataDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            File.AppendAllText(
                Path.Combine(logDirectory, "updater.log"),
                $"[{DateTimeOffset.Now:O}] {exception}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never turn a recoverable update error into a startup failure.
        }
    }
}

