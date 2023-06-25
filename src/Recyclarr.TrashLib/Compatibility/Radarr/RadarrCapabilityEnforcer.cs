using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.ExceptionTypes;

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
        var capabilities = await _capabilityFetcher.GetCapabilities(config);
        if (capabilities is null)
        {
            throw new ServiceIncompatibilityException("Capabilities could not be obtained");
        }

        // For the future: Add more capability checks here as needed
    }
}
