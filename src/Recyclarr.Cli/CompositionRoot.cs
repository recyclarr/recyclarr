using System.IO.Abstractions;
using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Console.Setup;
using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Cli.Server;
using Recyclarr.Common;
using Recyclarr.Common.FluentValidation;
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

        // Core supplies settings.yml loading, application paths, and log setup. Sync, guide, and
        // instance operations go through the server.
        builder.RegisterModule<CoreAutofacModule>();

        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.Register(_ => new ResourceDataReader(thisAssembly)).As<IResourceDataReader>();

        CliRegistrations(builder);
        RegisterServiceProcessors(builder);
    }

    private static void RegisterServiceProcessors(ContainerBuilder builder)
    {
        RegisterErrorHandling(builder);

        // Sync runs server-side; these types only send the request and render what comes back.
        builder.RegisterType<SyncCommandHandler>();
        builder.RegisterType<SyncProgressRenderer>();
    }

    private static void RegisterErrorHandling(ContainerBuilder builder)
    {
        builder.RegisterType<ExceptionHandler>();
    }

    private static void RegisterLogger(ContainerBuilder builder)
    {
        builder.RegisterType<LoggingLevelSwitch>().SingleInstance();
        builder.RegisterType<ReloadableLogger>().AsSelf().As<ILogger>().SingleInstance();

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
                typeof(ProgramInformationDisplayTask)
            )
            .As<IGlobalSetupTask>()
            .OrderByRegistration();

        builder.RegisterServerApi();
    }
}
