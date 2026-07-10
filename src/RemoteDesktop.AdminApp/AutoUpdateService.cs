using Velopack;
using Velopack.Sources;

namespace RemoteDesktop.AdminApp;

internal static class AutoUpdateService
{
    private const string RepositoryUrl = "https://github.com/Oncology-Hospital/Remote";

    public static async Task CheckAndApplyAsync()
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

            await manager.DownloadUpdatesAsync(update);
            manager.ApplyUpdatesAndRestart(update);
        }
        catch (Exception exception)
        {
            // Update failures must not prevent the remote desktop app from starting.
            TryWriteErrorLog(exception);
        }
    }

    private static void TryWriteErrorLog(Exception exception)
    {
        try
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Oncology-Hospital",
                "Remote",
                "logs");

            Directory.CreateDirectory(logDirectory);
            File.AppendAllText(
                Path.Combine(logDirectory, "updater.log"),
                $"[{DateTimeOffset.Now:O}] {exception}\n");
        }
        catch
        {
            // Logging must never turn a recoverable update error into a startup failure.
        }
    }
}

