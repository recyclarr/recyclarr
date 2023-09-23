using Recyclarr.Config.Models;

namespace Recyclarr.TrashLib.Compatibility.Sonarr;

public interface ISonarrCapabilityFetcher
{
    Task<SonarrCapabilities> GetCapabilities(IServiceConfiguration config);
}
