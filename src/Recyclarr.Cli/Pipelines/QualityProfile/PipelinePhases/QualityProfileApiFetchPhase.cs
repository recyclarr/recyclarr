using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public record QualityProfileServiceData(
    IReadOnlyList<QualityProfileDto> Profiles,
    QualityProfileDto Schema
);

public class QualityProfileApiFetchPhase(IQualityProfileApiService api)
    : IApiFetchPipelinePhase<QualityProfilePipelineContext>
{
    public async Task Execute(QualityProfilePipelineContext context, CancellationToken ct)
    {
        var profiles = await api.GetQualityProfiles(ct);
        var schema = await api.GetSchema(ct);
        context.ApiFetchOutput = new QualityProfileServiceData(profiles.AsReadOnly(), schema);
    }
}
