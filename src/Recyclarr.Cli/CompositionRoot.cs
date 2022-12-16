using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using Autofac.Core.Activators.Reflection;
using Autofac.Extras.Ordering;
using CliFx;
using Recyclarr.Cli.Command.Helpers;
using Recyclarr.Cli.Command.Setup;
using Recyclarr.Cli.Config;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Migration;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Cache;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Repo;
using Recyclarr.TrashLib.Repo.VersionControl;
using Recyclarr.TrashLib.Services.Common;
using Recyclarr.TrashLib.Services.CustomFormat;
using Recyclarr.TrashLib.Services.Radarr;
using Recyclarr.TrashLib.Services.Sonarr;
using Recyclarr.TrashLib.Services.System;
using Recyclarr.TrashLib.Startup;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Recyclarr.Cli;

public static class CompositionRoot
{
    public static ILifetimeScope Setup(Action<ContainerBuilder>? extraRegistrations = null)
    {
        return Setup(new ContainerBuilder(), extraRegistrations);
    }

    private static ILifetimeScope Setup(ContainerBuilder builder, Action<ContainerBuilder>? extraRegistrations = null)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => x.FullName?.StartsWithIgnoreCase("Recyclarr") ?? false)
            .ToArray();

        RegisterAppPaths(builder);
        RegisterLogger(builder);

        builder.RegisterModule<SonarrAutofacModule>();
        builder.RegisterModule<RadarrAutofacModule>();
        builder.RegisterModule<VersionControlAutofacModule>();
        builder.RegisterModule<MigrationAutofacModule>();
        builder.RegisterModule<RepoAutofacModule>();
        builder.RegisterModule<CustomFormatAutofacModule>();
        builder.RegisterModule<GuideServicesAutofacModule>();
        builder.RegisterModule<SystemServiceAutofacModule>();

        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();

        builder.RegisterModule<CacheAutofacModule>();
        builder.RegisterType<CacheStoragePath>().As<ICacheStoragePath>();
        builder.RegisterType<ServiceRequestBuilder>().As<IServiceRequestBuilder>();
        builder.RegisterType<ProgressBar>();

        ConfigurationRegistrations(builder, assemblies);
        CommandRegistrations(builder);

        builder.Register(_ => AutoMapperConfig.Setup()).SingleInstance();

        builder.RegisterType<FlurlClientFactory>().As<IFlurlClientFactory>().SingleInstance();

        extraRegistrations?.Invoke(builder);

        return builder.Build();
    }

    private static void RegisterLogger(ContainerBuilder builder)
    {
        builder.RegisterType<LogJanitor>().As<ILogJanitor>();
        builder.RegisterType<LoggerFactory>();
    }

    private static void RegisterAppPaths(ContainerBuilder builder)
    {
        builder.RegisterModule<CommonAutofacModule>();
        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.RegisterType<DefaultAppDataSetup>();
    }

    private static void ConfigurationRegistrations(ContainerBuilder builder, Assembly[] assemblies)
    {
        builder.RegisterModule(new ConfigAutofacModule(assemblies));

        builder.RegisterType<DefaultObjectFactory>().As<IObjectFactory>();
        builder.RegisterType<ConfigurationFinder>().As<IConfigurationFinder>();

        builder.RegisterGeneric(typeof(ConfigurationLoader<>))
            .WithProperty(new AutowiringParameter())
            .As(typeof(IConfigurationLoader<>));
    }

    private static void CommandRegistrations(ContainerBuilder builder)
    {
        builder.RegisterTypes(
                typeof(AppPathSetupTask),
                typeof(JanitorCleanupTask))
            .As<IBaseCommandSetupTask>()
            .OrderByRegistration();

        // Register all types deriving from CliFx's ICommand. These are all of our supported subcommands.
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AssignableTo<ICommand>();
    }
}
