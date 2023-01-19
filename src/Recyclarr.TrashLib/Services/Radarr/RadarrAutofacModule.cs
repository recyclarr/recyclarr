using Autofac;

namespace Recyclarr.TrashLib.Services.Radarr;

public class RadarrAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<RadarrCapabilityChecker>().As<IRadarrCapabilityChecker>()
            .InstancePerLifetimeScope();
    }
}
