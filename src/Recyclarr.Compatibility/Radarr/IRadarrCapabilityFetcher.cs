using Recyclarr.Config.Models;

namespace Recyclarr.Compatibility.Radarr;

public interface IRadarrCapabilityFetcher
{
    Task<RadarrCapabilities> GetCapabilities(IServiceConfiguration config);
}
