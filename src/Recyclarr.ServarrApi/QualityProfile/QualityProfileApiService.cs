using Flurl.Http;

namespace Recyclarr.ServarrApi.QualityProfile;

internal class QualityProfileApiService(IServarrRequestBuilder service) : IQualityProfileApiService
{
    private IFlurlRequest Request(params object[] path)
    {
        return service.Request(["qualityprofile", ..path]);
    }

    public async Task<IList<QualityProfileDto>> GetQualityProfiles()
    {
        var response = await Request()
            .GetJsonAsync<IList<QualityProfileDto>>();

        return response.Select(x => x.ReverseItems()).ToList();
    }

    public async Task<QualityProfileDto> GetSchema()
    {
        var response = await Request("schema")
            .GetJsonAsync<QualityProfileDto>();

        return response.ReverseItems();
    }

    public async Task UpdateQualityProfile(QualityProfileDto profile)
    {
        if (profile.Id is null)
        {
            throw new ArgumentException($"Profile's ID property must not be null: {profile.Name}");
        }

        await Request(profile.Id)
            .PutJsonAsync(profile.ReverseItems());
    }

    public async Task CreateQualityProfile(QualityProfileDto profile)
    {
        var response = await Request()
            .PostJsonAsync(profile.ReverseItems())
            .ReceiveJson<QualityProfileDto>();

        profile.Id = response.Id;
    }
}
