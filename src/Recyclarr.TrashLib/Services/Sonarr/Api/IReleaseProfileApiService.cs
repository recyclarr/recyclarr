using Recyclarr.TrashLib.Services.Sonarr.Api.Objects;

namespace Recyclarr.TrashLib.Services.Sonarr.Api;

public interface IReleaseProfileApiService
{
    Task UpdateReleaseProfile(SonarrReleaseProfile profile);
    Task<SonarrReleaseProfile> CreateReleaseProfile(SonarrReleaseProfile profile);
    Task<IList<SonarrReleaseProfile>> GetReleaseProfiles();
    Task DeleteReleaseProfile(int releaseProfileId);
}
