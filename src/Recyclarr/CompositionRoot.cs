using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using Autofac.Core.Activators.Reflection;
using Autofac.Extras.Ordering;
using CliFx;
using CliFx.Infrastructure;
using Common;
using Recyclarr.Command.Helpers;
using Recyclarr.Command.Services;
using Recyclarr.Config;
using Recyclarr.Migration;
using Serilog;
using Serilog.Core;
using TrashLib.Cache;
using TrashLib.Config;
using TrashLib.Radarr;
using TrashLib.Radarr.Config;
using TrashLib.Repo;
using TrashLib.Sonarr;
using TrashLib.Startup;
using VersionControl;
using YamlDotNet.Serialization;

namespace Recyclarr;

public static class CompositionRoot
{
    private static void SetupLogging(ContainerBuilder builder)
    {
        builder.RegisterType<LogJanitor>().As<ILogJanitor>();
        builder.RegisterType<LoggingLevelSwitch>().SingleInstance();
        builder.Register(c =>
            {
                var logPath = Path.Combine(AppPaths.LogDirectory,
                    $"trash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

                const string consoleTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";

                return new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console(outputTemplate: consoleTemplate, levelSwitch: c.Resolve<LoggingLevelSwitch>())
                    .WriteTo.File(logPath)
                    .CreateLogger();
            })
            .As<ILogger>()
            .SingleInstance();
    }

    private static void ConfigurationRegistrations(ContainerBuilder builder)
    {
        builder.RegisterModule<ConfigAutofacModule>();

        builder.RegisterType<ObjectFactory>().As<IObjectFactory>();
        builder.RegisterType<ResourcePaths>().As<IResourcePaths>();

        builder.RegisterGeneric(typeof(ConfigurationLoader<>))
            .WithProperty(new AutowiringParameter())
            .As(typeof(IConfigurationLoader<>));
    }

    private static void CommandRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<SonarrService>();
        builder.RegisterType<RadarrService>();
        builder.RegisterType<ServiceInitialization>().As<IServiceInitialization>();

        // Register all types deriving from CliFx's ICommand. These are all of our supported subcommands.
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AssignableTo<ICommand>();

        // Used to access the chosen command class. This is assigned from CliTypeActivator
        //
        // note: Do not allow consumers to resolve IServiceConfiguration directly; if this gets cached they end up using
        // the wrong configuration when multiple instances are used.
        builder.RegisterType<ActiveServiceCommandProvider>()
            .As<IActiveServiceCommandProvider>()
            .SingleInstance();
    }

    public static IContainer Setup()
    {
        return Setup(new ContainerBuilder());
    }

    public static IContainer Setup(ContainerBuilder builder)
    {
        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();

        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.RegisterType<FileUtilities>().As<IFileUtilities>();
        builder.RegisterType<SystemConsole>().As<IConsole>().SingleInstance();

        builder.RegisterModule<CacheAutofacModule>();
        builder.RegisterType<CacheStoragePath>().As<ICacheStoragePath>();
        builder.RegisterType<RepoUpdater>().As<IRepoUpdater>();

        ConfigurationRegistrations(builder);
        CommandRegistrations(builder);

        SetupLogging(builder);

        builder.RegisterModule<SonarrAutofacModule>();
        builder.RegisterModule<RadarrAutofacModule>();
        builder.RegisterModule<VersionControlAutofacModule>();
        builder.RegisterModule<MigrationAutofacModule>();

        builder.Register(_ => AutoMapperConfig.Setup()).SingleInstance();

        return builder.Build();
    }
}
