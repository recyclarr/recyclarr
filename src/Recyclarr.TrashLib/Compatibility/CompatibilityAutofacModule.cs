using Autofac;
using Recyclarr.TrashLib.Compatibility.Radarr;
using Recyclarr.TrashLib.Compatibility.Sonarr;

namespace Recyclarr.TrashLib.Compatibility;

public class CompatibilityAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<ServiceAgnosticCapabilityEnforcer>();

        // Sonarr
        builder.RegisterType<SonarrCapabilityFetcher>().As<ISonarrCapabilityFetcher>();
        builder.RegisterType<SonarrCapabilityEnforcer>();

        // Radarr
        builder.RegisterType<RadarrCapabilityFetcher>().As<IRadarrCapabilityFetcher>();
        builder.RegisterType<RadarrCapabilityEnforcer>();
    }
}
