using Recyclarr.TrashLib.ApiServices.System;

namespace Recyclarr.TrashLib.Compatibility.Radarr;

public class RadarrCapabilityFetcher : ServiceCapabilityFetcher<RadarrCapabilities>, IRadarrCapabilityFetcher
{
    public RadarrCapabilityFetcher(IServiceInformation info)
        : base(info)
    {
    }

    protected override RadarrCapabilities BuildCapabilitiesObject(Version? version)
    {
        return new RadarrCapabilities(version);
    }
}
