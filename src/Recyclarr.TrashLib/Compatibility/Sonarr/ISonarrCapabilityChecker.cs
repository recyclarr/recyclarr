using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Compatibility.Sonarr;

public interface ISonarrCapabilityChecker
{
    Task<SonarrCapabilities?> GetCapabilities(IServiceConfiguration config);
}
