using Microsoft.Extensions.FileProviders;

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
        app.MapHub<RemoteHub>("/remoteHub");

        return app;
    }
}
