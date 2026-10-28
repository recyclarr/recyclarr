using Autofac;
using Autofac.Extras.Ordering;
using FluentValidation;
using Recyclarr.Common;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Radarr;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Config.Parsing.PostProcessing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;
using Recyclarr.Config.Parsing.PostProcessing.Deprecations;
using Recyclarr.Config.Secrets;
using Recyclarr.Http;
using Recyclarr.Migration;
using Recyclarr.Migration.Steps;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.Servarr.CustomFormat;
using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.Servarr.QualitySize;
using Recyclarr.Servarr.SystemStatus;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.ServarrApi.MediaManagement;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.ServarrApi.QualityProfile;
using Recyclarr.ServarrApi.System;
using Recyclarr.Sync;
using Recyclarr.SyncState;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.CustomFormat;
using Recyclarr.VersionControl;
using Recyclarr.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;
using RadarrApi = Recyclarr.Api.Radarr;
using SonarrApi = Recyclarr.Api.Sonarr.V3;
using SonarrV5Api = Recyclarr.Api.Sonarr.V5;

namespace Recyclarr;

public class CoreAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        RegisterCache(builder);
        RegisterCommon(builder);
        RegisterCompatibility(builder);
        RegisterConfig(builder);
        RegisterPlatform(builder);
        RegisterRepo(builder);
        RegisterServarrApiClients(builder);
        RegisterServarrGateways(builder);
        RegisterTrashGuide(builder);
        RegisterYaml(builder);
        RegisterVersionControl(builder);
        RegisterSync(builder);
        RegisterMigrations(builder);
        RegisterPorts(builder);
    }

    private static void RegisterMigrations(ContainerBuilder builder)
    {
        builder.RegisterType<MigrationExecutor>().As<IMigrationExecutor>();
        builder
            .RegisterTypes(
                // Order-sensitive! A step may depend on the layout an earlier step produces.
                typeof(FlattenNestedStateMigrationStep),
                typeof(DeleteRepoDirMigrationStep),
                typeof(MoveCacheToStateMigrationStep),
                typeof(NormalizeStateMappingsPropertyStep)
            )
            .As<IMigrationStep>()
            .OrderByRegistration();
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
        builder.RegisterType<ConfigurationLoader>();
        builder.RegisterType<ConfigurationFinder>().As<IConfigurationFinder>();
        builder.RegisterType<ConfigValidationExecutor>();
        builder.RegisterType<ConfigParser>();
        builder.RegisterType<InstanceScopeFactory>();
        builder.RegisterType<InstanceSyncProcessor>();

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

        // These validators are required by IncludePostProcessor
        builder.RegisterType<RadarrConfigYamlValidator>().As<IValidator>();
        builder.RegisterType<SonarrConfigYamlValidator>().As<IValidator>();

        builder.RegisterType<ServiceConfigYamlValidator>().As<IValidator<ServiceConfigYaml>>();
    }

    private static void RegisterPlatform(ContainerBuilder builder)
    {
        builder.RegisterModule<PlatformAutofacModule>();
        builder.RegisterType<DefaultRuntimeInformation>().As<IRuntimeInformation>();

        builder.RegisterType<DefaultAppDataSetup>();
        builder
            .Register(c => c.Resolve<DefaultAppDataSetup>().CreateAppPaths())
            .As<IAppPaths>()
            .SingleInstance();
    }

    private static void RegisterRepo(ContainerBuilder builder)
    {
        // Keep only Git infrastructure components
        builder.RegisterType<RepoUpdater>().As<IRepoUpdater>();
        builder.RegisterType<GitPath>().As<IGitPath>();
    }

    private static void RegisterServarrApiClients(ContainerBuilder builder)
    {
        // Refit clients
        builder.RegisterServarrRefitClient<SonarrApi.ICustomFormatApi>();
        builder.RegisterServarrRefitClient<RadarrApi.ICustomFormatApi>();
        builder.RegisterServarrRefitClient<SonarrApi.IQualityDefinitionApi>();
        builder.RegisterServarrRefitClient<RadarrApi.IQualityDefinitionApi>();
        builder.RegisterServarrRefitClient<SonarrApi.IQualityProfileApi>();
        builder.RegisterServarrRefitClient<RadarrApi.IQualityProfileApi>();
        builder.RegisterServarrRefitClient<SonarrApi.IQualityProfileSchemaApi>();
        builder.RegisterServarrRefitClient<RadarrApi.IQualityProfileSchemaApi>();
        builder.RegisterServarrRefitClient<RadarrApi.ILanguageApi>();
        builder.RegisterServarrRefitClient<SonarrApi.IMediaManagementConfigApi>();
        builder.RegisterServarrRefitClient<RadarrApi.IMediaManagementConfigApi>();
        builder.RegisterServarrRefitClient<SonarrApi.INamingConfigApi>();
        builder.RegisterServarrRefitClient<RadarrApi.INamingConfigApi>();
        builder.RegisterServarrRefitClient<SonarrApi.ISystemApi>();
        builder.RegisterServarrRefitClient<RadarrApi.ISystemApi>();

        builder.RegisterServarrRefitClient<SonarrV5Api.ICustomFormatApi>();
        builder.RegisterServarrRefitClient<SonarrV5Api.IQualityProfileApi>();
        builder.RegisterServarrRefitClient<SonarrV5Api.IQualityProfileSchemaApi>();
        builder.RegisterServarrRefitClient<SonarrV5Api.IMediaManagementSettingsApi>();
        builder.RegisterServarrRefitClient<SonarrV5Api.INamingSettingsApi>();
    }

    private static void RegisterServarrGateways(ContainerBuilder builder)
    {
        // The system gateway performs version detection, so it is selected by service type
        // rather than by the detected API target.
        builder
            .RegisterType<SonarrSystemGateway>()
            .Keyed<ISystemService>(SupportedServices.Sonarr)
            .InstancePerLifetimeScope();
        builder
            .RegisterType<RadarrSystemGateway>()
            .Keyed<ISystemService>(SupportedServices.Radarr)
            .InstancePerLifetimeScope();
        builder
            .Register(c =>
                c.ResolveKeyed<ISystemService>(c.Resolve<IServiceConfiguration>().ServiceType)
            )
            .As<ISystemService>()
            .InstancePerLifetimeScope();

        builder
            .RegisterServiceGateway<IQualityProfileService>()
            .For<SonarrV3QualityProfileGateway>(ServiceApiTarget.SonarrV3)
            .For<SonarrV5QualityProfileGateway>(ServiceApiTarget.SonarrV5)
            .For<RadarrQualityProfileGateway>(ServiceApiTarget.RadarrV3);
        builder
            .RegisterType<RadarrLanguageGateway>()
            .As<IRadarrLanguageService>()
            .InstancePerLifetimeScope();
        builder
            .RegisterServiceGateway<ICustomFormatService>()
            .For<SonarrV3CustomFormatGateway>(ServiceApiTarget.SonarrV3)
            .For<SonarrV5CustomFormatGateway>(ServiceApiTarget.SonarrV5)
            .For<RadarrCustomFormatGateway>(ServiceApiTarget.RadarrV3);
        builder
            .RegisterServiceGateway<IQualityDefinitionService>()
            .For<SonarrV3QualityDefinitionGateway>(ServiceApiTarget.SonarrV3)
            .For<RadarrQualityDefinitionGateway>(ServiceApiTarget.RadarrV3);
        builder
            .RegisterServiceGateway<ISonarrNamingService>()
            .For<SonarrV3NamingGateway>(ServiceApiTarget.SonarrV3)
            .For<SonarrV5NamingGateway>(ServiceApiTarget.SonarrV5);
        builder
            .RegisterType<RadarrNamingGateway>()
            .As<IRadarrNamingService>()
            .InstancePerLifetimeScope();
        builder
            .RegisterServiceGateway<IMediaManagementService>()
            .For<SonarrV3MediaManagementGateway>(ServiceApiTarget.SonarrV3)
            .For<SonarrV5MediaManagementGateway>(ServiceApiTarget.SonarrV5)
            .For<RadarrMediaManagementGateway>(ServiceApiTarget.RadarrV3);
    }

    private static void RegisterTrashGuide(ContainerBuilder builder)
    {
        builder.RegisterType<CustomFormatCategoryParser>().As<ICustomFormatCategoryParser>();
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
        builder.RegisterType<GitRepository>().As<IGitRepository>();
    }

    private static void RegisterSync(ContainerBuilder builder)
    {
        builder
            .RegisterType<SyncOrchestrator>()
            .As<ISyncOrchestrator>()
            .InstancePerMatchingLifetimeScope("run");

        builder.RegisterType<SyncRunScopeFactory>().SingleInstance();
    }

    private static void RegisterPorts(ContainerBuilder builder)
    {
        // Custom format deletion port
        builder.RegisterType<CustomFormatDeleter>().As<ICustomFormatDeleter>();
    }
}
