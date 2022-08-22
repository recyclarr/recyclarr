using Autofac;
using Autofac.Extras.AggregateService;
using TrashLib.Config.Services;
using TrashLib.Services.Radarr.Config;
using TrashLib.Services.Radarr.CustomFormat;
using TrashLib.Services.Radarr.CustomFormat.Api;
using TrashLib.Services.Radarr.CustomFormat.Guide;
using TrashLib.Services.Radarr.CustomFormat.Processors;
using TrashLib.Services.Radarr.CustomFormat.Processors.GuideSteps;
using TrashLib.Services.Radarr.CustomFormat.Processors.PersistenceSteps;
using TrashLib.Services.Radarr.QualityDefinition;
using TrashLib.Services.Radarr.QualityDefinition.Api;

namespace TrashLib.Services.Radarr;

public class RadarrAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Services
        builder.RegisterType<QualityDefinitionService>().As<IQualityDefinitionService>();
        builder.RegisterType<CustomFormatService>().As<ICustomFormatService>();
        builder.RegisterType<QualityProfileService>().As<IQualityProfileService>();

        // Configuration
        builder.RegisterType<RadarrValidationMessages>().As<IRadarrValidationMessages>();
        builder.RegisterType<ServerInfo>().As<IServerInfo>();

        // Quality Definition Support
        builder.RegisterType<RadarrQualityDefinitionUpdater>().As<IRadarrQualityDefinitionUpdater>();
        builder.RegisterType<RadarrQualityDefinitionGuideParser>().As<IRadarrQualityDefinitionGuideParser>();

        // Custom Format Support
        builder.RegisterType<CustomFormatUpdater>().As<ICustomFormatUpdater>();
        builder.RegisterType<LocalRepoCustomFormatJsonParser>().As<IRadarrGuideService>();
        builder.RegisterType<CachePersister>().As<ICachePersister>();
        builder.RegisterType<CustomFormatLister>().As<ICustomFormatLister>();

        // Guide Processor

        // todo: register as singleton to avoid parsing guide multiple times when using 2 or more instances in config
        builder.RegisterType<GuideProcessor>().As<IGuideProcessor>();
        builder.RegisterAggregateService<IGuideProcessorSteps>();
        builder.RegisterType<CustomFormatStep>().As<ICustomFormatStep>();
        builder.RegisterType<ConfigStep>().As<IConfigStep>();
        builder.RegisterType<QualityProfileStep>().As<IQualityProfileStep>();

        // Persistence Processor
        builder.RegisterType<PersistenceProcessor>().As<IPersistenceProcessor>();
        builder.RegisterAggregateService<IPersistenceProcessorSteps>();
        builder.RegisterType<JsonTransactionStep>().As<IJsonTransactionStep>();
        builder.RegisterType<CustomFormatApiPersistenceStep>().As<ICustomFormatApiPersistenceStep>();
        builder.RegisterType<QualityProfileApiPersistenceStep>().As<IQualityProfileApiPersistenceStep>();
    }
}
