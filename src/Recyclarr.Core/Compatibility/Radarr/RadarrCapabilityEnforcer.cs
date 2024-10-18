namespace Recyclarr.Compatibility.Radarr;

public class RadarrCapabilityEnforcer(IRadarrCapabilityFetcher capabilityFetcher)
{
    public async Task Check(CancellationToken ct)
    {
        _ = await capabilityFetcher.GetCapabilities(ct);

        // For the future: Add more capability checks here as needed
    }
}
