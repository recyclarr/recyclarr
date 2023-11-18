using Recyclarr.Config.Models;

namespace Recyclarr.Compatibility.Radarr;

public class RadarrCapabilityEnforcer(IRadarrCapabilityFetcher capabilityFetcher)
{
    public async Task Check(RadarrConfiguration config)
    {
        _ = await capabilityFetcher.GetCapabilities(config);

        // For the future: Add more capability checks here as needed
    }
}
