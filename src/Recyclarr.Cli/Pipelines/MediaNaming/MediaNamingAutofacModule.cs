using Autofac;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;
using Recyclarr.TrashGuide;

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

        builder.RegisterTypes(
                typeof(MediaNamingConfigPhase),
                typeof(MediaNamingApiFetchPhase),
                typeof(MediaNamingTransactionPhase),
                typeof(MediaNamingPreviewPhase),
                typeof(MediaNamingApiPersistencePhase),
                typeof(MediaNamingLogPhase))
            .AsImplementedInterfaces();
    }
}
