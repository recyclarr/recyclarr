using Autofac;
using TrashLib.Services.QualitySize;
using TrashLib.Services.QualitySize.Api;
using TrashLib.Services.Radarr.Config;

namespace TrashLib.Services.Radarr;

public class RadarrAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<QualityDefinitionService>().As<IQualityDefinitionService>();
        builder.RegisterType<RadarrGuideDataLister>().As<IRadarrGuideDataLister>();
        builder.RegisterType<RadarrValidationMessages>().As<IRadarrValidationMessages>();
        builder.RegisterType<QualitySizeUpdater>().As<IQualitySizeUpdater>();
        builder.RegisterType<LocalRepoRadarrGuideService>().As<IRadarrGuideService>();
        builder.RegisterType<RadarrGuideDataLister>().As<IRadarrGuideDataLister>();
        builder.RegisterType<RadarrCompatibility>();
    }
}
