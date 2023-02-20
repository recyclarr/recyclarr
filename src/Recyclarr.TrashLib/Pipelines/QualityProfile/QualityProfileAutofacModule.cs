using Autofac;
using Autofac.Extras.AggregateService;
using Recyclarr.TrashLib.Pipelines.QualityProfile.Api;
using Recyclarr.TrashLib.Pipelines.QualityProfile.PipelinePhases;

namespace Recyclarr.TrashLib.Pipelines.QualityProfile;

public class QualityProfileAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<QualityProfileService>().As<IQualityProfileService>();

        builder.RegisterAggregateService<IQualityProfilePipelinePhases>();
        builder.RegisterType<QualityProfileConfigPhase>();
        builder.RegisterType<QualityProfileApiFetchPhase>();
        builder.RegisterType<QualityProfileTransactionPhase>();
        builder.RegisterType<QualityProfilePreviewPhase>();
        builder.RegisterType<QualityProfileApiPersistencePhase>();
    }
}
