using Autofac;
using Autofac.Extras.AggregateService;
using Recyclarr.TrashLib.Services.CustomFormat.Api;
using Recyclarr.TrashLib.Services.CustomFormat.Guide;
using Recyclarr.TrashLib.Services.CustomFormat.Processors;
using Recyclarr.TrashLib.Services.CustomFormat.Processors.GuideSteps;
using Recyclarr.TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

namespace Recyclarr.TrashLib.Services.CustomFormat;

public class CustomFormatAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CustomFormatService>().As<ICustomFormatService>();
        builder.RegisterType<QualityProfileService>().As<IQualityProfileService>();
        builder.RegisterType<CustomFormatUpdater>().As<ICustomFormatUpdater>();
        builder.RegisterType<CustomFormatGuideService>().As<ICustomFormatGuideService>();
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
