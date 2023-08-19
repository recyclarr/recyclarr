using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Compatibility.Sonarr;

public interface ISonarrCapabilityFetcher
{
    Task<SonarrCapabilities> GetCapabilities(IServiceConfiguration config);
}
