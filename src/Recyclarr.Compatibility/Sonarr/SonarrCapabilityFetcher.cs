namespace Recyclarr.Compatibility.Sonarr;

public class SonarrCapabilityFetcher(IServiceInformation info)
    : ServiceCapabilityFetcher<SonarrCapabilities>(info), ISonarrCapabilityFetcher
{
    protected override SonarrCapabilities BuildCapabilitiesObject(Version version)
    {
        return new SonarrCapabilities
        {
            Version = version,

            SupportsCustomFormats =
                version >= new Version(4, 0)
        };
    }
}
