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
using Recyclarr.Cli.Migration;
using Recyclarr.Cli.Migration.Steps;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.Processors.Delete;
using Recyclarr.Cli.Processors.StateRepair;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Common;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Filtering;
using Recyclarr.Logging;
using Recyclarr.ResourceProviders;
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

        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.Register(_ => new ResourceDataReader(thisAssembly)).As<IResourceDataReader>();

        CliRegistrations(builder);
        RegisterMigrations(builder);
        RegisterServiceProcessors(builder);
        RegisterConfigServices(builder);
    }

    private static void RegisterServiceProcessors(ContainerBuilder builder)
    {
        RegisterErrorHandling(builder);

        // Sync (registered in named "sync" scope for lifecycle management)
        builder.RegisterMatchingScope(
            "sync",
            b =>
            {
                b.RegisterType<SyncScope>();
                b.RegisterType<SyncProcessor>();
                b.RegisterType<SyncProgressRenderer>();
                b.RegisterType<DiagnosticsRenderer>();
                b.RegisterType<DiagnosticsLogger>();
            }
        );

        // Instance-level (resolved from "instance" child scope of "sync")
        builder.RegisterType<InstanceScope>();
        builder.RegisterType<InstanceSyncProcessor>();

        // Configuration
        builder.RegisterType<ConfigCreationProcessor>().As<IConfigCreationProcessor>();
        builder.RegisterType<ConfigListLocalProcessor>();
        builder.RegisterType<ConfigListTemplateProcessor>();

        // Delete
        builder.RegisterType<DeleteCustomFormatsProcessor>();

        // State
        builder.RegisterType<StateRepairProcessor>();
        builder.RegisterType<StateRepairInstanceProcessor>();
        builder.RegisterType<CustomFormatResourceAdapter>().As<IResourceAdapter>();
        builder.RegisterType<QualityProfileResourceAdapter>().As<IResourceAdapter>();

        builder
            .RegisterTypes(typeof(TemplateConfigCreator), typeof(LocalConfigCreator))
            .As<IConfigCreator>()
            .OrderByRegistration();
    }

    private static void RegisterErrorHandling(ContainerBuilder builder)
    {
        // Exception strategies (dispatch)
        builder.RegisterType<HttpExceptionStrategy>().As<IExceptionStrategy>();
        builder.RegisterType<GitExceptionStrategy>().As<IExceptionStrategy>();
        builder.RegisterType<ConfigExceptionStrategy>().As<IExceptionStrategy>();
        builder.RegisterType<ServiceExceptionStrategy>().As<IExceptionStrategy>();
        builder.RegisterType<YamlExceptionStrategy>().As<IExceptionStrategy>();
        builder.RegisterType<ValidationExceptionStrategy>().As<IExceptionStrategy>();
        builder.RegisterType<MigrationExceptionStrategy>().As<IExceptionStrategy>();

        // Output strategies (routing)
        builder.RegisterType<FatalErrorOutputStrategy>();
        builder.RegisterType<SyncEventOutputStrategy>();

        // Handler (orchestrator)
        builder.RegisterType<ExceptionHandler>();
    }

    private static void RegisterMigrations(ContainerBuilder builder)
    {
        var thisAssembly = typeof(CompositionRoot).Assembly;

        builder.RegisterType<MigrationExecutor>();

        // Migration steps auto-discovered via assembly scanning, ordered by MigrationOrderAttribute metadata
        builder
            .RegisterAssemblyTypes(thisAssembly)
            .AssignableTo<IMigrationStep>()
            .As<IMigrationStep>()
            .WithMetadataFrom<MigrationOrderAttribute>();
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
