using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public record QualityProfileServiceData(IReadOnlyList<QualityProfileDto> Profiles, QualityProfileDto Schema);

public class QualityProfileApiFetchPhase(IQualityProfileApiService api)
{
    public async Task<QualityProfileServiceData> Execute(IServiceConfiguration config)
    {
        var profiles = await api.GetQualityProfiles(config);
        var schema = await api.GetSchema(config);
        return new QualityProfileServiceData(profiles.AsReadOnly(), schema);
    }
}
