namespace Recyclarr.Compatibility.Sonarr;

public class SonarrCapabilityEnforcer(ISonarrCapabilityFetcher capabilityFetcher)
{
    public async Task Check(CancellationToken ct)
    {
        var capabilities = await capabilityFetcher.GetCapabilities(ct);

        if (capabilities.Version < SonarrCapabilities.MinimumVersion)
        {
            throw new ServiceIncompatibilityException(
                $"Your Sonarr version {capabilities.Version} does not meet the minimum " +
                $"required version of {SonarrCapabilities.MinimumVersion}.");
        }
    }
}
