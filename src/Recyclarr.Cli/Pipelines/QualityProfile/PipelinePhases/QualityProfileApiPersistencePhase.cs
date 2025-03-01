using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class QualityProfileApiPersistencePhase(
    IQualityProfileApiService api,
    QualityProfileLogger logger
) : IPipelinePhase<QualityProfilePipelineContext>
{
    public async Task<bool> Execute(QualityProfilePipelineContext context, CancellationToken ct)
    {
        var changedProfiles = context.TransactionOutput.ChangedProfiles;
        foreach (var profile in changedProfiles.Select(x => x.Profile))
        {
            var dto = profile.BuildUpdatedDto();

            switch (profile.UpdateReason)
            {
                case QualityProfileUpdateReason.New:
                    await api.CreateQualityProfile(dto, ct);
                    break;

                case QualityProfileUpdateReason.Changed:
                    await api.UpdateQualityProfile(dto, ct);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported UpdateReason: {profile.UpdateReason}"
                    );
            }
        }

        logger.LogPersistenceResults(context);
        return true;
    }
}
