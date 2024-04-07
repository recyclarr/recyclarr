using Flurl.Http;
using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.QualityProfile;

internal class QualityProfileApiService(IServarrRequestBuilder service) : IQualityProfileApiService
{
    public async Task<IList<QualityProfileDto>> GetQualityProfiles(IServiceConfiguration config)
    {
        var response = await service.Request(config, "qualityprofile")
            .GetJsonAsync<IList<QualityProfileDto>>();

        return response.Select(x => x.ReverseItems()).ToList();
    }

    public async Task<QualityProfileDto> GetSchema(IServiceConfiguration config)
    {
        var response = await service.Request(config, "qualityprofile", "schema")
            .GetJsonAsync<QualityProfileDto>();

        return response.ReverseItems();
    }

    public async Task UpdateQualityProfile(IServiceConfiguration config, QualityProfileDto profile)
    {
        if (profile.Id is null)
        {
            throw new ArgumentException($"Profile's ID property must not be null: {profile.Name}");
        }

        await service.Request(config, "qualityprofile", profile.Id)
            .PutJsonAsync(profile.ReverseItems());
    }

    public async Task CreateQualityProfile(IServiceConfiguration config, QualityProfileDto profile)
    {
        var response = await service.Request(config, "qualityprofile")
            .PostJsonAsync(profile.ReverseItems())
            .ReceiveJson<QualityProfileDto>();

        profile.Id = response.Id;
    }
}
