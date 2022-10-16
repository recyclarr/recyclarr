using Autofac;
using Autofac.Extras.AggregateService;
using TrashLib.Services.CustomFormat.Api;
using TrashLib.Services.CustomFormat.Guide;
using TrashLib.Services.CustomFormat.Processors;
using TrashLib.Services.CustomFormat.Processors.GuideSteps;
using TrashLib.Services.CustomFormat.Processors.PersistenceSteps;
using TrashLib.Services.QualityProfile.Api;

namespace TrashLib.Services.CustomFormat;

public class CustomFormatAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CustomFormatService>().As<ICustomFormatService>();
        builder.RegisterType<QualityProfileService>().As<IQualityProfileService>();
        builder.RegisterType<CustomFormatUpdater>().As<ICustomFormatUpdater>();
        builder.RegisterType<CachePersister>().As<ICachePersister>();
        builder.RegisterType<GuideProcessor>().As<IGuideProcessor>();
        builder.RegisterType<CustomFormatLoader>().As<ICustomFormatLoader>();
        builder.RegisterType<CustomFormatParser>().As<ICustomFormatParser>();

        builder.RegisterAggregateService<IGuideProcessorSteps>();
        builder.RegisterType<CustomFormatStep>().As<ICustomFormatStep>();
        builder.RegisterType<ConfigStep>().As<IConfigStep>();
        builder.RegisterType<QualityProfileStep>().As<IQualityProfileStep>();
        builder.RegisterType<PersistenceProcessor>().As<IPersistenceProcessor>();

        builder.RegisterAggregateService<IPersistenceProcessorSteps>();
        builder.RegisterType<JsonTransactionStep>().As<IJsonTransactionStep>();
        builder.RegisterType<CustomFormatApiPersistenceStep>().As<ICustomFormatApiPersistenceStep>();
        builder.RegisterType<QualityProfileApiPersistenceStep>().As<IQualityProfileApiPersistenceStep>();
        builder.RegisterType<CustomFormatCategoryParser>().As<ICustomFormatCategoryParser>();
    }
}
