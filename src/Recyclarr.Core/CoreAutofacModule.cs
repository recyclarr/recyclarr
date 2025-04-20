using System.Text.Json;
using Autofac;
using Autofac.Extras.Ordering;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using FluentValidation;
using Flurl.Http.Configuration;
using Recyclarr.Cache;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Radarr;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.PostProcessing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;
using Recyclarr.Config.Parsing.PostProcessing.Deprecations;
using Recyclarr.Config.Secrets;
using Recyclarr.Http;
using Recyclarr.Json.Loading;
using Recyclarr.Logging;
using Recyclarr.Notifications;
using Recyclarr.Notifications.Apprise;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.ServarrApi;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.ServarrApi.QualityProfile;
using Recyclarr.ServarrApi.System;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.CustomFormat;
using Recyclarr.TrashGuide.MediaNaming;
using Recyclarr.TrashGuide.QualitySize;
using Recyclarr.VersionControl;
using Recyclarr.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Recyclarr;

public class CoreAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        RegisterCache(builder);
        RegisterCommon(builder);
        RegisterCompatibility(builder);
        RegisterConfig(builder);
        RegisterHttp(builder);
        RegisterJson(builder);
        RegisterNotifications(builder);
        RegisterPlatform(builder);
        RegisterRepo(builder);
        RegisterServarrApi(builder);
        RegisterSettings(builder);
        RegisterTrashGuide(builder);
        RegisterYaml(builder);
        RegisterVersionControl(builder);
    }

    private static void RegisterCache(ContainerBuilder builder)
    {
        builder.RegisterType<CacheStoragePath>().As<ICacheStoragePath>();
    }

    private static void RegisterCommon(ContainerBuilder builder)
    {
        builder.RegisterType<RuntimeValidationService>().As<IRuntimeValidationService>();
        builder.RegisterType<ValidationLogger>();
    }

    private static void RegisterCompatibility(ContainerBuilder builder)
    {
        builder.RegisterType<ServiceAgnosticCapabilityEnforcer>();
        builder
            .RegisterType<ServiceInformation>()
            .As<IServiceInformation>()
            .InstancePerLifetimeScope();

        // Sonarr
        builder.RegisterType<SonarrCapabilityFetcher>().As<ISonarrCapabilityFetcher>();
        builder.RegisterType<SonarrCapabilityEnforcer>();

        // Radarr
        builder.RegisterType<RadarrCapabilityFetcher>().As<IRadarrCapabilityFetcher>();
        builder.RegisterType<RadarrCapabilityEnforcer>();
    }

    private void RegisterConfig(ContainerBuilder builder)
    {
        builder.RegisterAutoMapper(ThisAssembly);

        builder.RegisterType<SecretsProvider>().As<ISecretsProvider>().SingleInstance();
        builder.RegisterType<YamlIncludeResolver>().As<IYamlIncludeResolver>();
        builder.RegisterType<ConfigurationRegistry>();
        builder.RegisterType<ConfigurationLoader>();
        builder.RegisterType<ConfigurationFinder>().As<IConfigurationFinder>();
        builder.RegisterType<ConfigValidationExecutor>();
        builder.RegisterType<ConfigParser>();
        builder.RegisterType<ConfigurationScopeFactory>();

        // Filter Processors
        builder.RegisterType<ConfigFilterProcessor>();
        builder
            .RegisterTypes(
                typeof(NonExistentInstancesFilter),
                typeof(DuplicateInstancesFilter),
                typeof(SplitInstancesFilter),
                typeof(InvalidInstancesFilter)
            )
            .As<IConfigFilter>()
            .OrderByRegistration();

        // Keyed include processors
        builder
            .RegisterType<ConfigIncludeProcessor>()
            .Keyed<IIncludeProcessor>(typeof(ConfigYamlInclude));
        builder
            .RegisterType<TemplateIncludeProcessor>()
            .Keyed<IIncludeProcessor>(typeof(TemplateYamlInclude));

        // Config Post Processors
        builder
            .RegisterTypes(
                // Order-sensitive!
                typeof(ConfigDeprecationPostProcessor),
                typeof(ImplicitUrlAndKeyPostProcessor),
                typeof(IncludePostProcessor)
            )
            .As<IConfigPostProcessor>()
            .OrderByRegistration();

        // Config Deprecations
        builder.RegisterType<ConfigDeprecations>();
        builder
            .RegisterTypes(
                // Order-sensitive!
                typeof(CfQualityProfilesDeprecationCheck)
            )
            .As<IConfigDeprecationCheck>()
            .OrderByRegistration();

        // These validators are required by IncludePostProcessor
        builder.RegisterType<RadarrConfigYamlValidator>().As<IValidator>();
        builder.RegisterType<SonarrConfigYamlValidator>().As<IValidator>();

        // Required by ConfigurationRegistry
        builder.RegisterType<ServiceConfigYamlValidator>().As<IValidator<ServiceConfigYaml>>();
    }

    private static void RegisterHttp(ContainerBuilder builder)
    {
        builder.RegisterType<FlurlClientCache>().As<IFlurlClientCache>().SingleInstance();

        builder
            .RegisterTypes(
                typeof(FlurlAfterCallLogRedactor),
                typeof(FlurlBeforeCallLogRedactor),
                typeof(FlurlRedirectPreventer)
            )
            .As<FlurlSpecificEventHandler>();
    }

    private static void RegisterJson(ContainerBuilder builder)
    {
        builder.Register<Func<JsonSerializerOptions, IBulkJsonLoader>>(c =>
        {
            return settings => new BulkJsonLoader(c.Resolve<ILogger>(), settings);
        });

        // Decorators for BulkJsonLoader. We do not use RegisterDecorator() here for these reasons:
        // - We consume the BulkJsonLoader as a delegate factory, not by instance
        // - We do not want all implementations of BulkJsonLoader to be decorated, only a specific implementation.
        builder.RegisterType<GuideJsonLoader>();
        builder.RegisterType<ServiceJsonLoader>();
    }

    private static void RegisterNotifications(ContainerBuilder builder)
    {
        builder.RegisterType<NotificationLogSinkConfigurator>().As<ILogConfigurator>();
        builder.RegisterType<NotificationService>().SingleInstance();
        builder.RegisterType<NotificationEmitter>().SingleInstance();

        // Apprise
        builder
            .RegisterType<AppriseStatefulNotificationApiService>()
            .Keyed<IAppriseNotificationApiService>(AppriseMode.Stateful);

        builder
            .RegisterType<AppriseStatelessNotificationApiService>()
            .Keyed<IAppriseNotificationApiService>(AppriseMode.Stateless);

        builder.RegisterType<AppriseRequestBuilder>().As<IAppriseRequestBuilder>();

        // Verbosity Strategies
        builder
            .RegisterType<MinimalVerbosityStrategy>()
            .Keyed<IVerbosityStrategy>(NotificationVerbosity.Minimal);
        builder
            .RegisterType<NormalVerbosityStrategy>()
            .Keyed<IVerbosityStrategy>(NotificationVerbosity.Normal);
        builder
            .RegisterType<DetailedVerbosityStrategy>()
            .Keyed<IVerbosityStrategy>(NotificationVerbosity.Detailed);
        builder.Register(c =>
        {
            var settings = c.Resolve<ISettings<NotificationSettings>>().Value;
            return c.ResolveKeyed<IVerbosityStrategy>(settings.Verbosity);
        });
    }

    private static void RegisterPlatform(ContainerBuilder builder)
    {
        builder.RegisterType<DefaultAppDataSetup>().As<IAppDataSetup>().AsSelf().SingleInstance();
        builder.RegisterType<DefaultEnvironment>().As<IEnvironment>();
        builder.RegisterType<DefaultRuntimeInformation>().As<IRuntimeInformation>();

        builder
            .Register(c => c.Resolve<DefaultAppDataSetup>().CreateAppPaths())
            .As<IAppPaths>()
            .SingleInstance();
    }

    private static void RegisterRepo(ContainerBuilder builder)
    {
        // Unique Repo Registrations
        builder
            .RegisterType<ConfigTemplatesRepo>()
            .As<IConfigTemplatesRepo>()
            .As<IUpdateableRepo>();
        builder.RegisterType<TrashGuidesRepo>().As<ITrashGuidesRepo>().As<IUpdateableRepo>();

        builder.RegisterType<RepoUpdater>().As<IRepoUpdater>();
        builder
            .RegisterType<TrashRepoMetadataBuilder>()
            .As<IRepoMetadataBuilder>()
            .InstancePerLifetimeScope();
        builder.RegisterType<GitPath>().As<IGitPath>();
    }

    private static void RegisterServarrApi(ContainerBuilder builder)
    {
        // This is used by all specific API service classes registered below.
        builder.RegisterType<ServarrRequestBuilder>().As<IServarrRequestBuilder>();

        builder.RegisterType<SystemApiService>().As<ISystemApiService>().InstancePerLifetimeScope();

        builder
            .RegisterType<QualityProfileApiService>()
            .As<IQualityProfileApiService>()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<CustomFormatApiService>()
            .As<ICustomFormatApiService>()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<QualityDefinitionApiService>()
            .As<IQualityDefinitionApiService>()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<MediaNamingApiService>()
            .As<IMediaNamingApiService>()
            .InstancePerLifetimeScope();
    }

    private static void RegisterSettings(ContainerBuilder builder)
    {
        builder.RegisterType<SettingsLoader>();
        builder.RegisterType<SettingsProvider>().SingleInstance();

        builder.RegisterSettings(x => x);
        builder.RegisterSettings(x => x.LogJanitor);
        builder.RegisterSettings(x => x.Repositories.ConfigTemplates);
        builder.RegisterSettings(x => x.Repositories.TrashGuides);
        builder.RegisterSettings(x => x.Notifications);
    }

    private static void RegisterTrashGuide(ContainerBuilder builder)
    {
        builder
            .RegisterType<ConfigTemplateGuideService>()
            .As<IConfigTemplateGuideService>()
            .SingleInstance();

        // Custom Format
        builder
            .RegisterType<CustomFormatGuideService>()
            .As<ICustomFormatGuideService>()
            .SingleInstance();
        builder.RegisterType<CustomFormatLoader>().As<ICustomFormatLoader>();
        builder.RegisterType<CustomFormatCategoryParser>().As<ICustomFormatCategoryParser>();

        // Quality Size
        builder
            .RegisterType<QualitySizeGuideService>()
            .As<IQualitySizeGuideService>()
            .SingleInstance();
        builder.RegisterType<QualitySizeGuideParser>();

        // Media Naming
        builder.RegisterType<MediaNamingGuideService>().As<IMediaNamingGuideService>();
    }

    private void RegisterYaml(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(ThisAssembly)
            .Where(t => typeof(IYamlBehavior).IsAssignableFrom(t) && !t.IsAbstract)
            .As<IYamlBehavior>();

        builder.RegisterType<YamlSerializerFactory>().As<IYamlSerializerFactory>();
        builder.RegisterType<DefaultObjectFactory>().As<IObjectFactory>();
        builder.RegisterType<YamlBehaviorProvider>();
    }

    private static void RegisterVersionControl(ContainerBuilder builder)
    {
        builder.RegisterType<GitRepositoryFactory>().As<IGitRepositoryFactory>();
    }
}
