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

            SupportsNamedReleaseProfiles =
                version >= SonarrCapabilities.MinimumVersion,

            ArraysNeededForReleaseProfileRequiredAndIgnored =
                version >= new Version("3.0.6.1355"),

            SupportsCustomFormats =
                version >= new Version(4, 0)
        };
    }
}
