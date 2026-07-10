using System.Text.Json;

namespace RemoteDesktop.AdminApp;

internal static class AccountStore
{
    private const string DefaultUsername = ".\\administrator";
    private const string DefaultPassword = "cntt@it";

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
                return CreateDefaultAccounts();
            }
        }

        var json = File.ReadAllText(AccountsPath);
        var accounts = JsonSerializer.Deserialize<List<UserAccount>>(json, JsonOptions) ?? [];
        return accounts.Count == 0 ? CreateDefaultAccounts() : accounts;
    }

    private static IReadOnlyList<UserAccount> CreateDefaultAccounts()
    {
        var accounts = new[]
        {
            new UserAccount
            {
                Username = DefaultUsername,
                Password = DefaultPassword,
                Role = "admin"
            }
        };

        File.WriteAllText(AccountsPath, JsonSerializer.Serialize(accounts, JsonOptions));
        return accounts;
    }
}

internal sealed class UserAccount
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "user";
}
