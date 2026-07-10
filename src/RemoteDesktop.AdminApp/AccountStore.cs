using System.Text.Json;

namespace RemoteDesktop.AdminApp;

internal static class AccountStore
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Oncology-Hospital",
        "Remote");

    private static readonly string AccountsPath = Path.Combine(DataDirectory, "accounts.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static UserAccount? Validate(string username, string password)
    {
        return LoadAccounts()
            .FirstOrDefault(account =>
                string.Equals(account.Username, username, StringComparison.OrdinalIgnoreCase) &&
                account.Password == password);
    }

    public static bool HasAccounts() => LoadAccounts().Count > 0;

    public static string ConfigurationPath => AccountsPath;

    private static IReadOnlyList<UserAccount> LoadAccounts()
    {
        Directory.CreateDirectory(DataDirectory);

        if (!File.Exists(AccountsPath))
        {
            // Migrate credentials from builds created before accounts.json was
            // moved outside the application directory. Update packages replace
            // application files, but LocalAppData is preserved.
            var legacyPath = Path.Combine(AppContext.BaseDirectory, "accounts.json");
            if (File.Exists(legacyPath))
            {
                File.Copy(legacyPath, AccountsPath);
            }
            else
            {
                File.WriteAllText(AccountsPath, "[]");
                return [];
            }
        }

        var json = File.ReadAllText(AccountsPath);
        return JsonSerializer.Deserialize<List<UserAccount>>(json, JsonOptions) ?? [];
    }
}

internal sealed class UserAccount
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "user";
}
