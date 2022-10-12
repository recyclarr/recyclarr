using TrashLib.Services.Sonarr.Config;

namespace Recyclarr.Command;

public interface ISonarrVersionEnforcement
{
    Task DoVersionEnforcement(SonarrConfiguration config);
}
