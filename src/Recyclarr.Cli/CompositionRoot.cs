using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using Autofac.Extras.Ordering;
using FluentValidation;
using Recyclarr.Cli.Cache;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Setup;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Migration;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Pipelines.ReleaseProfile;
using Recyclarr.Cli.Pipelines.Tags;
using Recyclarr.Cli.Processors;
using Recyclarr.Common;
using Recyclarr.Common.FluentValidation;
using Recyclarr.TrashLib;
using Recyclarr.TrashLib.Interfaces;
using Recyclarr.TrashLib.Startup;
using Serilog.Core;
using Spectre.Console.Cli;

namespace Recyclarr.Cli;

public static class CompositionRoot
{
    public static void Setup(ContainerBuilder builder)
    {
        var thisAssembly = typeof(CompositionRoot).Assembly;

        RegisterLogger(builder);

        builder.RegisterModule<MigrationAutofacModule>();
        builder.RegisterModule(new TrashLibAutofacModule {AdditionalMapperProfileAssembly = thisAssembly});
        builder.RegisterModule<ServiceProcessorsAutofacModule>();
        builder.RegisterModule<CacheAutofacModule>();

        builder.RegisterType<CacheStoragePath>().As<ICacheStoragePath>();
        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.Register(_ => new ResourceDataReader(thisAssembly)).As<IResourceDataReader>();

        CommandRegistrations(builder);
        PipelineRegistrations(builder);

        builder.RegisterAssemblyTypes(thisAssembly)
            .AsClosedTypesOf(typeof(IValidator<>))
            .Where(x => !typeof(IManualValidator).IsAssignableFrom(x))
            .As<IValidator>();
    }

    private static void PipelineRegistrations(ContainerBuilder builder)
    {
        builder.RegisterModule<TagsAutofacModule>();
        builder.RegisterModule<CustomFormatAutofacModule>();
        builder.RegisterModule<QualityProfileAutofacModule>();
        builder.RegisterModule<QualitySizeAutofacModule>();
        builder.RegisterModule<ReleaseProfileAutofacModule>();

        builder.RegisterTypes(
                typeof(TagSyncPipeline),
                typeof(CustomFormatSyncPipeline),
                typeof(QualityProfileSyncPipeline),
                typeof(QualitySizeSyncPipeline),
                typeof(ReleaseProfileSyncPipeline))
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
