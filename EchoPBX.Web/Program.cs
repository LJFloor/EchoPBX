using EchoPBX.Data;
using EchoPBX.Data.Clients.Ami;
using EchoPBX.Data.Clients.Stun;
using EchoPBX.Data.Helpers;
using EchoPBX.Data.Services.ContactSearch;
using EchoPBX.Data.Services.Settings;
using EchoPBX.Data.Workers;
using EchoPBX.Data.Workers.Asterisk;
using EchoPBX.Data.Workers.Cdr;
using EchoPBX.Repositories;
using EchoPBX.Repositories.CallFlowWrite;
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

    var certificate = CertificateHelper.LoadOrCreate();
    Log.Information("HTTPS certificate: {Subject}, valid until {NotAfter}", certificate.Subject, certificate.NotAfter);

    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(Constants.HttpPort);
        serverOptions.ListenAnyIP(Constants.HttpsPort, listenOptions => listenOptions.UseHttps(certificate));
    });

    builder.Services
        .AddSerilog()
        .AddWebSockets(x => x.KeepAliveInterval = TimeSpan.FromSeconds(30))
        .AddRepositories()
        .AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new EchoPBX.Web.Converters.UploadedFileJsonConverter());

            // Polymorphic types such as CallFlowNode need their "type" discriminator, and the
            // dashboard does not guarantee it is the first property of the object.
            options.JsonSerializerOptions.AllowOutOfOrderMetadataProperties = true;
        });

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
    var soundsProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.Combine(Constants.DataDirectory, "sounds"));

    // Uploads are stored as an 8 kHz .wav with a 16 kHz .wav16 next to it. The dashboard keeps
    // using the .wav URL, since that is how the save code recognises a sound, but gets the
    // better sounding file when there is one.
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/sounds", out var subpath)
            && subpath.Value!.EndsWith(".wav")
            && soundsProvider.GetFileInfo(subpath.Value + "16").Exists)
        {
            context.Request.Path += "16";
        }

        await next();
    });

    var soundContentTypes = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
    soundContentTypes.Mappings[".wav16"] = "audio/wav";
    app.UseStaticFiles(new StaticFileOptions
    {
        RequestPath = "/sounds",
        FileProvider = soundsProvider,
        ContentTypeProvider = soundContentTypes,
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

    await scope.ServiceProvider.GetRequiredService<ICallFlowWriteRepository>().MoveStraySounds();

    // Settings
    Log.Information("Loading settings");
    var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
    await settingsService.InitializeAsync();
    Log.Information("Settings loaded");
    
    var workerManager = scope.ServiceProvider.GetRequiredService<WorkerManager>();
    workerManager.Start();
    app.Lifetime.ApplicationStopping.Register(() => workerManager.StopAsync().GetAwaiter().GetResult());

    app.Run();
}
catch (Exception ex)
{
    Log.Logger.Fatal(ex, $"Fatal error: {ex.Message}");
}