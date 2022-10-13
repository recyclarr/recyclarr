using TrashLib.Services.Sonarr.Config;

namespace TrashLib.Services.Sonarr;

public interface ISonarrVersionEnforcement
{
    Task DoVersionEnforcement(SonarrConfiguration config);
}
