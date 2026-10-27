using System.IO.Abstractions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Options;
using Recyclarr.Logging;
using Recyclarr.Platform;
using Recyclarr.Server.Api;
using Recyclarr.Server.Logging;
using Recyclarr.Server.Observability;
using Recyclarr.Server.OutboundHttp;
using Recyclarr.Server.Persistence;
using Recyclarr.Server.Resources;
using Recyclarr.Server.Sync.Schedule;
using Recyclarr.Settings.Configuration;
using TickerQ.DependencyInjection;

namespace Recyclarr.Server.Hosting;

/// <summary>
/// Assembles the server host. <c>Program</c> passes the real host inputs; tests pass in-memory
/// ones and add their own container registrations between <see cref="CreateBuilder"/> and
/// <see cref="Build"/>.
/// </summary>
/// <remarks>
/// Host inputs are parameters, not container registrations, because configuration providers and
/// registration-time code run before the container exists. The given instances are also registered
/// in the container, so code that runs before and after the container is built sees the same
/// filesystem.
/// </remarks>
internal static class ServerHost
{
    public static WebApplicationBuilder CreateBuilder(
        string[] args,
        IFileSystem fileSystem,
        IReadOnlyDictionary<string, string?> environment
    )
    {
        var builder = WebApplication.CreateSlimBuilder(
            new WebApplicationOptions
            {
                Args = args,
                // A Windows service starts in C:\Windows\System32, which would otherwise be the
                // content root.
                ContentRootPath = WindowsServiceHelpers.IsWindowsService()
                    ? AppContext.BaseDirectory
                    : null,
            }
        );

        AddConfigurationSources(builder.Configuration, args, environment);

        var launch = ServerLaunchOptions.From(builder.Configuration);

        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        builder.Host.ConfigureContainer<ContainerBuilder>(b =>
        {
            CompositionRoot.Setup(b, launch.LogLevel, launch.Mode);
            b.RegisterInstance(launch);
            b.RegisterInstance(fileSystem).As<IFileSystem>();
        });

        var services = builder.Services;
        var (settings, settingsReport) = LoadSettings(builder.Configuration, fileSystem, launch);
        services.AddRecyclarrSettings(settings, settingsReport);
        services.AddServerLogging();
        services.AddOutboundHttpClients();
        services.AddServerTelemetry(
            builder.Configuration,
            new ManagementPort(
                Options.Create(SettingsBinder.BindOrDefault(settings, settingsReport).Server),
                builder.Configuration,
                launch.Mode
            )
        );

        // Each takes over the host lifetime only when its service manager launched this process
        // (readiness and stop notifications for a systemd unit, the SCM protocol for a Windows
        // service). Under a terminal, Docker, or Kubernetes, the default console lifetime remains.
        services.AddSystemd();
        services.AddWindowsService();

        services.AddServerApi();
        services.AddHealthChecks();

        if (launch.ParentPid is { } parentPid)
        {
            services.AddHostedService(sp => new StdinLifelineMonitor(
                sp.GetRequiredService<IHostApplicationLifetime>(),
                parentPid
            ));
        }

        if (!launch.IsDocumentGeneration)
        {
            services.AddServerPersistence();
            services.AddHostedService<ServerBootstrapService>();
            services.AddHostedService<SyncScheduler>();
            services.AddHostedService<ResourceRefreshWorker>();
        }

        return builder;
    }

    // settings.yml has its own configuration, separate from the host's, so environment variables
    // and command-line arguments can never set a setting. Settings problems, including a config
    // directory that cannot be located, do not throw here: startup reports them (ServerLifecycle).
    private static (IConfiguration Settings, SettingsLoadReport Report) LoadSettings(
        IConfiguration host,
        IFileSystem fileSystem,
        ServerLaunchOptions launch
    )
    {
        var settings = new ConfigurationBuilder();
        var report = new SettingsLoadReport();

        // Build-time document generation must not read the developer's settings.
        if (!launch.IsDocumentGeneration)
        {
            try
            {
                var configDirectory = new ConfigDirectoryLocator(
                    host,
                    new DefaultSpecialFolders(),
                    fileSystem
                ).Locate();
                report = settings.AddSettingsYaml(configDirectory, host);
            }
            catch (EnvironmentException)
            {
                // The same lookup fails again, with this message, when startup resolves the app
                // paths.
            }
        }

        return (settings.Build(), report);
    }

    // Replaces the default sources so the environment comes from the given variables, not the
    // process. Order and prefixes match the web host defaults; later sources win.
    private static void AddConfigurationSources(
        ConfigurationManager configuration,
        string[] args,
        IReadOnlyDictionary<string, string?> environment
    )
    {
        configuration.Sources.Clear();
        configuration
            .AddEnvironment(environment, "DOTNET_")
            .AddEnvironment(environment, "ASPNETCORE_")
            .AddEnvironment(environment)
            .AddCommandLine(args);
    }

    /// <summary>
    /// Builds the host and its request pipeline. Returns null when the environment is unusable
    /// (for example, a retired variable); the reason is already on stderr. Building resolves
    /// services that need the app paths, so this happens before the startup failure path exists.
    /// </summary>
    public static WebApplication? Build(WebApplicationBuilder builder)
    {
        try
        {
            return BuildPipeline(builder);
        }
        catch (Exception e)
            when ((e as EnvironmentException ?? e.FindInnerException<EnvironmentException>())
                    is { } environment
            )
        {
            using var log = ServerLogger.CreateErrorsToStderr();
            log.Fatal("Server startup failed: {Message}", environment.Message);
            return null;
        }
    }

    private static WebApplication BuildPipeline(WebApplicationBuilder builder)
    {
        var app = builder.Build();
        var launch = app.Services.GetRequiredService<ServerLaunchOptions>();

        if (launch.Mode == ServerMode.Persistent && !launch.IsDocumentGeneration)
        {
            app.UseManagementPort();
        }

        app.UseSerilogRequestLogging();
        app.UseAuthentication();
        app.UseAuthorization();
        if (!launch.IsDocumentGeneration)
        {
            app.UseMiddleware<GuideLeaseMiddleware>();
        }

        app.UseServerApi();

        if (!launch.IsDocumentGeneration)
        {
            app.UseTickerQ();
        }

        return app;
    }
}
