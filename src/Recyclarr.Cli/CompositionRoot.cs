using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using Autofac.Extras.Ordering;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Console.Setup;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Migration;
using Recyclarr.Cli.Migration.Steps;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.Processors.Delete;
using Recyclarr.Cli.Processors.ErrorHandling;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Common;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Logging;
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

        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.Register(_ => new ResourceDataReader(thisAssembly)).As<IResourceDataReader>();

        builder.RegisterAutoMapper(thisAssembly);

        CliRegistrations(builder);
        RegisterMigrations(builder);
        RegisterServiceProcessors(builder);
    }

    private static void RegisterServiceProcessors(ContainerBuilder builder)
    {
        builder.RegisterType<ConsoleExceptionHandler>();
        builder.RegisterType<FlurlHttpExceptionHandler>();

        // Sync
        builder.RegisterType<SyncProcessor>();

        // Configuration
        builder.RegisterType<ConfigCreationProcessor>().As<IConfigCreationProcessor>();
        builder.RegisterType<ConfigListLocalProcessor>();
        builder.RegisterType<ConfigListTemplateProcessor>();

        // Delete
        builder.RegisterType<DeleteCustomFormatsProcessor>().As<IDeleteCustomFormatsProcessor>();

        builder
            .RegisterTypes(typeof(TemplateConfigCreator), typeof(LocalConfigCreator))
            .As<IConfigCreator>()
            .OrderByRegistration();
    }

    private static void RegisterMigrations(ContainerBuilder builder)
    {
        builder.RegisterType<MigrationExecutor>();

        // Migration Steps
        builder
            .RegisterTypes(typeof(MoveOsxAppDataDotnet8), typeof(DeleteRepoDirMigrationStep))
            .As<IMigrationStep>()
            .OrderByRegistration();
    }

    private static void RegisterLogger(ContainerBuilder builder)
    {
        // Log Configurators
        builder.RegisterType<FileLogSinkConfigurator>().As<ILogConfigurator>();

        builder.RegisterType<LoggingLevelSwitch>().SingleInstance();
        builder.RegisterType<LoggerFactory>().SingleInstance();
        builder.RegisterType<IndirectLoggerDecorator>().As<ILogger>();

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
                typeof(AppDataDirSetupTask), // This must be first; ILogger creation depends on IAppPaths
                typeof(LoggerSetupTask),
                typeof(ProgramInformationDisplayTask),
                typeof(JanitorCleanupTask)
            )
            .As<IGlobalSetupTask>()
            .OrderByRegistration();

        builder
            .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AssignableTo<CommandSettings>();
    }
}
