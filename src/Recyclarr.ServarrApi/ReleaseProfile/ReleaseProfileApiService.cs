using Flurl.Http;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Http;

namespace Recyclarr.ServarrApi.ReleaseProfile;

public class ReleaseProfileApiService(IServarrRequestBuilder service) : IReleaseProfileApiService
{
    public async Task UpdateReleaseProfile(IServiceConfiguration config, SonarrReleaseProfile profile)
    {
        await service.Request(config, "releaseprofile", profile.Id)
            .PutJsonAsync(profile);
    }

    public async Task<SonarrReleaseProfile> CreateReleaseProfile(
        IServiceConfiguration config,
        SonarrReleaseProfile profile)
    {
        return await service.Request(config, "releaseprofile")
            .PostJsonAsync(profile)
            .ReceiveJson<SonarrReleaseProfile>();
    }

    public async Task<IList<SonarrReleaseProfile>> GetReleaseProfiles(IServiceConfiguration config)
    {
        return await service.Request(config, "releaseprofile")
            .GetJsonAsync<List<SonarrReleaseProfile>>();
    }

    public async Task DeleteReleaseProfile(IServiceConfiguration config, int releaseProfileId)
    {
        await service.Request(config, "releaseprofile", releaseProfileId)
            .DeleteAsync();
    }
}
