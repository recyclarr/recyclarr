using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Dto;

namespace Recyclarr.ServarrApi.Services;

public interface IReleaseProfileApiService
{
    Task UpdateReleaseProfile(IServiceConfiguration config, SonarrReleaseProfile profile);
    Task<SonarrReleaseProfile> CreateReleaseProfile(IServiceConfiguration config, SonarrReleaseProfile profile);
    Task<IList<SonarrReleaseProfile>> GetReleaseProfiles(IServiceConfiguration config);
    Task DeleteReleaseProfile(IServiceConfiguration config, int releaseProfileId);
}
