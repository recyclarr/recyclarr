using Recyclarr.Config.Models;

namespace Recyclarr.TrashLib.Compatibility.Radarr;

public class RadarrCapabilityEnforcer
{
    private readonly IRadarrCapabilityFetcher _capabilityFetcher;

    public RadarrCapabilityEnforcer(IRadarrCapabilityFetcher capabilityFetcher)
    {
        _capabilityFetcher = capabilityFetcher;
    }

    public async Task Check(RadarrConfiguration config)
    {
        _ = await _capabilityFetcher.GetCapabilities(config);

        // For the future: Add more capability checks here as needed
    }
}
