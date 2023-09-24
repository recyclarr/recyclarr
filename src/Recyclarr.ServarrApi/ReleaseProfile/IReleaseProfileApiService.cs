using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.ReleaseProfile;

public interface IReleaseProfileApiService
{
    Task UpdateReleaseProfile(IServiceConfiguration config, SonarrReleaseProfile profile);
    Task<SonarrReleaseProfile> CreateReleaseProfile(IServiceConfiguration config, SonarrReleaseProfile profile);
    Task<IList<SonarrReleaseProfile>> GetReleaseProfiles(IServiceConfiguration config);
    Task DeleteReleaseProfile(IServiceConfiguration config, int releaseProfileId);
}
