using Flurl.Http;

namespace Recyclarr.ServarrApi.QualityProfile;

internal class QualityProfileApiService(IServarrRequestBuilder service) : IQualityProfileApiService
{
    public async Task<IList<QualityProfileDto>> GetQualityProfiles()
    {
        var response = await service.Request("qualityprofile")
            .GetJsonAsync<IList<QualityProfileDto>>();

        return response.Select(x => x.ReverseItems()).ToList();
    }

    public async Task<QualityProfileDto> GetSchema()
    {
        var response = await service.Request("qualityprofile", "schema")
            .GetJsonAsync<QualityProfileDto>();

        return response.ReverseItems();
    }

    public async Task UpdateQualityProfile(QualityProfileDto profile)
    {
        if (profile.Id is null)
        {
            throw new ArgumentException($"Profile's ID property must not be null: {profile.Name}");
        }

        await service.Request("qualityprofile", profile.Id)
            .PutJsonAsync(profile.ReverseItems());
    }

    public async Task CreateQualityProfile(QualityProfileDto profile)
    {
        var response = await service.Request("qualityprofile")
            .PostJsonAsync(profile.ReverseItems())
            .ReceiveJson<QualityProfileDto>();

        profile.Id = response.Id;
    }
}
