using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class QualityProfileApiFetchPhase(IQualityProfileApiService api)
    : IPipelinePhase<QualityProfilePipelineContext>
{
    public async Task<PipelineFlow> Execute(
        QualityProfilePipelineContext context,
        CancellationToken ct
    )
    {
        var profiles = await api.GetQualityProfiles(ct);
        var schema = await api.GetSchema(ct);
        context.ApiFetchOutput = new QualityProfileServiceData(profiles.AsReadOnly(), schema);
        return PipelineFlow.Continue;
    }
}
