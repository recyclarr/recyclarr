namespace Recyclarr.Compatibility.Radarr;

public class RadarrCapabilityFetcher(IServiceInformation info)
    : ServiceCapabilityFetcher<RadarrCapabilities>(info),
        IRadarrCapabilityFetcher
{
    protected override RadarrCapabilities BuildCapabilitiesObject(Version version)
    {
        return new RadarrCapabilities(version);
    }
}
