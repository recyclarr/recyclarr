using Autofac;
using Autofac.Extras.AggregateService;
using Recyclarr.Cli.Pipelines.Tags.PipelinePhases;
using Recyclarr.ServarrApi.Tag;

namespace Recyclarr.Cli.Pipelines.Tags;

public class TagsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<ServiceTagCache>().As<IPipelineCache>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<SonarrTagApiService>().As<ISonarrTagApiService>();

        builder.RegisterAggregateService<ITagPipelinePhases>();
        builder.RegisterType<TagConfigPhase>();
        builder.RegisterType<TagPreviewPhase>();
        builder.RegisterType<TagApiFetchPhase>();
        builder.RegisterType<TagTransactionPhase>();
        builder.RegisterType<TagApiPersistencePhase>();
    }
}
