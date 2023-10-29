using Recyclarr.Cli.Pipelines.Generic;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;

public class ReleaseProfileLogPhase(ILogger log) : ILogPipelinePhase<ReleaseProfilePipelineContext>
{
    public bool LogConfigPhaseAndExitIfNeeded(ReleaseProfilePipelineContext context)
    {
        if (context.ConfigOutput is {Count: > 0})
        {
            return false;
        }

        log.Debug("No Release Profiles to process");
        return true;
    }

    public void LogTransactionNotices(ReleaseProfilePipelineContext context)
    {
    }

    public void LogPersistenceResults(ReleaseProfilePipelineContext context)
    {
        var transactions = context.TransactionOutput;
        var somethingChanged = false;

        if (transactions.UpdatedProfiles.Count != 0)
        {
            log.Information("Update existing profiles: {ProfileNames}",
                transactions.UpdatedProfiles.Select(x => x.Name));
            somethingChanged = true;
        }

        if (transactions.CreatedProfiles.Count != 0)
        {
            log.Information("Create new profiles: {ProfileNames}", transactions.CreatedProfiles.Select(x => x.Name));
            somethingChanged = true;
        }

        if (transactions.DeletedProfiles.Count != 0)
        {
            log.Information("Deleting old release profiles: {ProfileNames}",
                transactions.DeletedProfiles.Select(x => x.Name));
            somethingChanged = true;
        }

        if (!somethingChanged)
        {
            log.Information("All Release Profiles are up to date!");
        }
    }
}
