using Recyclarr.TrashLib.Config;

namespace Recyclarr.TrashLib.Compatibility.Radarr;

public interface IRadarrCapabilityFetcher
{
    Task<RadarrCapabilities> GetCapabilities(IServiceConfiguration config);
}
