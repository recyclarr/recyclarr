using Recyclarr.Sync.Events;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal class QualityProfileLogger(ILogger log, ISyncEventPublisher eventPublisher)
{
    public void LogTransactionNotices(QualityProfilePipelineContext context)
    {
        var transactions = context.TransactionOutput;

        if (transactions.NonExistentProfiles.Count > 0)
        {
            var message =
                "The following quality profile names have no definition in the top-level `quality_profiles` "
                + "list *and* do not exist in the remote service. Either create them manually in the service *or* add "
                + "them to the top-level `quality_profiles` section so that Recyclarr can create the profiles for "
                + $"you: {string.Join(", ", transactions.NonExistentProfiles)}";
            eventPublisher.AddWarning(message);
            context.Publisher.AddWarning(message);
        }

        foreach (var (profile, errors) in transactions.InvalidProfiles)
        {
            foreach (var error in errors)
            {
                var message = $"Profile '{profile.ProfileName}': {error.ErrorMessage}";
                eventPublisher.AddError(message);
                context.Publisher.AddError(message);
            }
        }

        // Log warnings for new profiles
        foreach (var profile in transactions.NewProfiles)
        {
            LogProfileWarnings(context, profile);
        }

        // Log warnings for updated profiles
        foreach (var profileWithStats in transactions.UpdatedProfiles)
        {
            LogProfileWarnings(context, profileWithStats.Profile);
        }
    }

    private void LogProfileWarnings(
        QualityProfilePipelineContext context,
        UpdatedQualityProfile profile
    )
    {
        var invalidQualityNames = profile.UpdatedQualities.InvalidQualityNames;
        if (invalidQualityNames.Count != 0)
        {
            var message =
                $"Quality profile '{profile.ProfileName}' references invalid quality names: "
                + string.Join(", ", invalidQualityNames);
            eventPublisher.AddWarning(message);
            context.Publisher.AddWarning(message);
        }

        var invalidCfExceptNames = profile.InvalidExceptCfNames;
        if (invalidCfExceptNames.Count != 0)
        {
            var message =
                $"`except` under `reset_unmatched_scores` in quality profile '{profile.ProfileName}' has "
                + $"invalid CF names: {string.Join(", ", invalidCfExceptNames)}";
            eventPublisher.AddWarning(message);
            context.Publisher.AddWarning(message);
        }

        var missingQualities = profile.MissingQualities;
        if (missingQualities.Count != 0)
        {
            log.Information(
                "Recyclarr detected that the following required qualities are missing from profile "
                    + "'{ProfileName}' and will re-add them: {QualityNames}",
                profile.ProfileName,
                missingQualities
            );
        }
    }

    public void LogPersistenceResults(QualityProfilePipelineContext context)
    {
        var transactions = context.TransactionOutput;

        // Profiles without changes get logged
        if (transactions.UnchangedProfiles.Count != 0)
        {
            log.Debug(
                "These profiles have no changes and will not be persisted: {Profiles}",
                transactions.UnchangedProfiles.Select(x => x.ProfileName)
            );
        }

        // Log created profiles
        if (transactions.NewProfiles.Count > 0)
        {
            log.Information(
                "Created {Count} Profiles: {Names}",
                transactions.NewProfiles.Count,
                transactions.NewProfiles.Select(x => x.ProfileName)
            );
        }

        // Log updated profiles
        if (transactions.UpdatedProfiles.Count > 0)
        {
            log.Information(
                "Updated {Count} Profiles: {Names}",
                transactions.UpdatedProfiles.Count,
                transactions.UpdatedProfiles.Select(x => x.Profile.ProfileName)
            );
        }

        var totalChanged = transactions.NewProfiles.Count + transactions.UpdatedProfiles.Count;
        if (totalChanged != 0)
        {
            var numQuality = transactions.UpdatedProfiles.Count(x => x.QualitiesChanged);
            var numScores = transactions.UpdatedProfiles.Count(x => x.ScoresChanged);

            log.Information(
                "A total of {NumProfiles} profiles were synced. {NumQuality} contain quality changes and "
                    + "{NumScores} contain updated scores",
                totalChanged,
                numQuality,
                numScores
            );
        }
        else
        {
            log.Information("All quality profiles are up to date!");
        }

        context.Progress.SetStatus(PipelineProgressStatus.Succeeded, totalChanged);
        context.Publisher.SetStatus(PipelineProgressStatus.Succeeded, totalChanged);
    }
}
