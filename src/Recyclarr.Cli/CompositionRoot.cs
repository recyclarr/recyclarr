using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using Autofac.Extras.Ordering;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using Recyclarr.Cache;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Setup;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Migration;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Cli.Pipelines.MediaNaming;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Common;
using Recyclarr.Compatibility;
using Recyclarr.Config;
using Recyclarr.Http;
using Recyclarr.Json;
using Recyclarr.Logging;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.ServarrApi;
using Recyclarr.Settings;
using Recyclarr.TrashGuide;
using Recyclarr.VersionControl;
using Recyclarr.Yaml;
using Spectre.Console.Cli;

namespace Recyclarr.Cli;

public static class CompositionRoot
{
    public static void Setup(ContainerBuilder builder)
    {
        var thisAssembly = typeof(CompositionRoot).Assembly;

        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();

        RegisterLogger(builder, thisAssembly);

        builder.RegisterModule<MigrationAutofacModule>();
        builder.RegisterModule<ConfigAutofacModule>();
        builder.RegisterModule<GuideAutofacModule>();
        builder.RegisterModule<YamlAutofacModule>();
        builder.RegisterModule<ServiceProcessorsAutofacModule>();
        builder.RegisterModule<CacheAutofacModule>();
        builder.RegisterModule<SettingsAutofacModule>();
        builder.RegisterModule<HttpAutofacModule>();
        builder.RegisterModule<ServarrApiAutofacModule>();
        builder.RegisterModule<VersionControlAutofacModule>();
        builder.RegisterModule<RepoAutofacModule>();
        builder.RegisterModule<CompatibilityAutofacModule>();
        builder.RegisterModule<JsonAutofacModule>();
        builder.RegisterModule<PlatformAutofacModule>();
        builder.RegisterModule<CommonAutofacModule>();

        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.Register(_ => new ResourceDataReader(thisAssembly)).As<IResourceDataReader>();

        builder.RegisterAutoMapper(thisAssembly);

        CliRegistrations(builder);
        PipelineRegistrations(builder);
    }

    private static void PipelineRegistrations(ContainerBuilder builder)
    {
        builder.RegisterModule<CustomFormatAutofacModule>();
        builder.RegisterModule<QualityProfileAutofacModule>();
        builder.RegisterModule<QualitySizeAutofacModule>();
        builder.RegisterModule<MediaNamingAutofacModule>();

        builder.RegisterGeneric(typeof(GenericPipelinePhases<>));
        builder.RegisterComposite<CompositeSyncPipeline, ISyncPipeline>();
        builder.RegisterTypes(
                // ORDER HERE IS IMPORTANT!
                // There are indirect dependencies between pipelines.
                typeof(GenericSyncPipeline<CustomFormatPipelineContext>),
                typeof(GenericSyncPipeline<QualityProfilePipelineContext>),
                typeof(GenericSyncPipeline<QualitySizePipelineContext>),
                typeof(GenericSyncPipeline<MediaNamingPipelineContext>))
            .As<ISyncPipeline>()
            .OrderByRegistration();
    }

    private static void RegisterLogger(ContainerBuilder builder, Assembly thisAssembly)
    {
        builder.RegisterAssemblyTypes(thisAssembly)
            .AssignableTo<ILogConfigurator>()
            .As<ILogConfigurator>();

        builder.RegisterModule<LoggingAutofacModule>();
        builder.RegisterType<LogJanitor>();
    }

    private static void CliRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<CommandSetupInterceptor>().As<ICommandInterceptor>();

        builder.RegisterComposite<CompositeGlobalSetupTask, IGlobalSetupTask>();
        builder.RegisterTypes(
                typeof(ProgramInformationDisplayTask),
                typeof(JanitorCleanupTask))
            .As<IGlobalSetupTask>()
            .OrderByRegistration();

        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AssignableTo<CommandSettings>();
    }
}
