using Autofac;
using Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

namespace Recyclarr.Cli.Pipelines.Tags;

public class TagsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<ServiceTagCache>().As<IPipelineCache>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterTypes(
                typeof(TagConfigPhase),
                typeof(TagPreviewPhase),
                typeof(TagApiFetchPhase),
                typeof(TagTransactionPhase),
                typeof(TagApiPersistencePhase),
                typeof(TagLogPhase))
            .AsImplementedInterfaces();
    }
}
