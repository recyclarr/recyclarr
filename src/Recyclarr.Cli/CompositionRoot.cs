using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using Autofac.Extras.Ordering;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using Recyclarr.Cli.Cache;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Setup;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Migration;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Pipelines.MediaNaming;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Pipelines.ReleaseProfile;
using Recyclarr.Cli.Pipelines.Tags;
using Recyclarr.Cli.Processors;
using Recyclarr.Common;
using Recyclarr.Compatibility;
using Recyclarr.Config;
using Recyclarr.Json;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.ServarrApi;
using Recyclarr.Settings;
using Recyclarr.TrashGuide;
using Recyclarr.VersionControl;
using Recyclarr.Yaml;
using Serilog.Core;
using Spectre.Console.Cli;

namespace Recyclarr.Cli;

public static class CompositionRoot
{
    public static void Setup(ContainerBuilder builder)
    {
        var thisAssembly = typeof(CompositionRoot).Assembly;

        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();

        RegisterLogger(builder);

        builder.RegisterModule<MigrationAutofacModule>();
        builder.RegisterModule<ConfigAutofacModule>();
        builder.RegisterModule<GuideAutofacModule>();
        builder.RegisterModule<YamlAutofacModule>();
        builder.RegisterModule<ServiceProcessorsAutofacModule>();
        builder.RegisterModule<CacheAutofacModule>();
        builder.RegisterModule<SettingsAutofacModule>();
        builder.RegisterModule<ApiServicesAutofacModule>();
        builder.RegisterModule<VersionControlAutofacModule>();
        builder.RegisterModule<RepoAutofacModule>();
        builder.RegisterModule<CompatibilityAutofacModule>();
        builder.RegisterModule<JsonAutofacModule>();
        builder.RegisterModule<PlatformAutofacModule>();
        builder.RegisterModule<CommonAutofacModule>();

        builder.RegisterType<CacheStoragePath>().As<ICacheStoragePath>();
        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.Register(_ => new ResourceDataReader(thisAssembly)).As<IResourceDataReader>();

        builder.RegisterAutoMapper(thisAssembly);

        CommandRegistrations(builder);
        PipelineRegistrations(builder);
    }

    private static void PipelineRegistrations(ContainerBuilder builder)
    {
        builder.RegisterModule<TagsAutofacModule>();
        builder.RegisterModule<CustomFormatAutofacModule>();
        builder.RegisterModule<QualityProfileAutofacModule>();
        builder.RegisterModule<QualitySizeAutofacModule>();
        builder.RegisterModule<ReleaseProfileAutofacModule>();
        builder.RegisterModule<MediaNamingAutofacModule>();

        builder.RegisterTypes(
                typeof(TagSyncPipeline),
                typeof(CustomFormatSyncPipeline),
                typeof(QualityProfileSyncPipeline),
                typeof(QualitySizeSyncPipeline),
                typeof(ReleaseProfileSyncPipeline),
                typeof(MediaNamingSyncPipeline))
            .As<ISyncPipeline>()
            .OrderByRegistration();
    }

    private static void RegisterLogger(ContainerBuilder builder)
    {
        builder.RegisterType<LogJanitor>().As<ILogJanitor>();
        builder.RegisterType<LoggerFactory>();
        builder.Register(c => c.Resolve<LoggerFactory>().Create()).As<ILogger>().SingleInstance();
    }

    private static void CommandRegistrations(ContainerBuilder builder)
    {
        builder.RegisterTypes(
                typeof(AppPathSetupTask),
                typeof(JanitorCleanupTask))
            .As<IBaseCommandSetupTask>()
            .OrderByRegistration();

        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AssignableTo<CommandSettings>();
    }

    public static void RegisterExternal(
        ContainerBuilder builder,
        LoggingLevelSwitch logLevelSwitch,
        AppDataPathProvider appDataPathProvider)
    {
        builder.RegisterInstance(logLevelSwitch);
        builder.RegisterInstance(appDataPathProvider);
    }
}
