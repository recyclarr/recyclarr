using Recyclarr.TrashLib.Services.Common;
using Recyclarr.TrashLib.Services.System;

namespace Recyclarr.TrashLib.Services.Sonarr;

public class SonarrCompatibility : ServiceCompatibility<SonarrCapabilities>, ISonarrCompatibility
{
    public SonarrCompatibility(IServiceInformation compatibility)
        : base(compatibility)
    {
    }

    protected override SonarrCapabilities BuildCapabilitiesObject(Version version)
    {
        return new SonarrCapabilities(version)
        {
            SupportsNamedReleaseProfiles =
                version >= SonarrCapabilities.MinimumVersion,

            ArraysNeededForReleaseProfileRequiredAndIgnored =
                version >= new Version("3.0.6.1355"),

            SupportsCustomFormats =
                version >= new Version(4, 0)
        };
    }
}
