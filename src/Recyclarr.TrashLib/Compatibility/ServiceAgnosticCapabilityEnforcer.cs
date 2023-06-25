using Recyclarr.TrashLib.Compatibility.Radarr;
using Recyclarr.TrashLib.Compatibility.Sonarr;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Compatibility;

public class ServiceAgnosticCapabilityEnforcer
{
    private readonly SonarrCapabilityEnforcer _sonarrEnforcer;
    private readonly RadarrCapabilityEnforcer _radarrEnforcer;

    public ServiceAgnosticCapabilityEnforcer(
        SonarrCapabilityEnforcer sonarrEnforcer,
        RadarrCapabilityEnforcer radarrEnforcer)
    {
        _sonarrEnforcer = sonarrEnforcer;
        _radarrEnforcer = radarrEnforcer;
    }

    public async Task Check(IServiceConfiguration config)
    {
        switch (config)
        {
            case SonarrConfiguration c:
                await _sonarrEnforcer.Check(c);
                break;

            case RadarrConfiguration c:
                await _radarrEnforcer.Check(c);
                break;
        }
    }
}
