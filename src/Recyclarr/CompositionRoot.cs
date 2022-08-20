using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using Autofac.Core.Activators.Reflection;
using Autofac.Extras.Ordering;
using CliFx;
using CliFx.Infrastructure;
using Common;
using Recyclarr.Command.Helpers;
using Recyclarr.Config;
using Recyclarr.Logging;
using Recyclarr.Migration;
using Serilog;
using Serilog.Events;
using TrashLib;
using TrashLib.Cache;
using TrashLib.Config;
using TrashLib.Radarr;
using TrashLib.Repo;
using TrashLib.Sonarr;
using TrashLib.Startup;
using VersionControl;
using YamlDotNet.Serialization;

namespace Recyclarr;

internal class CompositionRoot : ICompositionRoot
{
    public IServiceLocatorProxy Setup(string? appDataDir, IConsole console, LogEventLevel logLevel)
    {
        return Setup(new ContainerBuilder(), appDataDir, console, logLevel);
    }

    public IServiceLocatorProxy Setup(ContainerBuilder builder, string? appDataDir, IConsole console,
        LogEventLevel logLevel)
    {
        builder.RegisterInstance(console).As<IConsole>().ExternallyOwned();

        RegisterAppPaths(builder, appDataDir);
        RegisterLogger(builder, logLevel);

        builder.RegisterModule<SonarrAutofacModule>();
        builder.RegisterModule<RadarrAutofacModule>();
        builder.RegisterModule<VersionControlAutofacModule>();
        builder.RegisterModule<MigrationAutofacModule>();
        builder.RegisterModule<RepoAutofacModule>();

        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();

        builder.RegisterModule<CacheAutofacModule>();
        builder.RegisterType<CacheStoragePath>().As<ICacheStoragePath>();
        builder.RegisterType<ProgressBar>();

        ConfigurationRegistrations(builder);
        CommandRegistrations(builder);

        builder.Register(_ => AutoMapperConfig.Setup()).SingleInstance();

        return new ServiceLocatorProxy(builder.Build());
    }

    private void RegisterLogger(ContainerBuilder builder, LogEventLevel logLevel)
    {
        builder.RegisterType<LogJanitor>().As<ILogJanitor>();
        builder.RegisterType<LoggerFactory>();
        builder.Register(c => c.Resolve<LoggerFactory>().Create(logLevel))
            .As<ILogger>()
            .SingleInstance();
    }

    private void RegisterAppPaths(ContainerBuilder builder, string? appDataDir)
    {
        builder.RegisterModule<CommonAutofacModule>();
        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.RegisterType<DefaultAppDataSetup>();

        builder.Register(c => c.Resolve<DefaultAppDataSetup>().CreateAppPaths(appDataDir))
            .As<IAppPaths>()
            .SingleInstance();
    }

    private static void ConfigurationRegistrations(ContainerBuilder builder)
    {
        builder.RegisterModule<ConfigAutofacModule>();

        builder.RegisterType<ObjectFactory>().As<IObjectFactory>();
        builder.RegisterType<ConfigurationFinder>().As<IConfigurationFinder>();

        builder.RegisterGeneric(typeof(ConfigurationLoader<>))
            .WithProperty(new AutowiringParameter())
            .As(typeof(IConfigurationLoader<>));
    }

    private static void CommandRegistrations(ContainerBuilder builder)
    {
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
}
