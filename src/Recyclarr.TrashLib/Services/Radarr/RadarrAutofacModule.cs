using Autofac;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Services.Common;
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

        builder.RegisterType<LocalRepoRadarrGuideService>()
            .As<RadarrGuideService>()
            .Keyed<IGuideService>(SupportedServices.Radarr);

        builder.RegisterType<RadarrCapabilityChecker>().As<IRadarrCapabilityChecker>()
            .InstancePerLifetimeScope();
    }
}
