using Recyclarr.Config.Models;

namespace Recyclarr.Compatibility.Sonarr;

public interface ISonarrCapabilityFetcher
{
    Task<SonarrCapabilities> GetCapabilities(IServiceConfiguration config);
}
