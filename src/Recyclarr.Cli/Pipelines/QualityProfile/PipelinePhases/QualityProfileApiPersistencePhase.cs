using Recyclarr.Cli.Pipelines.QualityProfile.State;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class QualityProfileApiPersistencePhase(
    IQualityProfileService service,
    IQualityProfileStatePersister statePersister,
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
            profile.Profile = await service.CreateQualityProfile(profile.BuildMergedProfile(), ct);
        }

        // Update existing profiles with changes
        foreach (var profileWithStats in transactions.UpdatedProfiles)
        {
            var merged = profileWithStats.Profile.BuildMergedProfile();
            await service.UpdateQualityProfile(merged, ct);
        }

        context.State.Update(context);
        statePersister.Save(context.State);

        logger.LogPersistenceResults(context);
        return PipelineFlow.Continue;
    }
}
