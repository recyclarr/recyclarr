using Autofac;
using Autofac.Extras.AggregateService;
using TrashLib.Services.QualityProfile.Processors;
using TrashLib.Services.QualityProfile.Api;
// using TrashLib.Services.CustomFormat.Processors;
// using TrashLib.Services.CustomFormat.Processors.GuideSteps;
// using TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

namespace TrashLib.Services.QualityProfile;

public class QualityProfileAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<QualityProfileService>().As<IQualityProfileService>();
        builder.RegisterType<QualityProfileUpdater>().As<IQualityProfileUpdater>();
        builder.RegisterType<QualityProfileProcessor>().As<IQualityProfileProcessor>();
        // builder.RegisterType<CachePersister>().As<ICachePersister>();
        // builder.RegisterType<GuideProcessor>().As<IGuideProcessor>();
        // builder.RegisterType<CustomFormatLoader>().As<ICustomFormatLoader>();
        // builder.RegisterType<CustomFormatParser>().As<ICustomFormatParser>();

        builder.RegisterAggregateService<IQualityProfileProcessorSteps>();
        //builder.RegisterType<QualityGroupStep>().As<ICustomFormatStep>();
        // builder.RegisterType<ConfigStep>().As<IConfigStep>();
        // builder.RegisterType<QualityProfileStep>().As<IQualityProfileStep>();
        // builder.RegisterType<PersistenceProcessor>().As<IPersistenceProcessor>();

        // builder.RegisterAggregateService<IPersistenceProcessorSteps>();
        // builder.RegisterType<JsonTransactionStep>().As<IJsonTransactionStep>();
        // builder.RegisterType<CustomFormatApiPersistenceStep>().As<ICustomFormatApiPersistenceStep>();
        // builder.RegisterType<QualityProfileApiPersistenceStep>().As<IQualityProfileApiPersistenceStep>();
        // builder.RegisterType<CustomFormatCategoryParser>().As<ICustomFormatCategoryParser>();
    }
}
