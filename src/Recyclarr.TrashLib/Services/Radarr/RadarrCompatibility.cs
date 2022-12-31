using Recyclarr.TrashLib.Services.Common;
using Recyclarr.TrashLib.Services.System;

namespace Recyclarr.TrashLib.Services.Radarr;

public class RadarrCompatibility : ServiceCompatibility<RadarrCapabilities>
{
    public RadarrCompatibility(IServiceInformation compatibility)
        : base(compatibility)
    {
    }

    protected override RadarrCapabilities BuildCapabilitiesObject(Version version)
    {
        return new RadarrCapabilities(version);
    }
}
