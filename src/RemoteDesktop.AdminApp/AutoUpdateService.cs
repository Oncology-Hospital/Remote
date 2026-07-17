using RemoteDesktop.Agent;
using Velopack;
using Velopack.Sources;

namespace RemoteDesktop.AdminApp;

internal static class AutoUpdateService
{
    private const string RepositoryUrl = "https://github.com/Oncology-Hospital/Remote";
    private const string LanReleaseDirectory = @"\\10.100.100.4\Website\App_IT\Remote";

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
            var candidate = await FindAvailableUpdateAsync();
            if (candidate is null)
            {
                return;
            }

            var (manager, update) = candidate;
            var targetVersion = update.TargetFullRelease.Version.ToString();
            var confirmation = MessageBox.Show(
                owner,
                isVietnamese
                    ? $"Đã phát hiện phiên bản mới v{targetVersion}.{Environment.NewLine}{Environment.NewLine}Phiên bản hiện tại: {ApplicationVersionInfo.Display}{Environment.NewLine}Bạn có muốn cập nhật ngay bây giờ không?"
                    : $"A new version v{targetVersion} is available.{Environment.NewLine}{Environment.NewLine}Current version: {ApplicationVersionInfo.Display}{Environment.NewLine}Would you like to update now?",
                isVietnamese ? "Cập nhật phần mềm" : "Software update",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1);

            if (confirmation != DialogResult.Yes)
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

    private static async Task<UpdateCandidate?> FindAvailableUpdateAsync()
    {
        UpdateCandidate? latest = null;

        foreach (var manager in CreateUpdateManagers())
        {
            try
            {
                var update = await manager.CheckForUpdatesAsync();
                if (update is null)
                {
                    continue;
                }

                if (latest is null || IsNewer(update, latest.Update))
                {
                    latest = new UpdateCandidate(manager, update);
                }
            }
            catch (Exception exception)
            {
                // A source can be unavailable (for example, a laptop outside the LAN).
                // Continue with the remaining source instead of blocking application startup.
                LogError(exception);
            }
        }

        return latest;
    }

    private static IEnumerable<UpdateManager> CreateUpdateManagers()
    {
        if (Directory.Exists(LanReleaseDirectory))
        {
            yield return new UpdateManager(
                new SimpleFileSource(new DirectoryInfo(LanReleaseDirectory)));
        }

        yield return new UpdateManager(
            new GithubSource(
                RepositoryUrl,
                accessToken: null,
                prerelease: false));
    }

    private static bool IsNewer(UpdateInfo candidate, UpdateInfo current)
    {
        var candidateVersion = candidate.TargetFullRelease.Version.ToString();
        var currentVersion = current.TargetFullRelease.Version.ToString();

        if (Version.TryParse(candidateVersion, out var parsedCandidate)
            && Version.TryParse(currentVersion, out var parsedCurrent))
        {
            return parsedCandidate > parsedCurrent;
        }

        return string.Compare(candidateVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0;
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

    private sealed record UpdateCandidate(UpdateManager Manager, UpdateInfo Update);
}
