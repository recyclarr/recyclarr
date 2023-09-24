using Autofac;
using Recyclarr.Compatibility.Radarr;
using Recyclarr.Compatibility.Sonarr;

namespace Recyclarr.Compatibility;

public class CompatibilityAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<ServiceAgnosticCapabilityEnforcer>();
        builder.RegisterType<ServiceInformation>().As<IServiceInformation>()
            .InstancePerLifetimeScope();

        // Sonarr
        builder.RegisterType<SonarrCapabilityFetcher>().As<ISonarrCapabilityFetcher>();
        builder.RegisterType<SonarrCapabilityEnforcer>();

        // Radarr
        builder.RegisterType<RadarrCapabilityFetcher>().As<IRadarrCapabilityFetcher>();
        builder.RegisterType<RadarrCapabilityEnforcer>();
    }
}
