using Autofac;
using Recyclarr.TrashLib.Services.QualitySize;
using Recyclarr.TrashLib.Services.QualitySize.Api;

namespace Recyclarr.TrashLib.Services.Radarr;

public class RadarrAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<QualityDefinitionService>().As<IQualityDefinitionService>();
        builder.RegisterType<RadarrGuideDataLister>().As<IRadarrGuideDataLister>();
        builder.RegisterType<QualitySizeUpdater>().As<IQualitySizeUpdater>();
        builder.RegisterType<LocalRepoRadarrGuideService>().As<IRadarrGuideService>();
        builder.RegisterType<RadarrGuideDataLister>().As<IRadarrGuideDataLister>();

        builder.RegisterType<RadarrCompatibility>().InstancePerLifetimeScope();
        builder.Register(c => c.Resolve<RadarrCompatibility>().Capabilities);
    }
}
