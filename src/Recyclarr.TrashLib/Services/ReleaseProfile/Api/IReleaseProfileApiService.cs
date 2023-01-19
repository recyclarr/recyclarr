using Recyclarr.TrashLib.Services.ReleaseProfile.Api.Objects;

namespace Recyclarr.TrashLib.Services.ReleaseProfile.Api;

public interface IReleaseProfileApiService
{
    Task UpdateReleaseProfile(SonarrReleaseProfile profile);
    Task<SonarrReleaseProfile> CreateReleaseProfile(SonarrReleaseProfile profile);
    Task<IList<SonarrReleaseProfile>> GetReleaseProfiles();
    Task DeleteReleaseProfile(int releaseProfileId);
}
