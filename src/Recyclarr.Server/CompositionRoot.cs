using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Common;
using Recyclarr.Pipelines;
using Recyclarr.ResourceProviders;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Notifications;
using Recyclarr.Server.Sync.Notifications.Apprise;
using Recyclarr.Server.Sync.Progress;
using Recyclarr.Server.Sync.Results;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Recyclarr.Sync;
using Refit;
using Serilog.Events;
using LoggingLevelSwitch = Serilog.Core.LoggingLevelSwitch;

namespace Recyclarr.Server;

internal static class CompositionRoot
{
    private static readonly RefitSettings AppriseRefitSettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
            }
        ),
        CaptureRequestContent = true,
    };

    // Overload for tests and other in-process hosts: default standalone logging.
    public static void Setup(ContainerBuilder builder) =>
        Setup(builder, new ServerLogOptions(LogEventLevel.Information, UseParentProtocol: false));

    public static void Setup(ContainerBuilder builder, ServerLogOptions logOptions)
    {
        var thisAssembly = typeof(CompositionRoot).Assembly;

        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();

        RegisterLogger(builder, logOptions);
        RegisterNotifications(builder);

        builder.RegisterModule<CoreAutofacModule>();
        builder.RegisterModule<PipelineAutofacModule>();
        builder.RegisterModule<ResourceProviderAutofacModule>();

        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.Register(_ => new ResourceDataReader(thisAssembly)).As<IResourceDataReader>();

        builder.RegisterType<ConsoleReadySignal>().As<IReadySignal>().SingleInstance();

        RegisterSyncServices(builder);
    }

    private static void RegisterSyncServices(ContainerBuilder builder)
    {
        builder.RegisterType<ServerConfigLoader>();

        builder.RegisterType<InMemorySyncJobStore>().As<ISyncJobStore>().SingleInstance();

        builder.RegisterType<SyncJobLauncher>();
        builder.RegisterType<SyncJobFinalizer>();
        builder.RegisterType<NotificationService>().InstancePerMatchingLifetimeScope("run");
        builder
            .Register<INotificationService>(c =>
            {
                var settings = c.Resolve<ISettings<NotificationSettings>>().Value;
                return settings.Apprise is not null
                    ? c.Resolve<NotificationService>()
                    : new NoopNotificationService();
            })
            .InstancePerMatchingLifetimeScope("run");
        builder.RegisterMatchingScope(
            "run",
            b =>
            {
                b.RegisterType<SyncJobRunner>();
                b.RegisterType<SyncJobProgress>();
                b.RegisterType<SyncResultLogger>();
                b.RegisterType<SyncDiagnosticsLogger>();
                b.RegisterType<ServerSyncFaultReporter>().As<ISyncFaultReporter>();
            }
        );
    }

    private static void RegisterNotifications(ContainerBuilder builder)
    {
        builder.RegisterType<AppriseNotificationApiService>().As<IAppriseNotificationApiService>();
        builder
            .Register(c =>
            {
                var factory = c.Resolve<IHttpClientFactory>();
                var settings = c.Resolve<ISettings<NotificationSettings>>().Value;
                var apprise =
                    settings.Apprise
                    ?? throw new InvalidOperationException(
                        "No Apprise notification settings have been defined"
                    );
                var client = factory.CreateClient("apprise");
                client.BaseAddress = apprise.BaseUrl;
                return RestService.For<IAppriseApi>(client, AppriseRefitSettings);
            })
            .As<IAppriseApi>();
        builder.Register(c =>
        {
            var settings = c.Resolve<ISettings<NotificationSettings>>().Value;
            return VerbosityOptions.From(settings.Verbosity);
        });
    }

    private static void RegisterLogger(ContainerBuilder builder, ServerLogOptions logOptions)
    {
        builder.RegisterInstance(logOptions);
        builder.Register(_ => new LoggingLevelSwitch(logOptions.MinimumLevel)).SingleInstance();
        builder.RegisterType<ServerLogger>().SingleInstance();
        builder.RegisterType<ServerLogJanitor>();
    }
}
