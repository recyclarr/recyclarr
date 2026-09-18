using Recyclarr.Pipelines.QualityProfile.Models;

namespace Recyclarr.Pipelines.QualityProfile;

internal class QualityProfileLogger(ILogger log)
{
    public void LogTransactionNotices(QualityProfileTransactionData transactions)
    {
        foreach (var profile in transactions.NewProfiles)
        {
            LogProfileWarnings(profile);
        }

        foreach (var profileWithStats in transactions.UpdatedProfiles)
        {
            LogProfileWarnings(profileWithStats.Profile);
        }

        LogAmbiguousProfiles(transactions);
    }

    private void LogProfileWarnings(UpdatedQualityProfile profile)
    {
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

    private void LogAmbiguousProfiles(QualityProfileTransactionData transactions)
    {
        if (transactions.AmbiguousProfiles.Count == 0)
        {
            return;
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
    }
}
