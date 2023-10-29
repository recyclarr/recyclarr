using Autofac;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

namespace Recyclarr.Cli.Pipelines.QualitySize;

public class QualitySizeAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<QualitySizeDataLister>();

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
