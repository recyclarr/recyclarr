using Recyclarr.Pipelines.QualityProfile.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.QualityProfile;

internal class QualityProfileLogger(ILogger log)
{
    public void LogTransactionNotices(
        QualityProfileTransactionData transactions,
        IPipelinePublisher publisher
    )
    {
        if (transactions.NonExistentProfiles.Count > 0)
        {
            publisher.Add(
                new NonExistentQualityProfilesOutcome(
                    transactions.NonExistentProfiles.Select(x => x.Name).ToList()
                )
            );
        }

        foreach (var (profile, errors) in transactions.InvalidProfiles)
        {
            foreach (var error in errors)
            {
                publisher.Add(
                    new InvalidQualityProfileOutcome(
                        profile.ProfileName,
                        error.PropertyName,
                        error.ErrorMessage,
                        error.AttemptedValue?.ToString(),
                        error.ErrorCode
                    )
                );
            }
        }

        LogReplacedProfiles(publisher, transactions);
        LogRenameConflicts(publisher, transactions);
        LogAmbiguousProfiles(publisher, transactions);

        // Log warnings for new profiles
        foreach (var profile in transactions.NewProfiles)
        {
            LogProfileWarnings(publisher, profile);
        }

        // Log warnings for updated profiles
        foreach (var profileWithStats in transactions.UpdatedProfiles)
        {
            LogProfileWarnings(publisher, profileWithStats.Profile);
        }
    }

    private void LogProfileWarnings(IPipelinePublisher publisher, UpdatedQualityProfile profile)
    {
        var invalidQualityNames = profile.UpdatedQualities.InvalidQualityNames;
        if (invalidQualityNames.Count != 0)
        {
            publisher.Add(
                new InvalidQualityNamesOutcome(profile.ProfileName, invalidQualityNames.ToList())
            );
        }

        var invalidCfExceptNames = profile.InvalidExceptCfNames;
        if (invalidCfExceptNames.Count != 0)
        {
            publisher.Add(
                new InvalidExceptCustomFormatNamesOutcome(
                    profile.ProfileName,
                    invalidCfExceptNames.ToList()
                )
            );
        }

        var invalidCfExceptPatterns = profile.InvalidExceptCfPatterns;
        if (invalidCfExceptPatterns.Count != 0)
        {
            publisher.Add(
                new UnmatchedExceptCustomFormatPatternsOutcome(
                    profile.ProfileName,
                    invalidCfExceptPatterns.ToList()
                )
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

    private static void LogReplacedProfiles(
        IPipelinePublisher publisher,
        QualityProfileTransactionData transactions
    )
    {
        var replaced = transactions.ReplacedProfiles;
        if (replaced.Count == 0)
        {
            return;
        }

        publisher.Add(
            new ReplacedQualityProfilesOutcome(replaced.Select(x => x.Profile.Name).ToList())
        );
    }

    private static void LogRenameConflicts(
        IPipelinePublisher publisher,
        QualityProfileTransactionData transactions
    )
    {
        foreach (var conflict in transactions.RenameConflicts)
        {
            publisher.Add(new QualityProfileRenameConflictOutcome(conflict.Profile.Name));
        }
    }

    private void LogAmbiguousProfiles(
        IPipelinePublisher publisher,
        QualityProfileTransactionData transactions
    )
    {
        if (transactions.AmbiguousProfiles.Count == 0)
        {
            return;
        }

        foreach (var ambiguous in transactions.AmbiguousProfiles)
        {
            publisher.Add(
                new AmbiguousQualityProfileOutcome(
                    ambiguous.PlannedProfile.Name,
                    ambiguous.ServiceMatches
                )
            );
        }

        log.Debug(
            "Ambiguous Quality Profiles: {@Ambiguous}",
            transactions.AmbiguousProfiles.Select(x => new
            {
                x.PlannedProfile.Name,
                x.PlannedProfile.GuideResource?.TrashId,
                Matches = x.ServiceMatches,
            })
        );
    }

    public void LogPersistenceResults(
        QualityProfileTransactionData transactions,
        IPipelinePublisher publisher,
        QualityProfilePipelineResult result,
        IReadOnlyCollection<UpdatedQualityProfile> createdProfiles,
        IReadOnlyCollection<ProfileWithStats> updatedProfiles
    )
    {
        // Profiles without changes get logged
        if (transactions.UnchangedProfiles.Count != 0)
        {
            log.Debug(
                "These profiles have no changes and will not be persisted: {Profiles}",
                transactions.UnchangedProfiles.Select(x => x.ProfileName)
            );
        }

        // Log created profiles
        if (createdProfiles.Count > 0)
        {
            log.Information(
                "Created {Count} Profiles: {Names}",
                createdProfiles.Count,
                createdProfiles.Select(x => x.EffectiveName)
            );
        }

        // Log updated profiles
        if (updatedProfiles.Count > 0)
        {
            log.Information(
                "Updated {Count} Profiles: {Names}",
                updatedProfiles.Count,
                updatedProfiles.Select(x => x.Profile.EffectiveName)
            );
        }

        var totalChanged = createdProfiles.Count + updatedProfiles.Count;
        if (totalChanged != 0)
        {
            var numQuality = updatedProfiles.Count(x => x.QualitiesChanged);
            var numScores = updatedProfiles.Count(x => x.ScoresChanged);

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

        SetStatus(publisher, result);
    }

    public static void SetStatus(IPipelinePublisher publisher, QualityProfilePipelineResult result)
    {
        var status = result.Status switch
        {
            SyncResultStatus.Succeeded => PipelineProgressStatus.Succeeded,
            SyncResultStatus.Partial => PipelineProgressStatus.Partial,
            SyncResultStatus.Failed => PipelineProgressStatus.Failed,
            SyncResultStatus.Blocked => PipelineProgressStatus.Skipped,
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };
        publisher.SetStatus(status, result.Deltas.Count);
    }
}
