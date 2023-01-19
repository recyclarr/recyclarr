using Autofac;
using Recyclarr.TrashLib.Services.QualitySize.Api;
using Recyclarr.TrashLib.Services.QualitySize.Guide;

namespace Recyclarr.TrashLib.Services.QualitySize;

public class QualitySizeAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<QualityDefinitionService>().As<IQualityDefinitionService>();
        builder.RegisterType<QualitySizeUpdater>().As<IQualitySizeUpdater>();
        builder.RegisterType<QualityGuideService>().As<IQualityGuideService>();
        builder.RegisterType<QualitySizeGuideParser>();
    }
}
