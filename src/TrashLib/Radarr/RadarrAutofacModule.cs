using System;
using Autofac;
using Autofac.Extras.AggregateService;
using TrashLib.Cache;
using TrashLib.Config;
using TrashLib.Radarr.Config;
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
        class CachePersisterFactory
        {
            private readonly Func<IServiceConfiguration, ICacheGuidBuilder> _guidBuilderFactory;
            private readonly Func<ICacheGuidBuilder, ICachePersister> _persisterFactory;

            public CachePersisterFactory(
                Func<IServiceConfiguration, ICacheGuidBuilder> guidBuilderFactory,
                Func<ICacheGuidBuilder, ICachePersister> persisterFactory)
            {
                _guidBuilderFactory = guidBuilderFactory;
                _persisterFactory = persisterFactory;
            }

            public ICachePersister Create(IServiceConfiguration config)
            {
                return _persisterFactory(_guidBuilderFactory(config));
            }
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Services
            builder.RegisterType<QualityDefinitionService>().As<IQualityDefinitionService>();
            builder.RegisterType<CustomFormatService>().As<ICustomFormatService>();
            builder.RegisterType<QualityProfileService>().As<IQualityProfileService>();

            // Configuration
            builder.RegisterModule<ConfigAutofacModule>();
            builder.RegisterType<RadarrValidationMessages>().As<IRadarrValidationMessages>();

            // Quality Definition Support
            builder.RegisterType<RadarrQualityDefinitionUpdater>().As<IRadarrQualityDefinitionUpdater>();
            builder.RegisterType<RadarrQualityDefinitionGuideParser>().As<IRadarrQualityDefinitionGuideParser>();

            // Custom Format Support
            builder.RegisterType<CustomFormatUpdater>().As<ICustomFormatUpdater>();
            builder.RegisterType<LocalRepoCustomFormatJsonParser>().As<IRadarrGuideService>();
            builder.RegisterType<CachePersister>().As<ICachePersister>();

            builder.Register<Func<IServiceConfiguration, ICachePersister>>(c => config =>
            {
                var guidBuilderFactory = c.Resolve<Func<IServiceConfiguration, ICacheGuidBuilder>>();
                return c.Resolve<CachePersister>(TypedParameter.From(guidBuilderFactory(config)));
            });

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
}
