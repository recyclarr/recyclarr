namespace Recyclarr.Compatibility.Sonarr;

public class SonarrCapabilityFetcher(IServiceInformation info)
    : ServiceCapabilityFetcher<SonarrCapabilities>(info), ISonarrCapabilityFetcher
{
    protected override SonarrCapabilities BuildCapabilitiesObject(Version version)
    {
        return new SonarrCapabilities(version);
    }
}
