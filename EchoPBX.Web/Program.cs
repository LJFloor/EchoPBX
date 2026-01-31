using EchoPBX.Data;
using EchoPBX.Data.Clients.Ami;
using EchoPBX.Data.Clients.Stun;
using EchoPBX.Data.Services.ContactSearch;
using EchoPBX.Data.Services.Settings;
using EchoPBX.Data.Workers;
using EchoPBX.Data.Workers.Asterisk;
using EchoPBX.Data.Workers.Cdr;
using EchoPBX.Repositories;
using EchoPBX.Web.Authentication;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(Constants.DataDirectory, "logs", "echopbx-web-.log"), rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Logger.Information("=================== BOOTUP ==================");
Log.Logger.Information("Starting EchoPBX Web {Version}...", Constants.Version);

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.ConfigureKestrel(serverOptions => { serverOptions.ListenAnyIP(Constants.HttpPort); });

    builder.Services
        .AddSerilog()
        .AddWebSockets(x => x.KeepAliveInterval = TimeSpan.FromSeconds(30))
        .AddRepositories()
        .AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new EchoPBX.Web.Converters.UploadedFileJsonConverter()));

    builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    // Scoped
    builder.Services.AddScoped<EchoDbContext>();
    builder.Services.AddScoped<IContactSearchService, ContactSearchService>();
    builder.Services.AddScoped<IAmiClient, AmiClient>();
    builder.Services.AddScoped<AuthenticationMiddleware>();

    // Singleton
    builder.Services.AddSingleton<IStunClient, StunClient>();
    builder.Services.AddSingleton<ISettingsService, SettingsService>();
    builder.Services.AddSingleton<WorkerManager>();

    builder.Services.AddWorker<IAsteriskWorker, AsteriskWorker>();
    builder.Services.AddWorker<ICdrWorker, CdrWorker>();

    var app = builder.Build();
    app.MapControllers();
    app.UseSerilogRequestLogging();
    app.UseWebSockets();
    app.UseMiddleware<AuthenticationMiddleware>();
    app.MapReverseProxy();

    Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "wwwroot"));
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");

    Directory.CreateDirectory(Path.Combine(Constants.DataDirectory, "sounds"));
    app.UseStaticFiles(new StaticFileOptions
    {
        RequestPath = "/sounds",
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.Combine(Constants.DataDirectory, "sounds")),
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers.Pragma = "no-cache";
            ctx.Context.Response.Headers.Expires = "0";
        },
    });

    Log.Information("Data directory is set to: {DataDirectory}", Constants.DataDirectory);

    // Database
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EchoDbContext>();
    Log.Information("Checking database migrations...");
    var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();
    if (pendingMigrations.Length != 0)
    {
        Log.Information("Found {Count} pending migrations...", pendingMigrations.Length);
        foreach (var migration in pendingMigrations)
        {
            Log.Information(" - migration {Migration}...", migration);
        }

        Log.Information("Applying migrations...");
        await dbContext.Database.MigrateAsync();
    }
    else
    {
        Log.Information("No pending migrations found.");
    }

    // Settings
    Log.Information("Loading settings");
    var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
    await settingsService.InitializeAsync();
    Log.Information("Settings loaded");
    
    var workerManager = scope.ServiceProvider.GetRequiredService<WorkerManager>();
    workerManager.Start();

    app.Run();
}
catch (Exception ex)
{
    Log.Logger.Fatal(ex, $"Fatal error: {ex.Message}");
}