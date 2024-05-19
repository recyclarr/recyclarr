using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public record QualityProfileServiceData(IReadOnlyList<QualityProfileDto> Profiles, QualityProfileDto Schema);

public class QualityProfileApiFetchPhase(IQualityProfileApiService api)
    : IApiFetchPipelinePhase<QualityProfilePipelineContext>
{
    public async Task Execute(QualityProfilePipelineContext context)
    {
        var profiles = await api.GetQualityProfiles();
        var schema = await api.GetSchema();
        context.ApiFetchOutput = new QualityProfileServiceData(profiles.AsReadOnly(), schema);
    }
}
