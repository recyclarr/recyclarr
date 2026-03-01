using Flurl.Http;

namespace Recyclarr.ServarrApi.QualityProfile;

internal class QualityProfileApiService(IServarrRequestBuilder service) : IQualityProfileApiService
{
    private IFlurlRequest Request(params object[] path)
    {
        return service.Request(["qualityprofile", .. path]);
    }

    public async Task<IList<ServiceQualityProfileData>> GetQualityProfiles(CancellationToken ct)
    {
        var response = await Request()
            .GetJsonAsync<IList<ServiceQualityProfileData>>(cancellationToken: ct);

        return response.Select(x => x.ReverseItems()).ToList();
    }

    public async Task<ServiceQualityProfileData> GetSchema(CancellationToken ct)
    {
        var response = await Request("schema")
            .GetJsonAsync<ServiceQualityProfileData>(cancellationToken: ct);

        return response.ReverseItems();
    }

    public async Task UpdateQualityProfile(ServiceQualityProfileData profile, CancellationToken ct)
    {
        if (profile.Id is null)
        {
            throw new ArgumentException($"Profile's ID property must not be null: {profile.Name}");
        }

        await Request(profile.Id).PutJsonAsync(profile.ReverseItems(), cancellationToken: ct);
    }

    public async Task<ServiceQualityProfileData> CreateQualityProfile(
        ServiceQualityProfileData profile,
        CancellationToken ct
    )
    {
        var response = await Request()
            .PostJsonAsync(profile.ReverseItems(), cancellationToken: ct)
            .ReceiveJson<ServiceQualityProfileData>();

        return response.ReverseItems();
    }

    public async Task<IList<ServiceProfileLanguage>> GetLanguages(CancellationToken ct)
    {
        return await service
            .Request("language")
            .GetJsonAsync<IList<ServiceProfileLanguage>>(cancellationToken: ct);
    }
}
