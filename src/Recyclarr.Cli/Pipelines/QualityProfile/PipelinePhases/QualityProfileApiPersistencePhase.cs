using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public class QualityProfileApiPersistencePhase(IQualityProfileApiService api)
    : IApiPersistencePipelinePhase<QualityProfilePipelineContext>
{
    public async Task Execute(QualityProfilePipelineContext context, IServiceConfiguration config)
    {
        var changedProfiles = context.TransactionOutput.ChangedProfiles;
        foreach (var profile in changedProfiles.Select(x => x.Profile))
        {
            var dto = profile.BuildUpdatedDto();

            switch (profile.UpdateReason)
            {
                case QualityProfileUpdateReason.New:
                    await api.CreateQualityProfile(config, dto);
                    break;

                case QualityProfileUpdateReason.Changed:
                    await api.UpdateQualityProfile(config, dto);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported UpdateReason: {profile.UpdateReason}");
            }
        }
    }
}
