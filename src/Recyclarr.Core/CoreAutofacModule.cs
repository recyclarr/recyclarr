using System.IO.Abstractions;
using Autofac;
using Autofac.Extras.Ordering;
using FluentValidation;
using Flurl.Http.Configuration;
using Recyclarr.Common;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Radarr;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Config.Parsing.PostProcessing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;
using Recyclarr.Config.Parsing.PostProcessing.Deprecations;
using Recyclarr.Config.Secrets;
using Recyclarr.Http;
using Recyclarr.Notifications;
using Recyclarr.Notifications.Apprise;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.Servarr.CustomFormat;
using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Servarr.QualitySize;
using Recyclarr.Servarr.SystemStatus;
using Recyclarr.ServarrApi;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.ServarrApi.MediaManagement;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.ServarrApi.QualityProfile;
using Recyclarr.ServarrApi.System;
using Recyclarr.Settings;
using Recyclarr.Settings.Deprecations;
using Recyclarr.Settings.Models;
using Recyclarr.Sync;
using Recyclarr.SyncState;
using Recyclarr.TrashGuide.CustomFormat;
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
        RegisterNotifications(builder);
        RegisterPlatform(builder);
        RegisterRepo(builder);
        RegisterServarrApi(builder);
        RegisterSettings(builder);
        RegisterTrashGuide(builder);
        RegisterYaml(builder);
        RegisterVersionControl(builder);
        RegisterSyncEvents(builder);
    }

    private static void RegisterCache(ContainerBuilder builder)
    {
        builder.RegisterType<SyncStateStoragePath>().As<ISyncStateStoragePath>();
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

    private static void RegisterConfig(ContainerBuilder builder)
    {
        builder.RegisterType<SecretsProvider>().As<ISecretsProvider>().SingleInstance();
        builder.RegisterType<YamlIncludeResolver>().As<IYamlIncludeResolver>();
        builder.RegisterType<ConfigurationRegistry>();
        builder.RegisterType<ConfigurationLoader>();
        builder.RegisterType<ConfigurationFinder>().As<IConfigurationFinder>();
        builder.RegisterType<ConfigValidationExecutor>();
        builder.RegisterType<ConfigParser>();
        builder.RegisterType<LifetimeScopeFactory>();

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

        // Config Diagnostic Collector (shared between YAML type inspector and config registry)
        builder
            .RegisterType<ConfigDiagnosticCollector>()
            .As<IConfigDiagnosticCollector>()
            .InstancePerLifetimeScope();

        // Settings Deprecations
        builder.RegisterType<SettingsDeprecations>();

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

    private static void RegisterNotifications(ContainerBuilder builder)
    {
        builder.RegisterType<NotificationService>();
        builder.RegisterType<AppriseNotificationApiService>().As<IAppriseNotificationApiService>();

        builder.Register<INotificationService>(c =>
        {
            var settings = c.Resolve<ISettings<NotificationSettings>>().Value;
            return settings.Apprise is not null
                ? c.Resolve<NotificationService>()
                : new NoopNotificationService();
        });

        builder.RegisterType<AppriseRequestBuilder>().As<IAppriseRequestBuilder>();

        builder.Register(c =>
        {
            var settings = c.Resolve<ISettings<NotificationSettings>>().Value;
            return VerbosityOptions.From(settings.Verbosity);
        });
    }

    private static void RegisterPlatform(ContainerBuilder builder)
    {
        builder.RegisterType<DefaultEnvironment>().As<IEnvironment>();
        builder.RegisterType<DefaultRuntimeInformation>().As<IRuntimeInformation>();

        builder
            .Register(c =>
            {
                var setup = new DefaultAppDataSetup(
                    c.Resolve<IEnvironment>(),
                    c.Resolve<IFileSystem>()
                );
                return setup.CreateAppPaths();
            })
            .As<IAppPaths>()
            .SingleInstance();
    }

    private static void RegisterRepo(ContainerBuilder builder)
    {
        // Keep only Git infrastructure components
        builder.RegisterType<RepoUpdater>().As<IRepoUpdater>();
        builder.RegisterType<GitPath>().As<IGitPath>();
    }

    private static void RegisterServarrApi(ContainerBuilder builder)
    {
        // This is used by all specific API service classes registered below.
        builder.RegisterType<ServarrRequestBuilder>().As<IServarrRequestBuilder>();

        builder.RegisterType<SystemApiService>().As<ISystemApiService>().InstancePerLifetimeScope();
        builder.RegisterServiceGateway<ISystemService, SonarrSystemGateway, RadarrSystemGateway>();

        builder
            .RegisterType<QualityProfileApiService>()
            .As<IQualityProfileApiService>()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<CustomFormatApiService>()
            .As<ICustomFormatApiService>()
            .InstancePerLifetimeScope();
        builder.RegisterServiceGateway<
            ICustomFormatService,
            SonarrCustomFormatGateway,
            RadarrCustomFormatGateway
        >();
        builder
            .RegisterType<QualityDefinitionApiService>()
            .As<IQualityDefinitionApiService>()
            .InstancePerLifetimeScope();
        builder.RegisterServiceGateway<
            IQualityDefinitionService,
            SonarrQualityDefinitionGateway,
            RadarrQualityDefinitionGateway
        >();
        builder
            .RegisterType<SonarrMediaNamingApiService>()
            .As<ISonarrMediaNamingApiService>()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<SonarrNamingGateway>()
            .As<ISonarrNamingService>()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<RadarrMediaNamingApiService>()
            .As<IRadarrMediaNamingApiService>()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<RadarrNamingGateway>()
            .As<IRadarrNamingService>()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<MediaManagementApiService>()
            .As<IMediaManagementApiService>()
            .InstancePerLifetimeScope();
        builder.RegisterServiceGateway<
            IMediaManagementService,
            SonarrMediaManagementGateway,
            RadarrMediaManagementGateway
        >();
    }

    private static void RegisterSettings(ContainerBuilder builder)
    {
        builder.RegisterType<SettingsLoader>();
        builder.RegisterType<SettingsProvider>().SingleInstance();

        builder.RegisterSettings(x => x);
        builder.RegisterSettings(x => x.LogJanitor);
        builder.RegisterSettings(x => x.Notifications);
        builder.RegisterSettings(x => x.ResourceProviders);
    }

    private static void RegisterTrashGuide(ContainerBuilder builder)
    {
        builder.RegisterType<CustomFormatCategoryParser>().As<ICustomFormatCategoryParser>();
    }

    private void RegisterYaml(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(ThisAssembly)
            .Where(t =>
                typeof(IYamlBehavior).IsAssignableFrom(t)
                && !t.IsAbstract
                && t != typeof(SettingsDeprecatedPropertyBehavior)
            )
            .As<IYamlBehavior>();

        // Registered both as itself (for SettingsLoader to read deprecations) and as IYamlBehavior
        builder
            .RegisterType<SettingsDeprecatedPropertyBehavior>()
            .AsSelf()
            .As<IYamlBehavior>()
            .SingleInstance();

        builder.RegisterType<YamlSerializerFactory>().As<IYamlSerializerFactory>();
        builder.RegisterType<DefaultObjectFactory>().As<IObjectFactory>();
        builder.RegisterType<YamlBehaviorProvider>();
    }

    private static void RegisterVersionControl(ContainerBuilder builder)
    {
        builder.RegisterType<GitRepository>().As<IGitRepository>();
    }

    private static void RegisterSyncEvents(ContainerBuilder builder)
    {
        builder
            .RegisterType<SyncRunScope>()
            .AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope("sync");

        builder
            .RegisterType<InstancePublisher>()
            .As<IInstancePublisher>()
            .InstancePerMatchingLifetimeScope("instance");
    }
}
