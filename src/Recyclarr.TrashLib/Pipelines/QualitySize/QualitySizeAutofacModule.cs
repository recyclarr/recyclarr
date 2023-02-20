using Autofac;
using Autofac.Extras.AggregateService;
using Recyclarr.TrashLib.Pipelines.QualitySize.Api;
using Recyclarr.TrashLib.Pipelines.QualitySize.Guide;
using Recyclarr.TrashLib.Pipelines.QualitySize.PipelinePhases;

namespace Recyclarr.TrashLib.Pipelines.QualitySize;

public class QualitySizeAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<QualityDefinitionService>().As<IQualityDefinitionService>();
        builder.RegisterType<QualityGuideService>().As<IQualityGuideService>().SingleInstance();
        builder.RegisterType<QualitySizeGuideParser>();
        builder.RegisterType<QualitySizeDataLister>();

        builder.RegisterAggregateService<IQualitySizePipelinePhases>();
        builder.RegisterType<QualitySizeGuidePhase>();
        builder.RegisterType<QualitySizePreviewPhase>();
        builder.RegisterType<QualitySizeApiFetchPhase>();
        builder.RegisterType<QualitySizeTransactionPhase>();
        builder.RegisterType<QualitySizeApiPersistencePhase>();
    }
}
