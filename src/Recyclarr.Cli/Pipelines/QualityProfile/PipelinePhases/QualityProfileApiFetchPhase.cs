using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public record QualityProfileServiceData(IReadOnlyList<QualityProfileDto> Profiles, QualityProfileDto Schema);

public class QualityProfileApiFetchPhase(IQualityProfileApiService api)
    : IApiFetchPipelinePhase<QualityProfilePipelineContext>
{
    public async Task Execute(QualityProfilePipelineContext context, IServiceConfiguration config)
    {
        var profiles = await api.GetQualityProfiles(config);
        var schema = await api.GetSchema(config);
        context.ApiFetchOutput = new QualityProfileServiceData(profiles.AsReadOnly(), schema);
    }
}
