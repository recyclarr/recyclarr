namespace Recyclarr.Compatibility.Radarr;

public class RadarrCapabilityEnforcer(IRadarrCapabilityFetcher capabilityFetcher)
{
    public async Task<RadarrCapabilities> Check(CancellationToken ct)
    {
        // For the future: Add more capability checks here as needed
        return await capabilityFetcher.GetCapabilities(ct);
    }
}
