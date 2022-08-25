using TrashLib.Services.Sonarr.Config;

namespace TrashLib.Services.Sonarr.ReleaseProfile;

public interface IReleaseProfileUpdater
{
    Task Process(bool isPreview, SonarrConfiguration config);
}
