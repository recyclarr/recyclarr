using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Notifications;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public class QualityProfileLogPhase(
    ILogger log,
    ValidationLogger validationLogger,
    NotificationEmitter notificationEmitter)
    : ILogPipelinePhase<QualityProfilePipelineContext>
{
    public bool LogConfigPhaseAndExitIfNeeded(QualityProfilePipelineContext context)
    {
        if (!context.ConfigOutput.Any())
        {
            log.Debug("No Quality Profiles to process");
            return true;
        }

        return false;
    }

    public void LogTransactionNotices(QualityProfilePipelineContext context)
    {
        var transactions = context.TransactionOutput;

        if (transactions.NonExistentProfiles.Count > 0)
        {
            log.Warning(
                "The following quality profile names have no definition in the top-level `quality_profiles` " +
                "list *and* do not exist in the remote service. Either create them manually in the service *or* add " +
                "them to the top-level `quality_profiles` section so that Recyclarr can create the profiles for " +
                "you: {QualityProfileNames}", transactions.NonExistentProfiles);
        }

        if (transactions.InvalidProfiles.Count > 0)
        {
            log.Warning(
                "The following validation errors occurred for one or more quality profiles. " +
                "These profiles will *not* be synced");

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
                log.Warning("Quality profile '{ProfileName}' references invalid quality names: {InvalidNames}",
                    profile.ProfileName, invalidQualityNames);
            }

            var invalidCfExceptNames = profile.InvalidExceptCfNames;
            if (invalidCfExceptNames.Count != 0)
            {
                log.Warning(
                    "`except` under `reset_unmatched_scores` in quality profile '{ProfileName}' has invalid " +
                    "CF names: {CfNames}", profile.ProfileName, invalidCfExceptNames);
            }

            var missingQualities = profile.MissingQualities;
            if (missingQualities.Count != 0)
            {
                log.Information(
                    "Recyclarr detected that the following required qualities are missing from profile " +
                    "'{ProfileName}' and will re-add them: {QualityNames}", profile.ProfileName, missingQualities);
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
            log.Debug("These profiles have no changes and will not be persisted: {Profiles}",
                unchangedProfiles.Select(x => x.Profile.ProfileName));
        }

        var createdProfiles = changedProfiles
            .Where(x => x.Profile.UpdateReason == QualityProfileUpdateReason.New)
            .Select(x => x.Profile.ProfileName)
            .ToList();

        if (createdProfiles.Count > 0)
        {
            log.Information("Created {Count} Profiles: {Names}", createdProfiles.Count, createdProfiles);
        }

        var updatedProfiles = changedProfiles
            .Where(x => x.Profile.UpdateReason == QualityProfileUpdateReason.Changed)
            .Select(x => x.Profile.ProfileName)
            .ToList();

        if (updatedProfiles.Count > 0)
        {
            log.Information("Updated {Count} Profiles: {Names}", updatedProfiles.Count, updatedProfiles);
        }

        if (changedProfiles.Count != 0)
        {
            var numProfiles = changedProfiles.Count;
            var numQuality = changedProfiles.Count(x => x.QualitiesChanged);
            var numScores = changedProfiles.Count(x => x.ScoresChanged);

            log.Information(
                "A total of {NumProfiles} profiles were synced. {NumQuality} contain quality changes and " +
                "{NumScores} contain updated scores",
                numProfiles, numQuality, numScores);

            notificationEmitter.SendStatistic("Quality Profiles Synced", numProfiles);
        }
        else
        {
            log.Information("All quality profiles are up to date!");
        }
    }
}
