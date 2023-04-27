using Recyclarr.Cli.Pipelines.ReleaseProfile.Api.Objects;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Api;

public interface IReleaseProfileApiService
{
    Task UpdateReleaseProfile(IServiceConfiguration config, SonarrReleaseProfile profile);
    Task<SonarrReleaseProfile> CreateReleaseProfile(IServiceConfiguration config, SonarrReleaseProfile profile);
    Task<IList<SonarrReleaseProfile>> GetReleaseProfiles(IServiceConfiguration config);
    Task DeleteReleaseProfile(IServiceConfiguration config, int releaseProfileId);
}
