using Autofac;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize;

public class QualitySizeAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<QualitySizeDataLister>();

        // Setup factory for creation of concrete IQualityItemLimits types
        builder.RegisterType<QualityItemLimitFactory>();
        builder.RegisterType<RadarrQualityItemLimits>().Keyed<IQualityItemLimits>(SupportedServices.Radarr);
        builder.RegisterType<SonarrQualityItemLimits>().Keyed<IQualityItemLimits>(SupportedServices.Sonarr);

        builder.RegisterTypes(
                typeof(QualitySizeConfigPhase),
                typeof(QualitySizePreviewPhase),
                typeof(QualitySizeApiFetchPhase),
                typeof(QualitySizeTransactionPhase),
                typeof(QualitySizeApiPersistencePhase),
                typeof(QualitySizeLogPhase))
            .AsImplementedInterfaces();
    }
}
