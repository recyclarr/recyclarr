using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.QualityProfile.Cache;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class QualityProfileApiPersistencePhase(
    IQualityProfileApiService api,
    ICachePersister<QualityProfileCacheObject> cachePersister,
    QualityProfileLogger logger
) : IPipelinePhase<QualityProfilePipelineContext>
{
    public async Task<PipelineFlow> Execute(
        QualityProfilePipelineContext context,
        CancellationToken ct
    )
    {
        var transactions = context.TransactionOutput;

        // Create new profiles
        foreach (var profile in transactions.NewProfiles)
        {
            profile.ProfileDto = await api.CreateQualityProfile(profile.BuildUpdatedDto(), ct);
        }

        // Update existing profiles with changes
        foreach (var profileWithStats in transactions.UpdatedProfiles)
        {
            var dto = profileWithStats.Profile.BuildUpdatedDto();
            await api.UpdateQualityProfile(dto, ct);
        }

        context.Cache.Update(context);
        cachePersister.Save(context.Cache);

        logger.LogPersistenceResults(context);
        return PipelineFlow.Continue;
    }
}
