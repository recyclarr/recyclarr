using Recyclarr.TrashLib.Services.Common;
using Recyclarr.TrashLib.Services.System;

namespace Recyclarr.TrashLib.Services.Sonarr.Capabilities;

public class SonarrCapabilityChecker : ServiceCapabilityChecker<SonarrCapabilities>, ISonarrCapabilityChecker
{
    public SonarrCapabilityChecker(IServiceInformation info)
        : base(info)
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
