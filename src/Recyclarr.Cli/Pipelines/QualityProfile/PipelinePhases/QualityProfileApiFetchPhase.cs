using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public record QualityProfileServiceData(IReadOnlyList<QualityProfileDto> Profiles, QualityProfileDto Schema);

public class QualityProfileApiFetchPhase
{
    private readonly IQualityProfileApiService _api;

    public QualityProfileApiFetchPhase(IQualityProfileApiService api)
    {
        _api = api;
    }

    public async Task<QualityProfileServiceData> Execute(IServiceConfiguration config)
    {
        var profiles = await _api.GetQualityProfiles(config);
        var schema = await _api.GetSchema(config);
        return new QualityProfileServiceData(profiles.AsReadOnly(), schema);
    }
}
