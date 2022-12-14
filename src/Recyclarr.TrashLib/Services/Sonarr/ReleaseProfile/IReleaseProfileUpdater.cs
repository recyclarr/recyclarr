using Recyclarr.TrashLib.Services.Sonarr.Config;

namespace Recyclarr.TrashLib.Services.Sonarr.ReleaseProfile;

public interface IReleaseProfileUpdater
{
    Task Process(bool isPreview, SonarrConfiguration config);
}
