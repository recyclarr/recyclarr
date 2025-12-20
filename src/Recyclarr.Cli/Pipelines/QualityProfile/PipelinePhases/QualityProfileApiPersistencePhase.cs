using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.QualityProfile.Cache;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class QualityProfileApiPersistencePhase(
    IQualityProfileApiService api,
    ICachePersister<QualityProfileCache> cachePersister,
    QualityProfileLogger logger
) : IPipelinePhase<QualityProfilePipelineContext>
{
    public async Task<PipelineFlow> Execute(
        QualityProfilePipelineContext context,
        CancellationToken ct
    )
    {
        var changedProfiles = context.TransactionOutput.ChangedProfiles;
        foreach (var profile in changedProfiles.Select(x => x.Profile))
        {
            var dto = profile.BuildUpdatedDto();

            switch (profile.UpdateReason)
            {
                case QualityProfileUpdateReason.New:
                    await api.CreateQualityProfile(dto, ct);
                    // After creation, dto.Id is set by the API service
                    profile.ProfileDto.Id = dto.Id;
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

        context.Cache.Update(context.TransactionOutput, context.ApiFetchOutput.Profiles);
        cachePersister.Save(context.Cache);

        logger.LogPersistenceResults(context);
        return PipelineFlow.Continue;
    }
}
