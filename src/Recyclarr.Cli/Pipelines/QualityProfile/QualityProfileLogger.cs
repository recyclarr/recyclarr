using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Sync.Events;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal class QualityProfileLogger(
    ILogger log,
    ValidationLogger validationLogger,
    ISyncEventPublisher eventPublisher,
    IProgressSource progressSource
)
{
    public void LogTransactionNotices(QualityProfilePipelineContext context)
    {
        var transactions = context.TransactionOutput;

        if (transactions.NonExistentProfiles.Count > 0)
        {
            eventPublisher.AddWarning(
                "The following quality profile names have no definition in the top-level `quality_profiles` "
                    + "list *and* do not exist in the remote service. Either create them manually in the service *or* add "
                    + "them to the top-level `quality_profiles` section so that Recyclarr can create the profiles for "
                    + $"you: {string.Join(", ", transactions.NonExistentProfiles)}"
            );
        }

        if (transactions.InvalidProfiles.Count > 0)
        {
            eventPublisher.AddWarning(
                "The following validation errors occurred for one or more quality profiles. "
                    + "These profiles will *not* be synced"
            );

            foreach (var (profile, errors) in transactions.InvalidProfiles)
            {
                validationLogger.LogValidationErrors(errors, $"Profile '{profile.ProfileName}'");
            }

            validationLogger.LogTotalErrorCount("Profile validation");
        }

        foreach (var profile in transactions.ChangedProfiles.Select(x => x.Profile))
        {
            var invalidQualityNames = profile.UpdatedQualities.InvalidQualityNames;
            if (invalidQualityNames.Count != 0)
            {
                eventPublisher.AddWarning(
                    $"Quality profile '{profile.ProfileName}' references invalid quality names: "
                        + string.Join(", ", invalidQualityNames)
                );
            }

            var invalidCfExceptNames = profile.InvalidExceptCfNames;
            if (invalidCfExceptNames.Count != 0)
            {
                eventPublisher.AddWarning(
                    $"`except` under `reset_unmatched_scores` in quality profile '{profile.ProfileName}' has "
                        + $"invalid CF names: {string.Join(", ", invalidCfExceptNames)}"
                );
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
    }

    public void LogPersistenceResults(QualityProfilePipelineContext context)
    {
        var changedProfiles = context.TransactionOutput.ChangedProfiles;

        // Profiles without changes get logged
        var unchangedProfiles = context.TransactionOutput.UnchangedProfiles;
        if (unchangedProfiles.Count != 0)
        {
            log.Debug(
                "These profiles have no changes and will not be persisted: {Profiles}",
                unchangedProfiles.Select(x => x.Profile.ProfileName)
            );
        }

        var createdProfiles = changedProfiles
            .Where(x => x.Profile.UpdateReason == QualityProfileUpdateReason.New)
            .Select(x => x.Profile.ProfileName)
            .ToList();

        if (createdProfiles.Count > 0)
        {
            log.Information(
                "Created {Count} Profiles: {Names}",
                createdProfiles.Count,
                createdProfiles
            );
        }

        var updatedProfiles = changedProfiles
            .Where(x => x.Profile.UpdateReason == QualityProfileUpdateReason.Changed)
            .Select(x => x.Profile.ProfileName)
            .ToList();

        if (updatedProfiles.Count > 0)
        {
            log.Information(
                "Updated {Count} Profiles: {Names}",
                updatedProfiles.Count,
                updatedProfiles
            );
        }

        if (changedProfiles.Count != 0)
        {
            var numQuality = changedProfiles.Count(x => x.QualitiesChanged);
            var numScores = changedProfiles.Count(x => x.ScoresChanged);

            log.Information(
                "A total of {NumProfiles} profiles were synced. {NumQuality} contain quality changes and "
                    + "{NumScores} contain updated scores",
                changedProfiles.Count,
                numQuality,
                numScores
            );
        }
        else
        {
            log.Information("All quality profiles are up to date!");
        }

        progressSource.SetPipelineStatus(PipelineProgressStatus.Succeeded, changedProfiles.Count);
    }
}
