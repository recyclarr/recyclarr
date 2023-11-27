using Autofac;
using Autofac.Extras.AggregateService;
using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

public class CustomFormatAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ProcessedCustomFormatCache>().As<IPipelineCache>().AsSelf().InstancePerLifetimeScope();

        builder.RegisterType<CustomFormatCachePersister>().As<ICustomFormatCachePersister>();
        builder.RegisterType<CustomFormatDataLister>();

        builder.RegisterAggregateService<ICustomFormatPipelinePhases>();
        builder.RegisterType<CustomFormatConfigPhase>();
        builder.RegisterType<CustomFormatApiFetchPhase>();
        builder.RegisterType<CustomFormatTransactionPhase>();
        builder.RegisterType<CustomFormatPreviewPhase>();
        builder.RegisterType<CustomFormatApiPersistencePhase>();
    }
}
