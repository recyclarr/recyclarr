using System.IO.Abstractions;
using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Common;
using Recyclarr.Pipelines;
using Recyclarr.ResourceProviders;
using Recyclarr.Server.Sync;
using Serilog.Events;
using LoggingLevelSwitch = Serilog.Core.LoggingLevelSwitch;

namespace Recyclarr.Server;

internal static class CompositionRoot
{
    // Overload for tests and other in-process hosts: default standalone logging.
    public static void Setup(ContainerBuilder builder) =>
        Setup(builder, new ServerLogOptions(LogEventLevel.Information, UseParentProtocol: false));

    public static void Setup(ContainerBuilder builder, ServerLogOptions logOptions)
    {
        var thisAssembly = typeof(CompositionRoot).Assembly;

        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();

        RegisterLogger(builder, logOptions);

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
        builder.RegisterMatchingScope(
            "run",
            b =>
            {
                b.RegisterType<SyncJobRunner>();
                b.RegisterType<SyncDiagnosticsLogger>();
            }
        );
    }

    private static void RegisterLogger(ContainerBuilder builder, ServerLogOptions logOptions)
    {
        builder.RegisterInstance(logOptions);
        builder.Register(_ => new LoggingLevelSwitch(logOptions.MinimumLevel)).SingleInstance();
        builder.RegisterType<ServerLogger>().SingleInstance();
        builder.RegisterType<ServerLogJanitor>();
    }
}
