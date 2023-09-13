using Recyclarr.TrashLib.ApiServices.System;

namespace Recyclarr.TrashLib.Compatibility.Sonarr;

public class SonarrCapabilityFetcher : ServiceCapabilityFetcher<SonarrCapabilities>, ISonarrCapabilityFetcher
{
    public SonarrCapabilityFetcher(IServiceInformation info)
        : base(info)
    {
    }

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
