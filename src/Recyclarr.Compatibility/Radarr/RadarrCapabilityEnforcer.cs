namespace Recyclarr.Compatibility.Radarr;

public class RadarrCapabilityEnforcer(IRadarrCapabilityFetcher capabilityFetcher)
{
    public async Task Check()
    {
        _ = await capabilityFetcher.GetCapabilities();

        // For the future: Add more capability checks here as needed
    }
}
