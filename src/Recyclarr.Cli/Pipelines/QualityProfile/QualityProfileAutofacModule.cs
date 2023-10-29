using Autofac;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

public class QualityProfileAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<QualityProfileStatCalculator>();

        builder.RegisterTypes(
                typeof(QualityProfileConfigPhase),
                typeof(QualityProfilePreviewPhase),
                typeof(QualityProfileApiFetchPhase),
                typeof(QualityProfileTransactionPhase),
                typeof(QualityProfileApiPersistencePhase),
                typeof(QualityProfileLogPhase))
            .AsImplementedInterfaces();
    }
}
