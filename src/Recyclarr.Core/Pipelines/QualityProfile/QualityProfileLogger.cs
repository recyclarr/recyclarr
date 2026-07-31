using Recyclarr.Pipelines.QualityProfile.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

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
                new NonExistentQualityProfilesOutcome(transactions.NonExistentProfiles.ToList())
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

        publisher.Add(new ReplacedQualityProfilesOutcome(replaced.ToList()));
    }

    private static void LogRenameConflicts(
        IPipelinePublisher publisher,
        QualityProfileTransactionData transactions
    )
    {
        foreach (var name in transactions.RenameConflicts)
        {
            publisher.Add(new QualityProfileRenameConflictOutcome(name));
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
        IPipelinePublisher publisher
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

        var status = DetermineStatus(transactions);
        publisher.SetStatus(status, totalChanged);
    }

    private static PipelineProgressStatus DetermineStatus(
        QualityProfileTransactionData transactions
    )
    {
        var hasErrors =
            transactions.InvalidProfiles.Count > 0
            || transactions.RenameConflicts.Count > 0
            || transactions.AmbiguousProfiles.Count > 0;

        if (!hasErrors)
        {
            return PipelineProgressStatus.Succeeded;
        }

        var hasValidProfiles =
            transactions.NewProfiles.Count > 0
            || transactions.UpdatedProfiles.Count > 0
            || transactions.UnchangedProfiles.Count > 0;

        return hasValidProfiles ? PipelineProgressStatus.Partial : PipelineProgressStatus.Failed;
    }
}
