using Recyclarr.TrashLib.Services.Sonarr.Config;

namespace Recyclarr.TrashLib.Services.ReleaseProfile;

public interface IReleaseProfileUpdater
{
    Task Process(bool isPreview, SonarrConfiguration config);
}
