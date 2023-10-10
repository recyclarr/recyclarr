using Autofac;
using Autofac.Extras.AggregateService;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;
using Recyclarr.Common;

namespace Recyclarr.Cli.Pipelines.MediaNaming;

public class MediaNamingAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<MediaNamingDataLister>();

        builder.RegisterType<RadarrMediaNamingConfigPhase>()
            .Keyed<IServiceBasedMediaNamingConfigPhase>(SupportedServices.Radarr);
        builder.RegisterType<SonarrMediaNamingConfigPhase>()
            .Keyed<IServiceBasedMediaNamingConfigPhase>(SupportedServices.Sonarr);

        builder.RegisterAggregateService<IMediaNamingPipelinePhases>();
        builder.RegisterType<MediaNamingConfigPhase>();
        builder.RegisterType<MediaNamingApiFetchPhase>();
        builder.RegisterType<MediaNamingTransactionPhase>();
        builder.RegisterType<MediaNamingPreviewPhase>();
        builder.RegisterType<MediaNamingApiPersistencePhase>();
        builder.RegisterType<MediaNamingPhaseLogger>();
    }
}
