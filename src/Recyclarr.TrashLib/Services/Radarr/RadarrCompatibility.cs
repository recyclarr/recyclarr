using Recyclarr.TrashLib.Services.System;
using Serilog;

namespace Recyclarr.TrashLib.Services.Radarr;

public class RadarrCompatibility : ServiceCompatibility<RadarrCapabilities>
{
    public RadarrCompatibility(ISystemApiService api, ILogger log)
        : base(api, log)
    {
    }

    protected override RadarrCapabilities BuildCapabilitiesObject(Version version)
    {
        return new RadarrCapabilities(version);
    }
}
