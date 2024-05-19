using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public class QualityProfileApiPersistencePhase(IQualityProfileApiService api)
    : IApiPersistencePipelinePhase<QualityProfilePipelineContext>
{
    public async Task Execute(QualityProfilePipelineContext context)
    {
        var changedProfiles = context.TransactionOutput.ChangedProfiles;
        foreach (var profile in changedProfiles.Select(x => x.Profile))
        {
            var dto = profile.BuildUpdatedDto();

            switch (profile.UpdateReason)
            {
                case QualityProfileUpdateReason.New:
                    await api.CreateQualityProfile(dto);
                    break;

                case QualityProfileUpdateReason.Changed:
                    await api.UpdateQualityProfile(dto);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported UpdateReason: {profile.UpdateReason}");
            }
        }
    }
}
