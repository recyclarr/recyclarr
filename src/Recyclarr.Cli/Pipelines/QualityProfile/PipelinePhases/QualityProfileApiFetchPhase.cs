using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class QualityProfileApiFetchPhase(IQualityProfileApiService api)
    : IPipelinePhase<QualityProfilePipelineContext>
{
    public async Task<bool> Execute(QualityProfilePipelineContext context, CancellationToken ct)
    {
        var profiles = await api.GetQualityProfiles(ct);
        var schema = await api.GetSchema(ct);
        context.ApiFetchOutput = new QualityProfileServiceData(profiles.AsReadOnly(), schema);
        return true;
    }
}
