using Recyclarr.TrashLib.Services.Common;
using Recyclarr.TrashLib.Services.System;

namespace Recyclarr.TrashLib.Services.Radarr;

public class RadarrCapabilityChecker : ServiceCapabilityChecker<RadarrCapabilities>, IRadarrCapabilityChecker
{
    public RadarrCapabilityChecker(IServiceInformation info)
        : base(info)
    {
    }

    protected override RadarrCapabilities BuildCapabilitiesObject(Version? version)
    {
        return new RadarrCapabilities(version);
    }
}
