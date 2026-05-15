using System.IO.Abstractions;
using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Cli.ConfigFilterRendering;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Setup;
using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Cli.ErrorHandling.Strategies;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Preview;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Common;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Filtering;
using Recyclarr.ErrorHandling;
using Recyclarr.Logging;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.QualityProfile.Models;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.ResourceProviders;
using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Servarr.MediaNaming;
using Serilog.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli;

internal static class CompositionRoot
{
    public static void Setup(ContainerBuilder builder)
    {
        var thisAssembly = typeof(CompositionRoot).Assembly;

        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();

        RegisterLogger(builder);

        builder.RegisterModule<CoreAutofacModule>();
        builder.RegisterModule<PipelineAutofacModule>();
        builder.RegisterModule<ResourceProviderAutofacModule>();

        RegisterPreviewRenderers(builder);

        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.Register(_ => new ResourceDataReader(thisAssembly)).As<IResourceDataReader>();

        CliRegistrations(builder);
        RegisterServiceProcessors(builder);
        RegisterConfigServices(builder);
    }

    private static void RegisterPreviewRenderers(ContainerBuilder builder)
    {
        builder
            .RegisterType<CustomFormatPreviewRenderer>()
            .As<IPreviewRenderer<CustomFormatPreviewData>>();
        builder
            .RegisterType<QualityProfilePreviewRenderer>()
            .As<IPreviewRenderer<QualityProfileTransactionData>>();
        builder
            .RegisterType<QualitySizePreviewRenderer>()
            .As<IPreviewRenderer<QualitySizePreviewData>>();
        builder
            .RegisterType<SonarrNamingPreviewRenderer>()
            .As<IPreviewRenderer<SonarrNamingData>>();
        builder
            .RegisterType<RadarrNamingPreviewRenderer>()
            .As<IPreviewRenderer<RadarrNamingData>>();
        builder
            .RegisterType<MediaManagementPreviewRenderer>()
            .As<IPreviewRenderer<MediaManagementData>>();
    }

    private static void RegisterServiceProcessors(ContainerBuilder builder)
    {
        RegisterErrorHandling(builder);

        // Sync (registered in named "sync" scope for lifecycle management)
        builder.RegisterMatchingScope(
            "sync",
            b =>
            {
                b.RegisterType<SyncCommandHandler>();
                b.RegisterType<SyncProgressRenderer>();
                b.RegisterType<DiagnosticsRenderer>();
            }
        );

        // Configuration pipeline
        builder.RegisterType<ConfigPipelineFactory>();

        builder.RegisterType<ConfigListLocalProcessor>();
        builder.RegisterType<ConfigListTemplateProcessor>();
    }

    private static void RegisterErrorHandling(ContainerBuilder builder)
    {
        // CLI-specific exception strategies
        builder.RegisterType<ServiceExceptionStrategy>().As<IExceptionStrategy>();

        // Output strategies (routing)
        builder.RegisterType<FatalErrorOutputStrategy>();

        // Handler (orchestrator)
        builder.RegisterType<ExceptionHandler>();
    }

    private static void RegisterLogger(ContainerBuilder builder)
    {
        // Log Configurators
        builder.RegisterType<FileLogSinkConfigurator>().As<ILogConfigurator>();
        builder.RegisterType<ConsoleLogSinkConfigurator>();

        builder.RegisterType<LoggingLevelSwitch>().SingleInstance();
        builder.RegisterType<ReloadableLogger>().AsSelf().As<ILogger>().SingleInstance();

        builder.RegisterType<LogJanitor>();
        builder.RegisterType<ValidationLogger>();
    }

    private static void CliRegistrations(ContainerBuilder builder)
    {
        builder.RegisterInstance(AnsiConsole.Console);
        builder.RegisterType<AutofacTypeRegistrar>().As<ITypeRegistrar>();
        builder.RegisterType<CommandApp>();
        builder.RegisterType<CommandSetupInterceptor>().As<ICommandInterceptor>();

        builder.RegisterComposite<CompositeGlobalSetupTask, IGlobalSetupTask>();
        builder
            .RegisterTypes(
                typeof(ConsoleSetupTask), // Must run before LoggerSetupTask (handles console redirect)
                typeof(LoggerSetupTask),
                typeof(ProgramInformationDisplayTask),
                typeof(JanitorCleanupTask)
            )
            .As<IGlobalSetupTask>()
            .OrderByRegistration();

        builder.RegisterType<ProviderProgressHandler>();
    }

    private static void RegisterConfigServices(ContainerBuilder builder)
    {
        builder.RegisterType<ConsoleFilterResultRenderer>().As<IFilterResultRenderer>();
        builder
            .RegisterTypes(
                typeof(DuplicateInstancesFilterResultRenderer),
                typeof(InvalidInstancesFilterResultRenderer),
                typeof(NonExistentInstancesFilterResultRenderer),
                typeof(SplitInstancesFilterResultRenderer)
            )
            .As<IConsoleFilterResultRenderer>();
    }
}
