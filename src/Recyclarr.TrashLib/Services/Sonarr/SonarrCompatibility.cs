using Recyclarr.TrashLib.Services.System;
using Serilog;

namespace Recyclarr.TrashLib.Services.Sonarr;

public class SonarrCompatibility : ServiceCompatibility<SonarrCapabilities>, ISonarrCompatibility
{
    public SonarrCompatibility(ISystemApiService api, ILogger log)
        : base(api, log)
    {
    }

    public Version MinimumVersion => new("3.0.4.1098");

    protected override SonarrCapabilities BuildCapabilitiesObject(Version version)
    {
        return new SonarrCapabilities(version)
        {
            SupportsNamedReleaseProfiles =
                version >= MinimumVersion,

            ArraysNeededForReleaseProfileRequiredAndIgnored =
                version >= new Version("3.0.6.1355"),

            SupportsCustomFormats =
                version >= new Version(4, 0)
        };
    }
}
