using Autofac;
using Autofac.Extras.AggregateService;
using Recyclarr.Cli.Pipelines.CustomFormat.Api;
using Recyclarr.Cli.Pipelines.CustomFormat.Guide;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;
using Recyclarr.TrashLib.Cache;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

public class CustomFormatAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CustomFormatGuideService>().As<ICustomFormatGuideService>().SingleInstance();
        builder.RegisterType<ProcessedCustomFormatCache>().As<IPipelineCache>().AsSelf().InstancePerLifetimeScope();

        builder.RegisterType<CustomFormatService>().As<ICustomFormatService>();
        builder.RegisterType<CachePersister>().As<ICachePersister>();
        builder.RegisterType<CustomFormatLoader>().As<ICustomFormatLoader>();
        builder.RegisterType<CustomFormatParser>().As<ICustomFormatParser>();
        builder.RegisterType<CustomFormatCategoryParser>().As<ICustomFormatCategoryParser>();
        builder.RegisterType<CustomFormatDataLister>();

        builder.RegisterAggregateService<ICustomFormatPipelinePhases>();
        builder.RegisterType<CustomFormatConfigPhase>();
        builder.RegisterType<CustomFormatApiFetchPhase>();
        builder.RegisterType<CustomFormatTransactionPhase>();
        builder.RegisterType<CustomFormatPreviewPhase>();
        builder.RegisterType<CustomFormatApiPersistencePhase>();
    }
}
