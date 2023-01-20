using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Services.Sonarr.Capabilities;

public interface ISonarrCapabilityChecker
{
    Task<SonarrCapabilities?> GetCapabilities(IServiceConfiguration config);
}
