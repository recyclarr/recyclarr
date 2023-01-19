using Autofac;
using Recyclarr.TrashLib.Services.Sonarr.Api;
using Recyclarr.TrashLib.Services.Sonarr.Capabilities;

namespace Recyclarr.TrashLib.Services.Sonarr;

public class SonarrAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SonarrTagApiService>().As<ISonarrTagApiService>();
        builder.RegisterType<SonarrCapabilityEnforcer>();
        builder.RegisterType<SonarrCapabilityChecker>().As<ISonarrCapabilityChecker>()
            .InstancePerLifetimeScope();
    }
}
