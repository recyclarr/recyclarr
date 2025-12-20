using Flurl.Http;

namespace Recyclarr.ServarrApi.QualityProfile;

internal class QualityProfileApiService(IServarrRequestBuilder service) : IQualityProfileApiService
{
    private IFlurlRequest Request(params object[] path)
    {
        return service.Request(["qualityprofile", .. path]);
    }

    public async Task<IList<QualityProfileDto>> GetQualityProfiles(CancellationToken ct)
    {
        var response = await Request()
            .GetJsonAsync<IList<QualityProfileDto>>(cancellationToken: ct);

        return response.Select(x => x.ReverseItems()).ToList();
    }

    public async Task<QualityProfileDto> GetSchema(CancellationToken ct)
    {
        var response = await Request("schema")
            .GetJsonAsync<QualityProfileDto>(cancellationToken: ct);

        return response.ReverseItems();
    }

    public async Task UpdateQualityProfile(QualityProfileDto profile, CancellationToken ct)
    {
        if (profile.Id is null)
        {
            throw new ArgumentException($"Profile's ID property must not be null: {profile.Name}");
        }

        await Request(profile.Id).PutJsonAsync(profile.ReverseItems(), cancellationToken: ct);
    }

    public async Task CreateQualityProfile(QualityProfileDto profile, CancellationToken ct)
    {
        var response = await Request()
            .PostJsonAsync(profile.ReverseItems(), cancellationToken: ct)
            .ReceiveJson<QualityProfileDto>();

        profile.Id = response.Id;
    }

    public async Task<IList<ProfileLanguageDto>> GetLanguages(CancellationToken ct)
    {
        return await service
            .Request("language")
            .GetJsonAsync<IList<ProfileLanguageDto>>(cancellationToken: ct);
    }
}
