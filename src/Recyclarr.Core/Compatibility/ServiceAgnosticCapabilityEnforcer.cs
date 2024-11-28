using Recyclarr.Compatibility.Radarr;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config.Models;

namespace Recyclarr.Compatibility;

public class ServiceAgnosticCapabilityEnforcer(
    SonarrCapabilityEnforcer sonarrEnforcer,
    RadarrCapabilityEnforcer radarrEnforcer
)
{
    public async Task Check(IServiceConfiguration config, CancellationToken ct)
    {
        switch (config)
        {
            case SonarrConfiguration:
                await sonarrEnforcer.Check(ct);
                break;

            case RadarrConfiguration:
                await radarrEnforcer.Check(ct);
                break;
        }
    }
}
