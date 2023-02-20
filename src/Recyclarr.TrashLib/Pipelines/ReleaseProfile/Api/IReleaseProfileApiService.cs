using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.ReleaseProfile.Api.Objects;

namespace Recyclarr.TrashLib.Pipelines.ReleaseProfile.Api;

public interface IReleaseProfileApiService
{
    Task UpdateReleaseProfile(IServiceConfiguration config, SonarrReleaseProfile profile);
    Task<SonarrReleaseProfile> CreateReleaseProfile(IServiceConfiguration config, SonarrReleaseProfile profile);
    Task<IList<SonarrReleaseProfile>> GetReleaseProfiles(IServiceConfiguration config);
    Task DeleteReleaseProfile(IServiceConfiguration config, int releaseProfileId);
}
