using Recyclarr.TrashLib.Services.Sonarr.Config;

namespace Recyclarr.TrashLib.Services.Sonarr;

public interface ISonarrVersionEnforcement
{
    Task DoVersionEnforcement(SonarrConfiguration config);
}
