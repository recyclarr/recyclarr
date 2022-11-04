using Autofac;
using TrashLib.Services.Radarr.Config;
using TrashLib.Services.Radarr.QualityDefinition;
using TrashLib.Services.Radarr.QualityDefinition.Api;

namespace TrashLib.Services.Radarr;

public class RadarrAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<QualityDefinitionService>().As<IQualityDefinitionService>();
        builder.RegisterType<RadarrGuideDataLister>().As<IRadarrGuideDataLister>();
        builder.RegisterType<RadarrValidationMessages>().As<IRadarrValidationMessages>();
        builder.RegisterType<RadarrQualityDefinitionUpdater>().As<IRadarrQualityDefinitionUpdater>();
        builder.RegisterType<LocalRepoRadarrGuideService>().As<IRadarrGuideService>();
        builder.RegisterType<RadarrGuideDataLister>().As<IRadarrGuideDataLister>();
        builder.RegisterType<RadarrCompatibility>();
    }
}
