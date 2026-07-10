using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace RemoteDesktop.Server;

public static class ServerHost
{
    public static WebApplication Build(string[] args, string? contentRoot = null)
    {
        var options = new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = contentRoot ?? Directory.GetCurrentDirectory()
        };

        var builder = WebApplication.CreateBuilder(options);

        builder.Services.AddSingleton<MachineRegistry>();
        builder.Services.AddHostedService<LanDiscoveryBroadcaster>();
        builder.Services.AddSignalR(signalROptions =>
        {
            signalROptions.MaximumReceiveMessageSize = 8 * 1024 * 1024;
        });

        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        var signalRBrowserFiles = Path.Combine(
            app.Environment.ContentRootPath,
            "node_modules",
            "@microsoft",
            "signalr",
            "dist",
            "browser");

        if (Directory.Exists(signalRBrowserFiles))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(signalRBrowserFiles),
                RequestPath = "/vendor/signalr"
            });
        }

        app.MapGet("/health", () => Results.Ok(new { status = "ok", timeUtc = DateTime.UtcNow }));
        app.MapGet("/version", () => Results.Ok(new { version = ReadApplicationVersion() }));
        app.MapHub<RemoteHub>("/remoteHub");

        return app;
    }

    private static string ReadApplicationVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion.Split('+', 2)[0];
        }

        var version = assembly.GetName().Version;
        return version is null
            ? "0.0.0"
            : $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";
    }
}
