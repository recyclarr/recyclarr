using Autofac;
using Autofac.Extras.AggregateService;
using TrashLib.Config;
using TrashLib.Radarr.CustomFormat;
using TrashLib.Radarr.CustomFormat.Api;
using TrashLib.Radarr.CustomFormat.Guide;
using TrashLib.Radarr.CustomFormat.Processors;
using TrashLib.Radarr.CustomFormat.Processors.GuideSteps;
using TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps;
using TrashLib.Radarr.QualityDefinition;
using TrashLib.Radarr.QualityDefinition.Api;

namespace TrashLib.Radarr
{
    public class RadarrAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Services
            builder.RegisterType<QualityDefinitionService>().As<IQualityDefinitionService>();
            builder.RegisterType<CustomFormatService>().As<ICustomFormatService>();
            builder.RegisterType<QualityProfileService>().As<IQualityProfileService>();

            builder.Register(c =>
                {
                    var config = c.Resolve<IConfigurationProvider>().ActiveConfiguration;
                    return new ServerInfo(config.BaseUrl, config.ApiKey);
                })
                .As<IServerInfo>();

            // Quality Definition Support
            builder.RegisterType<RadarrQualityDefinitionUpdater>().As<IRadarrQualityDefinitionUpdater>();
            builder.RegisterType<RadarrQualityDefinitionGuideParser>().As<IRadarrQualityDefinitionGuideParser>();

            // Custom Format Support
            builder.RegisterType<CustomFormatUpdater>().As<ICustomFormatUpdater>();
            builder.RegisterType<GithubCustomFormatJsonRequester>().As<IRadarrGuideService>();
            builder.RegisterType<CachePersister>().As<ICachePersister>();

            // Guide Processor
            builder.RegisterType<GuideProcessor>()
                .As<
                    IGuideProcessor>(); // todo: register as singleton to avoid parsing guide multiple times when using 2 or more instances in config
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
}
