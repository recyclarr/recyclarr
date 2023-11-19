using Recyclarr.Compatibility.Radarr;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config.Models;

namespace Recyclarr.Compatibility;

public class ServiceAgnosticCapabilityEnforcer(
    SonarrCapabilityEnforcer sonarrEnforcer,
    RadarrCapabilityEnforcer radarrEnforcer)
{
    public async Task Check(IServiceConfiguration config)
    {
        switch (config)
        {
            case SonarrConfiguration c:
                await sonarrEnforcer.Check(c);
                break;

            case RadarrConfiguration c:
                await radarrEnforcer.Check(c);
                break;
        }
    }
}
