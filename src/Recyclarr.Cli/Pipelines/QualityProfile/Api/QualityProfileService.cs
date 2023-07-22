using Flurl.Http;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Http;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Api;

internal class QualityProfileService : IQualityProfileService
{
    private readonly IServiceRequestBuilder _service;

    public QualityProfileService(IServiceRequestBuilder service)
    {
        _service = service;
    }

    public async Task<IList<QualityProfileDto>> GetQualityProfiles(IServiceConfiguration config)
    {
        var response = await _service.Request(config, "qualityprofile")
            .GetJsonAsync<IList<QualityProfileDto>>();

        return response.Select(x => x.ReverseItems()).ToList();
    }

    public async Task<QualityProfileDto> GetSchema(IServiceConfiguration config)
    {
        var response = await _service.Request(config, "qualityprofile", "schema")
            .GetJsonAsync<QualityProfileDto>();

        return response.ReverseItems();
    }

    public async Task UpdateQualityProfile(IServiceConfiguration config, QualityProfileDto profile)
    {
        if (profile.Id is null)
        {
            throw new ArgumentException($"Profile's ID property must not be null: {profile.Name}");
        }

        await _service.Request(config, "qualityprofile", profile.Id)
            .PutJsonAsync(profile.ReverseItems());
    }

    public async Task CreateQualityProfile(IServiceConfiguration config, QualityProfileDto profile)
    {
        var response = await _service.Request(config, "qualityprofile")
            .PostJsonAsync(profile.ReverseItems())
            .ReceiveJson<QualityProfileDto>();

        profile.Id = response.Id;
    }
}
