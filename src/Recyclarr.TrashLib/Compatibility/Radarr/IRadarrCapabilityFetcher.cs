using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Compatibility.Radarr;

public interface IRadarrCapabilityFetcher
{
    Task<RadarrCapabilities?> GetCapabilities(IServiceConfiguration config);
}
