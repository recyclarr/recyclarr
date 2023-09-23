using Flurl.Http;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Dto;
using Recyclarr.ServarrApi.Http;

namespace Recyclarr.ServarrApi.Services;

public class ReleaseProfileApiService : IReleaseProfileApiService
{
    private readonly IServiceRequestBuilder _service;

    public ReleaseProfileApiService(IServiceRequestBuilder service)
    {
        _service = service;
    }

    public async Task UpdateReleaseProfile(IServiceConfiguration config, SonarrReleaseProfile profile)
    {
        await _service.Request(config, "releaseprofile", profile.Id)
            .PutJsonAsync(profile);
    }

    public async Task<SonarrReleaseProfile> CreateReleaseProfile(
        IServiceConfiguration config,
        SonarrReleaseProfile profile)
    {
        return await _service.Request(config, "releaseprofile")
            .PostJsonAsync(profile)
            .ReceiveJson<SonarrReleaseProfile>();
    }

    public async Task<IList<SonarrReleaseProfile>> GetReleaseProfiles(IServiceConfiguration config)
    {
        return await _service.Request(config, "releaseprofile")
            .GetJsonAsync<List<SonarrReleaseProfile>>();
    }

    public async Task DeleteReleaseProfile(IServiceConfiguration config, int releaseProfileId)
    {
        await _service.Request(config, "releaseprofile", releaseProfileId)
            .DeleteAsync();
    }
}
